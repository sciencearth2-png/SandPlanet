using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace SandPlanet.Prototype
{
    /// <summary>
    /// Prototype 0.3 playtest UX layer.
    ///
    /// Goals:
    /// - preserve portrait aspect ratio
    /// - replace text-only HUD with visible time / will / XP gauges
    /// - turn encounter execution into a two-step "select -> confirm -> result" flow
    /// - show player-facing result deltas while keeping persistent system logging
    /// - suppress the old on-screen debug history panel
    ///
    /// This is a prototype-only overlay. It intentionally uses reflection so the
    /// existing Prototype 0.3 controller can remain stable while UI is iterated.
    /// </summary>
    public sealed class SandPlanetPrototype03PlaytestUX : MonoBehaviour
    {
        private const BindingFlags InstancePrivate = BindingFlags.Instance | BindingFlags.NonPublic;
        private const BindingFlags NestedFields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private const int XpPerLevel = 6;

        private SandPlanetPrototype03Controller controller;
        private Type controllerType;

        private readonly Dictionary<string, FieldInfo> fields = new Dictionary<string, FieldInfo>();
        private readonly Dictionary<string, MethodInfo> methods = new Dictionary<string, MethodInfo>();

        private object pendingOwner;
        private object pendingChoice;
        private bool pendingIsStoryEvent;

        private bool resultOpen;
        private string resultTitle = string.Empty;
        private string resultNarrative = string.Empty;
        private readonly List<string> resultChanges = new List<string>();

        private Snapshot lastSnapshot;

        private GUIStyle hudLabel;
        private GUIStyle hudSmall;
        private GUIStyle modalTitle;
        private GUIStyle modalBody;
        private GUIStyle modalSmall;
        private GUIStyle buttonStyle;
        private GUIStyle choiceTitleStyle;
        private GUIStyle resultGainStyle;

        private string sessionLogPath;
        private string latestLogPath;
        private bool legacyOverlayDisabled;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Attach()
        {
            SandPlanetPrototype03Controller found = UnityEngine.Object.FindFirstObjectByType<SandPlanetPrototype03Controller>();
            if (found == null)
                return;

            if (found.GetComponent<SandPlanetPrototype03PlaytestUX>() == null)
                found.gameObject.AddComponent<SandPlanetPrototype03PlaytestUX>();
        }

        private sealed class Snapshot
        {
            public int Day;
            public int Hour;
            public int Will;
            public int MaxWill;
            public int DayStart;
            public int DayEnd;

            public int PersonalLevel;
            public int PersonalXp;
            public int SocialLevel;
            public int SocialXp;
            public int TechnicalLevel;
            public int TechnicalXp;

            public Dictionary<string, int> Affinities;
            public Dictionary<string, bool> Flags;
        }

        private void Awake()
        {
            controller = GetComponent<SandPlanetPrototype03Controller>();
            if (controller == null)
                controller = UnityEngine.Object.FindFirstObjectByType<SandPlanetPrototype03Controller>();

            if (controller == null)
            {
                enabled = false;
                return;
            }

            controllerType = controller.GetType();
            BindController();
            PrepareLogFiles();
            lastSnapshot = CaptureSnapshot();

            Log("SYSTEM", "Prototype 0.3 Playtest UX 시작");
        }

        private void Start()
        {
            DisableLegacyOverlay();
        }

        private void Update()
        {
            if (controller == null)
                return;

            if (!legacyOverlayDisabled)
                DisableLegacyOverlay();

            string mode = ReadMode();

            bool ownsInteraction = mode == "Encounter" || mode == "StoryEvent" || resultOpen || pendingChoice != null;
            if (ownsInteraction && controller.enabled)
                controller.enabled = false;
            else if (!ownsInteraction && !controller.enabled)
                controller.enabled = true;
        }

        private void LateUpdate()
        {
            if (controller == null)
                return;

            Snapshot current = CaptureSnapshot();
            if (lastSnapshot != null)
                LogPassiveStateChanges(lastSnapshot, current);
            lastSnapshot = current;
        }

        private void OnGUI()
        {
            if (controller == null)
                return;

            EnsureStyles();
            GUI.depth = -1000;

            string mode = ReadMode();
            if (mode != "Setup")
                DrawHudOverlay();

            if (resultOpen)
            {
                DrawResultModal();
                return;
            }

            if (pendingChoice != null)
            {
                DrawConfirmationModal();
                return;
            }

            if (mode == "Encounter")
            {
                DrawEncounterReplacement();
                return;
            }

            if (mode == "StoryEvent")
            {
                DrawStoryEventReplacement();
                return;
            }
        }

        private void DrawHudOverlay()
        {
            float rightReserve = 315f;
            float width = Mathf.Max(720f, Screen.width - rightReserve);
            Rect hudRect = new Rect(0f, 0f, width, 96f);

            FillRect(hudRect, new Color(0.035f, 0.03f, 0.03f, 1f));

            int day = ReadInt("day");
            int hour = ReadInt("currentHour");
            int dayStart = ReadInt("dayStartHour");
            int dayEnd = ReadInt("dayEndHour");
            int will = ReadInt("currentWillpower");
            int maxWill = Mathf.Max(1, ReadInt("maxWillpower"));

            GUI.Label(new Rect(14f, 8f, 130f, 25f), $"DAY {day} / 21", hudLabel);

            Rect timeRect = new Rect(148f, 9f, Mathf.Min(600f, width * 0.50f), 22f);
            DrawTimeGauge(timeRect, hour, dayStart, dayEnd);

            float willX = timeRect.xMax + 18f;
            DrawWillGauge(new Rect(willX, 7f, Mathf.Max(170f, width - willX - 14f), 27f), will, maxWill);

            float statY = 45f;
            float gap = 14f;
            float statWidth = (width - 28f - gap * 2f) / 3f;

            DrawStatGauge(new Rect(14f, statY, statWidth, 38f), "개인", ReadInt("personalLevel"), ReadInt("personalXp"),
                new Color(0.83f, 0.20f, 0.20f));
            DrawStatGauge(new Rect(14f + statWidth + gap, statY, statWidth, 38f), "대인", ReadInt("socialLevel"), ReadInt("socialXp"),
                new Color(0.22f, 0.72f, 0.28f));
            DrawStatGauge(new Rect(14f + (statWidth + gap) * 2f, statY, statWidth, 38f), "기술", ReadInt("technicalLevel"), ReadInt("technicalXp"),
                new Color(0.20f, 0.47f, 0.88f));
        }

        private void DrawTimeGauge(Rect rect, int hour, int startHour, int endHour)
        {
            int safeEnd = Mathf.Max(startHour + 1, endHour);
            float progress = Mathf.InverseLerp(startHour, safeEnd, hour);

            GUI.Label(new Rect(rect.x, rect.y - 1f, 55f, 22f), $"{hour:00}:00", hudLabel);

            Rect bar = new Rect(rect.x + 61f, rect.y + 4f, rect.width - 116f, 14f);
            FillRect(bar, new Color(0.14f, 0.13f, 0.13f));
            FillRect(new Rect(bar.x, bar.y, bar.width * progress, bar.height), new Color(0.78f, 0.66f, 0.38f));
            OutlineRect(bar, new Color(0.65f, 0.62f, 0.55f));

            DrawTimeMarker(bar, 12, startHour, safeEnd);
            DrawTimeMarker(bar, 17, startHour, safeEnd);
            DrawTimeMarker(bar, 22, startHour, safeEnd);

            GUI.Label(new Rect(bar.x, bar.y + 14f, 45f, 18f), $"{startHour:00}", hudSmall);
            GUI.Label(new Rect(bar.xMax - 32f, bar.y + 14f, 38f, 18f), $"{safeEnd:00}", hudSmall);
        }

        private void DrawTimeMarker(Rect bar, int markerHour, int startHour, int endHour)
        {
            if (markerHour <= startHour || markerHour >= endHour)
                return;

            float t = Mathf.InverseLerp(startHour, endHour, markerHour);
            float x = Mathf.Round(bar.x + bar.width * t);
            FillRect(new Rect(x, bar.y - 2f, 1f, bar.height + 4f), new Color(1f, 1f, 1f, 0.52f));
        }

        private void DrawWillGauge(Rect rect, int value, int maxValue)
        {
            GUI.Label(new Rect(rect.x, rect.y + 1f, 58f, 22f), $"의지 {value}/{maxValue}", hudLabel);

            float x = rect.x + 66f;
            float available = Mathf.Max(70f, rect.width - 68f);
            float gap = 4f;
            float blockWidth = (available - gap * (maxValue - 1)) / maxValue;

            for (int i = 0; i < maxValue; i++)
            {
                Rect block = new Rect(x + i * (blockWidth + gap), rect.y + 5f, blockWidth, 14f);
                FillRect(block, i < value ? new Color(0.92f, 0.80f, 0.35f) : new Color(0.15f, 0.14f, 0.13f));
                OutlineRect(block, new Color(0.55f, 0.52f, 0.45f));
            }
        }

        private void DrawStatGauge(Rect rect, string label, int level, int xp, Color color)
        {
            GUI.Label(new Rect(rect.x, rect.y, 96f, 24f), $"{label} Lv.{level}", hudLabel);

            float x = rect.x + 102f;
            float available = Mathf.Max(90f, rect.width - 108f);
            float gap = 5f;
            float blockWidth = (available - gap * (XpPerLevel - 1)) / XpPerLevel;

            for (int i = 0; i < XpPerLevel; i++)
            {
                Rect block = new Rect(x + i * (blockWidth + gap), rect.y + 5f, blockWidth, 16f);
                FillRect(block, i < xp ? color : new Color(0.13f, 0.12f, 0.12f));
                OutlineRect(block, new Color(color.r, color.g, color.b, 0.85f));
            }

            GUI.Label(new Rect(x, rect.y + 23f, available, 15f), $"{xp}/{XpPerLevel} XP", hudSmall);
        }

        private void DrawEncounterReplacement()
        {
            object encounter = ReadField("currentEncounter");
            if (encounter == null)
            {
                ReleaseController();
                return;
            }

            DrawFullScreenBackdrop();

            string title = ReadNestedString(encounter, "Title");
            string body = ReadNestedString(encounter, "Body");
            string npcId = ReadNestedString(encounter, "PrimaryNpcId");
            string narrativeType = ReadNestedObject(encounter, "NarrativeType")?.ToString() ?? "Encounter";

            DrawNarrativeScreenHeader(narrativeType, npcId, title);

            Rect content = new Rect(34f, 126f, Screen.width - 68f, Screen.height - 166f);
            float portraitWidth = Mathf.Min(360f, content.width * 0.29f);
            Rect portraitPanel = new Rect(content.x, content.y, portraitWidth, content.height);
            Rect textPanel = new Rect(portraitPanel.xMax + 24f, content.y, content.width - portraitWidth - 24f, content.height);

            DrawPortraitPanel(portraitPanel, npcId);
            DrawEncounterTextAndChoices(textPanel, encounter, body, false);
        }

        private void DrawStoryEventReplacement()
        {
            object storyEvent = ReadField("currentStoryEvent");
            if (storyEvent == null)
            {
                ReleaseController();
                return;
            }

            DrawFullScreenBackdrop();

            string title = ReadNestedString(storyEvent, "Title");
            string body = ReadNestedString(storyEvent, "Body");
            string npcId = ReadNestedString(storyEvent, "PrimaryNpcId");

            DrawNarrativeScreenHeader("EVENT", npcId, title);

            Rect content = new Rect(34f, 126f, Screen.width - 68f, Screen.height - 166f);
            float portraitWidth = Mathf.Min(360f, content.width * 0.29f);
            Rect portraitPanel = new Rect(content.x, content.y, portraitWidth, content.height);
            Rect textPanel = new Rect(portraitPanel.xMax + 24f, content.y, content.width - portraitWidth - 24f, content.height);

            DrawPortraitPanel(portraitPanel, npcId);
            DrawEncounterTextAndChoices(textPanel, storyEvent, body, true);
        }

        private void DrawNarrativeScreenHeader(string type, string npcId, string title)
        {
            GUI.Label(new Rect(34f, 103f, 180f, 24f), $"• {type.ToUpperInvariant()}", modalSmall);
            if (!string.IsNullOrEmpty(npcId))
                GUI.Label(new Rect(190f, 103f, 150f, 24f), NpcName(npcId), modalSmall);

            GUI.Label(new Rect(360f, 101f, Screen.width - 500f, 30f), title, modalTitle);
        }

        private void DrawPortraitPanel(Rect panel, string npcId)
        {
            FillRect(panel, new Color(0.06f, 0.055f, 0.055f, 0.98f));
            OutlineRect(panel, new Color(0.30f, 0.29f, 0.27f));

            Sprite portrait = PortraitForNpc(npcId);
            Rect imageArea = new Rect(panel.x + 12f, panel.y + 12f, panel.width - 24f, panel.height - 62f);

            if (portrait != null && portrait.texture != null)
            {
                // ScaleToFit keeps the original aspect ratio. The image uses as much horizontal
                // space as possible without ever distorting or cropping the source.
                GUI.DrawTexture(imageArea, portrait.texture, ScaleMode.ScaleToFit, true);
            }
            else
            {
                GUI.Label(imageArea, "NO PORTRAIT", modalSmall);
            }

            string name = string.IsNullOrEmpty(npcId) ? string.Empty : NpcName(npcId);
            if (!string.IsNullOrEmpty(name))
            {
                FillRect(new Rect(panel.x + 12f, panel.yMax - 42f, panel.width - 24f, 30f), new Color(0.08f, 0.07f, 0.07f));
                GUI.Label(new Rect(panel.x + 20f, panel.yMax - 39f, panel.width - 40f, 24f), name, hudLabel);
            }
        }

        private void DrawEncounterTextAndChoices(Rect panel, object owner, string body, bool isStoryEvent)
        {
            FillRect(panel, new Color(0.045f, 0.041f, 0.041f, 0.98f));
            OutlineRect(panel, new Color(0.26f, 0.25f, 0.23f));

            GUI.Label(new Rect(panel.x + 20f, panel.y + 18f, panel.width - 40f, 100f), body, modalBody);
            GUI.Label(new Rect(panel.x + 20f, panel.y + 126f, panel.width - 40f, 24f), "선택", modalTitle);

            object choicesObject = ReadNestedObject(owner, "Choices");
            IEnumerable choices = choicesObject as IEnumerable;
            if (choices == null)
                return;

            float y = panel.y + 160f;
            foreach (object choice in choices)
            {
                if (choice == null)
                    continue;

                int timeCost = ReadNestedInt(choice, "TimeCost");
                int willCost = EffectiveWillCost(choice);
                bool executable = IsChoiceExecutable(choice);

                string label = ReadNestedString(choice, "Label");
                string softText = SoftRequirementText(choice);

                Rect card = new Rect(panel.x + 20f, y, panel.width - 40f, 78f);
                FillRect(card, executable ? new Color(0.12f, 0.11f, 0.105f) : new Color(0.075f, 0.07f, 0.07f));
                OutlineRect(card, executable ? new Color(0.42f, 0.39f, 0.34f) : new Color(0.20f, 0.19f, 0.18f));

                GUI.Label(new Rect(card.x + 12f, card.y + 8f, card.width - 165f, 25f), label, choiceTitleStyle);
                GUI.Label(new Rect(card.x + 12f, card.y + 38f, card.width - 170f, 22f),
                    $"시간 {timeCost}h · 의지 {willCost}" + (string.IsNullOrEmpty(softText) ? string.Empty : $" · {softText}"),
                    modalSmall);

                GUI.enabled = executable;
                if (GUI.Button(new Rect(card.xMax - 138f, card.y + 17f, 122f, 42f), "선택", buttonStyle))
                {
                    pendingOwner = owner;
                    pendingChoice = choice;
                    pendingIsStoryEvent = isStoryEvent;
                }
                GUI.enabled = true;

                y += 90f;
                if (y > panel.yMax - 85f)
                    break;
            }

            if (!isStoryEvent)
            {
                if (GUI.Button(new Rect(panel.xMax - 130f, panel.yMax - 50f, 110f, 34f), "뒤로", buttonStyle))
                    BackFromEncounter();
            }
        }

        private void DrawConfirmationModal()
        {
            DrawFullScreenBackdrop();

            object owner = pendingOwner;
            object choice = pendingChoice;
            if (owner == null || choice == null)
            {
                pendingOwner = null;
                pendingChoice = null;
                ReleaseController();
                return;
            }

            string title = ReadNestedString(owner, "Title");
            string npcId = ReadNestedString(owner, "PrimaryNpcId");
            string choiceLabel = ReadNestedString(choice, "Label");
            int timeCost = ReadNestedInt(choice, "TimeCost");
            int willCost = EffectiveWillCost(choice);

            Rect panel = CenteredPanel(760f, 420f);
            FillRect(panel, new Color(0.045f, 0.04f, 0.04f, 0.99f));
            OutlineRect(panel, new Color(0.56f, 0.51f, 0.42f));

            GUI.Label(new Rect(panel.x + 28f, panel.y + 24f, panel.width - 56f, 34f), "선택 확인", modalTitle);
            GUI.Label(new Rect(panel.x + 28f, panel.y + 70f, panel.width - 56f, 26f), title, hudLabel);

            string secondBeat = string.IsNullOrEmpty(npcId)
                ? $"제이는 「{choiceLabel}」 행동을 진행하기로 한다."
                : $"{NpcName(npcId)}와 마주한 제이는 「{choiceLabel}」 쪽으로 행동을 정한다.";

            GUI.Label(new Rect(panel.x + 28f, panel.y + 112f, panel.width - 56f, 110f), secondBeat, modalBody);

            Rect costBox = new Rect(panel.x + 28f, panel.y + 238f, panel.width - 56f, 58f);
            FillRect(costBox, new Color(0.09f, 0.085f, 0.08f));
            OutlineRect(costBox, new Color(0.34f, 0.32f, 0.28f));
            GUI.Label(new Rect(costBox.x + 16f, costBox.y + 16f, costBox.width - 32f, 24f),
                $"시간 {timeCost}h    의지 {willCost}" +
                (string.IsNullOrEmpty(SoftRequirementText(choice)) ? string.Empty : $"    {SoftRequirementText(choice)}"),
                hudLabel);

            if (GUI.Button(new Rect(panel.x + 28f, panel.yMax - 70f, 160f, 42f), "취소", buttonStyle))
            {
                pendingChoice = null;
                pendingOwner = null;
                pendingIsStoryEvent = false;
            }

            GUI.enabled = IsChoiceExecutable(choice);
            if (GUI.Button(new Rect(panel.xMax - 188f, panel.yMax - 70f, 160f, 42f), "확인", buttonStyle))
                ExecutePendingChoice();
            GUI.enabled = true;
        }

        private void ExecutePendingChoice()
        {
            object owner = pendingOwner;
            object choice = pendingChoice;
            if (owner == null || choice == null)
                return;

            Snapshot before = CaptureSnapshot();
            string narrativeResult = ReadNestedString(choice, "ResultText");
            string ownerTitle = ReadNestedString(owner, "Title");
            string choiceLabel = ReadNestedString(choice, "Label");

            try
            {
                if (pendingIsStoryEvent)
                    Invoke("ExecuteStoryEventChoice", owner, choice);
                else
                    Invoke("ExecuteEncounterChoice", owner, choice);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                return;
            }

            Snapshot after = CaptureSnapshot();

            resultTitle = ownerTitle;
            resultNarrative = narrativeResult;
            resultChanges.Clear();
            BuildPlayerFacingChanges(before, after, resultChanges);

            resultOpen = true;
            pendingChoice = null;
            pendingOwner = null;
            pendingIsStoryEvent = false;

            Log("RESULT", $"{resultTitle} / {choiceLabel} / {resultNarrative}");
            foreach (string change in resultChanges)
                Log("CHANGE", change);

            lastSnapshot = after;
        }

        private void DrawResultModal()
        {
            DrawFullScreenBackdrop();

            Rect panel = CenteredPanel(820f, 500f);
            FillRect(panel, new Color(0.045f, 0.04f, 0.04f, 0.995f));
            OutlineRect(panel, new Color(0.63f, 0.58f, 0.47f));

            GUI.Label(new Rect(panel.x + 30f, panel.y + 24f, panel.width - 60f, 34f), resultTitle, modalTitle);
            GUI.Label(new Rect(panel.x + 30f, panel.y + 76f, panel.width - 60f, 112f), resultNarrative, modalBody);

            GUI.Label(new Rect(panel.x + 30f, panel.y + 204f, panel.width - 60f, 28f), "결과", modalTitle);

            Rect resultBox = new Rect(panel.x + 30f, panel.y + 244f, panel.width - 60f, 150f);
            FillRect(resultBox, new Color(0.08f, 0.075f, 0.07f));
            OutlineRect(resultBox, new Color(0.34f, 0.32f, 0.27f));

            if (resultChanges.Count == 0)
            {
                GUI.Label(new Rect(resultBox.x + 16f, resultBox.y + 16f, resultBox.width - 32f, 24f),
                    "수치 변화 없음", modalSmall);
            }
            else
            {
                float y = resultBox.y + 14f;
                foreach (string line in resultChanges)
                {
                    GUI.Label(new Rect(resultBox.x + 16f, y, resultBox.width - 32f, 24f), "• " + line, resultGainStyle);
                    y += 26f;
                    if (y > resultBox.yMax - 24f)
                        break;
                }
            }

            if (GUI.Button(new Rect(panel.xMax - 190f, panel.yMax - 66f, 160f, 40f), "계속", buttonStyle))
            {
                resultOpen = false;
                resultTitle = string.Empty;
                resultNarrative = string.Empty;
                resultChanges.Clear();
                ReleaseController();
            }
        }

        private void BackFromEncounter()
        {
            object returnMode = ReadField("encounterReturnMode");
            if (returnMode != null)
                WriteField("mode", returnMode);

            WriteField("currentEncounter", null);
            pendingChoice = null;
            pendingOwner = null;
            pendingIsStoryEvent = false;
            ReleaseController();
        }

        private void ReleaseController()
        {
            if (controller != null)
                controller.enabled = true;
        }

        private bool IsChoiceExecutable(object choice)
        {
            if (choice == null)
                return false;

            int timeCost = ReadNestedInt(choice, "TimeCost");
            int willCost = EffectiveWillCost(choice);
            int currentHour = ReadInt("currentHour");
            int endHour = ReadInt("dayEndHour");
            int will = ReadInt("currentWillpower");

            bool timeOk = currentHour + timeCost <= endHour;
            bool willOk = will >= willCost;

            object hardConditions = ReadNestedObject(choice, "HardConditions");
            bool hardOk = true;
            MethodInfo conditions = Method("ConditionsMet");
            if (conditions != null && hardConditions != null)
            {
                try
                {
                    hardOk = (bool)conditions.Invoke(controller, new[] { hardConditions });
                }
                catch
                {
                    hardOk = true;
                }
            }

            return timeOk && willOk && hardOk;
        }

        private int EffectiveWillCost(object choice)
        {
            MethodInfo method = Method("EffectiveWillCost");
            if (method != null && choice != null)
            {
                try
                {
                    return Convert.ToInt32(method.Invoke(controller, new[] { choice }));
                }
                catch
                {
                    // Fall through to base will.
                }
            }

            return ReadNestedInt(choice, "BaseWillCost");
        }

        private string SoftRequirementText(object choice)
        {
            object stat = ReadNestedObject(choice, "SoftStat");
            int requirement = ReadNestedInt(choice, "SoftRequirement");

            if (stat == null || requirement <= 0 || stat.ToString() == "None")
                return string.Empty;

            string statName = stat.ToString() switch
            {
                "Personal" => "개인",
                "Social" => "대인",
                "Technical" => "기술",
                _ => stat.ToString()
            };

            return $"{statName} Lv.{requirement}";
        }

        private void BuildPlayerFacingChanges(Snapshot before, Snapshot after, List<string> output)
        {
            int spentHours = after.Hour - before.Hour;
            if (spentHours != 0)
                output.Add($"시간 {before.Hour:00}:00 → {after.Hour:00}:00 ({(spentHours > 0 ? "+" : string.Empty)}{spentHours}h)");

            int willDelta = after.Will - before.Will;
            if (willDelta != 0)
                output.Add($"의지 {before.Will}/{before.MaxWill} → {after.Will}/{after.MaxWill} ({Signed(willDelta)})");

            AddStatResult(output, "개인", before.PersonalLevel, before.PersonalXp, after.PersonalLevel, after.PersonalXp);
            AddStatResult(output, "대인", before.SocialLevel, before.SocialXp, after.SocialLevel, after.SocialXp);
            AddStatResult(output, "기술", before.TechnicalLevel, before.TechnicalXp, after.TechnicalLevel, after.TechnicalXp);

            AddAffinityResults(output, before.Affinities, after.Affinities);
        }

        private static void AddStatResult(List<string> output, string label, int beforeLevel, int beforeXp, int afterLevel, int afterXp)
        {
            if (beforeLevel == afterLevel && beforeXp == afterXp)
                return;

            if (beforeLevel != afterLevel)
            {
                output.Add($"{label} Lv.{beforeLevel} {beforeXp}/6 XP → Lv.{afterLevel} {afterXp}/6 XP");
                return;
            }

            int delta = afterXp - beforeXp;
            output.Add($"{label} XP {Signed(delta)} ({beforeXp}/6 → {afterXp}/6)");
        }

        private static void AddAffinityResults(List<string> output, Dictionary<string, int> before, Dictionary<string, int> after)
        {
            HashSet<string> keys = new HashSet<string>();
            foreach (string key in before.Keys)
                keys.Add(key);
            foreach (string key in after.Keys)
                keys.Add(key);

            foreach (string key in keys)
            {
                int b = before.TryGetValue(key, out int bv) ? bv : 0;
                int a = after.TryGetValue(key, out int av) ? av : 0;
                if (a == b)
                    continue;

                output.Add($"{NpcNameStatic(key)} 호감도 {b} → {a} ({Signed(a - b)})");
            }
        }

        private void LogPassiveStateChanges(Snapshot before, Snapshot after)
        {
            if (before.Day != after.Day)
                Log("CHANGE", $"DAY {before.Day} → DAY {after.Day}");

            if (before.Hour != after.Hour && !resultOpen)
                Log("CHANGE", $"시간 {before.Hour:00}:00 → {after.Hour:00}:00");

            if ((before.Will != after.Will || before.MaxWill != after.MaxWill) && !resultOpen)
                Log("CHANGE", $"의지 {before.Will}/{before.MaxWill} → {after.Will}/{after.MaxWill}");

            LogStatIfChanged("개인", before.PersonalLevel, before.PersonalXp, after.PersonalLevel, after.PersonalXp);
            LogStatIfChanged("대인", before.SocialLevel, before.SocialXp, after.SocialLevel, after.SocialXp);
            LogStatIfChanged("기술", before.TechnicalLevel, before.TechnicalXp, after.TechnicalLevel, after.TechnicalXp);

            LogDictionaryChanges("호감도", before.Affinities, after.Affinities);
            LogFlagChanges(before.Flags, after.Flags);
        }

        private void LogStatIfChanged(string label, int beforeLevel, int beforeXp, int afterLevel, int afterXp)
        {
            if (beforeLevel == afterLevel && beforeXp == afterXp)
                return;

            Log("CHANGE", $"{label} Lv.{beforeLevel} {beforeXp}/6 XP → Lv.{afterLevel} {afterXp}/6 XP");
        }

        private void LogDictionaryChanges(string label, Dictionary<string, int> before, Dictionary<string, int> after)
        {
            HashSet<string> keys = new HashSet<string>();
            foreach (string key in before.Keys)
                keys.Add(key);
            foreach (string key in after.Keys)
                keys.Add(key);

            foreach (string key in keys)
            {
                int b = before.TryGetValue(key, out int bv) ? bv : 0;
                int a = after.TryGetValue(key, out int av) ? av : 0;
                if (a != b)
                    Log("CHANGE", $"{label} {key}: {b} → {a}");
            }
        }

        private void LogFlagChanges(Dictionary<string, bool> before, Dictionary<string, bool> after)
        {
            HashSet<string> keys = new HashSet<string>();
            foreach (string key in before.Keys)
                keys.Add(key);
            foreach (string key in after.Keys)
                keys.Add(key);

            foreach (string key in keys)
            {
                bool b = before.TryGetValue(key, out bool bv) && bv;
                bool a = after.TryGetValue(key, out bool av) && av;
                if (a != b)
                    Log("FLAG", $"{key}: {(b ? "ON" : "OFF")} → {(a ? "ON" : "OFF")}");
            }
        }

        private Snapshot CaptureSnapshot()
        {
            return new Snapshot
            {
                Day = ReadInt("day"),
                Hour = ReadInt("currentHour"),
                Will = ReadInt("currentWillpower"),
                MaxWill = ReadInt("maxWillpower"),
                DayStart = ReadInt("dayStartHour"),
                DayEnd = ReadInt("dayEndHour"),

                PersonalLevel = ReadInt("personalLevel"),
                PersonalXp = ReadInt("personalXp"),
                SocialLevel = ReadInt("socialLevel"),
                SocialXp = ReadInt("socialXp"),
                TechnicalLevel = ReadInt("technicalLevel"),
                TechnicalXp = ReadInt("technicalXp"),

                Affinities = CopyIntDictionary(ReadField("affinities")),
                Flags = CopyBoolDictionary(ReadField("flags"))
            };
        }

        private void BindController()
        {
            string[] fieldNames =
            {
                "day", "currentHour", "dayStartHour", "dayEndHour",
                "currentWillpower", "maxWillpower",
                "personalLevel", "personalXp", "socialLevel", "socialXp", "technicalLevel", "technicalXp",
                "mode", "encounterReturnMode", "currentEncounter", "currentStoryEvent",
                "affinities", "flags",
                "samPortrait", "jinaPortrait", "fayePortrait", "benjaminPortrait", "borichiPortrait", "diyaPortrait"
            };

            foreach (string name in fieldNames)
            {
                FieldInfo field = controllerType.GetField(name, InstancePrivate);
                if (field != null)
                    fields[name] = field;
                else
                    Debug.LogWarning("[P0.3 UX] Missing controller field: " + name);
            }

            string[] methodNames =
            {
                "ExecuteEncounterChoice", "ExecuteStoryEventChoice", "EffectiveWillCost", "ConditionsMet"
            };

            foreach (string name in methodNames)
            {
                MethodInfo method = FindMethod(name);
                if (method != null)
                    methods[name] = method;
                else
                    Debug.LogWarning("[P0.3 UX] Missing controller method: " + name);
            }
        }

        private MethodInfo FindMethod(string name)
        {
            MethodInfo[] all = controllerType.GetMethods(InstancePrivate);
            foreach (MethodInfo method in all)
            {
                if (method.Name == name)
                    return method;
            }
            return null;
        }

        private MethodInfo Method(string name)
        {
            return methods.TryGetValue(name, out MethodInfo method) ? method : null;
        }

        private object Invoke(string name, params object[] args)
        {
            MethodInfo method = Method(name);
            if (method == null)
                throw new MissingMethodException(controllerType.FullName, name);

            try
            {
                return method.Invoke(controller, args);
            }
            catch (TargetInvocationException exception)
            {
                throw exception.InnerException ?? exception;
            }
        }

        private object ReadField(string name)
        {
            return fields.TryGetValue(name, out FieldInfo field) ? field.GetValue(controller) : null;
        }

        private int ReadInt(string name)
        {
            object value = ReadField(name);
            return value == null ? 0 : Convert.ToInt32(value);
        }

        private void WriteField(string name, object value)
        {
            if (fields.TryGetValue(name, out FieldInfo field))
                field.SetValue(controller, value);
        }

        private string ReadMode()
        {
            return ReadField("mode")?.ToString() ?? string.Empty;
        }

        private static object ReadNestedObject(object owner, string fieldName)
        {
            if (owner == null)
                return null;

            FieldInfo field = owner.GetType().GetField(fieldName, NestedFields);
            return field?.GetValue(owner);
        }

        private static string ReadNestedString(object owner, string fieldName)
        {
            return ReadNestedObject(owner, fieldName) as string ?? string.Empty;
        }

        private static int ReadNestedInt(object owner, string fieldName)
        {
            object value = ReadNestedObject(owner, fieldName);
            return value == null ? 0 : Convert.ToInt32(value);
        }

        private Sprite PortraitForNpc(string npcId)
        {
            string fieldName = npcId switch
            {
                "sam" => "samPortrait",
                "jina" => "jinaPortrait",
                "faye" => "fayePortrait",
                "benjamin" => "benjaminPortrait",
                "borichi" => "borichiPortrait",
                "diya" => "diyaPortrait",
                _ => string.Empty
            };

            return string.IsNullOrEmpty(fieldName) ? null : ReadField(fieldName) as Sprite;
        }

        private string NpcName(string id)
        {
            return NpcNameStatic(id);
        }

        private static string NpcNameStatic(string id)
        {
            return id switch
            {
                "sam" => "샘",
                "jina" => "지나",
                "faye" => "페이",
                "benjamin" => "벤자민",
                "borichi" => "보리치",
                "diya" => "디야",
                _ => id
            };
        }

        private static Dictionary<string, int> CopyIntDictionary(object source)
        {
            Dictionary<string, int> copy = new Dictionary<string, int>();
            if (source is IDictionary dictionary)
            {
                foreach (DictionaryEntry entry in dictionary)
                {
                    if (entry.Key is string key)
                        copy[key] = Convert.ToInt32(entry.Value);
                }
            }
            return copy;
        }

        private static Dictionary<string, bool> CopyBoolDictionary(object source)
        {
            Dictionary<string, bool> copy = new Dictionary<string, bool>();
            if (source is IDictionary dictionary)
            {
                foreach (DictionaryEntry entry in dictionary)
                {
                    if (entry.Key is string key)
                        copy[key] = Convert.ToBoolean(entry.Value);
                }
            }
            return copy;
        }

        private void DisableLegacyOverlay()
        {
            MonoBehaviour[] behaviours = GetComponents<MonoBehaviour>();
            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour == null || behaviour == this)
                    continue;

                if (behaviour.GetType().Name == "SandPlanetPrototype03DebugLogOverlay")
                {
                    behaviour.enabled = false;
                    legacyOverlayDisabled = true;
                    Log("SYSTEM", "기존 화면용 최근 변화 로그 패널 비활성화");
                    return;
                }
            }

            // It may attach one frame later; keep trying until found.
            legacyOverlayDisabled = false;
        }

        private void DrawFullScreenBackdrop()
        {
            FillRect(new Rect(0f, 96f, Screen.width, Screen.height - 96f), new Color(0.025f, 0.022f, 0.022f, 1f));
        }

        private static Rect CenteredPanel(float preferredWidth, float preferredHeight)
        {
            float width = Mathf.Min(preferredWidth, Screen.width - 60f);
            float height = Mathf.Min(preferredHeight, Screen.height - 140f);
            return new Rect((Screen.width - width) * 0.5f, 112f + Mathf.Max(0f, (Screen.height - 112f - height) * 0.5f), width, height);
        }

        private void FillRect(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }

        private void OutlineRect(Rect rect, Color color)
        {
            FillRect(new Rect(rect.x, rect.y, rect.width, 1f), color);
            FillRect(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), color);
            FillRect(new Rect(rect.x, rect.y, 1f, rect.height), color);
            FillRect(new Rect(rect.xMax - 1f, rect.y, 1f, rect.height), color);
        }

        private static string Signed(int value)
        {
            return value > 0 ? "+" + value : value.ToString();
        }

        private void EnsureStyles()
        {
            if (hudLabel != null)
                return;

            hudLabel = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = Color.white }
            };

            hudSmall = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                alignment = TextAnchor.UpperLeft,
                normal = { textColor = new Color(0.76f, 0.74f, 0.70f) }
            };

            modalTitle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                wordWrap = true,
                normal = { textColor = Color.white }
            };

            modalBody = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                wordWrap = true,
                normal = { textColor = new Color(0.91f, 0.89f, 0.85f) }
            };

            modalSmall = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                wordWrap = true,
                normal = { textColor = new Color(0.70f, 0.74f, 0.79f) }
            };

            choiceTitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                wordWrap = true,
                normal = { textColor = Color.white }
            };

            resultGainStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                wordWrap = true,
                normal = { textColor = new Color(0.78f, 0.92f, 0.72f) }
            };

            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
        }

        private void PrepareLogFiles()
        {
            try
            {
                string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                string logDirectory = Path.Combine(projectRoot, "Logs");
                Directory.CreateDirectory(logDirectory);

                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                sessionLogPath = Path.Combine(logDirectory, $"SandPlanet_Prototype03_{timestamp}.log");
                latestLogPath = Path.Combine(logDirectory, "SandPlanet_Prototype03_latest.log");

                string header =
                    $"SandPlanet Prototype 0.3 Playtest Log{Environment.NewLine}" +
                    $"Started: {DateTime.Now:yyyy-MM-dd HH:mm:ss}{Environment.NewLine}" +
                    $"Unity: {Application.unityVersion}{Environment.NewLine}" +
                    new string('-', 72) + Environment.NewLine;

                File.WriteAllText(sessionLogPath, header);
                File.WriteAllText(latestLogPath, header);
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[P0.3 UX] Could not create log file: " + exception.Message);
                sessionLogPath = null;
                latestLogPath = null;
            }
        }

        private void Log(string category, string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;

            int day = ReadInt("day");
            int hour = ReadInt("currentHour");
            string stamp = day > 0 ? $"D{day} {hour:00}:00" : "SETUP";
            string entry = $"[{stamp}] [{category}] {message}";

            Debug.Log("[P0.3 LOG] " + entry);

            try
            {
                string line = $"{DateTime.Now:HH:mm:ss.fff} {entry}{Environment.NewLine}";
                if (!string.IsNullOrEmpty(sessionLogPath))
                    File.AppendAllText(sessionLogPath, line);
                if (!string.IsNullOrEmpty(latestLogPath))
                    File.AppendAllText(latestLogPath, line);
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[P0.3 UX] Could not append log: " + exception.Message);
            }
        }
    }
}
