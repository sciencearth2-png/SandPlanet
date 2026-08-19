using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SandPlanet.Prototype
{
    /// <summary>
    /// Prototype 0.2: keeps the working 0.1 gameplay rules, but presents them through
    /// the intended SandPlanet screen flow:
    /// 3D planet hub -> location card screen -> 2D encounter screen -> result.
    /// The ship also has a separate repair-board screen.
    ///
    /// This is still TEMP prototype content. Do not treat the sample quests or
    /// exact costs as final design data.
    /// </summary>
    public sealed class SandPlanetPrototype02Controller : MonoBehaviour
    {
        private const int FinalGameDays = 21;
        private const int PrototypeDays = 3;
        private const int DayStartHour = 8;
        private const int DayEndHour = 22;
        private const int BaseMaxWillpower = 5;
        private const int SleepRecovery = 2;
        private const int NapHours = 2;
        private const int NapRecovery = 1;
        private const int MaxStat = 20;

        private enum ScreenMode
        {
            Setup,
            Hub,
            Location,
            Encounter,
            ShipRepair,
            Complete
        }

        private enum TrendType
        {
            Personal,
            Social,
            Technical
        }

        private enum HardRequirement
        {
            None,
            TechnicianCompanion,
            BenjaminPersuasionReady
        }

        private enum EffectType
        {
            None,
            InvestigateSmoke,
            BenjaminAffinity,
            LearnWaterRisk,
            RecruitTechnician,
            PersuadeBenjamin,
            RepairDefense
        }

        [Serializable]
        private sealed class SampleEncounter
        {
            public string Id;
            public bool IsGlobal;
            public string LocationId;
            public string Title;
            [TextArea] public string Body;
            public string ChoiceLabel;
            public string ResultText;
            public int TimeCost;
            public int WillCost;
            public TrendType Trend;
            public bool RepeatDaily;
            public TrendType SoftStat;
            public int SoftRequirement;
            public HardRequirement HardRequirement;
            public EffectType Effect;

            [NonSerialized] public bool Completed;
            [NonSerialized] public int LastCompletedDay = -1;
        }

        private readonly List<SampleEncounter> encounters = new List<SampleEncounter>();
        private readonly Dictionary<PrototypeLocationNode, Vector3> nodeBaseScales = new Dictionary<PrototypeLocationNode, Vector3>();

        private ScreenMode mode = ScreenMode.Setup;
        private ScreenMode encounterReturnMode = ScreenMode.Hub;
        private string currentLocationId;
        private SampleEncounter currentEncounter;
        private PrototypeLocationNode hoveredNode;
        private PrototypeLocationNode[] locationNodes;

        private int day;
        private int currentHour;
        private int currentWillpower;
        private int maxWillpower;

        private int personal;
        private int social;
        private int technical;

        private int setupPersonal = 2;
        private int setupSocial = 2;
        private int setupTechnical = 2;

        private int personalTrend;
        private int socialTrend;
        private int technicalTrend;
        private TrendType lastTrend = TrendType.Personal;

        // TEMP NPC / quest state.
        private int benjaminAffinity;
        private bool waterRiskKnown;
        private bool technicianCompanion;
        private bool benjaminPersuaded;
        private bool smokeInvestigated;

        // TEMP ship facility state. Only defense is currently playable.
        private int defenseState;

        private string statusMessage = "시작 능력치를 배분하세요.";
        private string dayGrowthMessage = string.Empty;

        private GUIStyle titleStyle;
        private GUIStyle headingStyle;
        private GUIStyle bodyStyle;
        private GUIStyle smallStyle;
        private GUIStyle centeredStyle;
        private GUIStyle buttonStyle;
        private GUIStyle cardStyle;
        private GUIStyle cardDisabledStyle;
        private GUIStyle badgeStyle;
        private GUIStyle locationTitleStyle;
        private GUIStyle portraitStyle;

        private Texture2D darkTexture;
        private Texture2D cardTexture;
        private Texture2D disabledCardTexture;
        private Texture2D accentTexture;
        private Texture2D blueTexture;
        private Texture2D oasisTexture;
        private Texture2D settlementTexture;
        private Texture2D graveyardTexture;
        private Texture2D shipTexture;

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

            // Do not let world clicks pass through HUD / debug panel / bottom feedback.
            bool blocked = guiY < 70f || mouse.x > Screen.width - 390f || guiY > Screen.height - 92f;
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
                case ScreenMode.ShipRepair:
                    DrawShipRepairScreen();
                    break;
            }

            DrawStatusMessage();
        }

        private void DrawSetupScreen()
        {
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), darkTexture, ScaleMode.StretchToFill);

            float width = Mathf.Min(700f, Screen.width - 60f);
            Rect panel = new Rect((Screen.width - width) * 0.5f, 70f, width, 520f);
            GUI.Box(panel, string.Empty);

            GUILayout.BeginArea(new Rect(panel.x + 34f, panel.y + 28f, panel.width - 68f, panel.height - 56f));
            GUILayout.Label("SAND PLANET — PROTOTYPE 0.2", titleStyle);
            GUILayout.Space(8f);
            GUILayout.Label("시작 능력치 배분", headingStyle);
            GUILayout.Label("개인 / 대인 / 기술에 총 6포인트를 배분합니다. 실제 게임의 최종 상한은 각각 20입니다.", bodyStyle);
            GUILayout.Space(18f);

            DrawSetupStatRow("개인", ref setupPersonal);
            DrawSetupStatRow("대인", ref setupSocial);
            DrawSetupStatRow("기술", ref setupTechnical);

            int total = setupPersonal + setupSocial + setupTechnical;
            GUILayout.Space(12f);
            GUILayout.Label($"배분: {total} / 6", headingStyle);
            GUILayout.Space(16f);

            GUI.enabled = total == 6;
            if (GUILayout.Button("DAY 1 시작", buttonStyle, GUILayout.Height(50f)))
                StartRun();
            GUI.enabled = true;

            GUILayout.Space(14f);
            GUILayout.Label("0.2 검증 목표: 3D 허브 → 장소 카드 → 2D 인카운터의 화면 전환이 SandPlanet의 실제 플레이 흐름처럼 느껴지는지 확인합니다.", smallStyle);
            GUILayout.EndArea();
        }

        private void DrawSetupStatRow(string label, ref int value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, headingStyle, GUILayout.Width(140f));

            if (GUILayout.Button("-", buttonStyle, GUILayout.Width(58f), GUILayout.Height(38f)) && value > 0)
                value--;

            GUILayout.Label(value.ToString(), centeredStyle, GUILayout.Width(76f), GUILayout.Height(38f));

            int total = setupPersonal + setupSocial + setupTechnical;
            GUI.enabled = total < 6 && value < MaxStat;
            if (GUILayout.Button("+", buttonStyle, GUILayout.Width(58f), GUILayout.Height(38f)))
                value++;
            GUI.enabled = true;

            GUILayout.EndHorizontal();
            GUILayout.Space(9f);
        }

        private void DrawHud()
        {
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, 70f), darkTexture, ScaleMode.StretchToFill);

            GUILayout.BeginArea(new Rect(18f, 10f, Screen.width - 36f, 52f));
            GUILayout.BeginHorizontal();
            GUILayout.Label($"DAY {day} / {FinalGameDays}", headingStyle, GUILayout.Width(145f));
            GUILayout.Label($"{currentHour:00}:00", headingStyle, GUILayout.Width(95f));
            GUILayout.Label($"의지 {currentWillpower}/{maxWillpower}", headingStyle, GUILayout.Width(130f));
            GUILayout.Label($"개인 {personal}   대인 {social}   기술 {technical}", bodyStyle, GUILayout.Width(275f));
            GUILayout.Label($"오늘 경향  개인 {personalTrend} / 대인 {socialTrend} / 기술 {technicalTrend}", smallStyle);
            GUILayout.FlexibleSpace();

            GUI.enabled = currentWillpower < maxWillpower && CanSpendTime(NapHours);
            if (GUILayout.Button("낮잠 2h / 의지 +1", buttonStyle, GUILayout.Width(155f), GUILayout.Height(38f)))
                TakeNap();
            GUI.enabled = true;

            if (GUILayout.Button("하루 종료", buttonStyle, GUILayout.Width(105f), GUILayout.Height(38f)))
                EndDay();

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void DrawHubScreen()
        {
            DrawWorldLocationLabelsAndMarkers();

            Rect panel = new Rect(Screen.width - 375f, 84f, 355f, Mathf.Min(560f, Screen.height - 175f));
            GUI.Box(panel, string.Empty);
            GUILayout.BeginArea(new Rect(panel.x + 18f, panel.y + 16f, panel.width - 36f, panel.height - 32f));

            GUILayout.Label("행성 허브", titleStyle);
            GUILayout.Label("랜드마크를 클릭해 장소로 들어갑니다. ! 는 긴급/행성 인카운터, … 은 장소 인카운터, 인물은 NPC 콘텐츠가 있음을 뜻합니다.", smallStyle);
            GUILayout.Space(10f);

            GUILayout.Label("행성에서 바로 실행", headingStyle);
            bool anyGlobal = false;
            foreach (SampleEncounter encounter in encounters)
            {
                if (!encounter.IsGlobal || !IsRepeatAvailable(encounter))
                    continue;

                anyGlobal = true;
                DrawCompactEncounterCard(encounter);
            }

            if (!anyGlobal)
                GUILayout.Label("현재 행성 인카운터 없음", smallStyle);

            GUILayout.Space(14f);
            GUILayout.Label("디버그 상태", headingStyle);
            GUILayout.Label($"벤자민 호감도: {benjaminAffinity}/5", smallStyle);
            GUILayout.Label($"기술자 동료: {(technicianCompanion ? "확보" : "미확보")}", smallStyle);
            GUILayout.Label($"벤자민 설득: {(benjaminPersuaded ? "성공" : "미완료")}", smallStyle);
            GUILayout.Label($"수송선 방호: {FacilityLabel(defenseState)} ({defenseState}/3)", smallStyle);

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
                string name = LocationName(node.LocationId);

                GUI.Label(new Rect(x - 95f, y - 24f, 190f, 28f), name, locationTitleStyle);

                float badgeX = x - 54f;
                if (LocationHasAvailableEncounter(node.LocationId))
                {
                    GUI.Label(new Rect(badgeX, y + 2f, 46f, 28f), "…", badgeStyle);
                    badgeX += 50f;
                }

                if (LocationHasNpcMarker(node.LocationId))
                {
                    GUI.Label(new Rect(badgeX, y + 2f, 58f, 28f), "인물", badgeStyle);
                    badgeX += 62f;
                }

                if (node.LocationId == "ship" && HasAvailableGlobalEncounter())
                    GUI.Label(new Rect(badgeX, y + 2f, 34f, 28f), "!", badgeStyle);
            }
        }

        private void DrawLocationScreen()
        {
            Rect screenRect = new Rect(0f, 70f, Screen.width, Screen.height - 70f);
            GUI.DrawTexture(screenRect, LocationTexture(currentLocationId), ScaleMode.StretchToFill);

            // Soft dark veil so the placeholder background behaves like a future 2D location image.
            Color previous = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, 0.72f);
            GUI.DrawTexture(screenRect, darkTexture, ScaleMode.StretchToFill);
            GUI.color = previous;

            Rect content = new Rect(54f, 92f, Screen.width - 108f, Screen.height - 188f);
            GUI.Box(content, string.Empty);

            Rect header = new Rect(content.x + 24f, content.y + 18f, content.width - 48f, 62f);
            GUI.Label(new Rect(header.x, header.y, 360f, 46f), LocationName(currentLocationId), titleStyle);

            if (currentLocationId == "ship")
            {
                if (GUI.Button(new Rect(header.x + header.width - 355f, header.y + 4f, 170f, 40f), "수송선 정비도", buttonStyle))
                {
                    mode = ScreenMode.ShipRepair;
                    statusMessage = "수송선 정비도를 열었습니다.";
                }
            }

            if (GUI.Button(new Rect(header.x + header.width - 170f, header.y + 4f, 170f, 40f), "행성으로 돌아가기", buttonStyle))
            {
                mode = ScreenMode.Hub;
                currentLocationId = null;
                statusMessage = "행성 허브로 돌아왔습니다.";
                return;
            }

            GUI.Label(new Rect(content.x + 24f, content.y + 78f, content.width - 48f, 32f), "현재 장소의 인카운터", headingStyle);
            GUI.Label(new Rect(content.x + 24f, content.y + 108f, content.width - 48f, 28f), "카드를 선택하면 2D 인카운터 화면으로 전환됩니다.", smallStyle);

            List<SampleEncounter> list = GetLocationEncounters(currentLocationId);
            DrawEncounterGrid(list, new Rect(content.x + 24f, content.y + 145f, content.width - 48f, content.height - 175f));
        }

        private void DrawEncounterGrid(List<SampleEncounter> list, Rect rect)
        {
            const int columns = 3;
            const int rows = 2;
            const float gap = 14f;

            float cardWidth = (rect.width - gap * (columns - 1)) / columns;
            float cardHeight = (rect.height - gap * (rows - 1)) / rows;
            cardHeight = Mathf.Min(cardHeight, 220f);

            for (int i = 0; i < columns * rows; i++)
            {
                int col = i % columns;
                int row = i / columns;
                Rect cardRect = new Rect(rect.x + col * (cardWidth + gap), rect.y + row * (cardHeight + gap), cardWidth, cardHeight);

                if (i < list.Count)
                    DrawEncounterCardRect(list[i], cardRect);
                else
                    DrawEmptyCard(cardRect);
            }
        }

        private void DrawEncounterCardRect(SampleEncounter encounter, Rect rect)
        {
            bool selectable = IsEncounterSelectable(encounter);
            GUI.Box(rect, string.Empty, selectable ? cardStyle : cardDisabledStyle);

            float pad = 16f;
            GUI.Label(new Rect(rect.x + pad, rect.y + 12f, rect.width - pad * 2f, 54f), encounter.Title, headingStyle);
            GUI.Label(new Rect(rect.x + pad, rect.y + 66f, rect.width - pad * 2f, 30f), $"시간 {encounter.TimeCost}h  ·  의지 {encounter.WillCost}{SoftRequirementPreview(encounter)}", smallStyle);

            string preview = encounter.Body;
            if (preview.Length > 72)
                preview = preview.Substring(0, 72) + "…";
            GUI.Label(new Rect(rect.x + pad, rect.y + 98f, rect.width - pad * 2f, 62f), preview, smallStyle);

            string availability = EncounterAvailabilityText(encounter);
            if (!string.IsNullOrEmpty(availability))
                GUI.Label(new Rect(rect.x + pad, rect.y + rect.height - 56f, rect.width - pad * 2f, 24f), availability, smallStyle);

            GUI.enabled = selectable;
            if (GUI.Button(new Rect(rect.x + pad, rect.y + rect.height - 38f, rect.width - pad * 2f, 30f), selectable ? "인카운터 열기" : "현재 실행 불가", buttonStyle))
                OpenEncounter(encounter);
            GUI.enabled = true;
        }

        private void DrawEmptyCard(Rect rect)
        {
            GUI.Box(rect, string.Empty, cardDisabledStyle);
            GUI.Label(rect, "EMPTY\n추후 인카운터 슬롯", centeredStyle);
        }

        private void DrawCompactEncounterCard(SampleEncounter encounter)
        {
            GUILayout.BeginVertical(cardStyle);
            GUILayout.Label(encounter.Title, headingStyle);
            GUILayout.Label($"시간 {encounter.TimeCost}h / 의지 {encounter.WillCost}", smallStyle);
            string availability = EncounterAvailabilityText(encounter);
            if (!string.IsNullOrEmpty(availability))
                GUILayout.Label(availability, smallStyle);

            GUI.enabled = IsEncounterSelectable(encounter);
            if (GUILayout.Button("인카운터 열기", buttonStyle, GUILayout.Height(34f)))
                OpenEncounter(encounter);
            GUI.enabled = true;
            GUILayout.EndVertical();
            GUILayout.Space(8f);
        }

        private void DrawEncounterScreen()
        {
            if (currentEncounter == null)
            {
                mode = ScreenMode.Hub;
                return;
            }

            Rect screenRect = new Rect(0f, 70f, Screen.width, Screen.height - 70f);
            GUI.DrawTexture(screenRect, LocationTexture(currentEncounter.IsGlobal ? "ship" : currentEncounter.LocationId), ScaleMode.StretchToFill);

            Color previous = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, 0.76f);
            GUI.DrawTexture(screenRect, darkTexture, ScaleMode.StretchToFill);
            GUI.color = previous;

            float margin = Mathf.Max(38f, Screen.width * 0.035f);
            Rect main = new Rect(margin, 100f, Screen.width - margin * 2f, Screen.height - 210f);

            // Left: future 2D background / character portrait area.
            float visualWidth = main.width * 0.38f;
            Rect visual = new Rect(main.x, main.y, visualWidth, main.height);
            GUI.Box(visual, string.Empty);
            GUI.Label(new Rect(visual.x + 18f, visual.y + 18f, visual.width - 36f, 34f), "2D VISUAL PLACEHOLDER", headingStyle);
            GUI.Label(new Rect(visual.x + 18f, visual.y + 56f, visual.width - 36f, 60f), $"장소 배경: {LocationName(currentEncounter.IsGlobal ? "ship" : currentEncounter.LocationId)}", bodyStyle);

            string character = EncounterCharacterName(currentEncounter);
            Rect portrait = new Rect(visual.x + visual.width * 0.18f, visual.y + visual.height * 0.32f, visual.width * 0.64f, visual.height * 0.48f);
            GUI.Box(portrait, string.Empty, portraitStyle);
            GUI.Label(portrait, string.IsNullOrEmpty(character) ? "상황 이미지" : $"PORTRAIT\n{character}", centeredStyle);

            // Right: narrative / choices.
            Rect narrative = new Rect(visual.xMax + 18f, main.y, main.width - visual.width - 18f, main.height);
            GUI.Box(narrative, string.Empty);

            GUILayout.BeginArea(new Rect(narrative.x + 28f, narrative.y + 24f, narrative.width - 56f, narrative.height - 48f));
            GUILayout.Label(currentEncounter.Title, titleStyle);
            GUILayout.Space(12f);
            GUILayout.Label(currentEncounter.Body, bodyStyle);
            GUILayout.Space(18f);

            string requirementText = RequirementDescription(currentEncounter);
            if (!string.IsNullOrEmpty(requirementText))
            {
                GUILayout.Label("조건", headingStyle);
                GUILayout.Label(requirementText, smallStyle);
                GUILayout.Space(10f);
            }

            int extraWill = SoftRequirementExtraWill(currentEncounter);
            string cost = $"시간 {currentEncounter.TimeCost}h / 의지 {currentEncounter.WillCost}";
            if (extraWill > 0)
                cost += $" + 강행 {extraWill}";

            GUI.enabled = IsEncounterSelectable(currentEncounter);
            if (GUILayout.Button($"{currentEncounter.ChoiceLabel}\n{cost}", buttonStyle, GUILayout.Height(76f)))
                ExecuteEncounter(currentEncounter);
            GUI.enabled = true;

            GUILayout.Space(10f);
            if (GUILayout.Button("돌아가기", buttonStyle, GUILayout.Height(42f)))
                ReturnFromEncounter();

            GUILayout.EndArea();
        }

        private void DrawShipRepairScreen()
        {
            Rect screenRect = new Rect(0f, 70f, Screen.width, Screen.height - 70f);
            GUI.DrawTexture(screenRect, shipTexture, ScaleMode.StretchToFill);
            Color previous = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, 0.78f);
            GUI.DrawTexture(screenRect, darkTexture, ScaleMode.StretchToFill);
            GUI.color = previous;

            Rect panel = new Rect(46f, 94f, Screen.width - 92f, Screen.height - 192f);
            GUI.Box(panel, string.Empty);

            GUI.Label(new Rect(panel.x + 24f, panel.y + 18f, 430f, 46f), "수송선 정비도", titleStyle);
            GUI.Label(new Rect(panel.x + 24f, panel.y + 62f, 620f, 32f), "스킬트리가 아니라 실제 수송선의 부위와 수리 상태를 보는 화면의 그레이박스입니다.", smallStyle);

            if (GUI.Button(new Rect(panel.x + panel.width - 190f, panel.y + 22f, 165f, 40f), "수송선 장소로", buttonStyle))
            {
                currentLocationId = "ship";
                mode = ScreenMode.Location;
                return;
            }

            Rect shipBody = new Rect(panel.x + panel.width * 0.18f, panel.y + 120f, panel.width * 0.64f, panel.height - 170f);
            GUI.Box(shipBody, "수송선 단면 / 정비 도면 PLACEHOLDER", centeredStyle);

            Rect core = new Rect(shipBody.center.x - 105f, shipBody.center.y - 64f, 210f, 128f);
            Rect propulsion = new Rect(shipBody.x + 35f, shipBody.center.y - 64f, 190f, 128f);
            Rect habitation = new Rect(shipBody.xMax - 225f, shipBody.y + 45f, 190f, 128f);
            Rect defense = new Rect(shipBody.xMax - 225f, shipBody.yMax - 173f, 190f, 128f);

            DrawFacilityNode(core, "코어", 1, false, null);
            DrawFacilityNode(propulsion, "추진", 0, false, null);
            DrawFacilityNode(habitation, "거주", 1, false, null);
            DrawFacilityNode(defense, "방호", defenseState, true, FindEncounter("ship_repair_defense"));
        }

        private void DrawFacilityNode(Rect rect, string label, int state, bool playable, SampleEncounter repairEncounter)
        {
            GUI.Box(rect, string.Empty, playable ? cardStyle : cardDisabledStyle);
            GUI.Label(new Rect(rect.x + 10f, rect.y + 10f, rect.width - 20f, 30f), label, headingStyle);
            GUI.Label(new Rect(rect.x + 10f, rect.y + 44f, rect.width - 20f, 24f), $"{FacilityLabel(state)}  ({state}/3)", smallStyle);

            if (!playable)
            {
                GUI.Label(new Rect(rect.x + 10f, rect.y + 76f, rect.width - 20f, 34f), "0.2 PLACEHOLDER", smallStyle);
                return;
            }

            bool canOpen = repairEncounter != null && IsEncounterSelectable(repairEncounter) && state < 3;
            GUI.enabled = canOpen;
            if (GUI.Button(new Rect(rect.x + 10f, rect.y + rect.height - 40f, rect.width - 20f, 30f), state >= 3 ? "보강 완료" : "수리 작업", buttonStyle))
                OpenEncounter(repairEncounter);
            GUI.enabled = true;
        }

        private void DrawStatusMessage()
        {
            Rect rect = new Rect(18f, Screen.height - 84f, Mathf.Min(980f, Screen.width - 36f), 62f);
            GUI.Box(rect, string.Empty);
            GUI.Label(new Rect(rect.x + 14f, rect.y + 8f, rect.width - 28f, rect.height - 16f), statusMessage, bodyStyle);
        }

        private void DrawCompleteScreen()
        {
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), darkTexture, ScaleMode.StretchToFill);
            float width = Mathf.Min(760f, Screen.width - 60f);
            Rect panel = new Rect((Screen.width - width) * 0.5f, 64f, width, 620f);
            GUI.Box(panel, string.Empty);

            GUILayout.BeginArea(new Rect(panel.x + 34f, panel.y + 26f, panel.width - 68f, panel.height - 52f));
            GUILayout.Label("PROTOTYPE 0.2 COMPLETE", titleStyle);
            GUILayout.Label("정식 게임은 21일이지만 현재 화면 흐름 검증용 콘텐츠는 3일에서 종료됩니다.", bodyStyle);
            GUILayout.Space(18f);
            GUILayout.Label("최종 상태", headingStyle);
            GUILayout.Label($"개인 {personal} / 대인 {social} / 기술 {technical}", bodyStyle);
            GUILayout.Label($"의지 {currentWillpower}/{maxWillpower}", bodyStyle);
            GUILayout.Label($"벤자민 호감도 {benjaminAffinity}/5", bodyStyle);
            GUILayout.Label($"기술자 동료: {(technicianCompanion ? "확보" : "미확보")}", bodyStyle);
            GUILayout.Label($"벤자민 설득: {(benjaminPersuaded ? "성공" : "미완료")}", bodyStyle);
            GUILayout.Label($"수송선 방호: {FacilityLabel(defenseState)} ({defenseState}/3)", bodyStyle);
            GUILayout.Space(12f);
            GUILayout.Label(dayGrowthMessage, smallStyle);
            GUILayout.Space(22f);
            GUILayout.Label("0.2에서 확인할 것", headingStyle);
            GUILayout.Label("• 3D 행성 → 장소 카드 → 2D 인카운터의 전환이 명확한가?\n• 장소별 2×3 카드가 콘텐츠 선택 화면으로 읽히는가?\n• 행성의 ! / … / 인물 정보가 탐색 동기를 만드는가?\n• 수송선 정비도가 '테크트리'보다 '배를 고친다'는 느낌에 가까운가?\n• UI가 실제 이미지/아트를 넣기 전에 구조적으로 납득되는가?", bodyStyle);
            GUILayout.Space(20f);

            if (GUILayout.Button("프로토타입 다시 시작", buttonStyle, GUILayout.Height(48f)))
                ResetRun();
            GUILayout.EndArea();
        }

        private void OpenLocation(string locationId)
        {
            currentLocationId = locationId;
            mode = ScreenMode.Location;
            statusMessage = $"{LocationName(locationId)}에 들어왔습니다.";
        }

        private void OpenEncounter(SampleEncounter encounter)
        {
            if (encounter == null)
                return;

            encounterReturnMode = mode;
            currentEncounter = encounter;
            mode = ScreenMode.Encounter;
            statusMessage = $"인카운터: {encounter.Title}";
        }

        private void ExecuteEncounter(SampleEncounter encounter)
        {
            int extraWill = SoftRequirementExtraWill(encounter);
            int totalWill = encounter.WillCost + extraWill;

            if (!HardRequirementMet(encounter))
            {
                statusMessage = "필수 조건을 충족하지 못했습니다.";
                return;
            }

            if (!TrySpend(encounter.TimeCost, totalWill))
                return;

            RecordTrend(encounter.Trend);
            encounter.Completed = true;
            encounter.LastCompletedDay = day;
            ApplyEffect(encounter.Effect);

            string forced = extraWill > 0 ? $" 부족 능력치를 의지 {extraWill}로 강행했습니다." : string.Empty;
            statusMessage = encounter.ResultText + forced;

            ReturnFromEncounter();
        }

        private void ReturnFromEncounter()
        {
            mode = encounterReturnMode;
            currentEncounter = null;
        }

        private void TakeNap()
        {
            if (currentWillpower >= maxWillpower)
            {
                statusMessage = "의지력이 이미 최대입니다.";
                return;
            }

            if (!CanSpendTime(NapHours))
            {
                statusMessage = "낮잠을 자기에는 오늘 남은 시간이 부족합니다.";
                return;
            }

            currentHour += NapHours;
            currentWillpower = Mathf.Min(maxWillpower, currentWillpower + NapRecovery);
            RecordTrend(TrendType.Personal);
            statusMessage = $"2시간 낮잠을 잤습니다. 의지 +{NapRecovery}. 개인 경향 +1.";
        }

        private void EndDay()
        {
            string growth = ApplyDailyGrowth();
            currentWillpower = Mathf.Min(maxWillpower, currentWillpower + SleepRecovery);
            dayGrowthMessage = $"DAY {day} 종료: {growth} / 수면으로 의지 +{SleepRecovery}";

            if (day >= PrototypeDays)
            {
                mode = ScreenMode.Complete;
                return;
            }

            day++;
            currentHour = DayStartHour;
            personalTrend = 0;
            socialTrend = 0;
            technicalTrend = 0;
            statusMessage = dayGrowthMessage + $"  → DAY {day} 시작";
            mode = ScreenMode.Hub;
            currentLocationId = null;
        }

        private string ApplyDailyGrowth()
        {
            int max = Mathf.Max(personalTrend, Mathf.Max(socialTrend, technicalTrend));
            TrendType winner;

            if (max == 0)
                winner = TrendType.Personal;
            else if (TrendValue(lastTrend) == max)
                winner = lastTrend;
            else if (personalTrend == max)
                winner = TrendType.Personal;
            else if (socialTrend == max)
                winner = TrendType.Social;
            else
                winner = TrendType.Technical;

            switch (winner)
            {
                case TrendType.Personal:
                    personal = Mathf.Min(MaxStat, personal + 1);
                    return "개인 +1";
                case TrendType.Social:
                    social = Mathf.Min(MaxStat, social + 1);
                    return "대인 +1";
                default:
                    technical = Mathf.Min(MaxStat, technical + 1);
                    return "기술 +1";
            }
        }

        private void ApplyEffect(EffectType effect)
        {
            switch (effect)
            {
                case EffectType.InvestigateSmoke:
                    smokeInvestigated = true;
                    break;
                case EffectType.BenjaminAffinity:
                    benjaminAffinity = Mathf.Min(5, benjaminAffinity + 1);
                    break;
                case EffectType.LearnWaterRisk:
                    waterRiskKnown = true;
                    break;
                case EffectType.RecruitTechnician:
                    technicianCompanion = true;
                    break;
                case EffectType.PersuadeBenjamin:
                    benjaminPersuaded = true;
                    break;
                case EffectType.RepairDefense:
                    defenseState = Mathf.Min(3, defenseState + 1);
                    break;
            }
        }

        private bool TrySpend(int hours, int will)
        {
            if (!CanSpendTime(hours))
            {
                statusMessage = $"시간이 부족합니다. 기본 일과는 {DayEndHour:00}:00에 종료됩니다.";
                return false;
            }

            if (currentWillpower < will)
            {
                statusMessage = $"의지력이 부족합니다. 필요 {will}, 현재 {currentWillpower}.";
                return false;
            }

            currentHour += hours;
            currentWillpower -= will;
            return true;
        }

        private bool HasResources(int hours, int will)
        {
            return CanSpendTime(hours) && currentWillpower >= will;
        }

        private bool CanSpendTime(int hours)
        {
            return currentHour + hours <= DayEndHour;
        }

        private void RecordTrend(TrendType trend)
        {
            lastTrend = trend;
            switch (trend)
            {
                case TrendType.Personal:
                    personalTrend++;
                    break;
                case TrendType.Social:
                    socialTrend++;
                    break;
                case TrendType.Technical:
                    technicalTrend++;
                    break;
            }
        }

        private int TrendValue(TrendType trend)
        {
            switch (trend)
            {
                case TrendType.Personal: return personalTrend;
                case TrendType.Social: return socialTrend;
                default: return technicalTrend;
            }
        }

        private int StatValue(TrendType stat)
        {
            switch (stat)
            {
                case TrendType.Personal: return personal;
                case TrendType.Social: return social;
                default: return technical;
            }
        }

        private int SoftRequirementExtraWill(SampleEncounter encounter)
        {
            if (encounter.SoftRequirement <= 0)
                return 0;

            int gap = Mathf.Max(0, encounter.SoftRequirement - StatValue(encounter.SoftStat));
            // TEMP 0.2 formula: missing 1 stat = 1 additional Willpower.
            return gap;
        }

        private string SoftRequirementPreview(SampleEncounter encounter)
        {
            if (encounter.SoftRequirement <= 0)
                return string.Empty;
            return $" · {TrendName(encounter.SoftStat)} {encounter.SoftRequirement}";
        }

        private bool HardRequirementMet(SampleEncounter encounter)
        {
            switch (encounter.HardRequirement)
            {
                case HardRequirement.None:
                    return true;
                case HardRequirement.TechnicianCompanion:
                    return technicianCompanion;
                case HardRequirement.BenjaminPersuasionReady:
                    return benjaminAffinity >= 1 && waterRiskKnown;
                default:
                    return false;
            }
        }

        private string RequirementDescription(SampleEncounter encounter)
        {
            List<string> parts = new List<string>();

            if (encounter.SoftRequirement > 0)
            {
                int gap = Mathf.Max(0, encounter.SoftRequirement - StatValue(encounter.SoftStat));
                string soft = $"Soft · {TrendName(encounter.SoftStat)} {encounter.SoftRequirement}";
                if (gap > 0)
                    soft += $"  (부족 {gap} → 의지 {gap}로 강행 가능 / TEMP)";
                else
                    soft += "  (충족)";
                parts.Add(soft);
            }

            switch (encounter.HardRequirement)
            {
                case HardRequirement.TechnicianCompanion:
                    parts.Add($"Hard · 기술자 동료 필요 — {(technicianCompanion ? "충족" : "미충족")}");
                    break;
                case HardRequirement.BenjaminPersuasionReady:
                    parts.Add($"Hard · 벤자민 호감도 1 이상 + 장기 거주 위험 정보 — {((benjaminAffinity >= 1 && waterRiskKnown) ? "충족" : "미충족")}");
                    break;
            }

            return string.Join("\n", parts);
        }

        private string EncounterAvailabilityText(SampleEncounter encounter)
        {
            if (!IsRepeatAvailable(encounter))
                return encounter.RepeatDaily ? "오늘 이미 실행함" : "완료됨";

            if (!HardRequirementMet(encounter))
                return "조건 미충족";

            int extra = SoftRequirementExtraWill(encounter);
            if (!HasResources(encounter.TimeCost, encounter.WillCost + extra))
                return "현재 시간 또는 의지 부족";

            return string.Empty;
        }

        private bool IsEncounterSelectable(SampleEncounter encounter)
        {
            return encounter != null
                   && IsRepeatAvailable(encounter)
                   && HardRequirementMet(encounter)
                   && HasResources(encounter.TimeCost, encounter.WillCost + SoftRequirementExtraWill(encounter));
        }

        private bool IsRepeatAvailable(SampleEncounter encounter)
        {
            if (encounter == null)
                return false;

            if (encounter.RepeatDaily)
                return encounter.LastCompletedDay != day;

            return !encounter.Completed;
        }

        private string TrendName(TrendType trend)
        {
            switch (trend)
            {
                case TrendType.Personal: return "개인";
                case TrendType.Social: return "대인";
                default: return "기술";
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
                default: return id ?? "알 수 없는 장소";
            }
        }

        private Texture2D LocationTexture(string id)
        {
            switch (id)
            {
                case "ship": return shipTexture;
                case "graveyard": return graveyardTexture;
                case "settlement": return settlementTexture;
                case "oasis": return oasisTexture;
                default: return darkTexture;
            }
        }

        private string FacilityLabel(int state)
        {
            switch (state)
            {
                case 0: return "기능 불능";
                case 1: return "응급 복구";
                case 2: return "정상 작동";
                default: return "완전 복구/보강";
            }
        }

        private void StartRun()
        {
            personal = setupPersonal;
            social = setupSocial;
            technical = setupTechnical;
            day = 1;
            currentHour = DayStartHour;
            currentWillpower = BaseMaxWillpower;
            maxWillpower = BaseMaxWillpower;
            mode = ScreenMode.Hub;
            statusMessage = "DAY 1. 3D 행성에서 장소를 선택하거나 ! 인카운터를 확인하세요.";
        }

        private void ResetRun()
        {
            setupPersonal = 2;
            setupSocial = 2;
            setupTechnical = 2;

            day = 1;
            currentHour = DayStartHour;
            currentWillpower = BaseMaxWillpower;
            maxWillpower = BaseMaxWillpower;
            personal = 0;
            social = 0;
            technical = 0;
            personalTrend = 0;
            socialTrend = 0;
            technicalTrend = 0;
            lastTrend = TrendType.Personal;

            benjaminAffinity = 0;
            waterRiskKnown = false;
            technicianCompanion = false;
            benjaminPersuaded = false;
            smokeInvestigated = false;
            defenseState = 0;

            foreach (SampleEncounter encounter in encounters)
            {
                encounter.Completed = false;
                encounter.LastCompletedDay = -1;
            }

            currentLocationId = null;
            currentEncounter = null;
            encounterReturnMode = ScreenMode.Hub;
            dayGrowthMessage = string.Empty;
            statusMessage = "시작 능력치를 배분하세요.";
            mode = ScreenMode.Setup;
            SetHoveredNode(null);
        }

        private void BuildPrototypeContent()
        {
            encounters.Clear();

            encounters.Add(new SampleEncounter
            {
                Id = "global_smoke",
                IsGlobal = true,
                LocationId = "ship",
                Title = "수송선에서 연기가 난다",
                Body = "멀리 있는 수송선 상부에서 검은 연기가 피어오른다. 지금 확인한다면 하루 계획이 틀어지겠지만, 방치하기에도 마음에 걸린다.",
                ChoiceLabel = "상태를 확인한다",
                ResultText = "연기의 원인을 확인했다. 아직 치명적인 손상은 아니지만 수송선 방호 상태가 좋지 않다는 것을 알았다.",
                TimeCost = 1,
                WillCost = 1,
                Trend = TrendType.Technical,
                RepeatDaily = false,
                SoftStat = TrendType.Technical,
                SoftRequirement = 0,
                HardRequirement = HardRequirement.None,
                Effect = EffectType.InvestigateSmoke
            });

            encounters.Add(new SampleEncounter
            {
                Id = "oasis_benjamin",
                LocationId = "oasis",
                Title = "벤자민과 이야기한다",
                Body = "벤자민은 물가에 앉아 수송선 쪽을 바라보고 있다. 별 내용 없는 대화라도 지금은 서로를 알아갈 기회다.",
                ChoiceLabel = "곁에 앉아 대화를 이어간다",
                ResultText = "벤자민과 조금 가까워졌다. 호감도 +1.",
                TimeCost = 1,
                WillCost = 1,
                Trend = TrendType.Social,
                RepeatDaily = true,
                SoftStat = TrendType.Social,
                SoftRequirement = 0,
                HardRequirement = HardRequirement.None,
                Effect = EffectType.BenjaminAffinity
            });

            encounters.Add(new SampleEncounter
            {
                Id = "oasis_water_test",
                LocationId = "oasis",
                Title = "오아시스의 물을 조사한다",
                Body = "수면과 주변 퇴적층을 비교하면 이곳의 물이 얼마나 안정적인지 단서를 얻을 수 있을 것 같다.",
                ChoiceLabel = "장기적인 수질 상태를 분석한다",
                ResultText = "이 오아시스만으로 장기간 버티기는 어렵다는 단서를 확보했다. [정보: 장기 거주 위험] 획득.",
                TimeCost = 2,
                WillCost = 1,
                Trend = TrendType.Technical,
                RepeatDaily = false,
                SoftStat = TrendType.Technical,
                SoftRequirement = 3,
                HardRequirement = HardRequirement.None,
                Effect = EffectType.LearnWaterRisk
            });

            encounters.Add(new SampleEncounter
            {
                Id = "oasis_carry_water",
                LocationId = "oasis",
                Title = "물을 옮기는 일을 돕는다",
                Body = "사람들이 거주지로 물을 옮기고 있다. 특별한 기술은 필요 없지만 몸을 써야 하는 일이다.",
                ChoiceLabel = "물 운반을 돕는다",
                ResultText = "몸은 피곤하지만 필요한 일을 끝냈다.",
                TimeCost = 2,
                WillCost = 1,
                Trend = TrendType.Personal,
                RepeatDaily = true,
                SoftStat = TrendType.Personal,
                SoftRequirement = 0,
                HardRequirement = HardRequirement.None,
                Effect = EffectType.None
            });

            encounters.Add(new SampleEncounter
            {
                Id = "settlement_fay",
                LocationId = "settlement",
                Title = "페이에게 수송선 이야기를 꺼낸다",
                Body = "페이는 전자 장비를 만지작거리며 시간을 보내고 있다. 수송선을 고칠 생각이 있다면 이 사람의 전문성이 필요할 것이다.",
                ChoiceLabel = "수송선 정비를 함께 해달라고 부탁한다",
                ResultText = "페이가 수송선 상태를 함께 살펴보기로 했다. [동료: 기술자] 확보.",
                TimeCost = 1,
                WillCost = 1,
                Trend = TrendType.Social,
                RepeatDaily = false,
                SoftStat = TrendType.Social,
                SoftRequirement = 2,
                HardRequirement = HardRequirement.None,
                Effect = EffectType.RecruitTechnician
            });

            encounters.Add(new SampleEncounter
            {
                Id = "ship_inspect",
                LocationId = "ship",
                Title = "방호 구역을 점검한다",
                Body = "수송선 외벽과 방호 패널을 눈으로 확인한다. 지금 당장 고치지 않더라도 현재 상태를 파악할 수 있다.",
                ChoiceLabel = "파손 부위를 확인한다",
                ResultText = "방호 구역의 상태를 확인했다. 본격적인 수리는 전문가가 필요하다.",
                TimeCost = 1,
                WillCost = 0,
                Trend = TrendType.Technical,
                RepeatDaily = true,
                SoftStat = TrendType.Technical,
                SoftRequirement = 0,
                HardRequirement = HardRequirement.None,
                Effect = EffectType.None
            });

            encounters.Add(new SampleEncounter
            {
                Id = "ship_repair_defense",
                LocationId = "ship",
                Title = "방호 패널을 수리한다",
                Body = "수송선의 방호 패널을 단계적으로 복구한다. 페이와 같은 기술자의 도움이 있어야 안전하게 작업할 수 있다.",
                ChoiceLabel = "방호 구역 수리를 진행한다",
                ResultText = "방호 구역의 복구 단계가 1 상승했다.",
                TimeCost = 3,
                WillCost = 1,
                Trend = TrendType.Technical,
                RepeatDaily = true,
                SoftStat = TrendType.Technical,
                SoftRequirement = 2,
                HardRequirement = HardRequirement.TechnicianCompanion,
                Effect = EffectType.RepairDefense
            });

            encounters.Add(new SampleEncounter
            {
                Id = "graveyard_benjamin_persuasion",
                LocationId = "graveyard",
                Title = "벤자민에게 대피 이야기를 꺼낸다",
                Body = "벤자민은 쉽게 이곳을 떠날 사람이 아니다. 하지만 충분히 가까워졌고 행성의 위험을 알고 있다면, 지금이 말을 꺼낼 기회일 수 있다.",
                ChoiceLabel = "위험을 설명하고 수송선으로 대피하자고 설득한다",
                ResultText = "벤자민은 한참 침묵한 뒤 고개를 끄덕였다. 설득 기회 하나에 성공했다.",
                TimeCost = 1,
                WillCost = 1,
                Trend = TrendType.Social,
                RepeatDaily = false,
                SoftStat = TrendType.Social,
                SoftRequirement = 3,
                HardRequirement = HardRequirement.BenjaminPersuasionReady,
                Effect = EffectType.PersuadeBenjamin
            });
        }

        private List<SampleEncounter> GetLocationEncounters(string locationId)
        {
            List<SampleEncounter> list = new List<SampleEncounter>();
            foreach (SampleEncounter encounter in encounters)
            {
                if (!encounter.IsGlobal && encounter.LocationId == locationId)
                    list.Add(encounter);
            }
            return list;
        }

        private SampleEncounter FindEncounter(string id)
        {
            foreach (SampleEncounter encounter in encounters)
            {
                if (encounter.Id == id)
                    return encounter;
            }
            return null;
        }

        private bool LocationHasAvailableEncounter(string locationId)
        {
            foreach (SampleEncounter encounter in encounters)
            {
                if (!encounter.IsGlobal && encounter.LocationId == locationId && IsRepeatAvailable(encounter))
                    return true;
            }
            return false;
        }

        private bool HasAvailableGlobalEncounter()
        {
            foreach (SampleEncounter encounter in encounters)
            {
                if (encounter.IsGlobal && IsRepeatAvailable(encounter))
                    return true;
            }
            return false;
        }

        private bool LocationHasNpcMarker(string locationId)
        {
            if (locationId == "oasis")
                return !benjaminPersuaded;
            if (locationId == "settlement")
                return !technicianCompanion;
            if (locationId == "graveyard")
                return benjaminAffinity > 0 && !benjaminPersuaded;
            return false;
        }

        private string EncounterCharacterName(SampleEncounter encounter)
        {
            if (encounter == null)
                return string.Empty;
            if (encounter.Id.Contains("benjamin"))
                return "벤자민";
            if (encounter.Id.Contains("fay"))
                return "페이";
            return string.Empty;
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

            if (hoveredNode != null && nodeBaseScales.TryGetValue(hoveredNode, out Vector3 previousScale))
                hoveredNode.transform.localScale = previousScale;

            hoveredNode = node;

            if (hoveredNode != null)
            {
                if (!nodeBaseScales.ContainsKey(hoveredNode))
                    nodeBaseScales[hoveredNode] = hoveredNode.transform.localScale;

                hoveredNode.transform.localScale = nodeBaseScales[hoveredNode] * 1.06f;
                statusMessage = $"{LocationName(hoveredNode.LocationId)} — 클릭해서 들어가기";
            }
        }

        private void EnsureStyles()
        {
            if (titleStyle != null)
                return;

            darkTexture = MakeTexture(new Color(0.055f, 0.045f, 0.038f, 0.96f));
            cardTexture = MakeTexture(new Color(0.16f, 0.14f, 0.12f, 0.98f));
            disabledCardTexture = MakeTexture(new Color(0.11f, 0.10f, 0.095f, 0.96f));
            accentTexture = MakeTexture(new Color(0.82f, 0.45f, 0.18f, 1f));
            blueTexture = MakeTexture(new Color(0.17f, 0.30f, 0.49f, 1f));
            oasisTexture = MakeTexture(new Color(0.12f, 0.40f, 0.43f, 1f));
            settlementTexture = MakeTexture(new Color(0.48f, 0.32f, 0.19f, 1f));
            graveyardTexture = MakeTexture(new Color(0.20f, 0.21f, 0.26f, 1f));
            shipTexture = MakeTexture(new Color(0.24f, 0.32f, 0.38f, 1f));

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 28,
                fontStyle = FontStyle.Bold,
                wordWrap = true,
                normal = { textColor = Color.white }
            };

            headingStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 19,
                fontStyle = FontStyle.Bold,
                wordWrap = true,
                normal = { textColor = Color.white }
            };

            bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                wordWrap = true,
                normal = { textColor = new Color(0.94f, 0.94f, 0.94f) }
            };

            smallStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                wordWrap = true,
                normal = { textColor = new Color(0.78f, 0.78f, 0.78f) }
            };

            centeredStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 17,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
                normal = { textColor = Color.white }
            };

            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                wordWrap = true,
                alignment = TextAnchor.MiddleCenter
            };

            cardStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(14, 14, 14, 14),
                normal = { background = cardTexture }
            };

            cardDisabledStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(14, 14, 14, 14),
                normal = { background = disabledCardTexture }
            };

            badgeStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { background = accentTexture, textColor = Color.white }
            };

            locationTitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 17,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };

            portraitStyle = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { background = blueTexture }
            };
        }

        private Texture2D MakeTexture(Color color)
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.name = "SandPlanet_Prototype02_RuntimeColor";
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }
    }
}
