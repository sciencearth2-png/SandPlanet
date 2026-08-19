using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SandPlanet.Prototype
{
    /// <summary>
    /// Prototype 0.3 — Week 1 Encounter Foundation.
    ///
    /// Validates the current SandPlanet rules without modifying Prototype 0.1/0.2:
    /// - Personal / Interpersonal / Technical Lv + XP (6 XP per Lv)
    /// - no daily trend growth
    /// - 3h rest -> +1 will, sleep -> +2 will
    /// - state/condition driven dynamic Encounter pool
    /// - Main / Character / Activity / World as visual/narrative tags only
    /// - forced Story Events separated from player-selected Encounters
    /// - character portrait switching by Primary NPC
    ///
    /// Content and the Soft Requirement will formula in this file are TEMP prototype data.
    /// </summary>
    public sealed class SandPlanetPrototype03Controller : MonoBehaviour
    {
        private const int FinalGameDays = 21;
        private const int PrototypeDays = 7;
        private const int BaseDayStartHour = 8;
        private const int BaseDayEndHour = 22;
        private const int BaseMaxWillpower = 5;
        private const int SleepRecovery = 2;
        private const int RestHours = 3;
        private const int RestRecovery = 1;
        private const int XpPerLevel = 6;
        private const int MaxStatLevel = 20;

        private const string FlagAwake = "W1_AWAKE";
        private const string FlagSituationHeard = "W1_SITUATION_HEARD";
        private const string FlagGraveSeen = "W1_GRAVE_SEEN";
        private const string FlagMemorialDone = "W1_MEMORIAL_DONE";
        private const string FlagInvestigationOpen = "W1_INVESTIGATION_OPEN";
        private const string FlagWaterRiskKnown = "W1_WATER_RISK_KNOWN";
        private const string FlagReportDone = "W1_REPORT_DONE";
        private const string FlagShipRepairPriority = "W1_SHIP_REPAIR_PRIORITY";
        private const string FlagPublicReactionPrepared = "W1_PUBLIC_REACTION_PREPARED";
        private const string FlagSettlementPerspective = "W1_SETTLEMENT_PERSPECTIVE";
        private const string FlagDisclosureAll = "W1_DISCLOSURE_ALL";
        private const string FlagDisclosureLimited = "W1_DISCLOSURE_LIMITED";

        private enum ScreenMode
        {
            Setup,
            Hub,
            Location,
            Encounter,
            StoryEvent,
            Complete
        }

        private enum StatType
        {
            None,
            Personal,
            Social,
            Technical
        }

        private enum NarrativeType
        {
            Main,
            Character,
            Activity,
            World
        }

        [Flags]
        private enum TimeSlotMask
        {
            None = 0,
            Morning = 1 << 0,
            Afternoon = 1 << 1,
            Evening = 1 << 2,
            All = Morning | Afternoon | Evening
        }

        private enum RepeatRule
        {
            Once,
            Daily,
            Unlimited
        }

        private enum ConditionType
        {
            FlagTrue,
            FlagFalse,
            StatAtLeast,
            AffinityAtLeast
        }

        private enum ActionType
        {
            AddXp,
            AddAffinity,
            SetFlag
        }

        [Serializable]
        private sealed class PrototypeCondition
        {
            public ConditionType Type;
            public string Key;
            public StatType Stat;
            public int IntValue;
        }

        [Serializable]
        private sealed class PrototypeAction
        {
            public ActionType Type;
            public string Key;
            public StatType Stat;
            public int IntValue;
            public bool BoolValue;
        }

        [Serializable]
        private sealed class PrototypeChoice
        {
            public string Id;
            public string Label;
            [TextArea] public string ResultText;
            public int TimeCost;
            public int BaseWillCost;
            public StatType SoftStat;
            public int SoftRequirement;
            public readonly List<PrototypeCondition> HardConditions = new List<PrototypeCondition>();
            public readonly List<PrototypeAction> Actions = new List<PrototypeAction>();
        }

        [Serializable]
        private sealed class PrototypeEncounter
        {
            public string Id;
            public string VariantGroup;
            public NarrativeType NarrativeType;
            public bool IsGlobal;
            public string LocationId;
            public string PrimaryNpcId;
            public string Title;
            [TextArea] public string Body;
            public int OpenDay = 1;
            public int CloseDay = PrototypeDays;
            public TimeSlotMask AllowedSlots = TimeSlotMask.All;
            public RepeatRule RepeatRule = RepeatRule.Once;
            public readonly List<PrototypeCondition> ShowConditions = new List<PrototypeCondition>();
            public readonly List<PrototypeCondition> HideConditions = new List<PrototypeCondition>();
            public readonly List<PrototypeChoice> Choices = new List<PrototypeChoice>();

            [NonSerialized] public bool Completed;
            [NonSerialized] public int LastCompletedDay = -1;
        }

        [Serializable]
        private sealed class PrototypeStoryEvent
        {
            public string Id;
            public int Day;
            public string PrimaryNpcId;
            public string Title;
            [TextArea] public string Body;
            public readonly List<PrototypeChoice> Choices = new List<PrototypeChoice>();
            [NonSerialized] public bool Completed;
        }

        [Header("Prototype 0.3 Portraits")]
        [SerializeField] private Sprite samPortrait;
        [SerializeField] private Sprite jinaPortrait;
        [SerializeField] private Sprite fayePortrait;
        [SerializeField] private Sprite benjaminPortrait;
        [SerializeField] private Sprite borichiPortrait;
        [SerializeField] private Sprite diyaPortrait;

        private readonly List<PrototypeEncounter> encounters = new List<PrototypeEncounter>();
        private readonly List<PrototypeStoryEvent> storyEvents = new List<PrototypeStoryEvent>();
        private readonly Dictionary<string, bool> flags = new Dictionary<string, bool>();
        private readonly Dictionary<string, int> affinities = new Dictionary<string, int>();
        private readonly Dictionary<string, int> variantGroupLastCompletedDay = new Dictionary<string, int>();
        private readonly Dictionary<PrototypeLocationNode, Vector3> nodeBaseScales = new Dictionary<PrototypeLocationNode, Vector3>();

        private ScreenMode mode = ScreenMode.Setup;
        private ScreenMode encounterReturnMode = ScreenMode.Hub;
        private string currentLocationId;
        private PrototypeEncounter currentEncounter;
        private PrototypeStoryEvent currentStoryEvent;
        private PrototypeLocationNode hoveredNode;
        private PrototypeLocationNode[] locationNodes;

        private int day;
        private int currentHour;
        private int dayStartHour;
        private int dayEndHour;
        private int currentWillpower;
        private int maxWillpower;

        private int personalLevel;
        private int socialLevel;
        private int technicalLevel;
        private int personalXp;
        private int socialXp;
        private int technicalXp;

        private int setupPersonal = 2;
        private int setupSocial = 2;
        private int setupTechnical = 2;

        private string statusMessage = "시작 능력치를 배분하세요.";
        private Vector2 encounterScroll;

        private GUIStyle titleStyle;
        private GUIStyle headingStyle;
        private GUIStyle bodyStyle;
        private GUIStyle smallStyle;
        private GUIStyle centeredStyle;
        private GUIStyle buttonStyle;
        private GUIStyle cardStyle;
        private GUIStyle disabledCardStyle;
        private GUIStyle badgeStyle;
        private GUIStyle mainBadgeStyle;
        private GUIStyle characterBadgeStyle;
        private GUIStyle activityBadgeStyle;
        private GUIStyle worldBadgeStyle;
        private GUIStyle locationTitleStyle;

        private Texture2D darkTexture;
        private Texture2D panelTexture;
        private Texture2D disabledTexture;
        private Texture2D mainTexture;
        private Texture2D characterTexture;
        private Texture2D activityTexture;
        private Texture2D worldTexture;
        private Texture2D oasisTexture;
        private Texture2D settlementTexture;
        private Texture2D graveyardTexture;
        private Texture2D shipTexture;

        public void ConfigurePortraits(
            Sprite sam,
            Sprite jina,
            Sprite faye,
            Sprite benjamin,
            Sprite borichi,
            Sprite diya)
        {
            samPortrait = sam;
            jinaPortrait = jina;
            fayePortrait = faye;
            benjaminPortrait = benjamin;
            borichiPortrait = borichi;
            diyaPortrait = diya;
        }

        private void Awake()
        {
            BuildPrototypeContent();
            ResetRun();
        }

        private void Start()
        {
            RefreshLocationNodes();
        }

        private void Update()
        {
            if (mode != ScreenMode.Hub || Mouse.current == null || Camera.main == null)
            {
                SetHoveredNode(null);
                return;
            }

            Vector2 mouse = Mouse.current.position.ReadValue();
            float guiY = Screen.height - mouse.y;
            bool blocked = guiY < 94f || mouse.x > Screen.width - 390f || guiY > Screen.height - 92f;
            if (blocked)
            {
                SetHoveredNode(null);
                return;
            }

            Ray ray = Camera.main.ScreenPointToRay(mouse);
            PrototypeLocationNode node = null;
            if (Physics.Raycast(ray, out RaycastHit hit, 500f))
                node = hit.collider.GetComponentInParent<PrototypeLocationNode>();

            SetHoveredNode(node);

            if (node != null && Mouse.current.leftButton.wasPressedThisFrame)
                OpenLocation(node.LocationId);
        }

        private void OnGUI()
        {
            EnsureStyles();

            if (mode == ScreenMode.Setup)
            {
                DrawSetupScreen();
                return;
            }

            if (mode == ScreenMode.Complete)
            {
                DrawCompleteScreen();
                return;
            }

            DrawHud();

            switch (mode)
            {
                case ScreenMode.Hub:
                    DrawHubScreen();
                    break;
                case ScreenMode.Location:
                    DrawLocationScreen();
                    break;
                case ScreenMode.Encounter:
                    DrawEncounterScreen();
                    break;
                case ScreenMode.StoryEvent:
                    DrawStoryEventScreen();
                    break;
            }

            DrawStatusMessage();
        }

        private void DrawSetupScreen()
        {
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), darkTexture, ScaleMode.StretchToFill);

            float width = Mathf.Min(760f, Screen.width - 60f);
            Rect panel = new Rect((Screen.width - width) * 0.5f, 56f, width, 590f);
            GUI.Box(panel, string.Empty);

            GUILayout.BeginArea(new Rect(panel.x + 36f, panel.y + 28f, panel.width - 72f, panel.height - 56f));
            GUILayout.Label("SAND PLANET — PROTOTYPE 0.3", titleStyle);
            GUILayout.Space(6f);
            GUILayout.Label("Week 1 Encounter Foundation", headingStyle);
            GUILayout.Label("개인 / 대인 / 기술 Lv의 합이 6이 되도록 배분합니다. 각 능력치는 6XP마다 즉시 Lv이 1 상승합니다.", bodyStyle);
            GUILayout.Space(18f);

            DrawSetupStatRow("개인", ref setupPersonal);
            DrawSetupStatRow("대인", ref setupSocial);
            DrawSetupStatRow("기술", ref setupTechnical);

            int total = setupPersonal + setupSocial + setupTechnical;
            GUILayout.Space(10f);
            GUILayout.Label($"시작 Lv 합계: {total} / 6", headingStyle);
            GUILayout.Space(12f);

            GUI.enabled = total == 6;
            if (GUILayout.Button("DAY 1 시작", buttonStyle, GUILayout.Height(50f)))
                StartRun();
            GUI.enabled = true;

            GUILayout.Space(16f);
            GUILayout.Label("0.3 검증 포인트", headingStyle);
            GUILayout.Label("• 추모 전/후 묘지 카드 변화\n• DAY 3 오아시스 조사 개방과 조사 전/후 카드 변화\n• 1h XP 행동과 3~6h 핵심 행동의 시간 경쟁\n• Lv 부족을 의지로 강행하는 TEMP Soft Requirement\n• Primary NPC에 따른 포트레이트 전환\n• DAY 7 강제 Event 선택", smallStyle);
            GUILayout.EndArea();
        }

        private void DrawSetupStatRow(string label, ref int value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, headingStyle, GUILayout.Width(150f));

            if (GUILayout.Button("-", buttonStyle, GUILayout.Width(58f), GUILayout.Height(38f)) && value > 0)
                value--;

            GUILayout.Label($"Lv {value}", centeredStyle, GUILayout.Width(90f), GUILayout.Height(38f));

            int total = setupPersonal + setupSocial + setupTechnical;
            GUI.enabled = total < 6 && value < MaxStatLevel;
            if (GUILayout.Button("+", buttonStyle, GUILayout.Width(58f), GUILayout.Height(38f)))
                value++;
            GUI.enabled = true;

            GUILayout.EndHorizontal();
            GUILayout.Space(8f);
        }

        private void DrawHud()
        {
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, 92f), darkTexture, ScaleMode.StretchToFill);

            GUILayout.BeginArea(new Rect(16f, 8f, Screen.width - 32f, 78f));
            GUILayout.BeginHorizontal();
            GUILayout.Label($"DAY {day} / {FinalGameDays}", headingStyle, GUILayout.Width(145f));
            GUILayout.Label($"{currentHour:00}:00", headingStyle, GUILayout.Width(90f));
            GUILayout.Label($"의지 {currentWillpower}/{maxWillpower}", headingStyle, GUILayout.Width(125f));
            GUILayout.Label($"시간대 {CurrentTimeSlotLabel()}", smallStyle, GUILayout.Width(130f));
            GUILayout.FlexibleSpace();

            GUI.enabled = currentWillpower < maxWillpower && CanSpendTime(RestHours);
            if (GUILayout.Button("휴식 3h / 의지 +1", buttonStyle, GUILayout.Width(170f), GUILayout.Height(36f)))
                TakeRest();
            GUI.enabled = true;

            if (GUILayout.Button("하루 종료", buttonStyle, GUILayout.Width(110f), GUILayout.Height(36f)))
                EndDay();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(StatHudLabel("개인", personalLevel, personalXp), bodyStyle, GUILayout.Width(200f));
            GUILayout.Label(StatHudLabel("대인", socialLevel, socialXp), bodyStyle, GUILayout.Width(200f));
            GUILayout.Label(StatHudLabel("기술", technicalLevel, technicalXp), bodyStyle, GUILayout.Width(200f));
            GUILayout.FlexibleSpace();
            GUILayout.Label("6XP → 즉시 Lv +1", smallStyle, GUILayout.Width(160f));
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void DrawHubScreen()
        {
            DrawWorldLocationLabelsAndMarkers();

            Rect panel = new Rect(Screen.width - 375f, 106f, 355f, Mathf.Min(570f, Screen.height - 198f));
            GUI.Box(panel, string.Empty);
            GUILayout.BeginArea(new Rect(panel.x + 18f, panel.y + 16f, panel.width - 36f, panel.height - 32f));

            GUILayout.Label("행성 허브", titleStyle);
            GUILayout.Label("현재 Game State와 시간대에 따라 각 장소의 Encounter Pool이 달라집니다.", smallStyle);
            GUILayout.Space(10f);

            GUILayout.Label("현재 상태", headingStyle);
            GUILayout.Label($"추모: {(GetFlag(FlagMemorialDone) ? "완료" : "미완료")}", smallStyle);
            GUILayout.Label($"조사 단계: {(GetFlag(FlagInvestigationOpen) ? "개방" : "미개방")}", smallStyle);
            GUILayout.Label($"오아시스 위험: {(GetFlag(FlagWaterRiskKnown) ? "확인" : "미확인")}", smallStyle);
            GUILayout.Label($"샘 보고: {(GetFlag(FlagReportDone) ? "완료" : "미완료")}", smallStyle);
            GUILayout.Space(10f);

            GUILayout.Label("관계", headingStyle);
            GUILayout.Label($"벤자민 {GetAffinity("benjamin")}/5   페이 {GetAffinity("faye")}/5", smallStyle);
            GUILayout.Label($"샘 {GetAffinity("sam")}/5   지나 {GetAffinity("jina")}/5", smallStyle);
            GUILayout.Space(10f);

            GUILayout.Label("메인 방향", headingStyle);
            if (GetFlag(FlagDisclosureAll))
                GUILayout.Label("모두에게 공개", bodyStyle);
            else if (GetFlag(FlagDisclosureLimited))
                GUILayout.Label("일부에게만 공개", bodyStyle);
            else if (GetFlag(FlagReportDone))
                GUILayout.Label("DAY 7 결정을 앞두고 있음", bodyStyle);
            else if (GetFlag(FlagWaterRiskKnown))
                GUILayout.Label("샘에게 조사 결과를 보고할 수 있음", bodyStyle);
            else if (GetFlag(FlagInvestigationOpen))
                GUILayout.Label("장기 거주 가능성을 조사 중", bodyStyle);
            else
                GUILayout.Label("상황 파악과 재적응", bodyStyle);

            GUILayout.Space(14f);
            GUILayout.Label("비주얼 태그", headingStyle);
            GUILayout.Label("MAIN / CHARACTER / ACTIVITY / WORLD는 데이터 구조가 아니라 카드의 중요도와 성격을 보여주는 태그입니다.", smallStyle);
            GUILayout.EndArea();
        }

        private void DrawWorldLocationLabelsAndMarkers()
        {
            if (Camera.main == null)
                return;

            if (locationNodes == null || locationNodes.Length == 0)
                RefreshLocationNodes();

            foreach (PrototypeLocationNode node in locationNodes)
            {
                if (node == null)
                    continue;

                Vector3 world = node.transform.position + Vector3.up * Mathf.Max(1.8f, node.transform.localScale.y * 0.75f + 0.8f);
                Vector3 screen = Camera.main.WorldToScreenPoint(world);
                if (screen.z <= 0f)
                    continue;

                float x = screen.x;
                float y = Screen.height - screen.y;
                GUI.Label(new Rect(x - 100f, y - 24f, 200f, 28f), LocationName(node.LocationId), locationTitleStyle);

                float badgeX = x - 60f;
                if (LocationHasAvailableEncounter(node.LocationId))
                {
                    GUI.Label(new Rect(badgeX, y + 2f, 44f, 28f), "…", badgeStyle);
                    badgeX += 48f;
                }

                if (LocationHasNpcMarker(node.LocationId))
                {
                    GUI.Label(new Rect(badgeX, y + 2f, 58f, 28f), "인물", badgeStyle);
                    badgeX += 62f;
                }

                if (LocationHasMainMarker(node.LocationId))
                    GUI.Label(new Rect(badgeX, y + 2f, 42f, 28f), "◆", mainBadgeStyle);
            }
        }

        private void DrawLocationScreen()
        {
            Rect screenRect = new Rect(0f, 92f, Screen.width, Screen.height - 92f);
            GUI.DrawTexture(screenRect, LocationTexture(currentLocationId), ScaleMode.StretchToFill);

            Color previous = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, 0.74f);
            GUI.DrawTexture(screenRect, darkTexture, ScaleMode.StretchToFill);
            GUI.color = previous;

            Rect content = new Rect(48f, 112f, Screen.width - 96f, Screen.height - 206f);
            GUI.Box(content, string.Empty);

            GUI.Label(new Rect(content.x + 24f, content.y + 16f, 360f, 44f), LocationName(currentLocationId), titleStyle);
            GUI.Label(new Rect(content.x + 24f, content.y + 55f, 600f, 30f), $"{CurrentTimeSlotLabel()} · 현재 가능한 Encounter", smallStyle);

            if (GUI.Button(new Rect(content.x + content.width - 190f, content.y + 18f, 165f, 40f), "행성으로 돌아가기", buttonStyle))
            {
                mode = ScreenMode.Hub;
                currentLocationId = null;
                statusMessage = "행성 허브로 돌아왔습니다.";
                return;
            }

            List<PrototypeEncounter> available = GetAvailableEncounters(currentLocationId, false);
            Rect grid = new Rect(content.x + 22f, content.y + 96f, content.width - 44f, content.height - 118f);

            if (available.Count == 0)
            {
                GUI.Label(new Rect(grid.x, grid.y, grid.width, 50f), "현재 이 장소에서 가능한 Encounter가 없습니다.\n시간대나 세계 상태가 바뀌면 카드 풀이 달라질 수 있습니다.", bodyStyle);
                return;
            }

            int columns = 3;
            float gap = 14f;
            float cardWidth = (grid.width - gap * (columns - 1)) / columns;
            float cardHeight = 182f;

            for (int i = 0; i < available.Count; i++)
            {
                int row = i / columns;
                int col = i % columns;
                Rect card = new Rect(grid.x + col * (cardWidth + gap), grid.y + row * (cardHeight + gap), cardWidth, cardHeight);
                DrawEncounterCard(card, available[i]);
            }
        }

        private void DrawEncounterCard(Rect rect, PrototypeEncounter encounter)
        {
            bool anyChoiceExecutable = HasExecutableChoice(encounter);
            GUIStyle style = anyChoiceExecutable ? cardStyle : disabledCardStyle;
            GUI.Box(rect, string.Empty, style);

            GUI.Label(new Rect(rect.x + 12f, rect.y + 10f, 112f, 26f), NarrativeLabel(encounter.NarrativeType), BadgeStyle(encounter.NarrativeType));
            if (!string.IsNullOrEmpty(encounter.PrimaryNpcId))
                GUI.Label(new Rect(rect.x + rect.width - 110f, rect.y + 12f, 98f, 24f), NpcName(encounter.PrimaryNpcId), smallStyle);

            GUI.Label(new Rect(rect.x + 12f, rect.y + 42f, rect.width - 24f, 50f), encounter.Title, headingStyle);
            GUI.Label(new Rect(rect.x + 12f, rect.y + 92f, rect.width - 24f, 44f), EncounterCostSummary(encounter), smallStyle);

            GUI.enabled = anyChoiceExecutable;
            if (GUI.Button(new Rect(rect.x + 12f, rect.y + rect.height - 42f, rect.width - 24f, 32f), anyChoiceExecutable ? "열기" : "현재 실행 불가", buttonStyle))
                OpenEncounter(encounter, ScreenMode.Location);
            GUI.enabled = true;
        }

        private void DrawEncounterScreen()
        {
            if (currentEncounter == null)
            {
                mode = encounterReturnMode;
                return;
            }

            Rect full = new Rect(0f, 92f, Screen.width, Screen.height - 92f);
            GUI.DrawTexture(full, darkTexture, ScaleMode.StretchToFill);

            float margin = 42f;
            float visualWidth = Mathf.Min(440f, Screen.width * 0.36f);
            Rect visualPanel = new Rect(margin, 118f, visualWidth, Screen.height - 176f);
            Rect textPanel = new Rect(visualPanel.xMax + 22f, 118f, Screen.width - visualPanel.xMax - margin - 22f, Screen.height - 176f);
            GUI.Box(visualPanel, string.Empty);
            GUI.Box(textPanel, string.Empty);

            DrawEncounterVisual(visualPanel, currentEncounter.PrimaryNpcId, currentEncounter.LocationId, currentEncounter.NarrativeType);

            GUILayout.BeginArea(new Rect(textPanel.x + 24f, textPanel.y + 20f, textPanel.width - 48f, textPanel.height - 40f));
            GUILayout.BeginHorizontal();
            GUILayout.Label(NarrativeLabel(currentEncounter.NarrativeType), BadgeStyle(currentEncounter.NarrativeType), GUILayout.Width(116f), GUILayout.Height(28f));
            if (!string.IsNullOrEmpty(currentEncounter.PrimaryNpcId))
                GUILayout.Label(NpcName(currentEncounter.PrimaryNpcId), smallStyle);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("뒤로", buttonStyle, GUILayout.Width(90f), GUILayout.Height(32f)))
            {
                mode = encounterReturnMode;
                currentEncounter = null;
                GUILayout.EndHorizontal();
                GUILayout.EndArea();
                return;
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(8f);
            GUILayout.Label(currentEncounter.Title, titleStyle);
            GUILayout.Space(6f);
            GUILayout.Label(currentEncounter.Body, bodyStyle);
            GUILayout.Space(14f);
            GUILayout.Label("선택", headingStyle);

            encounterScroll = GUILayout.BeginScrollView(encounterScroll);
            foreach (PrototypeChoice choice in currentEncounter.Choices)
                DrawChoice(currentEncounter, choice);
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawStoryEventScreen()
        {
            if (currentStoryEvent == null)
            {
                mode = ScreenMode.Hub;
                return;
            }

            Rect full = new Rect(0f, 92f, Screen.width, Screen.height - 92f);
            GUI.DrawTexture(full, darkTexture, ScaleMode.StretchToFill);

            float margin = 70f;
            float visualWidth = Mathf.Min(420f, Screen.width * 0.34f);
            Rect visualPanel = new Rect(margin, 126f, visualWidth, Screen.height - 198f);
            Rect textPanel = new Rect(visualPanel.xMax + 26f, 126f, Screen.width - visualPanel.xMax - margin - 26f, Screen.height - 198f);
            GUI.Box(visualPanel, string.Empty);
            GUI.Box(textPanel, string.Empty);

            DrawEventVisual(visualPanel, currentStoryEvent.PrimaryNpcId);

            GUILayout.BeginArea(new Rect(textPanel.x + 26f, textPanel.y + 22f, textPanel.width - 52f, textPanel.height - 44f));
            GUILayout.Label("STORY EVENT", mainBadgeStyle, GUILayout.Width(140f), GUILayout.Height(30f));
            GUILayout.Space(10f);
            GUILayout.Label(currentStoryEvent.Title, titleStyle);
            GUILayout.Space(8f);
            GUILayout.Label(GetDynamicEventBody(currentStoryEvent), bodyStyle);
            GUILayout.Space(18f);

            foreach (PrototypeChoice choice in currentStoryEvent.Choices)
            {
                string label = choice.Label;
                if (GUILayout.Button(label, buttonStyle, GUILayout.Height(52f)))
                {
                    ExecuteStoryEventChoice(currentStoryEvent, choice);
                    GUILayout.EndArea();
                    return;
                }
                GUILayout.Space(8f);
            }

            GUILayout.EndArea();
        }

        private void DrawChoice(PrototypeEncounter encounter, PrototypeChoice choice)
        {
            int willCost = EffectiveWillCost(choice);
            bool hardOk = ConditionsMet(choice.HardConditions);
            bool timeOk = CanSpendTime(choice.TimeCost);
            bool willOk = currentWillpower >= willCost;
            bool enabled = hardOk && timeOk && willOk;

            GUILayout.BeginVertical(cardStyle);
            GUILayout.Label(choice.Label, headingStyle);
            GUILayout.Label(ChoiceCostLabel(choice, willCost), smallStyle);

            if (choice.SoftStat != StatType.None && choice.SoftRequirement > 0)
            {
                int current = GetStatLevel(choice.SoftStat);
                int deficit = Mathf.Max(0, choice.SoftRequirement - current);
                if (deficit > 0)
                    GUILayout.Label($"TEMP Soft 강행: {StatName(choice.SoftStat)} Lv {current}/{choice.SoftRequirement}, 부족 {deficit} → 의지 +{deficit}", smallStyle);
            }

            if (!hardOk)
                GUILayout.Label("Hard Requirement 미충족", smallStyle);
            else if (!timeOk)
                GUILayout.Label($"오늘 남은 시간으로 완료할 수 없음 ({currentHour:00}:00 → {currentHour + choice.TimeCost:00}:00)", smallStyle);
            else if (!willOk)
                GUILayout.Label($"의지 부족: 필요 {willCost}, 현재 {currentWillpower}", smallStyle);

            GUI.enabled = enabled;
            if (GUILayout.Button(enabled ? "선택" : "선택 불가", buttonStyle, GUILayout.Height(36f)))
                ExecuteEncounterChoice(encounter, choice);
            GUI.enabled = true;
            GUILayout.EndVertical();
            GUILayout.Space(8f);
        }

        private void DrawEncounterVisual(Rect panel, string npcId, string locationId, NarrativeType narrativeType)
        {
            Rect inner = new Rect(panel.x + 18f, panel.y + 18f, panel.width - 36f, panel.height - 36f);
            Sprite portrait = GetPortrait(npcId);

            if (portrait != null)
            {
                DrawSprite(inner, portrait);
                GUI.Label(new Rect(inner.x + 10f, inner.yMax - 42f, inner.width - 20f, 32f), NpcName(npcId), locationTitleStyle);
                return;
            }

            GUI.DrawTexture(inner, LocationTexture(locationId), ScaleMode.StretchToFill);
            GUI.Label(new Rect(inner.x + 12f, inner.y + 12f, 140f, 30f), NarrativeLabel(narrativeType), BadgeStyle(narrativeType));
            GUI.Label(new Rect(inner.x + 14f, inner.yMax - 48f, inner.width - 28f, 36f), LocationName(locationId), locationTitleStyle);
        }

        private void DrawEventVisual(Rect panel, string npcId)
        {
            Rect inner = new Rect(panel.x + 18f, panel.y + 18f, panel.width - 36f, panel.height - 36f);
            Sprite portrait = GetPortrait(npcId);
            if (portrait != null)
            {
                DrawSprite(inner, portrait);
                GUI.Label(new Rect(inner.x + 10f, inner.yMax - 42f, inner.width - 20f, 32f), NpcName(npcId), locationTitleStyle);
            }
            else
            {
                GUI.DrawTexture(inner, darkTexture, ScaleMode.StretchToFill);
                GUI.Label(new Rect(inner.x + 12f, inner.y + inner.height * 0.45f, inner.width - 24f, 60f), "STORY EVENT", titleStyle);
            }
        }

        private void DrawSprite(Rect rect, Sprite sprite)
        {
            if (sprite == null || sprite.texture == null)
                return;

            Texture2D texture = sprite.texture;
            Rect sr = sprite.rect;
            Rect uv = new Rect(sr.x / texture.width, sr.y / texture.height, sr.width / texture.width, sr.height / texture.height);
            GUI.DrawTextureWithTexCoords(rect, texture, uv, true);
        }

        private void DrawStatusMessage()
        {
            Rect rect = new Rect(18f, Screen.height - 72f, Screen.width - 36f, 54f);
            GUI.Box(rect, string.Empty);
            GUI.Label(new Rect(rect.x + 16f, rect.y + 12f, rect.width - 32f, 34f), statusMessage, smallStyle);
        }

        private void DrawCompleteScreen()
        {
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), darkTexture, ScaleMode.StretchToFill);
            float width = Mathf.Min(760f, Screen.width - 60f);
            Rect panel = new Rect((Screen.width - width) * 0.5f, 60f, width, 610f);
            GUI.Box(panel, string.Empty);

            GUILayout.BeginArea(new Rect(panel.x + 36f, panel.y + 30f, panel.width - 72f, panel.height - 60f));
            GUILayout.Label("PROTOTYPE 0.3 — WEEK 1 COMPLETE", titleStyle);
            GUILayout.Space(10f);
            GUILayout.Label("최종 성장", headingStyle);
            GUILayout.Label(StatHudLabel("개인", personalLevel, personalXp), bodyStyle);
            GUILayout.Label(StatHudLabel("대인", socialLevel, socialXp), bodyStyle);
            GUILayout.Label(StatHudLabel("기술", technicalLevel, technicalXp), bodyStyle);
            GUILayout.Space(12f);
            GUILayout.Label($"벤자민 호감도 {GetAffinity("benjamin")}/5", bodyStyle);
            GUILayout.Space(12f);
            GUILayout.Label("Week 1 선택", headingStyle);
            GUILayout.Label(GetFlag(FlagDisclosureAll) ? "모두에게 공개" : GetFlag(FlagDisclosureLimited) ? "일부에게만 공개" : "미결정", bodyStyle);
            GUILayout.Space(16f);
            GUILayout.Label("플레이 피드백 체크", headingStyle);
            GUILayout.Label("1. 같은 장소의 카드 풀이 상태 변화에 따라 달라졌는가?\n2. 1h 성장 행동과 3~6h 행동 사이에서 실제 고민이 생겼는가?\n3. Lv + XP가 행동의 성장 의미를 읽기 쉽게 했는가?\n4. 캐릭터 포트레이트가 Encounter 구분에 도움이 되었는가?\n5. Main 카드가 일반 카드와 한 테이블 안에 있어도 충분히 구분되었는가?", smallStyle);
            GUILayout.Space(18f);

            if (GUILayout.Button("다시 시작", buttonStyle, GUILayout.Height(48f)))
                ResetRun();
            GUILayout.EndArea();
        }

        private void OpenLocation(string locationId)
        {
            currentLocationId = locationId;
            mode = ScreenMode.Location;
            statusMessage = $"{LocationName(locationId)}에 들어왔습니다. 현재 상태에 맞는 Encounter만 표시됩니다.";
        }

        private void OpenEncounter(PrototypeEncounter encounter, ScreenMode returnMode)
        {
            currentEncounter = encounter;
            encounterReturnMode = returnMode;
            encounterScroll = Vector2.zero;
            mode = ScreenMode.Encounter;
            statusMessage = $"{NarrativeLabel(encounter.NarrativeType)} · {encounter.Title}";
        }

        private void ExecuteEncounterChoice(PrototypeEncounter encounter, PrototypeChoice choice)
        {
            int willCost = EffectiveWillCost(choice);
            if (!CanSpendTime(choice.TimeCost) || currentWillpower < willCost || !ConditionsMet(choice.HardConditions))
                return;

            currentHour += choice.TimeCost;
            currentWillpower -= willCost;

            List<string> changes = new List<string>();
            foreach (PrototypeAction action in choice.Actions)
                ApplyAction(action, changes);

            MarkEncounterCompleted(encounter);

            string changeText = changes.Count > 0 ? " · " + string.Join(" / ", changes) : string.Empty;
            statusMessage = choice.ResultText + changeText;
            mode = encounterReturnMode;
            currentEncounter = null;
        }

        private void ExecuteStoryEventChoice(PrototypeStoryEvent storyEvent, PrototypeChoice choice)
        {
            List<string> changes = new List<string>();
            foreach (PrototypeAction action in choice.Actions)
                ApplyAction(action, changes);

            storyEvent.Completed = true;
            flags["EVT_" + storyEvent.Id + "_DONE"] = true;

            string changeText = changes.Count > 0 ? " · " + string.Join(" / ", changes) : string.Empty;
            statusMessage = choice.ResultText + changeText;
            currentStoryEvent = null;

            if (storyEvent.Id == "DAY7_DISCLOSURE")
                mode = ScreenMode.Complete;
            else
                mode = ScreenMode.Hub;
        }

        private void ApplyAction(PrototypeAction action, List<string> changes)
        {
            switch (action.Type)
            {
                case ActionType.AddXp:
                    AddXp(action.Stat, action.IntValue, changes);
                    break;

                case ActionType.AddAffinity:
                {
                    int before = GetAffinity(action.Key);
                    int after = Mathf.Clamp(before + action.IntValue, 0, 5);
                    affinities[action.Key] = after;
                    if (after != before)
                        changes.Add($"{NpcName(action.Key)} 호감도 {before}→{after}");
                    break;
                }

                case ActionType.SetFlag:
                    flags[action.Key] = action.BoolValue;
                    changes.Add(FlagFeedback(action.Key, action.BoolValue));
                    break;
            }
        }

        private void AddXp(StatType stat, int amount, List<string> changes)
        {
            if (amount <= 0 || stat == StatType.None)
                return;

            int oldLevel = GetStatLevel(stat);
            int oldXp = GetStatXp(stat);
            int newLevel = oldLevel;
            int newXp = oldXp + amount;

            while (newXp >= XpPerLevel && newLevel < MaxStatLevel)
            {
                newXp -= XpPerLevel;
                newLevel++;
            }

            if (newLevel >= MaxStatLevel)
                newXp = Mathf.Min(newXp, XpPerLevel - 1);

            SetStat(stat, newLevel, newXp);

            if (newLevel > oldLevel)
                changes.Add($"{StatName(stat)} XP +{amount}, Lv {oldLevel}→{newLevel} ({newXp}/{XpPerLevel})");
            else
                changes.Add($"{StatName(stat)} XP +{amount} ({oldXp}→{newXp}/{XpPerLevel})");
        }

        private int EffectiveWillCost(PrototypeChoice choice)
        {
            int deficit = 0;
            if (choice.SoftStat != StatType.None && choice.SoftRequirement > 0)
                deficit = Mathf.Max(0, choice.SoftRequirement - GetStatLevel(choice.SoftStat));

            // TEMP Prototype 0.3 formula: 1 missing Lv = 1 Will.
            return choice.BaseWillCost + deficit;
        }

        private bool HasExecutableChoice(PrototypeEncounter encounter)
        {
            foreach (PrototypeChoice choice in encounter.Choices)
            {
                if (CanSpendTime(choice.TimeCost) && ConditionsMet(choice.HardConditions) && currentWillpower >= EffectiveWillCost(choice))
                    return true;
            }
            return false;
        }

        private bool CanSpendTime(int hours)
        {
            return hours >= 0 && currentHour + hours <= dayEndHour;
        }

        private void TakeRest()
        {
            if (currentWillpower >= maxWillpower || !CanSpendTime(RestHours))
                return;

            currentHour += RestHours;
            currentWillpower = Mathf.Min(maxWillpower, currentWillpower + RestRecovery);
            statusMessage = $"3시간 쉬었습니다. 의지 +{RestRecovery}. 현재 {currentWillpower}/{maxWillpower}.";
        }

        private void EndDay()
        {
            if (day >= PrototypeDays)
            {
                TriggerStoryEvent("DAY7_DISCLOSURE");
                return;
            }

            day++;
            currentHour = dayStartHour;
            int before = currentWillpower;
            currentWillpower = Mathf.Min(maxWillpower, currentWillpower + SleepRecovery);
            statusMessage = $"DAY {day} 시작. 수면 회복으로 의지 {before}→{currentWillpower}.";

            if (day == 3)
                TriggerStoryEvent("DAY3_INVESTIGATION_OPEN");
            else
                mode = ScreenMode.Hub;
        }

        private void StartRun()
        {
            ResetRuntimeStateOnly();
            personalLevel = setupPersonal;
            socialLevel = setupSocial;
            technicalLevel = setupTechnical;
            day = 1;
            currentHour = dayStartHour;
            currentWillpower = maxWillpower;
            mode = ScreenMode.Hub;
            TriggerStoryEvent("DAY1_WAKE");
        }

        private void ResetRun()
        {
            ResetRuntimeStateOnly();
            setupPersonal = 2;
            setupSocial = 2;
            setupTechnical = 2;
            mode = ScreenMode.Setup;
            statusMessage = "시작 능력치를 배분하세요.";
        }

        private void ResetRuntimeStateOnly()
        {
            dayStartHour = BaseDayStartHour;
            dayEndHour = BaseDayEndHour;
            maxWillpower = BaseMaxWillpower;
            day = 1;
            currentHour = dayStartHour;
            currentWillpower = maxWillpower;

            personalLevel = 0;
            socialLevel = 0;
            technicalLevel = 0;
            personalXp = 0;
            socialXp = 0;
            technicalXp = 0;

            flags.Clear();
            affinities.Clear();
            variantGroupLastCompletedDay.Clear();
            affinities["sam"] = 0;
            affinities["jina"] = 0;
            affinities["faye"] = 0;
            affinities["benjamin"] = 0;
            affinities["borichi"] = 0;
            affinities["diya"] = 0;

            foreach (PrototypeEncounter encounter in encounters)
            {
                encounter.Completed = false;
                encounter.LastCompletedDay = -1;
            }

            foreach (PrototypeStoryEvent storyEvent in storyEvents)
                storyEvent.Completed = false;

            currentLocationId = null;
            currentEncounter = null;
            currentStoryEvent = null;
            hoveredNode = null;
            encounterScroll = Vector2.zero;
        }

        private void TriggerStoryEvent(string eventId)
        {
            PrototypeStoryEvent storyEvent = storyEvents.Find(e => e.Id == eventId);
            if (storyEvent == null || storyEvent.Completed)
            {
                mode = ScreenMode.Hub;
                return;
            }

            currentStoryEvent = storyEvent;
            mode = ScreenMode.StoryEvent;
            statusMessage = "강제 Story Event가 호출되었습니다.";
        }

        private string GetDynamicEventBody(PrototypeStoryEvent storyEvent)
        {
            if (storyEvent.Id != "DAY7_DISCLOSURE")
                return storyEvent.Body;

            if (GetFlag(FlagReportDone))
                return "일주일 동안 모은 정보와 사람들의 반응이 머릿속을 맴돈다. 샘은 이제 더 미룰 수 없다고 말한다. 장기 거주가 어렵다는 사실을 누구까지 알릴 것인가?";

            if (GetFlag(FlagWaterRiskKnown))
                return "오아시스의 위험은 알아냈지만 충분히 정리해 샘에게 보고하지 못했다. 그래도 DAY 7은 끝나간다. 불완전한 정보 속에서도 공개 범위를 결정해야 한다.";

            return "충분한 조사를 끝내지 못한 채 DAY 7이 왔다. 공동체는 지금의 생활을 계속하고 있지만, 제이는 불확실성을 안고 있다. 무엇을 어디까지 말할 것인가?";
        }

        private List<PrototypeEncounter> GetAvailableEncounters(string locationId, bool global)
        {
            List<PrototypeEncounter> result = new List<PrototypeEncounter>();
            foreach (PrototypeEncounter encounter in encounters)
            {
                if (encounter.IsGlobal != global)
                    continue;
                if (!global && encounter.LocationId != locationId)
                    continue;
                if (IsEncounterVisible(encounter))
                    result.Add(encounter);
            }
            return result;
        }

        private bool IsEncounterVisible(PrototypeEncounter encounter)
        {
            if (day < encounter.OpenDay || day > encounter.CloseDay)
                return false;
            if ((encounter.AllowedSlots & CurrentTimeSlotMask()) == 0)
                return false;
            if (!IsRepeatAvailable(encounter))
                return false;
            if (!ConditionsMet(encounter.ShowConditions))
                return false;

            foreach (PrototypeCondition hide in encounter.HideConditions)
            {
                if (ConditionMet(hide))
                    return false;
            }

            return true;
        }

        private bool IsRepeatAvailable(PrototypeEncounter encounter)
        {
            switch (encounter.RepeatRule)
            {
                case RepeatRule.Once:
                    return !encounter.Completed;
                case RepeatRule.Daily:
                    if (encounter.LastCompletedDay == day)
                        return false;
                    if (!string.IsNullOrEmpty(encounter.VariantGroup) &&
                        variantGroupLastCompletedDay.TryGetValue(encounter.VariantGroup, out int groupDay) && groupDay == day)
                        return false;
                    return true;
                default:
                    return true;
            }
        }

        private void MarkEncounterCompleted(PrototypeEncounter encounter)
        {
            encounter.Completed = true;
            encounter.LastCompletedDay = day;
            flags["ENC_" + encounter.Id + "_DONE"] = true;

            if (encounter.RepeatRule == RepeatRule.Daily && !string.IsNullOrEmpty(encounter.VariantGroup))
                variantGroupLastCompletedDay[encounter.VariantGroup] = day;
        }

        private bool ConditionsMet(List<PrototypeCondition> conditions)
        {
            foreach (PrototypeCondition condition in conditions)
            {
                if (!ConditionMet(condition))
                    return false;
            }
            return true;
        }

        private bool ConditionMet(PrototypeCondition condition)
        {
            switch (condition.Type)
            {
                case ConditionType.FlagTrue:
                    return GetFlag(condition.Key);
                case ConditionType.FlagFalse:
                    return !GetFlag(condition.Key);
                case ConditionType.StatAtLeast:
                    return GetStatLevel(condition.Stat) >= condition.IntValue;
                case ConditionType.AffinityAtLeast:
                    return GetAffinity(condition.Key) >= condition.IntValue;
                default:
                    return true;
            }
        }

        private bool GetFlag(string key)
        {
            return !string.IsNullOrEmpty(key) && flags.TryGetValue(key, out bool value) && value;
        }

        private int GetAffinity(string npcId)
        {
            if (string.IsNullOrEmpty(npcId))
                return 0;
            return affinities.TryGetValue(npcId, out int value) ? value : 0;
        }

        private int GetStatLevel(StatType stat)
        {
            switch (stat)
            {
                case StatType.Personal: return personalLevel;
                case StatType.Social: return socialLevel;
                case StatType.Technical: return technicalLevel;
                default: return 0;
            }
        }

        private int GetStatXp(StatType stat)
        {
            switch (stat)
            {
                case StatType.Personal: return personalXp;
                case StatType.Social: return socialXp;
                case StatType.Technical: return technicalXp;
                default: return 0;
            }
        }

        private void SetStat(StatType stat, int level, int xp)
        {
            switch (stat)
            {
                case StatType.Personal:
                    personalLevel = level;
                    personalXp = xp;
                    break;
                case StatType.Social:
                    socialLevel = level;
                    socialXp = xp;
                    break;
                case StatType.Technical:
                    technicalLevel = level;
                    technicalXp = xp;
                    break;
            }
        }

        private TimeSlotMask CurrentTimeSlotMask()
        {
            if (currentHour >= 6 && currentHour < 12)
                return TimeSlotMask.Morning;
            if (currentHour >= 12 && currentHour < 17)
                return TimeSlotMask.Afternoon;
            if (currentHour >= 17 && currentHour <= 24)
                return TimeSlotMask.Evening;
            return TimeSlotMask.None;
        }

        private string CurrentTimeSlotLabel()
        {
            switch (CurrentTimeSlotMask())
            {
                case TimeSlotMask.Morning: return "Morning";
                case TimeSlotMask.Afternoon: return "Afternoon";
                case TimeSlotMask.Evening: return "Evening";
                default: return "Outside";
            }
        }

        private void RefreshLocationNodes()
        {
            locationNodes = FindObjectsByType<PrototypeLocationNode>(FindObjectsSortMode.None);
            nodeBaseScales.Clear();
            foreach (PrototypeLocationNode node in locationNodes)
            {
                if (node != null)
                    nodeBaseScales[node] = node.transform.localScale;
            }
        }

        private void SetHoveredNode(PrototypeLocationNode node)
        {
            if (hoveredNode == node)
                return;

            if (hoveredNode != null && nodeBaseScales.TryGetValue(hoveredNode, out Vector3 oldScale))
                hoveredNode.transform.localScale = oldScale;

            hoveredNode = node;

            if (hoveredNode != null && nodeBaseScales.TryGetValue(hoveredNode, out Vector3 baseScale))
                hoveredNode.transform.localScale = baseScale * 1.10f;
        }

        private bool LocationHasAvailableEncounter(string locationId)
        {
            foreach (PrototypeEncounter encounter in encounters)
            {
                if (!encounter.IsGlobal && encounter.LocationId == locationId && IsEncounterVisible(encounter))
                    return true;
            }
            return false;
        }

        private bool LocationHasNpcMarker(string locationId)
        {
            foreach (PrototypeEncounter encounter in encounters)
            {
                if (!encounter.IsGlobal && encounter.LocationId == locationId && !string.IsNullOrEmpty(encounter.PrimaryNpcId) && IsEncounterVisible(encounter))
                    return true;
            }
            return false;
        }

        private bool LocationHasMainMarker(string locationId)
        {
            foreach (PrototypeEncounter encounter in encounters)
            {
                if (!encounter.IsGlobal && encounter.LocationId == locationId && encounter.NarrativeType == NarrativeType.Main && IsEncounterVisible(encounter))
                    return true;
            }
            return false;
        }

        private string EncounterCostSummary(PrototypeEncounter encounter)
        {
            if (encounter.Choices.Count == 0)
                return "선택 없음";

            int minTime = int.MaxValue;
            int maxTime = int.MinValue;
            int minWill = int.MaxValue;
            int maxWill = int.MinValue;

            foreach (PrototypeChoice choice in encounter.Choices)
            {
                minTime = Mathf.Min(minTime, choice.TimeCost);
                maxTime = Mathf.Max(maxTime, choice.TimeCost);
                int will = EffectiveWillCost(choice);
                minWill = Mathf.Min(minWill, will);
                maxWill = Mathf.Max(maxWill, will);
            }

            string time = minTime == maxTime ? $"{minTime}h" : $"{minTime}~{maxTime}h";
            string willText = maxWill <= 0 ? "의지 0" : minWill == maxWill ? $"의지 {minWill}" : $"의지 {minWill}~{maxWill}";
            return $"시간 {time} · {willText}\n{encounter.Choices.Count}개 접근 방식";
        }

        private string ChoiceCostLabel(PrototypeChoice choice, int effectiveWill)
        {
            string soft = choice.SoftStat == StatType.None || choice.SoftRequirement <= 0
                ? string.Empty
                : $" · {StatName(choice.SoftStat)} Lv {choice.SoftRequirement} 효율 조건";
            return $"시간 {choice.TimeCost}h · 의지 {effectiveWill}{soft}";
        }

        private string StatHudLabel(string label, int level, int xp)
        {
            return $"{label} Lv.{level}   XP {xp}/{XpPerLevel}";
        }

        private string NarrativeLabel(NarrativeType type)
        {
            switch (type)
            {
                case NarrativeType.Main: return "◆ MAIN";
                case NarrativeType.Character: return "● CHARACTER";
                case NarrativeType.Activity: return "▲ ACTIVITY";
                case NarrativeType.World: return "■ WORLD";
                default: return type.ToString();
            }
        }

        private GUIStyle BadgeStyle(NarrativeType type)
        {
            switch (type)
            {
                case NarrativeType.Main: return mainBadgeStyle;
                case NarrativeType.Character: return characterBadgeStyle;
                case NarrativeType.Activity: return activityBadgeStyle;
                default: return worldBadgeStyle;
            }
        }

        private string StatName(StatType stat)
        {
            switch (stat)
            {
                case StatType.Personal: return "개인";
                case StatType.Social: return "대인";
                case StatType.Technical: return "기술";
                default: return "없음";
            }
        }

        private string NpcName(string npcId)
        {
            switch (npcId)
            {
                case "sam": return "샘";
                case "jina": return "지나";
                case "faye": return "페이";
                case "benjamin": return "벤자민";
                case "borichi": return "보리치";
                case "diya": return "디야";
                default: return string.IsNullOrEmpty(npcId) ? string.Empty : npcId;
            }
        }

        private Sprite GetPortrait(string npcId)
        {
            switch (npcId)
            {
                case "sam": return samPortrait;
                case "jina": return jinaPortrait;
                case "faye": return fayePortrait;
                case "benjamin": return benjaminPortrait;
                case "borichi": return borichiPortrait;
                case "diya": return diyaPortrait;
                default: return null;
            }
        }

        private string LocationName(string locationId)
        {
            switch (locationId)
            {
                case "ship": return "수송선";
                case "graveyard": return "묘지";
                case "settlement": return "거주지";
                case "oasis": return "오아시스";
                default: return string.IsNullOrEmpty(locationId) ? "행성" : locationId;
            }
        }

        private Texture2D LocationTexture(string locationId)
        {
            switch (locationId)
            {
                case "ship": return shipTexture;
                case "graveyard": return graveyardTexture;
                case "settlement": return settlementTexture;
                case "oasis": return oasisTexture;
                default: return panelTexture;
            }
        }

        private string FlagFeedback(string key, bool value)
        {
            if (!value)
                return key + " 해제";

            switch (key)
            {
                case FlagSituationHeard: return "사고 이후 상황 파악";
                case FlagGraveSeen: return "묘지의 존재를 이해함";
                case FlagMemorialDone: return "추모 완료";
                case FlagInvestigationOpen: return "오아시스 조사 단계 개방";
                case FlagWaterRiskKnown: return "오아시스 장기 위험 확인";
                case FlagReportDone: return "샘에게 조사 결과 보고 완료";
                case FlagShipRepairPriority: return "Week 2 수리 우선순위 정보 확보";
                case FlagPublicReactionPrepared: return "공개 후 주민 반응 대비";
                case FlagSettlementPerspective: return "정착 관점 정보 확보";
                case FlagDisclosureAll: return "Week 2: 모두에게 공개";
                case FlagDisclosureLimited: return "Week 2: 일부에게만 공개";
                default: return key + " 획득";
            }
        }

        private void BuildPrototypeContent()
        {
            encounters.Clear();
            storyEvents.Clear();

            BuildStoryEvents();
            BuildDay12Content();
            BuildDynamicWorldActivities();
            BuildRelationshipContent();
            BuildInvestigationContent();
            BuildPostReportContent();
        }

        private void BuildStoryEvents()
        {
            PrototypeStoryEvent wake = new PrototypeStoryEvent
            {
                Id = "DAY1_WAKE",
                Day = 1,
                PrimaryNpcId = "sam",
                Title = "다시 눈을 뜨다",
                Body = "냉동 장치의 문이 열리고, 익숙한 얼굴인 샘이 제이를 맞는다. 사고 이후 시간이 흘렀고 사람들은 이미 이 행성에서 임시 생활을 시작했다. 첫 이틀은 답을 내리는 시간이 아니라, 제이가 자신이 빠져 있던 시간을 따라잡는 시간이다."
            };
            wake.Choices.Add(Choice("DAY1_WAKE_CONTINUE", "샘을 따라 밖으로 나간다", 0, 0, StatType.None, 0,
                "DAY 1이 시작됩니다.",
                FlagAction(FlagAwake)));
            storyEvents.Add(wake);

            PrototypeStoryEvent investigation = new PrototypeStoryEvent
            {
                Id = "DAY3_INVESTIGATION_OPEN",
                Day = 3,
                PrimaryNpcId = "sam",
                Title = "당연했던 물에 질문이 생기다",
                Body = "DAY 3. 샘은 지금까지 공동체를 살려 온 오아시스가 장기적으로도 안전한지 확인할 필요가 있다고 말한다. 이제 오아시스와 관련 인물들의 Encounter Pool이 조사 맥락으로 바뀐다."
            };
            investigation.Choices.Add(Choice("DAY3_INVESTIGATION_CONTINUE", "조사를 시작한다", 0, 0, StatType.None, 0,
                "오아시스 조사 단계가 열렸습니다.",
                FlagAction(FlagInvestigationOpen)));
            storyEvents.Add(investigation);

            PrototypeStoryEvent disclosure = new PrototypeStoryEvent
            {
                Id = "DAY7_DISCLOSURE",
                Day = 7,
                PrimaryNpcId = "sam",
                Title = "누가 진실을 알아야 하는가",
                Body = "DAY 7 종료 시점의 강제 선택입니다."
            };
            disclosure.Choices.Add(Choice("DISCLOSE_ALL", "모두에게 알린다", 0, 0, StatType.None, 0,
                "사람들은 진실을 공유하게 됩니다. Week 2는 기술 부담이 줄어드는 대신 대인 압박이 커지는 방향으로 이어집니다.",
                FlagAction(FlagDisclosureAll)));
            disclosure.Choices.Add(Choice("DISCLOSE_LIMITED", "일부 핵심 인물에게만 알린다", 0, 0, StatType.None, 0,
                "공동체의 혼란은 줄어듭니다. Week 2는 플레이어와 전문가가 직접 준비를 떠안는 방향으로 이어집니다.",
                FlagAction(FlagDisclosureLimited)));
            storyEvents.Add(disclosure);
        }

        private void BuildDay12Content()
        {
            PrototypeEncounter situation = Encounter(
                "SAM_SITUATION",
                NarrativeType.Main,
                "settlement",
                "sam",
                "내가 잠든 동안",
                "샘에게 사고 이후 사람들이 어떻게 살아남았고, 지금 누가 어떤 역할을 맡고 있는지 차분히 듣는다. 메인 진행이지만 시간 대비 XP 환율은 의도적으로 낮다.",
                1, 2,
                TimeSlotMask.All,
                RepeatRule.Once);
            situation.ShowConditions.Add(FlagTrue(FlagAwake));
            situation.Choices.Add(Choice("SAM_SITUATION_LISTEN", "사고 이후 상황을 끝까지 듣는다", 2, 0, StatType.None, 0,
                "제이가 자신이 빠져 있던 시간을 조금 따라잡았습니다.",
                XpAction(StatType.Personal, 1),
                FlagAction(FlagSituationHeard)));
            encounters.Add(situation);

            PrototypeEncounter graveFirst = Encounter(
                "GRAVE_FIRST_VISIT",
                NarrativeType.World,
                "graveyard",
                null,
                "처음 보는 묘지",
                "제이가 잠들기 전에는 없었던 장소다. 이름과 흔적을 따라가며 사고가 숫자가 아니라 사람의 죽음이었다는 사실을 받아들인다.",
                1, 6,
                TimeSlotMask.All,
                RepeatRule.Once);
            graveFirst.HideConditions.Add(FlagTrue(FlagGraveSeen));
            graveFirst.Choices.Add(Choice("GRAVE_FIRST_LOOK", "묘지를 천천히 둘러본다", 1, 0, StatType.None, 0,
                "묘지와 사망자의 흔적을 알게 되었습니다.",
                XpAction(StatType.Personal, 1),
                FlagAction(FlagGraveSeen)));
            encounters.Add(graveFirst);

            PrototypeEncounter memorial = Encounter(
                "MEMORIAL",
                NarrativeType.Main,
                "graveyard",
                null,
                "남겨진 이름들",
                "누군가의 이름 앞에 잠시 머문다. 이 선택은 순수한 추모 장면이므로 시간과 의지를 받지 않는다. 대신 이후 묘지의 Encounter 문맥이 바뀐다.",
                1, 6,
                TimeSlotMask.All,
                RepeatRule.Once);
            memorial.ShowConditions.Add(FlagTrue(FlagGraveSeen));
            memorial.HideConditions.Add(FlagTrue(FlagMemorialDone));
            memorial.Choices.Add(Choice("MEMORIAL_SILENCE", "조용히 추모한다", 0, 0, StatType.None, 0,
                "추모를 마쳤습니다. 묘지가 더 이상 낯선 장소만은 아닙니다.",
                FlagAction(FlagMemorialDone)));
            encounters.Add(memorial);

            PrototypeEncounter fayeBriefing = Encounter(
                "FAYE_SHIP_BRIEFING",
                NarrativeType.Character,
                "ship",
                "faye",
                "망가진 배, 살아 있는 기술",
                "페이는 현재 수송선이 어디까지 망가졌고 무엇이 아직 살아 있는지 설명한다. 설정을 듣는 시간이지만 제이도 실제 구조를 배우므로 기술 XP를 조금 얻는다.",
                1, 4,
                TimeSlotMask.All,
                RepeatRule.Once);
            fayeBriefing.Choices.Add(Choice("FAYE_SHIP_LISTEN", "수송선 상태를 함께 확인한다", 2, 0, StatType.None, 0,
                "수송선의 현재 상태를 이해했습니다.",
                XpAction(StatType.Technical, 1)));
            encounters.Add(fayeBriefing);

            PrototypeEncounter jinaLife = Encounter(
                "JINA_LIFE_OVERVIEW",
                NarrativeType.Character,
                "settlement",
                "jina",
                "생활은 계속된다",
                "지나에게 배급, 숙소, 물 사용이 어떻게 굴러가는지 듣는다. 사람들의 생활을 이해하는 과정 자체가 대인 경험이 된다.",
                1, 4,
                TimeSlotMask.All,
                RepeatRule.Once);
            jinaLife.Choices.Add(Choice("JINA_LIFE_LISTEN", "생활 관리 현황을 듣는다", 2, 0, StatType.None, 0,
                "거주지의 일상이 어떻게 유지되는지 알게 되었습니다.",
                XpAction(StatType.Social, 1)));
            encounters.Add(jinaLife);

            PrototypeEncounter diyaView = Encounter(
                "DIYA_SETTLEMENT_VIEW_PRE",
                NarrativeType.Character,
                "settlement",
                "diya",
                "임시 거주지라는 도시",
                "디야는 임시 텐트와 동선을 하나의 작은 도시처럼 바라본다. 아직 정착을 주장하는 단계가 아니라, 사람과 공간을 보는 그의 시선을 배우는 장면이다.",
                1, 5,
                TimeSlotMask.Afternoon | TimeSlotMask.Evening,
                RepeatRule.Once);
            diyaView.HideConditions.Add(FlagTrue(FlagReportDone));
            diyaView.Choices.Add(Choice("DIYA_SETTLEMENT_LISTEN", "디야와 거주지를 한 바퀴 돈다", 2, 0, StatType.None, 0,
                "공동체와 공간을 보는 새로운 관점을 얻었습니다.",
                XpAction(StatType.Social, 1)));
            encounters.Add(diyaView);
        }

        private void BuildDynamicWorldActivities()
        {
            PrototypeEncounter graveCleanPre = Encounter(
                "GRAVE_CLEAN_PRE",
                NarrativeType.Activity,
                "graveyard",
                null,
                "낯선 묘지 주변을 정리한다",
                "아직 누구의 이름이 어디에 있는지도 모른다. 바람에 흩어진 돌과 천 조각을 정리하며 이 장소를 천천히 받아들인다.",
                1, 7,
                TimeSlotMask.All,
                RepeatRule.Daily,
                "GRAVE_CLEAN");
            graveCleanPre.HideConditions.Add(FlagTrue(FlagMemorialDone));
            graveCleanPre.Choices.Add(Choice("GRAVE_CLEAN_PRE_WORK", "1시간 정리한다", 1, 0, StatType.None, 0,
                "짧은 육체적/정서적 정리를 마쳤습니다.",
                XpAction(StatType.Personal, 1)));
            encounters.Add(graveCleanPre);

            PrototypeEncounter graveCleanPost = Encounter(
                "GRAVE_CLEAN_POST",
                NarrativeType.Activity,
                "graveyard",
                null,
                "이름을 아는 이들의 묘지를 정돈한다",
                "추모를 마친 뒤에는 같은 일이 다르게 느껴진다. 이제 제이는 누구의 이름 앞을 지나고 있는지 알고 있다.",
                1, 7,
                TimeSlotMask.All,
                RepeatRule.Daily,
                "GRAVE_CLEAN");
            graveCleanPost.ShowConditions.Add(FlagTrue(FlagMemorialDone));
            graveCleanPost.Choices.Add(Choice("GRAVE_CLEAN_POST_WORK", "1시간 정돈한다", 1, 0, StatType.None, 0,
                "묘지를 돌보며 자신의 감정을 정리했습니다.",
                XpAction(StatType.Personal, 1)));
            encounters.Add(graveCleanPost);

            PrototypeEncounter fayeTools = Encounter(
                "FAYE_TOOL_SORT",
                NarrativeType.Activity,
                "ship",
                "faye",
                "페이의 공구와 부품을 정리한다",
                "큰 수리를 맡는 것은 아니지만 부품 이름과 위치를 익히며 손을 보탠다. 남는 1시간을 기술 성장에 쓰는 대표 소형 Encounter다.",
                1, 7,
                TimeSlotMask.All,
                RepeatRule.Daily,
                "FAYE_SMALL_TECH");
            fayeTools.Choices.Add(Choice("FAYE_TOOL_SORT_WORK", "1시간 돕는다", 1, 0, StatType.None, 0,
                "작은 정비 경험을 쌓았습니다.",
                XpAction(StatType.Technical, 1)));
            encounters.Add(fayeTools);

            PrototypeEncounter oasisCarryPre = Encounter(
                "OASIS_CARRY_PRE",
                NarrativeType.Activity,
                "oasis",
                null,
                "물통을 나른다",
                "오아시스는 아직 공동체를 살려 준 고마운 수원으로 보인다. 특별한 의심 없이 필요한 물을 거주지로 옮긴다.",
                1, 7,
                TimeSlotMask.Morning | TimeSlotMask.Afternoon,
                RepeatRule.Daily,
                "OASIS_CARRY");
            oasisCarryPre.HideConditions.Add(FlagTrue(FlagInvestigationOpen));
            oasisCarryPre.Choices.Add(Choice("OASIS_CARRY_PRE_WORK", "1시간 물을 나른다", 1, 0, StatType.None, 0,
                "짧은 현장 경험을 쌓았습니다.",
                XpAction(StatType.Personal, 1)));
            encounters.Add(oasisCarryPre);

            PrototypeEncounter oasisCarryInvestigating = Encounter(
                "OASIS_CARRY_INVESTIGATING",
                NarrativeType.Activity,
                "oasis",
                null,
                "수량을 의식하며 물통을 나른다",
                "조사가 시작된 뒤에는 같은 물 운반도 관찰이 된다. 수위와 이동 동선을 자연스럽게 확인하며 일을 돕는다.",
                3, 7,
                TimeSlotMask.Morning | TimeSlotMask.Afternoon,
                RepeatRule.Daily,
                "OASIS_CARRY");
            oasisCarryInvestigating.ShowConditions.Add(FlagTrue(FlagInvestigationOpen));
            oasisCarryInvestigating.HideConditions.Add(FlagTrue(FlagWaterRiskKnown));
            oasisCarryInvestigating.Choices.Add(Choice("OASIS_CARRY_INV_WORK", "1시간 물을 나른다", 1, 0, StatType.None, 0,
                "같은 일을 하면서도 오아시스를 더 주의 깊게 보게 되었습니다.",
                XpAction(StatType.Personal, 1)));
            encounters.Add(oasisCarryInvestigating);

            PrototypeEncounter oasisCarryPost = Encounter(
                "OASIS_CARRY_POST",
                NarrativeType.Activity,
                "oasis",
                null,
                "물을 아껴 나른다",
                "장기 위험을 알아버린 뒤, 사람들의 평범한 물 사용이 이전과 다르게 보인다. 필요한 만큼만 조심스럽게 옮긴다.",
                3, 7,
                TimeSlotMask.Morning | TimeSlotMask.Afternoon,
                RepeatRule.Daily,
                "OASIS_CARRY");
            oasisCarryPost.ShowConditions.Add(FlagTrue(FlagWaterRiskKnown));
            oasisCarryPost.Choices.Add(Choice("OASIS_CARRY_POST_WORK", "1시간 물을 나른다", 1, 0, StatType.None, 0,
                "위험을 아는 상태에서 일상을 이어 갔습니다.",
                XpAction(StatType.Personal, 1)));
            encounters.Add(oasisCarryPost);
        }

        private void BuildRelationshipContent()
        {
            PrototypeEncounter benWork = Encounter(
                "BENJAMIN_WORK_TOGETHER",
                NarrativeType.Character,
                "ship",
                "benjamin",
                "말없이 일하는 사람",
                "벤자민은 필요한 말만 하고 작업에 집중한다. 같은 목표인 호감도 +1을 얻더라도 제이의 대인 능력에 따라 필요한 시간이 달라진다. 빠른 선택은 Soft Requirement를 의지로 강행할 수도 있다.",
                1, 6,
                TimeSlotMask.Afternoon | TimeSlotMask.Evening,
                RepeatRule.Daily,
                "BEN_AFFINITY_WORK");
            benWork.HideConditions.Add(AffinityAtLeast("benjamin", 5));
            benWork.Choices.Add(Choice("BEN_SLOW", "말을 억지로 만들지 않고 오래 함께 일한다", 6, 0, StatType.None, 0,
                "오랜 시간을 함께 보내며 벤자민의 경계가 조금 누그러졌습니다.",
                AffinityAction("benjamin", 1)));
            benWork.Choices.Add(Choice("BEN_STANDARD", "일의 흐름에 맞춰 자연스럽게 말을 건다", 4, 0, StatType.Social, 2,
                "작업과 대화의 균형을 맞추며 벤자민과 가까워졌습니다.",
                AffinityAction("benjamin", 1)));
            benWork.Choices.Add(Choice("BEN_FAST", "벤자민이 말을 받아들일 순간을 정확히 고른다", 3, 0, StatType.Social, 4,
                "짧은 시간 안에 의미 있는 대화를 만들었습니다.",
                AffinityAction("benjamin", 1)));
            encounters.Add(benWork);

            PrototypeEncounter benGrave = Encounter(
                "BENJAMIN_GRAVE_HINT",
                NarrativeType.Character,
                "graveyard",
                "benjamin",
                "묘지를 바라보는 이유",
                "저녁의 묘지에서 벤자민을 마주친다. 아직 그의 상실을 전부 듣는 장면은 아니지만, 왜 그가 이곳을 쉽게 떠나지 못하는지 처음으로 짐작하게 된다.",
                1, 6,
                TimeSlotMask.Evening,
                RepeatRule.Once);
            benGrave.ShowConditions.Add(FlagTrue(FlagMemorialDone));
            benGrave.ShowConditions.Add(AffinityAtLeast("benjamin", 1));
            benGrave.Choices.Add(Choice("BEN_GRAVE_LISTEN", "벤자민 곁에 잠시 머문다", 2, 0, StatType.None, 0,
                "벤자민이 묘지에 머무는 이유에 대한 단서를 얻었습니다.",
                XpAction(StatType.Personal, 1),
                FlagAction("BEN_LOSS_HINT_KNOWN")));
            encounters.Add(benGrave);

            PrototypeEncounter borichiPre = Encounter(
                "BORICHI_WEATHER_PRE",
                NarrativeType.Character,
                "oasis",
                "borichi",
                "날씨를 기록하는 사람",
                "보리치는 이 행성의 기상 기록을 꾸준히 쌓고 있다. 아직 오아시스 조사 임무와 직접 연결되지는 않았지만, 제이는 그의 관측 방식과 과로하는 습관을 함께 본다.",
                1, 6,
                TimeSlotMask.Morning,
                RepeatRule.Once,
                "BORICHI_WEATHER_TALK");
            borichiPre.HideConditions.Add(FlagTrue(FlagInvestigationOpen));
            borichiPre.Choices.Add(Choice("BORICHI_PRE_LISTEN", "관측을 도우며 설명을 듣는다", 2, 0, StatType.None, 0,
                "기상 관측의 기본과 보리치의 현재 모습을 알게 되었습니다.",
                XpAction(StatType.Technical, 1)));
            encounters.Add(borichiPre);

            PrototypeEncounter borichiInvestigating = Encounter(
                "BORICHI_WEATHER_INVESTIGATION",
                NarrativeType.Character,
                "oasis",
                "borichi",
                "기록에서 이상을 찾는다",
                "조사 임무가 시작되자 보리치의 평범한 기록도 의미가 달라진다. 수량과 기후의 장기 변화를 염두에 두고 자료를 다시 본다.",
                3, 6,
                TimeSlotMask.Morning,
                RepeatRule.Once,
                "BORICHI_WEATHER_TALK");
            borichiInvestigating.ShowConditions.Add(FlagTrue(FlagInvestigationOpen));
            borichiInvestigating.HideConditions.Add(FlagTrue(FlagWaterRiskKnown));
            borichiInvestigating.Choices.Add(Choice("BORICHI_INVESTIGATE_LISTEN", "관측 기록을 함께 검토한다", 2, 0, StatType.None, 0,
                "오아시스 조사에 참고할 기상 맥락을 얻었습니다.",
                XpAction(StatType.Technical, 1),
                FlagAction("BORICHI_CONTEXT_KNOWN")));
            encounters.Add(borichiInvestigating);
        }

        private void BuildInvestigationContent()
        {
            PrototypeEncounter investigate = Encounter(
                "OASIS_MAIN_INVESTIGATION",
                NarrativeType.Main,
                "oasis",
                null,
                "이곳에서 살아갈 수 있을까",
                "오아시스가 지금 사람들을 살리고 있다는 사실과, 앞으로도 이곳에서 살 수 있다는 결론은 다르다. 같은 핵심 사실에 도달하더라도 제이의 능력과 선택에 따라 조사 방법과 시간이 달라진다. Main Encounter이므로 일반 성장 환율은 일부러 정확히 맞추지 않았다.",
                3, 6,
                TimeSlotMask.Morning | TimeSlotMask.Afternoon,
                RepeatRule.Once);
            investigate.ShowConditions.Add(FlagTrue(FlagInvestigationOpen));
            investigate.HideConditions.Add(FlagTrue(FlagWaterRiskKnown));
            investigate.Choices.Add(Choice("OASIS_DIRECT", "직접 수원 경로와 주변을 오래 훑는다", 6, 0, StatType.None, 0,
                "현장을 직접 확인한 끝에 장기적인 물 공급이 불안정하다는 징후를 찾았습니다.",
                XpAction(StatType.Personal, 3),
                FlagAction(FlagWaterRiskKnown)));
            investigate.Choices.Add(Choice("OASIS_SOCIAL", "사람들의 사용 기록과 증언을 연결한다", 4, 0, StatType.Social, 2,
                "여러 사람의 기록을 연결해 장기적인 공급 문제가 숨어 있음을 확인했습니다.",
                XpAction(StatType.Social, 2),
                FlagAction(FlagWaterRiskKnown)));
            investigate.Choices.Add(Choice("OASIS_TECH", "관측 장비와 센서 자료를 집중 분석한다", 3, 0, StatType.Technical, 4,
                "짧은 시간에 핵심 자료를 분석해 오아시스의 장기 위험을 확인했습니다.",
                XpAction(StatType.Technical, 2),
                FlagAction(FlagWaterRiskKnown)));
            encounters.Add(investigate);

            PrototypeEncounter verify = Encounter(
                "OASIS_POST_VERIFY",
                NarrativeType.World,
                "oasis",
                "borichi",
                "알아낸 사실을 다시 확인한다",
                "이미 핵심 위험을 확인했지만, 보리치와 자료를 다시 대조하면 그 결론이 우연이 아니라는 확신을 얻을 수 있다. 메인 진행에는 필수가 아니다.",
                3, 6,
                TimeSlotMask.Morning | TimeSlotMask.Afternoon,
                RepeatRule.Once);
            verify.ShowConditions.Add(FlagTrue(FlagWaterRiskKnown));
            verify.HideConditions.Add(FlagTrue(FlagReportDone));
            verify.Choices.Add(Choice("OASIS_POST_VERIFY_WORK", "보리치와 자료를 재검토한다", 2, 0, StatType.None, 0,
                "조사 결론에 대한 확신과 추가 맥락을 얻었습니다.",
                XpAction(StatType.Technical, 1),
                FlagAction("W1_RISK_VERIFIED")));
            encounters.Add(verify);

            PrototypeEncounter report = Encounter(
                "REPORT_TO_SAM",
                NarrativeType.Main,
                "settlement",
                "sam",
                "조사 결과를 보고한다",
                "이제 문제는 물의 상태만이 아니다. 샘과 결론을 공유하는 순간부터 질문은 '살 수 있는가'에서 '이 사실을 어떻게 처리할 것인가'로 바뀐다.",
                3, 6,
                TimeSlotMask.All,
                RepeatRule.Once);
            report.ShowConditions.Add(FlagTrue(FlagWaterRiskKnown));
            report.HideConditions.Add(FlagTrue(FlagReportDone));
            report.Choices.Add(Choice("REPORT_TO_SAM_MAIN", "알아낸 내용을 샘에게 보고한다", 1, 0, StatType.None, 0,
                "샘과 장기 거주 위험을 공유했습니다. 이제 측근들과 대응을 고민할 수 있습니다.",
                FlagAction(FlagReportDone)));
            encounters.Add(report);
        }

        private void BuildPostReportContent()
        {
            PrototypeEncounter fayeAdvice = Encounter(
                "FAYE_DEPARTURE_ADVICE",
                NarrativeType.Character,
                "ship",
                "faye",
                "다시 띄울 수 있을까",
                "오아시스의 위험을 안 뒤, 페이와 대화의 의미가 달라진다. 이제 수송선은 고장 난 유물이 아니라 떠날 가능성을 가진 수단이다. 실제 우선순위까지 검토하면 Week 2에 도움이 된다.",
                3, 7,
                TimeSlotMask.All,
                RepeatRule.Once);
            fayeAdvice.ShowConditions.Add(FlagTrue(FlagReportDone));
            fayeAdvice.Choices.Add(Choice("FAYE_DEPARTURE_PLAN", "출항을 가정하고 수리 우선순위를 검토한다", 4, 0, StatType.None, 0,
                "Week 2 수송선 정비에 사용할 우선순위 정보를 미리 확보했습니다.",
                XpAction(StatType.Technical, 1),
                FlagAction(FlagShipRepairPriority)));
            encounters.Add(fayeAdvice);

            PrototypeEncounter jinaAdvice = Encounter(
                "JINA_PUBLIC_REACTION",
                NarrativeType.Character,
                "settlement",
                "jina",
                "사람들이 이 말을 버틸 수 있을까",
                "지나는 물이 부족하다는 사실보다, 그 사실을 들은 사람들이 내일부터 어떻게 살아갈지를 걱정한다. 공개 이후의 반응을 미리 검토하면 Week 2의 혼란을 줄일 준비가 된다.",
                3, 7,
                TimeSlotMask.Afternoon | TimeSlotMask.Evening,
                RepeatRule.Once);
            jinaAdvice.ShowConditions.Add(FlagTrue(FlagReportDone));
            jinaAdvice.Choices.Add(Choice("JINA_REACTION_PLAN", "주민 반응과 생활 대책을 함께 정리한다", 4, 0, StatType.None, 0,
                "사람들에게 공개할 경우의 생활 혼란에 대비했습니다.",
                XpAction(StatType.Social, 1),
                FlagAction(FlagPublicReactionPrepared)));
            encounters.Add(jinaAdvice);

            PrototypeEncounter diyaAdvice = Encounter(
                "DIYA_SETTLEMENT_AFTER_REPORT",
                NarrativeType.Character,
                "settlement",
                "diya",
                "정착이라는 말을 다시 생각한다",
                "장기 거주가 어렵다는 결론과 '정착'이라는 선택은 같은 말이 아니다. 디야는 어떤 조건이 갖춰져야 이곳을 삶의 터전이라고 부를 수 있는지 다시 묻는다.",
                3, 7,
                TimeSlotMask.Afternoon | TimeSlotMask.Evening,
                RepeatRule.Once);
            diyaAdvice.ShowConditions.Add(FlagTrue(FlagReportDone));
            diyaAdvice.Choices.Add(Choice("DIYA_AFTER_REPORT_TALK", "디야의 관점을 끝까지 듣는다", 2, 0, StatType.None, 0,
                "정착을 단순한 잔류가 아니라 준비해야 할 삶의 형태로 보는 관점을 얻었습니다.",
                XpAction(StatType.Social, 1),
                FlagAction(FlagSettlementPerspective)));
            encounters.Add(diyaAdvice);
        }

        private PrototypeEncounter Encounter(
            string id,
            NarrativeType narrativeType,
            string locationId,
            string primaryNpcId,
            string title,
            string body,
            int openDay,
            int closeDay,
            TimeSlotMask slots,
            RepeatRule repeatRule,
            string variantGroup = null)
        {
            return new PrototypeEncounter
            {
                Id = id,
                NarrativeType = narrativeType,
                IsGlobal = false,
                LocationId = locationId,
                PrimaryNpcId = primaryNpcId,
                Title = title,
                Body = body,
                OpenDay = openDay,
                CloseDay = closeDay,
                AllowedSlots = slots,
                RepeatRule = repeatRule,
                VariantGroup = variantGroup
            };
        }

        private PrototypeChoice Choice(
            string id,
            string label,
            int timeCost,
            int baseWillCost,
            StatType softStat,
            int softRequirement,
            string resultText,
            params PrototypeAction[] actions)
        {
            PrototypeChoice choice = new PrototypeChoice
            {
                Id = id,
                Label = label,
                TimeCost = timeCost,
                BaseWillCost = baseWillCost,
                SoftStat = softStat,
                SoftRequirement = softRequirement,
                ResultText = resultText
            };
            choice.Actions.AddRange(actions);
            return choice;
        }

        private PrototypeCondition FlagTrue(string key)
        {
            return new PrototypeCondition { Type = ConditionType.FlagTrue, Key = key };
        }

        private PrototypeCondition AffinityAtLeast(string npcId, int value)
        {
            return new PrototypeCondition { Type = ConditionType.AffinityAtLeast, Key = npcId, IntValue = value };
        }

        private PrototypeAction XpAction(StatType stat, int amount)
        {
            return new PrototypeAction { Type = ActionType.AddXp, Stat = stat, IntValue = amount };
        }

        private PrototypeAction AffinityAction(string npcId, int amount)
        {
            return new PrototypeAction { Type = ActionType.AddAffinity, Key = npcId, IntValue = amount };
        }

        private PrototypeAction FlagAction(string key, bool value = true)
        {
            return new PrototypeAction { Type = ActionType.SetFlag, Key = key, BoolValue = value };
        }

        private void EnsureStyles()
        {
            if (titleStyle != null)
                return;

            darkTexture = MakeTexture(new Color(0.075f, 0.06f, 0.055f, 0.98f));
            panelTexture = MakeTexture(new Color(0.14f, 0.13f, 0.12f, 0.96f));
            disabledTexture = MakeTexture(new Color(0.10f, 0.10f, 0.10f, 0.92f));
            mainTexture = MakeTexture(new Color(0.50f, 0.28f, 0.16f, 0.95f));
            characterTexture = MakeTexture(new Color(0.22f, 0.34f, 0.48f, 0.95f));
            activityTexture = MakeTexture(new Color(0.28f, 0.43f, 0.31f, 0.95f));
            worldTexture = MakeTexture(new Color(0.36f, 0.33f, 0.30f, 0.95f));
            oasisTexture = MakeTexture(new Color(0.14f, 0.42f, 0.43f, 1f));
            settlementTexture = MakeTexture(new Color(0.48f, 0.35f, 0.22f, 1f));
            graveyardTexture = MakeTexture(new Color(0.25f, 0.25f, 0.28f, 1f));
            shipTexture = MakeTexture(new Color(0.34f, 0.40f, 0.44f, 1f));

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 27,
                fontStyle = FontStyle.Bold,
                wordWrap = true,
                normal = { textColor = Color.white }
            };

            headingStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                wordWrap = true,
                normal = { textColor = Color.white }
            };

            bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                wordWrap = true,
                normal = { textColor = new Color(0.94f, 0.92f, 0.88f) }
            };

            smallStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                wordWrap = true,
                normal = { textColor = new Color(0.80f, 0.79f, 0.76f) }
            };

            centeredStyle = new GUIStyle(bodyStyle)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold
            };

            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                wordWrap = true,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white },
                hover = { textColor = Color.white },
                active = { textColor = Color.white }
            };

            cardStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(12, 12, 10, 10),
                normal = { background = panelTexture }
            };

            disabledCardStyle = new GUIStyle(cardStyle)
            {
                normal = { background = disabledTexture }
            };

            badgeStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                fontSize = 13,
                normal = { textColor = Color.white, background = worldTexture }
            };

            mainBadgeStyle = new GUIStyle(badgeStyle) { normal = { textColor = Color.white, background = mainTexture } };
            characterBadgeStyle = new GUIStyle(badgeStyle) { normal = { textColor = Color.white, background = characterTexture } };
            activityBadgeStyle = new GUIStyle(badgeStyle) { normal = { textColor = Color.white, background = activityTexture } };
            worldBadgeStyle = new GUIStyle(badgeStyle) { normal = { textColor = Color.white, background = worldTexture } };

            locationTitleStyle = new GUIStyle(headingStyle)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white, background = darkTexture }
            };
        }

        private Texture2D MakeTexture(Color color)
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }
    }
}
