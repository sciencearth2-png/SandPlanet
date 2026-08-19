using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SandPlanet.Prototype
{
    /// <summary>
    /// Prototype 0.3 playtest UX v2.
    ///
    /// This layer supersedes the previous prototype UX at runtime and focuses on
    /// readability / playtest feedback without changing the encounter data model.
    ///
    /// - larger, two-row HUD with time / will / XP gauges
    /// - larger typography and spacing
    /// - location cards with target NPC portraits
    /// - Lost-Ark-like main quest tracker on the planet hub
    /// - encounter -> confirmation -> result flow
    /// - explicit "소모" label before time / will costs
    /// - persistent system logging without an on-screen debug history panel
    /// </summary>
    public sealed class SandPlanetPrototype03PlaytestUXV2 : MonoBehaviour
    {
        private const BindingFlags InstancePrivate = BindingFlags.Instance | BindingFlags.NonPublic;
        private const BindingFlags NestedFields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private const int XpPerLevel = 6;
        private const float HudHeight = 136f;

        private SandPlanetPrototype03Controller controller;
        private Type controllerType;
        private SandPlanetPrototype03PlaytestUX legacyUx;
        private SandPlanetPrototype03DebugLogOverlay legacyDebug;

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
        private Vector2 locationScroll;
        private Vector2 choiceScroll;

        private GUIStyle hudLabel;
        private GUIStyle hudSmall;
        private GUIStyle modalTitle;
        private GUIStyle modalBody;
        private GUIStyle modalSmall;
        private GUIStyle buttonStyle;
        private GUIStyle choiceTitleStyle;
        private GUIStyle resultGainStyle;
        private GUIStyle questHeaderStyle;

        private string sessionLogPath;
        private string latestLogPath;
        private bool controllerStylesBoosted;

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

        private struct QuestTrackerInfo
        {
            public string Title;
            public string Objective;
            public string Location;
            public string Progress;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Attach()
        {
            SandPlanetPrototype03Controller found = UnityEngine.Object.FindFirstObjectByType<SandPlanetPrototype03Controller>();
            if (found == null)
                return;

            if (found.GetComponent<SandPlanetPrototype03PlaytestUXV2>() == null)
                found.gameObject.AddComponent<SandPlanetPrototype03PlaytestUXV2>();
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
            DisableLegacyUx();
            PrepareLogFiles();
            lastSnapshot = CaptureSnapshot();
            Log("SYSTEM", "Prototype 0.3 Playtest UX v2 시작");
        }

        private void Start()
        {
            DisableLegacyUx();
        }

        private void Update()
        {
            if (controller == null)
                return;

            DisableLegacyUx();
            BoostControllerStylesWhenReady();

            string mode = ReadMode();
            bool ownsScreen = mode == "Location" || mode == "Encounter" || mode == "StoryEvent" || resultOpen || pendingChoice != null;
            bool blockHubInput = mode == "Hub" && PointerOverOverlay();

            if ((ownsScreen || blockHubInput) && controller.enabled)
                controller.enabled = false;
            else if (!ownsScreen && !blockHubInput && !controller.enabled)
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
            GUI.depth = -2000;

            string mode = ReadMode();
            if (mode != "Setup")
                DrawHud();

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

            switch (mode)
            {
                case "Hub":
                    DrawMainQuestTracker();
                    break;
                case "Location":
                    DrawLocationReplacement();
                    break;
                case "Encounter":
                    DrawEncounterReplacement();
                    break;
                case "StoryEvent":
                    DrawStoryEventReplacement();
                    break;
            }
        }

        // ---------------------------------------------------------------------
        // HUD
        // ---------------------------------------------------------------------

        private void DrawHud()
        {
            Rect hud = new Rect(0f, 0f, Screen.width, HudHeight);
            FillRect(hud, new Color(0.032f, 0.028f, 0.027f, 1f));
            FillRect(new Rect(0f, HudHeight - 1f, Screen.width, 1f), new Color(0.23f, 0.21f, 0.19f));

            int day = ReadInt("day");
            int hour = ReadInt("currentHour");
            int dayStart = ReadInt("dayStartHour");
            int dayEnd = ReadInt("dayEndHour");
            int will = ReadInt("currentWillpower");
            int maxWill = Mathf.Max(1, ReadInt("maxWillpower"));

            float buttonsWidth = 292f;
            float buttonsStart = Screen.width - buttonsWidth - 14f;
            float timeWidth = Mathf.Clamp(Screen.width * 0.34f, 420f, 620f);
            float timeX = 128f;
            float willX = timeX + timeWidth + 18f;
            float willWidth = Mathf.Max(240f, buttonsStart - willX - 12f);

            GUI.Label(new Rect(14f, 12f, 106f, 30f), $"DAY {day} / 21", hudLabel);
            DrawTimeGauge(new Rect(timeX, 10f, timeWidth, 42f), hour, dayStart, dayEnd);
            DrawWillGauge(new Rect(willX, 10f, willWidth, 40f), will, maxWill);

            bool canRest = will < maxWill && hour + 3 <= dayEnd;
            GUI.enabled = canRest;
            if (GUI.Button(new Rect(buttonsStart, 10f, 172f, 42f), "휴식 3h / 의지 +1", buttonStyle))
                InvokeSafe("TakeRest");
            GUI.enabled = true;

            if (GUI.Button(new Rect(buttonsStart + 182f, 10f, 110f, 42f), "하루 종료", buttonStyle))
                InvokeSafe("EndDay");

            float statY = 70f;
            float gap = 18f;
            float statWidth = (Screen.width - 28f - gap * 2f) / 3f;
            DrawStatGauge(new Rect(14f, statY, statWidth, 52f), "개인", ReadInt("personalLevel"), ReadInt("personalXp"), new Color(0.85f, 0.22f, 0.22f));
            DrawStatGauge(new Rect(14f + statWidth + gap, statY, statWidth, 52f), "대인", ReadInt("socialLevel"), ReadInt("socialXp"), new Color(0.24f, 0.75f, 0.31f));
            DrawStatGauge(new Rect(14f + (statWidth + gap) * 2f, statY, statWidth, 52f), "기술", ReadInt("technicalLevel"), ReadInt("technicalXp"), new Color(0.22f, 0.50f, 0.92f));
        }

        private void DrawTimeGauge(Rect rect, int hour, int startHour, int endHour)
        {
            int safeEnd = Mathf.Max(startHour + 1, endHour);
            float progress = Mathf.InverseLerp(startHour, safeEnd, hour);

            GUI.Label(new Rect(rect.x, rect.y, 72f, 30f), $"{hour:00}:00", hudLabel);
            Rect bar = new Rect(rect.x + 78f, rect.y + 6f, rect.width - 86f, 18f);
            FillRect(bar, new Color(0.13f, 0.12f, 0.115f));
            FillRect(new Rect(bar.x, bar.y, bar.width * progress, bar.height), new Color(0.79f, 0.67f, 0.39f));
            OutlineRect(bar, new Color(0.62f, 0.59f, 0.53f));

            DrawTimeMarker(bar, 12, startHour, safeEnd);
            DrawTimeMarker(bar, 17, startHour, safeEnd);
            DrawTimeMarker(bar, 22, startHour, safeEnd);

            GUI.Label(new Rect(bar.x, bar.y + 20f, 46f, 20f), $"{startHour:00}", hudSmall);
            GUI.Label(new Rect(bar.xMax - 34f, bar.y + 20f, 42f, 20f), $"{safeEnd:00}", hudSmall);
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
            GUI.Label(new Rect(rect.x, rect.y, 104f, 30f), $"의지 {value}/{maxValue}", hudLabel);
            float x = rect.x + 112f;
            float available = Mathf.Max(96f, rect.width - 116f);
            float gap = 5f;
            float blockWidth = Mathf.Max(18f, (available - gap * (maxValue - 1)) / maxValue);

            for (int i = 0; i < maxValue; i++)
            {
                Rect block = new Rect(x + i * (blockWidth + gap), rect.y + 6f, blockWidth, 18f);
                FillRect(block, i < value ? new Color(0.93f, 0.80f, 0.34f) : new Color(0.14f, 0.13f, 0.12f));
                OutlineRect(block, new Color(0.57f, 0.53f, 0.44f));
            }
        }

        private void DrawStatGauge(Rect rect, string label, int level, int xp, Color color)
        {
            GUI.Label(new Rect(rect.x, rect.y, 106f, 30f), $"{label} Lv.{level}", hudLabel);
            float x = rect.x + 112f;
            float available = Mathf.Max(120f, rect.width - 118f);
            float gap = 6f;
            float blockWidth = (available - gap * (XpPerLevel - 1)) / XpPerLevel;

            for (int i = 0; i < XpPerLevel; i++)
            {
                Rect block = new Rect(x + i * (blockWidth + gap), rect.y + 5f, blockWidth, 20f);
                FillRect(block, i < xp ? color : new Color(0.12f, 0.11f, 0.11f));
                OutlineRect(block, new Color(color.r, color.g, color.b, 0.9f));
            }

            GUI.Label(new Rect(x, rect.y + 29f, available, 20f), $"{xp}/{XpPerLevel} XP", hudSmall);
        }

        // ---------------------------------------------------------------------
        // Planet hub quest tracker
        // ---------------------------------------------------------------------

        private void DrawMainQuestTracker()
        {
            float width = 372f;
            float height = Mathf.Min(500f, Screen.height - HudHeight - 78f);
            Rect panel = new Rect(Screen.width - width - 20f, HudHeight + 18f, width, height);
            FillRect(panel, new Color(0.055f, 0.047f, 0.043f, 0.985f));
            OutlineRect(panel, new Color(0.30f, 0.27f, 0.23f));

            QuestTrackerInfo info = BuildQuestTrackerInfo();

            GUI.Label(new Rect(panel.x + 20f, panel.y + 18f, panel.width - 40f, 32f), "메인 퀘스트", questHeaderStyle);
            FillRect(new Rect(panel.x + 20f, panel.y + 58f, 104f, 28f), new Color(0.50f, 0.28f, 0.16f));
            GUI.Label(new Rect(panel.x + 28f, panel.y + 61f, 94f, 24f), "◆ MAIN", modalSmall);

            GUI.Label(new Rect(panel.x + 20f, panel.y + 100f, panel.width - 40f, 60f), info.Title, modalTitle);
            GUI.Label(new Rect(panel.x + 20f, panel.y + 166f, panel.width - 40f, 92f), info.Objective, modalBody);

            float y = panel.y + 276f;
            DrawQuestTrackerRow(panel, ref y, "장소", info.Location);
            DrawQuestTrackerRow(panel, ref y, "진행", info.Progress);

            y += 12f;
            GUI.Label(new Rect(panel.x + 20f, y, panel.width - 40f, 28f), "현재 상태", choiceTitleStyle);
            y += 34f;
            GUI.Label(new Rect(panel.x + 20f, y, panel.width - 40f, 84f),
                $"추모 {(Flag("W1_MEMORIAL_DONE") ? "완료" : "미완료")}\n" +
                $"오아시스 위험 {(Flag("W1_WATER_RISK_KNOWN") ? "확인" : "미확인")}\n" +
                $"샘 보고 {(Flag("W1_REPORT_DONE") ? "완료" : "미완료")}",
                modalSmall);
        }

        private void DrawQuestTrackerRow(Rect panel, ref float y, string label, string value)
        {
            GUI.Label(new Rect(panel.x + 20f, y, 58f, 26f), label, modalSmall);
            GUI.Label(new Rect(panel.x + 82f, y, panel.width - 102f, 38f), value, hudLabel);
            y += 42f;
        }

        private QuestTrackerInfo BuildQuestTrackerInfo()
        {
            int day = ReadInt("day");
            bool situation = Flag("W1_SITUATION_HEARD");
            bool investigation = Flag("W1_INVESTIGATION_OPEN");
            bool water = Flag("W1_WATER_RISK_KNOWN");
            bool report = Flag("W1_REPORT_DONE");
            bool disclosureAll = Flag("W1_DISCLOSURE_ALL");
            bool disclosureLimited = Flag("W1_DISCLOSURE_LIMITED");

            if (!situation && day <= 2)
            {
                return new QuestTrackerInfo
                {
                    Title = "내가 잠든 동안",
                    Objective = "제이가 빠져 있던 시간 동안 무슨 일이 있었는지 샘에게 듣는다.",
                    Location = "거주지 · 샘",
                    Progress = "사고 이후 상황 파악"
                };
            }

            if (!investigation && day <= 2)
            {
                return new QuestTrackerInfo
                {
                    Title = "상황 파악과 재적응",
                    Objective = "사람들과 현재 생활을 살펴보고, 제이가 돌아온 세계를 다시 익힌다.",
                    Location = "행성 전역",
                    Progress = "DAY 3 조사 전"
                };
            }

            if (!water)
            {
                return new QuestTrackerInfo
                {
                    Title = "이곳에서 살아갈 수 있을까",
                    Objective = "오아시스의 물과 장기 거주 가능성을 조사해 위험 여부를 확인한다.",
                    Location = "오아시스",
                    Progress = "장기 거주 위험 조사"
                };
            }

            if (!report)
            {
                return new QuestTrackerInfo
                {
                    Title = "조사 결과를 보고한다",
                    Objective = "알아낸 오아시스의 위험을 샘과 공유하고 다음 판단으로 넘어간다.",
                    Location = "거주지 · 샘",
                    Progress = "조사 완료 / 보고 대기"
                };
            }

            if (!disclosureAll && !disclosureLimited)
            {
                return new QuestTrackerInfo
                {
                    Title = "결정을 앞두고",
                    Objective = "DAY 7 전까지 측근들과 대응 방안을 고민하고, 누구에게 진실을 알릴지 준비한다.",
                    Location = "행성 전역",
                    Progress = $"DAY {day} / 7"
                };
            }

            return new QuestTrackerInfo
            {
                Title = "Week 1 결정 완료",
                Objective = disclosureAll ? "모두에게 진실을 공개하기로 했다." : "일부 핵심 인물에게만 진실을 알리기로 했다.",
                Location = "-",
                Progress = "Week 2 대기"
            };
        }

        // ---------------------------------------------------------------------
        // Location replacement
        // ---------------------------------------------------------------------

        private void DrawLocationReplacement()
        {
            string locationId = ReadField("currentLocationId") as string;
            if (string.IsNullOrEmpty(locationId))
            {
                SetMode("Hub");
                return;
            }

            Rect full = new Rect(0f, HudHeight, Screen.width, Screen.height - HudHeight);
            FillRect(full, LocationBackdrop(locationId));
            FillRect(full, new Color(0.035f, 0.030f, 0.028f, 0.80f));

            GUI.Label(new Rect(26f, HudHeight + 24f, 420f, 42f), LocationName(locationId), modalTitle);
            GUI.Label(new Rect(26f, HudHeight + 66f, 540f, 28f), $"{CurrentSlotLabel()} · 현재 가능한 인카운터", modalSmall);

            if (GUI.Button(new Rect(Screen.width - 196f, HudHeight + 24f, 170f, 44f), "행성으로 돌아가기", buttonStyle))
            {
                WriteField("currentLocationId", null);
                SetMode("Hub");
                WriteStatus("행성 허브로 돌아왔습니다.");
                locationScroll = Vector2.zero;
                return;
            }

            IEnumerable available = GetAvailableEncounters(locationId);
            List<object> items = new List<object>();
            if (available != null)
            {
                foreach (object item in available)
                    if (item != null) items.Add(item);
            }

            Rect grid = new Rect(24f, HudHeight + 112f, Screen.width - 48f, Screen.height - HudHeight - 136f);
            if (items.Count == 0)
            {
                GUI.Label(new Rect(grid.x, grid.y, grid.width, 70f), "현재 이 장소에서 가능한 인카운터가 없습니다.\n시간대나 세계 상태가 바뀌면 인카운터 풀이 달라질 수 있습니다.", modalBody);
                return;
            }

            int columns = Screen.width >= 1400 ? 2 : 1;
            float gap = 18f;
            float scrollbarReserve = 22f;
            float cardWidth = (grid.width - scrollbarReserve - gap * (columns - 1)) / columns;
            float cardHeight = 178f;
            int rows = Mathf.CeilToInt(items.Count / (float)columns);
            float contentHeight = Mathf.Max(grid.height, rows * (cardHeight + gap));

            Rect viewRect = new Rect(0f, 0f, grid.width - scrollbarReserve, contentHeight);
            locationScroll = GUI.BeginScrollView(grid, locationScroll, viewRect);
            for (int i = 0; i < items.Count; i++)
            {
                int row = i / columns;
                int col = i % columns;
                Rect card = new Rect(col * (cardWidth + gap), row * (cardHeight + gap), cardWidth, cardHeight);
                DrawLocationEncounterCard(card, items[i]);
            }
            GUI.EndScrollView();
        }

        private void DrawLocationEncounterCard(Rect card, object encounter)
        {
            string title = ReadNestedString(encounter, "Title");
            string npcId = ReadNestedString(encounter, "PrimaryNpcId");
            string narrative = ReadNestedObject(encounter, "NarrativeType")?.ToString() ?? "World";
            bool executable = EncounterHasExecutableChoice(encounter);

            FillRect(card, executable ? new Color(0.12f, 0.105f, 0.095f, 0.98f) : new Color(0.075f, 0.068f, 0.064f, 0.98f));
            OutlineRect(card, executable ? new Color(0.34f, 0.30f, 0.26f) : new Color(0.18f, 0.17f, 0.16f));

            Color tag = NarrativeColor(narrative);
            FillRect(new Rect(card.x + 14f, card.y + 12f, 116f, 30f), tag);
            GUI.Label(new Rect(card.x + 22f, card.y + 15f, 104f, 26f), NarrativeLabel(narrative), modalSmall);

            float portraitReserve = 0f;
            if (!string.IsNullOrEmpty(npcId))
            {
                Rect portraitBox = new Rect(card.xMax - 98f, card.y + 14f, 78f, 78f);
                DrawSmallPortrait(portraitBox, npcId);
                GUI.Label(new Rect(card.xMax - 114f, card.y + 96f, 96f, 24f), "대상 " + NpcName(npcId), modalSmall);
                portraitReserve = 118f;
            }

            GUI.Label(new Rect(card.x + 14f, card.y + 50f, card.width - 28f - portraitReserve, 54f), title, choiceTitleStyle);
            GUI.Label(new Rect(card.x + 14f, card.y + 108f, card.width - 28f - portraitReserve, 30f), EncounterCostSummary(encounter), modalSmall);

            GUI.enabled = executable;
            if (GUI.Button(new Rect(card.x + 14f, card.yMax - 46f, card.width - 28f, 34f), executable ? "열기" : "현재 실행 불가", buttonStyle))
                OpenEncounterFromLocation(encounter);
            GUI.enabled = true;
        }

        private void DrawSmallPortrait(Rect box, string npcId)
        {
            FillRect(box, new Color(0.055f, 0.05f, 0.048f));
            OutlineRect(box, new Color(0.32f, 0.30f, 0.27f));
            Sprite portrait = PortraitForNpc(npcId);
            if (portrait != null && portrait.texture != null)
                GUI.DrawTexture(new Rect(box.x + 4f, box.y + 4f, box.width - 8f, box.height - 8f), portrait.texture, ScaleMode.ScaleToFit, true);
        }

        // ---------------------------------------------------------------------
        // Encounter / story event replacement
        // ---------------------------------------------------------------------

        private void DrawEncounterReplacement()
        {
            object encounter = ReadField("currentEncounter");
            if (encounter == null)
            {
                SetModeFromObject(ReadField("encounterReturnMode"));
                return;
            }

            DrawFullScreenBackdrop();
            string title = ReadNestedString(encounter, "Title");
            string body = ReadNestedString(encounter, "Body");
            string npcId = ReadNestedString(encounter, "PrimaryNpcId");
            string narrative = ReadNestedObject(encounter, "NarrativeType")?.ToString() ?? "Encounter";

            DrawNarrativeHeader(narrative, npcId, title);
            Rect content = new Rect(34f, HudHeight + 56f, Screen.width - 68f, Screen.height - HudHeight - 82f);
            float portraitWidth = Mathf.Min(390f, content.width * 0.29f);
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
                SetMode("Hub");
                return;
            }

            DrawFullScreenBackdrop();
            string title = ReadNestedString(storyEvent, "Title");
            string npcId = ReadNestedString(storyEvent, "PrimaryNpcId");
            string body = DynamicStoryEventBody(storyEvent);

            DrawNarrativeHeader("EVENT", npcId, title);
            Rect content = new Rect(34f, HudHeight + 56f, Screen.width - 68f, Screen.height - HudHeight - 82f);
            float portraitWidth = Mathf.Min(390f, content.width * 0.29f);
            Rect portraitPanel = new Rect(content.x, content.y, portraitWidth, content.height);
            Rect textPanel = new Rect(portraitPanel.xMax + 24f, content.y, content.width - portraitWidth - 24f, content.height);

            DrawPortraitPanel(portraitPanel, npcId);
            DrawEncounterTextAndChoices(textPanel, storyEvent, body, true);
        }

        private void DrawNarrativeHeader(string type, string npcId, string title)
        {
            float y = HudHeight + 14f;
            GUI.Label(new Rect(34f, y, 170f, 30f), NarrativeLabel(type), modalSmall);
            if (!string.IsNullOrEmpty(npcId))
                GUI.Label(new Rect(194f, y, 150f, 30f), NpcName(npcId), modalSmall);
            GUI.Label(new Rect(360f, y - 2f, Screen.width - 500f, 36f), title, modalTitle);
        }

        private void DrawPortraitPanel(Rect panel, string npcId)
        {
            FillRect(panel, new Color(0.055f, 0.050f, 0.049f, 0.99f));
            OutlineRect(panel, new Color(0.31f, 0.29f, 0.27f));

            Sprite portrait = PortraitForNpc(npcId);
            Rect imageArea = new Rect(panel.x + 14f, panel.y + 14f, panel.width - 28f, panel.height - 72f);
            if (portrait != null && portrait.texture != null)
                GUI.DrawTexture(imageArea, portrait.texture, ScaleMode.ScaleToFit, true);
            else
                GUI.Label(imageArea, "NO PORTRAIT", modalSmall);

            if (!string.IsNullOrEmpty(npcId))
            {
                FillRect(new Rect(panel.x + 14f, panel.yMax - 46f, panel.width - 28f, 32f), new Color(0.08f, 0.07f, 0.068f));
                GUI.Label(new Rect(panel.x + 22f, panel.yMax - 43f, panel.width - 44f, 28f), NpcName(npcId), hudLabel);
            }
        }

        private void DrawEncounterTextAndChoices(Rect panel, object owner, string body, bool isStoryEvent)
        {
            FillRect(panel, new Color(0.042f, 0.038f, 0.037f, 0.995f));
            OutlineRect(panel, new Color(0.27f, 0.25f, 0.23f));

            float textWidth = panel.width - 40f;
            float bodyHeight = Mathf.Clamp(modalBody.CalcHeight(new GUIContent(body), textWidth), 88f, 170f);
            GUI.Label(new Rect(panel.x + 20f, panel.y + 20f, textWidth, bodyHeight), body, modalBody);

            float selectionY = panel.y + 30f + bodyHeight;
            GUI.Label(new Rect(panel.x + 20f, selectionY, textWidth, 38f), "선택", modalTitle);

            float choicesTop = selectionY + 46f;
            float bottomReserve = isStoryEvent ? 24f : 64f;
            Rect scrollRect = new Rect(panel.x + 20f, choicesTop, panel.width - 40f, panel.yMax - choicesTop - bottomReserve);

            object choicesObject = ReadNestedObject(owner, "Choices");
            IEnumerable choices = choicesObject as IEnumerable;
            if (choices == null)
                return;

            List<object> list = new List<object>();
            foreach (object choice in choices)
                if (choice != null) list.Add(choice);

            float cardHeight = 96f;
            float gap = 12f;
            float viewHeight = Mathf.Max(scrollRect.height, list.Count * (cardHeight + gap));
            choiceScroll = GUI.BeginScrollView(scrollRect, choiceScroll, new Rect(0f, 0f, scrollRect.width - 22f, viewHeight));

            float y = 0f;
            foreach (object choice in list)
            {
                int timeCost = ReadNestedInt(choice, "TimeCost");
                int willCost = EffectiveWillCost(choice);
                bool executable = IsChoiceExecutable(choice);
                string label = ReadNestedString(choice, "Label");
                string soft = SoftRequirementText(choice);

                Rect card = new Rect(0f, y, scrollRect.width - 24f, cardHeight);
                FillRect(card, executable ? new Color(0.12f, 0.108f, 0.102f) : new Color(0.073f, 0.068f, 0.065f));
                OutlineRect(card, executable ? new Color(0.42f, 0.39f, 0.34f) : new Color(0.20f, 0.19f, 0.18f));

                GUI.Label(new Rect(card.x + 14f, card.y + 10f, card.width - 174f, 32f), label, choiceTitleStyle);
                string costText = $"소모  시간 {timeCost}h · 의지 {willCost}" + (string.IsNullOrEmpty(soft) ? string.Empty : $" · {soft}");
                GUI.Label(new Rect(card.x + 14f, card.y + 50f, card.width - 178f, 28f), costText, modalSmall);

                GUI.enabled = executable;
                if (GUI.Button(new Rect(card.xMax - 142f, card.y + 23f, 126f, 48f), "선택", buttonStyle))
                {
                    pendingOwner = owner;
                    pendingChoice = choice;
                    pendingIsStoryEvent = isStoryEvent;
                }
                GUI.enabled = true;

                y += cardHeight + gap;
            }
            GUI.EndScrollView();

            if (!isStoryEvent && GUI.Button(new Rect(panel.xMax - 136f, panel.yMax - 50f, 116f, 36f), "뒤로", buttonStyle))
                BackFromEncounter();
        }

        // ---------------------------------------------------------------------
        // Confirmation / result
        // ---------------------------------------------------------------------

        private void DrawConfirmationModal()
        {
            DrawFullScreenBackdrop();
            object owner = pendingOwner;
            object choice = pendingChoice;
            if (owner == null || choice == null)
            {
                ClearPending();
                return;
            }

            string ownerTitle = ReadNestedString(owner, "Title");
            string npcId = ReadNestedString(owner, "PrimaryNpcId");
            string choiceLabel = ReadNestedString(choice, "Label");
            int timeCost = ReadNestedInt(choice, "TimeCost");
            int willCost = EffectiveWillCost(choice);
            string soft = SoftRequirementText(choice);

            Rect panel = CenteredPanel(820f, 500f);
            FillRect(panel, new Color(0.043f, 0.038f, 0.037f, 0.997f));
            OutlineRect(panel, new Color(0.57f, 0.52f, 0.43f));

            GUI.Label(new Rect(panel.x + 30f, panel.y + 24f, panel.width - 60f, 38f), "선택 확인", modalTitle);
            GUI.Label(new Rect(panel.x + 30f, panel.y + 76f, panel.width - 60f, 30f), ownerTitle, hudLabel);

            string secondBeat = string.IsNullOrEmpty(npcId)
                ? $"제이는 「{choiceLabel}」 행동을 진행하기로 한다."
                : $"{NpcName(npcId)}와 마주한 제이는 「{choiceLabel}」 쪽으로 행동을 정한다.";
            GUI.Label(new Rect(panel.x + 30f, panel.y + 122f, panel.width - 60f, 116f), secondBeat, modalBody);

            Rect costBox = new Rect(panel.x + 30f, panel.y + 254f, panel.width - 60f, 86f);
            FillRect(costBox, new Color(0.085f, 0.078f, 0.073f));
            OutlineRect(costBox, new Color(0.35f, 0.32f, 0.28f));
            GUI.Label(new Rect(costBox.x + 16f, costBox.y + 12f, 72f, 28f), "소모", choiceTitleStyle);
            GUI.Label(new Rect(costBox.x + 88f, costBox.y + 11f, costBox.width - 104f, 30f), $"시간 {timeCost}h  ·  의지 {willCost}", hudLabel);
            if (!string.IsNullOrEmpty(soft))
                GUI.Label(new Rect(costBox.x + 88f, costBox.y + 48f, costBox.width - 104f, 26f), $"효율 조건  {soft}", modalSmall);

            if (GUI.Button(new Rect(panel.x + 30f, panel.yMax - 72f, 166f, 44f), "취소", buttonStyle))
                ClearPending();

            GUI.enabled = IsChoiceExecutable(choice);
            if (GUI.Button(new Rect(panel.xMax - 196f, panel.yMax - 72f, 166f, 44f), "확인", buttonStyle))
                ExecutePendingChoice();
            GUI.enabled = true;
        }

        private void DrawResultModal()
        {
            DrawFullScreenBackdrop();
            Rect panel = CenteredPanel(860f, 540f);
            FillRect(panel, new Color(0.043f, 0.038f, 0.037f, 0.997f));
            OutlineRect(panel, new Color(0.64f, 0.59f, 0.48f));

            GUI.Label(new Rect(panel.x + 32f, panel.y + 26f, panel.width - 64f, 40f), resultTitle, modalTitle);
            GUI.Label(new Rect(panel.x + 32f, panel.y + 82f, panel.width - 64f, 124f), resultNarrative, modalBody);
            GUI.Label(new Rect(panel.x + 32f, panel.y + 220f, panel.width - 64f, 36f), "결과", modalTitle);

            Rect box = new Rect(panel.x + 32f, panel.y + 266f, panel.width - 64f, 166f);
            FillRect(box, new Color(0.078f, 0.073f, 0.069f));
            OutlineRect(box, new Color(0.35f, 0.32f, 0.27f));

            float y = box.y + 16f;
            if (resultChanges.Count == 0)
            {
                GUI.Label(new Rect(box.x + 18f, y, box.width - 36f, 28f), "수치 변화 없음", modalSmall);
            }
            else
            {
                foreach (string line in resultChanges)
                {
                    GUI.Label(new Rect(box.x + 18f, y, box.width - 36f, 28f), "• " + line, resultGainStyle);
                    y += 30f;
                    if (y > box.yMax - 28f) break;
                }
            }

            if (GUI.Button(new Rect(panel.xMax - 196f, panel.yMax - 72f, 164f, 44f), "계속", buttonStyle))
            {
                resultOpen = false;
                resultTitle = string.Empty;
                resultNarrative = string.Empty;
                resultChanges.Clear();
                ReleaseController();
            }
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

            Log("RESULT", $"{ownerTitle} / {choiceLabel} / {narrativeResult}");
            foreach (string change in resultChanges)
                Log("CHANGE", change);

            lastSnapshot = after;
            ClearPending(false);
        }

        private void ClearPending(bool release = true)
        {
            pendingOwner = null;
            pendingChoice = null;
            pendingIsStoryEvent = false;
            if (release) ReleaseController();
        }

        // ---------------------------------------------------------------------
        // Controller bridge
        // ---------------------------------------------------------------------

        private void OpenEncounterFromLocation(object encounter)
        {
            object locationMode = EnumValue("Location");
            WriteField("currentEncounter", encounter);
            WriteField("encounterReturnMode", locationMode);
            WriteField("mode", EnumValue("Encounter"));
            WriteStatus("인카운터를 열었습니다.");
            choiceScroll = Vector2.zero;
        }

        private void BackFromEncounter()
        {
            object returnMode = ReadField("encounterReturnMode");
            if (returnMode != null) WriteField("mode", returnMode);
            WriteField("currentEncounter", null);
            choiceScroll = Vector2.zero;
            ReleaseController();
        }

        private IEnumerable GetAvailableEncounters(string locationId)
        {
            MethodInfo method = Method("GetAvailableEncounters");
            if (method == null) return null;
            try { return method.Invoke(controller, new object[] { locationId, false }) as IEnumerable; }
            catch { return null; }
        }

        private string DynamicStoryEventBody(object storyEvent)
        {
            MethodInfo method = Method("GetDynamicEventBody");
            if (method != null)
            {
                try { return method.Invoke(controller, new[] { storyEvent }) as string ?? ReadNestedString(storyEvent, "Body"); }
                catch { }
            }
            return ReadNestedString(storyEvent, "Body");
        }

        private bool EncounterHasExecutableChoice(object encounter)
        {
            IEnumerable choices = ReadNestedObject(encounter, "Choices") as IEnumerable;
            if (choices == null) return false;
            foreach (object choice in choices)
                if (choice != null && IsChoiceExecutable(choice)) return true;
            return false;
        }

        private bool IsChoiceExecutable(object choice)
        {
            int timeCost = ReadNestedInt(choice, "TimeCost");
            int willCost = EffectiveWillCost(choice);
            bool timeOk = ReadInt("currentHour") + timeCost <= ReadInt("dayEndHour");
            bool willOk = ReadInt("currentWillpower") >= willCost;
            bool hardOk = true;

            object hard = ReadNestedObject(choice, "HardConditions");
            MethodInfo conditions = Method("ConditionsMet");
            if (conditions != null && hard != null)
            {
                try { hardOk = (bool)conditions.Invoke(controller, new[] { hard }); }
                catch { hardOk = true; }
            }
            return timeOk && willOk && hardOk;
        }

        private int EffectiveWillCost(object choice)
        {
            MethodInfo method = Method("EffectiveWillCost");
            if (method != null)
            {
                try { return Convert.ToInt32(method.Invoke(controller, new[] { choice })); }
                catch { }
            }
            return ReadNestedInt(choice, "BaseWillCost");
        }

        private string SoftRequirementText(object choice)
        {
            object stat = ReadNestedObject(choice, "SoftStat");
            int requirement = ReadNestedInt(choice, "SoftRequirement");
            if (stat == null || requirement <= 0 || stat.ToString() == "None") return string.Empty;
            string name = stat.ToString() == "Personal" ? "개인" : stat.ToString() == "Social" ? "대인" : stat.ToString() == "Technical" ? "기술" : stat.ToString();
            return $"{name} Lv.{requirement}";
        }

        private object EnumValue(string name)
        {
            object current = ReadField("mode");
            if (current == null) return null;
            return Enum.Parse(current.GetType(), name);
        }

        private void SetMode(string name)
        {
            object value = EnumValue(name);
            if (value != null) WriteField("mode", value);
            ReleaseController();
        }

        private void SetModeFromObject(object value)
        {
            if (value != null) WriteField("mode", value);
            ReleaseController();
        }

        private void WriteStatus(string text)
        {
            WriteField("statusMessage", text);
        }

        private void ReleaseController()
        {
            if (controller != null) controller.enabled = true;
        }

        private void InvokeSafe(string methodName)
        {
            try { Invoke(methodName); }
            catch (Exception exception) { Debug.LogException(exception); }
        }

        private object Invoke(string name, params object[] args)
        {
            MethodInfo method = Method(name);
            if (method == null) throw new MissingMethodException(controllerType.FullName, name);
            try { return method.Invoke(controller, args); }
            catch (TargetInvocationException ex) { throw ex.InnerException ?? ex; }
        }

        // ---------------------------------------------------------------------
        // Text / display helpers
        // ---------------------------------------------------------------------

        private string EncounterCostSummary(object encounter)
        {
            IEnumerable choices = ReadNestedObject(encounter, "Choices") as IEnumerable;
            if (choices == null) return "선택 없음";

            int minTime = int.MaxValue, maxTime = int.MinValue, minWill = int.MaxValue, maxWill = int.MinValue, count = 0;
            foreach (object choice in choices)
            {
                if (choice == null) continue;
                count++;
                int time = ReadNestedInt(choice, "TimeCost");
                int will = EffectiveWillCost(choice);
                minTime = Mathf.Min(minTime, time); maxTime = Mathf.Max(maxTime, time);
                minWill = Mathf.Min(minWill, will); maxWill = Mathf.Max(maxWill, will);
            }
            if (count == 0) return "선택 없음";
            string timeText = minTime == maxTime ? $"{minTime}h" : $"{minTime}~{maxTime}h";
            string willText = minWill == maxWill ? minWill.ToString() : $"{minWill}~{maxWill}";
            return $"소모  시간 {timeText} · 의지 {willText} · {count}개 접근";
        }

        private string NarrativeLabel(string type)
        {
            switch (type.ToUpperInvariant())
            {
                case "MAIN": return "◆ MAIN";
                case "CHARACTER": return "● CHARACTER";
                case "ACTIVITY": return "▲ ACTIVITY";
                case "WORLD": return "■ WORLD";
                case "EVENT": return "◆ EVENT";
                default: return "• " + type.ToUpperInvariant();
            }
        }

        private Color NarrativeColor(string type)
        {
            switch (type.ToUpperInvariant())
            {
                case "MAIN": return new Color(0.50f, 0.28f, 0.16f);
                case "CHARACTER": return new Color(0.22f, 0.34f, 0.48f);
                case "ACTIVITY": return new Color(0.28f, 0.43f, 0.31f);
                default: return new Color(0.36f, 0.33f, 0.30f);
            }
        }

        private string LocationName(string id)
        {
            switch (id)
            {
                case "ship": return "수송선";
                case "graveyard": return "묘지";
                case "settlement": return "거주지";
                case "oasis": return "오아시스";
                default: return id ?? "행성";
            }
        }

        private Color LocationBackdrop(string id)
        {
            switch (id)
            {
                case "ship": return new Color(0.23f, 0.28f, 0.31f);
                case "graveyard": return new Color(0.19f, 0.19f, 0.22f);
                case "settlement": return new Color(0.30f, 0.22f, 0.14f);
                case "oasis": return new Color(0.10f, 0.29f, 0.30f);
                default: return new Color(0.13f, 0.11f, 0.10f);
            }
        }

        private string CurrentSlotLabel()
        {
            int hour = ReadInt("currentHour");
            if (hour >= 6 && hour < 12) return "Morning";
            if (hour >= 12 && hour < 17) return "Afternoon";
            if (hour >= 17 && hour <= 24) return "Evening";
            return "Outside";
        }

        private bool Flag(string key)
        {
            object source = ReadField("flags");
            if (source is IDictionary dict && dict.Contains(key))
                return Convert.ToBoolean(dict[key]);
            return false;
        }

        private Sprite PortraitForNpc(string npcId)
        {
            string fieldName = npcId == "sam" ? "samPortrait" : npcId == "jina" ? "jinaPortrait" : npcId == "faye" ? "fayePortrait" : npcId == "benjamin" ? "benjaminPortrait" : npcId == "borichi" ? "borichiPortrait" : npcId == "diya" ? "diyaPortrait" : string.Empty;
            return string.IsNullOrEmpty(fieldName) ? null : ReadField(fieldName) as Sprite;
        }

        private string NpcName(string id)
        {
            return id == "sam" ? "샘" : id == "jina" ? "지나" : id == "faye" ? "페이" : id == "benjamin" ? "벤자민" : id == "borichi" ? "보리치" : id == "diya" ? "디야" : id;
        }

        // ---------------------------------------------------------------------
        // Result / logging
        // ---------------------------------------------------------------------

        private void BuildPlayerFacingChanges(Snapshot before, Snapshot after, List<string> output)
        {
            int hours = after.Hour - before.Hour;
            if (hours != 0) output.Add($"시간 {before.Hour:00}:00 → {after.Hour:00}:00 ({(hours > 0 ? "+" : string.Empty)}{hours}h)");
            int will = after.Will - before.Will;
            if (will != 0) output.Add($"의지 {before.Will}/{before.MaxWill} → {after.Will}/{after.MaxWill} ({Signed(will)})");
            AddStatResult(output, "개인", before.PersonalLevel, before.PersonalXp, after.PersonalLevel, after.PersonalXp);
            AddStatResult(output, "대인", before.SocialLevel, before.SocialXp, after.SocialLevel, after.SocialXp);
            AddStatResult(output, "기술", before.TechnicalLevel, before.TechnicalXp, after.TechnicalLevel, after.TechnicalXp);
            AddAffinityResults(output, before.Affinities, after.Affinities);
        }

        private static void AddStatResult(List<string> output, string label, int bl, int bx, int al, int ax)
        {
            if (bl == al && bx == ax) return;
            if (bl != al) output.Add($"{label} Lv.{bl} {bx}/6 XP → Lv.{al} {ax}/6 XP");
            else output.Add($"{label} XP {Signed(ax - bx)} ({bx}/6 → {ax}/6)");
        }

        private void AddAffinityResults(List<string> output, Dictionary<string, int> before, Dictionary<string, int> after)
        {
            HashSet<string> keys = new HashSet<string>(before.Keys);
            foreach (string key in after.Keys) keys.Add(key);
            foreach (string key in keys)
            {
                int b = before.TryGetValue(key, out int bv) ? bv : 0;
                int a = after.TryGetValue(key, out int av) ? av : 0;
                if (a != b) output.Add($"{NpcName(key)} 호감도 {b} → {a} ({Signed(a - b)})");
            }
        }

        private void LogPassiveStateChanges(Snapshot before, Snapshot after)
        {
            if (before.Day != after.Day) Log("CHANGE", $"DAY {before.Day} → DAY {after.Day}");
            if (before.Hour != after.Hour && !resultOpen) Log("CHANGE", $"시간 {before.Hour:00}:00 → {after.Hour:00}:00");
            if ((before.Will != after.Will || before.MaxWill != after.MaxWill) && !resultOpen) Log("CHANGE", $"의지 {before.Will}/{before.MaxWill} → {after.Will}/{after.MaxWill}");
            LogStatIfChanged("개인", before.PersonalLevel, before.PersonalXp, after.PersonalLevel, after.PersonalXp);
            LogStatIfChanged("대인", before.SocialLevel, before.SocialXp, after.SocialLevel, after.SocialXp);
            LogStatIfChanged("기술", before.TechnicalLevel, before.TechnicalXp, after.TechnicalLevel, after.TechnicalXp);
            LogFlagChanges(before.Flags, after.Flags);
        }

        private void LogStatIfChanged(string label, int bl, int bx, int al, int ax)
        {
            if (bl != al || bx != ax) Log("CHANGE", $"{label} Lv.{bl} {bx}/6 XP → Lv.{al} {ax}/6 XP");
        }

        private void LogFlagChanges(Dictionary<string, bool> before, Dictionary<string, bool> after)
        {
            HashSet<string> keys = new HashSet<string>(before.Keys);
            foreach (string key in after.Keys) keys.Add(key);
            foreach (string key in keys)
            {
                bool b = before.TryGetValue(key, out bool bv) && bv;
                bool a = after.TryGetValue(key, out bool av) && av;
                if (a != b) Log("FLAG", $"{key}: {(b ? "ON" : "OFF")} → {(a ? "ON" : "OFF")}");
            }
        }

        private Snapshot CaptureSnapshot()
        {
            return new Snapshot
            {
                Day = ReadInt("day"), Hour = ReadInt("currentHour"), Will = ReadInt("currentWillpower"), MaxWill = ReadInt("maxWillpower"),
                DayStart = ReadInt("dayStartHour"), DayEnd = ReadInt("dayEndHour"),
                PersonalLevel = ReadInt("personalLevel"), PersonalXp = ReadInt("personalXp"),
                SocialLevel = ReadInt("socialLevel"), SocialXp = ReadInt("socialXp"),
                TechnicalLevel = ReadInt("technicalLevel"), TechnicalXp = ReadInt("technicalXp"),
                Affinities = CopyIntDictionary(ReadField("affinities")), Flags = CopyBoolDictionary(ReadField("flags"))
            };
        }

        // ---------------------------------------------------------------------
        // Reflection / setup
        // ---------------------------------------------------------------------

        private void BindController()
        {
            string[] fieldNames =
            {
                "day", "currentHour", "dayStartHour", "dayEndHour", "currentWillpower", "maxWillpower",
                "personalLevel", "personalXp", "socialLevel", "socialXp", "technicalLevel", "technicalXp",
                "mode", "encounterReturnMode", "currentLocationId", "currentEncounter", "currentStoryEvent", "statusMessage",
                "affinities", "flags", "samPortrait", "jinaPortrait", "fayePortrait", "benjaminPortrait", "borichiPortrait", "diyaPortrait",
                "titleStyle", "headingStyle", "bodyStyle", "smallStyle", "buttonStyle", "badgeStyle", "locationTitleStyle"
            };
            foreach (string name in fieldNames)
            {
                FieldInfo field = controllerType.GetField(name, InstancePrivate);
                if (field != null) fields[name] = field;
            }

            string[] methodNames = { "ExecuteEncounterChoice", "ExecuteStoryEventChoice", "EffectiveWillCost", "ConditionsMet", "GetAvailableEncounters", "GetDynamicEventBody", "TakeRest", "EndDay" };
            foreach (string name in methodNames)
            {
                MethodInfo method = FindMethod(name);
                if (method != null) methods[name] = method;
            }
        }

        private void DisableLegacyUx()
        {
            if (legacyUx == null) legacyUx = GetComponent<SandPlanetPrototype03PlaytestUX>();
            if (legacyDebug == null) legacyDebug = GetComponent<SandPlanetPrototype03DebugLogOverlay>();
            if (legacyUx != null && legacyUx.enabled) legacyUx.enabled = false;
            if (legacyDebug != null && legacyDebug.enabled) legacyDebug.enabled = false;
        }

        private void BoostControllerStylesWhenReady()
        {
            if (controllerStylesBoosted) return;
            GUIStyle title = ReadField("titleStyle") as GUIStyle;
            GUIStyle heading = ReadField("headingStyle") as GUIStyle;
            GUIStyle body = ReadField("bodyStyle") as GUIStyle;
            GUIStyle small = ReadField("smallStyle") as GUIStyle;
            GUIStyle button = ReadField("buttonStyle") as GUIStyle;
            GUIStyle badge = ReadField("badgeStyle") as GUIStyle;
            GUIStyle location = ReadField("locationTitleStyle") as GUIStyle;
            if (title == null || heading == null || body == null || small == null || button == null) return;

            title.fontSize = Mathf.Max(title.fontSize, 29);
            heading.fontSize = Mathf.Max(heading.fontSize, 20);
            body.fontSize = Mathf.Max(body.fontSize, 18);
            small.fontSize = Mathf.Max(small.fontSize, 15);
            button.fontSize = Mathf.Max(button.fontSize, 16);
            if (badge != null) badge.fontSize = Mathf.Max(badge.fontSize, 15);
            if (location != null) location.fontSize = Mathf.Max(location.fontSize, 20);
            controllerStylesBoosted = true;
        }

        private bool PointerOverOverlay()
        {
            if (Mouse.current == null) return false;
            Vector2 p = Mouse.current.position.ReadValue();
            float guiY = Screen.height - p.y;
            if (guiY <= HudHeight) return true;
            Rect quest = new Rect(Screen.width - 392f, HudHeight + 10f, 392f, Mathf.Min(520f, Screen.height - HudHeight - 30f));
            return quest.Contains(new Vector2(p.x, guiY));
        }

        private MethodInfo FindMethod(string name)
        {
            foreach (MethodInfo method in controllerType.GetMethods(InstancePrivate))
                if (method.Name == name) return method;
            return null;
        }

        private MethodInfo Method(string name) => methods.TryGetValue(name, out MethodInfo value) ? value : null;
        private object ReadField(string name) => fields.TryGetValue(name, out FieldInfo field) ? field.GetValue(controller) : null;
        private int ReadInt(string name) { object value = ReadField(name); return value == null ? 0 : Convert.ToInt32(value); }
        private void WriteField(string name, object value) { if (fields.TryGetValue(name, out FieldInfo field)) field.SetValue(controller, value); }
        private string ReadMode() => ReadField("mode")?.ToString() ?? string.Empty;

        private static object ReadNestedObject(object owner, string fieldName)
        {
            if (owner == null) return null;
            return owner.GetType().GetField(fieldName, NestedFields)?.GetValue(owner);
        }
        private static string ReadNestedString(object owner, string fieldName) => ReadNestedObject(owner, fieldName) as string ?? string.Empty;
        private static int ReadNestedInt(object owner, string fieldName) { object value = ReadNestedObject(owner, fieldName); return value == null ? 0 : Convert.ToInt32(value); }

        private static Dictionary<string, int> CopyIntDictionary(object source)
        {
            Dictionary<string, int> copy = new Dictionary<string, int>();
            if (source is IDictionary dictionary)
                foreach (DictionaryEntry entry in dictionary)
                    if (entry.Key is string key) copy[key] = Convert.ToInt32(entry.Value);
            return copy;
        }

        private static Dictionary<string, bool> CopyBoolDictionary(object source)
        {
            Dictionary<string, bool> copy = new Dictionary<string, bool>();
            if (source is IDictionary dictionary)
                foreach (DictionaryEntry entry in dictionary)
                    if (entry.Key is string key) copy[key] = Convert.ToBoolean(entry.Value);
            return copy;
        }

        // ---------------------------------------------------------------------
        // GUI / logging utilities
        // ---------------------------------------------------------------------

        private void EnsureStyles()
        {
            if (hudLabel != null) return;
            hudLabel = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft, normal = { textColor = Color.white } };
            hudSmall = new GUIStyle(GUI.skin.label) { fontSize = 13, wordWrap = true, alignment = TextAnchor.UpperLeft, normal = { textColor = new Color(0.77f, 0.75f, 0.71f) } };
            modalTitle = new GUIStyle(GUI.skin.label) { fontSize = 25, fontStyle = FontStyle.Bold, wordWrap = true, normal = { textColor = Color.white } };
            modalBody = new GUIStyle(GUI.skin.label) { fontSize = 18, wordWrap = true, normal = { textColor = new Color(0.92f, 0.90f, 0.86f) } };
            modalSmall = new GUIStyle(GUI.skin.label) { fontSize = 15, wordWrap = true, normal = { textColor = new Color(0.72f, 0.76f, 0.81f) } };
            choiceTitleStyle = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold, wordWrap = true, normal = { textColor = Color.white } };
            resultGainStyle = new GUIStyle(GUI.skin.label) { fontSize = 17, wordWrap = true, normal = { textColor = new Color(0.79f, 0.93f, 0.73f) } };
            buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 17, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, wordWrap = true };
            questHeaderStyle = new GUIStyle(GUI.skin.label) { fontSize = 24, fontStyle = FontStyle.Bold, normal = { textColor = Color.white } };
        }

        private void DrawFullScreenBackdrop()
        {
            FillRect(new Rect(0f, HudHeight, Screen.width, Screen.height - HudHeight), new Color(0.024f, 0.021f, 0.021f, 1f));
        }

        private Rect CenteredPanel(float preferredWidth, float preferredHeight)
        {
            float width = Mathf.Min(preferredWidth, Screen.width - 60f);
            float height = Mathf.Min(preferredHeight, Screen.height - HudHeight - 40f);
            return new Rect((Screen.width - width) * 0.5f, HudHeight + Mathf.Max(20f, (Screen.height - HudHeight - height) * 0.5f), width, height);
        }

        private void FillRect(Rect rect, Color color)
        {
            Color previous = GUI.color; GUI.color = color; GUI.DrawTexture(rect, Texture2D.whiteTexture); GUI.color = previous;
        }

        private void OutlineRect(Rect rect, Color color)
        {
            FillRect(new Rect(rect.x, rect.y, rect.width, 1f), color); FillRect(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), color);
            FillRect(new Rect(rect.x, rect.y, 1f, rect.height), color); FillRect(new Rect(rect.xMax - 1f, rect.y, 1f, rect.height), color);
        }

        private static string Signed(int value) => value > 0 ? "+" + value : value.ToString();

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
                string header = $"SandPlanet Prototype 0.3 Playtest Log{Environment.NewLine}Started: {DateTime.Now:yyyy-MM-dd HH:mm:ss}{Environment.NewLine}Unity: {Application.unityVersion}{Environment.NewLine}" + new string('-', 72) + Environment.NewLine;
                File.WriteAllText(sessionLogPath, header); File.WriteAllText(latestLogPath, header);
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[P0.3 UX2] Could not create log file: " + exception.Message);
                sessionLogPath = null; latestLogPath = null;
            }
        }

        private void Log(string category, string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return;
            string stamp = ReadInt("day") > 0 ? $"D{ReadInt("day")} {ReadInt("currentHour"):00}:00" : "SETUP";
            string entry = $"[{stamp}] [{category}] {message}";
            Debug.Log("[P0.3 LOG] " + entry);
            try
            {
                string line = $"{DateTime.Now:HH:mm:ss.fff} {entry}{Environment.NewLine}";
                if (!string.IsNullOrEmpty(sessionLogPath)) File.AppendAllText(sessionLogPath, line);
                if (!string.IsNullOrEmpty(latestLogPath)) File.AppendAllText(latestLogPath, line);
            }
            catch (Exception exception) { Debug.LogWarning("[P0.3 UX2] Could not append log: " + exception.Message); }
        }
    }
}
