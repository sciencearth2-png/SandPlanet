using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SandPlanet.Prototype
{
    /// <summary>
    /// TEMP playable vertical slice for SandPlanet Prototype 0.1.
    ///
    /// Purpose:
    /// - prove the 3D planet hub -> location -> 2D encounter loop,
    /// - make Day / Time / Willpower / Personal-Social-Technical growth tangible,
    /// - demonstrate relationship, quest-based persuasion and one ship facility.
    ///
    /// Content values in this class are prototype-only. They are intentionally
    /// kept together so we can change the game feel quickly before moving the
    /// content to ScriptableObject/data files.
    /// </summary>
    public sealed class SandPlanetPrototypeController : MonoBehaviour
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

        private ScreenMode mode = ScreenMode.Setup;
        private string currentLocationId;
        private SampleEncounter currentEncounter;

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

        // Prototype NPC / quest state.
        private int benjaminAffinity;
        private bool waterRiskKnown;
        private bool technicianCompanion;
        private bool benjaminPersuaded;
        private bool smokeInvestigated;

        // Prototype ship facility state: 0 broken, 1 emergency repair, 2 functional, 3 reinforced.
        private int defenseState;

        private string statusMessage = "시작 능력치를 배분하세요.";
        private string dayGrowthMessage = string.Empty;

        private GUIStyle titleStyle;
        private GUIStyle headingStyle;
        private GUIStyle bodyStyle;
        private GUIStyle smallStyle;
        private GUIStyle centeredStyle;
        private GUIStyle buttonStyle;

        private void Awake()
        {
            BuildPrototypeContent();
            ResetRun();
        }

        private void Update()
        {
            if (mode != ScreenMode.Hub || Mouse.current == null || Camera.main == null)
                return;

            if (!Mouse.current.leftButton.wasPressedThisFrame)
                return;

            Vector2 mouse = Mouse.current.position.ReadValue();

            // Reserve the top HUD and right-side global encounter panel for IMGUI.
            if (mouse.y > Screen.height - 72f || mouse.x > Screen.width - 380f)
                return;

            Ray ray = Camera.main.ScreenPointToRay(mouse);
            if (!Physics.Raycast(ray, out RaycastHit hit, 500f))
                return;

            PrototypeLocationNode node = hit.collider.GetComponentInParent<PrototypeLocationNode>();
            if (node != null)
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
                    DrawHubOverlay();
                    break;
                case ScreenMode.Location:
                    DrawLocationScreen();
                    break;
                case ScreenMode.Encounter:
                    DrawEncounterScreen();
                    break;
            }

            DrawStatusMessage();
        }

        private void DrawSetupScreen()
        {
            float width = Mathf.Min(620f, Screen.width - 40f);
            Rect panel = new Rect((Screen.width - width) * 0.5f, 70f, width, 500f);
            GUI.Box(panel, string.Empty);

            GUILayout.BeginArea(new Rect(panel.x + 28f, panel.y + 24f, panel.width - 56f, panel.height - 48f));
            GUILayout.Label("SAND PLANET — PROTOTYPE 0.1", titleStyle);
            GUILayout.Space(8f);
            GUILayout.Label("시작 능력치 배분", headingStyle);
            GUILayout.Label("개인 / 대인 / 기술에 총 6포인트를 배분합니다. (각 능력치 최종 상한 20)", bodyStyle);
            GUILayout.Space(18f);

            DrawSetupStatRow("개인", ref setupPersonal);
            DrawSetupStatRow("대인", ref setupSocial);
            DrawSetupStatRow("기술", ref setupTechnical);

            int total = setupPersonal + setupSocial + setupTechnical;
            GUILayout.Space(12f);
            GUILayout.Label($"배분: {total} / 6", headingStyle);
            GUILayout.Space(18f);

            GUI.enabled = total == 6;
            if (GUILayout.Button("DAY 1 시작", buttonStyle, GUILayout.Height(48f)))
                StartRun();
            GUI.enabled = true;

            GUILayout.Space(14f);
            GUILayout.Label("프로토타입 목표: 3D 행성 허브 → 장소 → 인카운터 → 시간/의지 소비 → 하루 성장의 감각을 검증합니다.", smallStyle);
            GUILayout.EndArea();
        }

        private void DrawSetupStatRow(string label, ref int value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, headingStyle, GUILayout.Width(120f));

            if (GUILayout.Button("-", buttonStyle, GUILayout.Width(54f), GUILayout.Height(36f)) && value > 0)
                value--;

            GUILayout.Label(value.ToString(), centeredStyle, GUILayout.Width(70f), GUILayout.Height(36f));

            int total = setupPersonal + setupSocial + setupTechnical;
            GUI.enabled = total < 6 && value < MaxStat;
            if (GUILayout.Button("+", buttonStyle, GUILayout.Width(54f), GUILayout.Height(36f)))
                value++;
            GUI.enabled = true;

            GUILayout.EndHorizontal();
            GUILayout.Space(8f);
        }

        private void DrawHud()
        {
            GUI.Box(new Rect(0f, 0f, Screen.width, 68f), string.Empty);

            GUILayout.BeginArea(new Rect(18f, 9f, Screen.width - 36f, 54f));
            GUILayout.BeginHorizontal();
            GUILayout.Label($"DAY {day} / {FinalGameDays}", headingStyle, GUILayout.Width(145f));
            GUILayout.Label($"{currentHour:00}:00", headingStyle, GUILayout.Width(95f));
            GUILayout.Label($"의지 {currentWillpower}/{maxWillpower}", headingStyle, GUILayout.Width(130f));
            GUILayout.Label($"개인 {personal}   대인 {social}   기술 {technical}", bodyStyle, GUILayout.Width(270f));
            GUILayout.Label($"오늘 경향  개인 {personalTrend} / 대인 {socialTrend} / 기술 {technicalTrend}", smallStyle);
            GUILayout.FlexibleSpace();

            GUI.enabled = currentWillpower < maxWillpower && CanSpendTime(NapHours);
            if (GUILayout.Button("낮잠 2h / 의지 +1", buttonStyle, GUILayout.Width(155f), GUILayout.Height(38f)))
                TakeNap();
            GUI.enabled = true;

            if (GUILayout.Button("하루 종료", buttonStyle, GUILayout.Width(100f), GUILayout.Height(38f)))
                EndDay();

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void DrawHubOverlay()
        {
            Rect panel = new Rect(Screen.width - 365f, 82f, 345f, Mathf.Min(510f, Screen.height - 150f));
            GUI.Box(panel, string.Empty);

            GUILayout.BeginArea(new Rect(panel.x + 16f, panel.y + 14f, panel.width - 32f, panel.height - 28f));
            GUILayout.Label("행성 허브", titleStyle);
            GUILayout.Label("3D 랜드마크를 클릭하면 장소 화면으로 들어갑니다.", bodyStyle);
            GUILayout.Space(8f);
            GUILayout.Label("행성에서 바로 실행 가능한 인카운터", headingStyle);

            bool any = false;
            foreach (SampleEncounter encounter in encounters)
            {
                if (!encounter.IsGlobal)
                    continue;

                any = true;
                DrawEncounterCard(encounter);
            }

            if (!any)
                GUILayout.Label("현재 행성 인카운터 없음", smallStyle);

            GUILayout.Space(12f);
            GUILayout.Label("프로토타입 상태", headingStyle);
            GUILayout.Label($"벤자민 호감도: {benjaminAffinity}/5", smallStyle);
            GUILayout.Label($"기술자 동료: {(technicianCompanion ? "확보" : "미확보")}", smallStyle);
            GUILayout.Label($"벤자민 설득: {(benjaminPersuaded ? "성공" : "미완료")}", smallStyle);
            GUILayout.Label($"수송선 방호: {FacilityLabel(defenseState)} ({defenseState}/3)", smallStyle);
            GUILayout.EndArea();
        }

        private void DrawLocationScreen()
        {
            Rect panel = new Rect(70f, 92f, Screen.width - 140f, Screen.height - 180f);
            GUI.Box(panel, string.Empty);

            GUILayout.BeginArea(new Rect(panel.x + 24f, panel.y + 20f, panel.width - 48f, panel.height - 40f));
            GUILayout.BeginHorizontal();
            GUILayout.Label(LocationName(currentLocationId), titleStyle);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("행성으로 돌아가기", buttonStyle, GUILayout.Width(170f), GUILayout.Height(38f)))
                mode = ScreenMode.Hub;
            GUILayout.EndHorizontal();

            GUILayout.Label("현재 장소에서 실행 가능한 인카운터", headingStyle);
            GUILayout.Space(8f);

            bool any = false;
            foreach (SampleEncounter encounter in encounters)
            {
                if (encounter.IsGlobal || encounter.LocationId != currentLocationId)
                    continue;

                any = true;
                DrawEncounterCard(encounter);
            }

            if (!any)
                GUILayout.Label("이 장소에는 아직 프로토타입 인카운터가 없습니다.", bodyStyle);

            GUILayout.FlexibleSpace();
            GUILayout.Label("※ 장소 화면은 최종적으로 2D 장소 이미지 + 캐릭터/인카운터 카드 형태로 교체할 예정입니다.", smallStyle);
            GUILayout.EndArea();
        }

        private void DrawEncounterCard(SampleEncounter encounter)
        {
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label(encounter.Title, headingStyle);
            GUILayout.Label($"시간 {encounter.TimeCost}h / 의지 {encounter.WillCost}{SoftRequirementPreview(encounter)}", smallStyle);

            string availability = EncounterAvailabilityText(encounter);
            if (!string.IsNullOrEmpty(availability))
                GUILayout.Label(availability, smallStyle);

            GUI.enabled = IsEncounterSelectable(encounter);
            if (GUILayout.Button("인카운터 열기", buttonStyle, GUILayout.Height(34f)))
            {
                currentEncounter = encounter;
                mode = ScreenMode.Encounter;
            }
            GUI.enabled = true;
            GUILayout.EndVertical();
            GUILayout.Space(7f);
        }

        private void DrawEncounterScreen()
        {
            if (currentEncounter == null)
            {
                mode = ScreenMode.Hub;
                return;
            }

            GUI.Box(new Rect(0f, 68f, Screen.width, Screen.height - 68f), string.Empty);

            float width = Mathf.Min(760f, Screen.width - 80f);
            Rect panel = new Rect((Screen.width - width) * 0.5f, 110f, width, Mathf.Min(560f, Screen.height - 160f));
            GUI.Box(panel, string.Empty);

            GUILayout.BeginArea(new Rect(panel.x + 30f, panel.y + 26f, panel.width - 60f, panel.height - 52f));
            GUILayout.Label(currentEncounter.Title, titleStyle);
            GUILayout.Space(12f);
            GUILayout.Label(currentEncounter.Body, bodyStyle);
            GUILayout.Space(22f);

            int extraWill = SoftRequirementExtraWill(currentEncounter);
            string requirementText = RequirementDescription(currentEncounter);
            if (!string.IsNullOrEmpty(requirementText))
                GUILayout.Label(requirementText, smallStyle);

            string cost = $"시간 {currentEncounter.TimeCost}h / 의지 {currentEncounter.WillCost}";
            if (extraWill > 0)
                cost += $" + 강행 {extraWill}";

            bool hardMet = HardRequirementMet(currentEncounter);
            bool resourceMet = HasResources(currentEncounter.TimeCost, currentEncounter.WillCost + extraWill);
            bool repeatMet = IsRepeatAvailable(currentEncounter);

            GUI.enabled = hardMet && resourceMet && repeatMet;
            if (GUILayout.Button($"{currentEncounter.ChoiceLabel}\n({cost})", buttonStyle, GUILayout.Height(68f)))
                ExecuteEncounter(currentEncounter);
            GUI.enabled = true;

            GUILayout.Space(10f);
            if (GUILayout.Button("돌아가기", buttonStyle, GUILayout.Height(42f)))
                ReturnFromEncounter();

            GUILayout.EndArea();
        }

        private void DrawStatusMessage()
        {
            Rect rect = new Rect(18f, Screen.height - 82f, Mathf.Min(920f, Screen.width - 36f), 60f);
            GUI.Box(rect, string.Empty);
            GUI.Label(new Rect(rect.x + 14f, rect.y + 8f, rect.width - 28f, rect.height - 16f), statusMessage, bodyStyle);
        }

        private void DrawCompleteScreen()
        {
            float width = Mathf.Min(720f, Screen.width - 60f);
            Rect panel = new Rect((Screen.width - width) * 0.5f, 70f, width, 580f);
            GUI.Box(panel, string.Empty);

            GUILayout.BeginArea(new Rect(panel.x + 30f, panel.y + 24f, panel.width - 60f, panel.height - 48f));
            GUILayout.Label("PROTOTYPE 0.1 COMPLETE", titleStyle);
            GUILayout.Label("정식 게임은 21일이지만 현재 샌드박스 콘텐츠는 3일에서 종료됩니다.", bodyStyle);
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
            GUILayout.Label("체크할 것", headingStyle);
            GUILayout.Label("• 시간과 의지 중 무엇을 아껴야 할지 고민이 생겼는가?\n• 장소를 고르고 인카운터를 소비하는 흐름이 자연스러운가?\n• 대인/기술/개인 행동의 차이가 체감되는가?\n• 호감도와 설득을 별개로 두는 것이 이해되는가?\n• 수송선 수리가 사람 관계와 연결되는가?", bodyStyle);
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
            if (currentEncounter != null && currentEncounter.IsGlobal)
                mode = ScreenMode.Hub;
            else
                mode = string.IsNullOrEmpty(currentLocationId) ? ScreenMode.Hub : ScreenMode.Location;

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
            {
                // TEMP rule: a day with no tagged actions still grows Personal so that
                // the prototype preserves the 'one stat always grows per day' rule.
                winner = TrendType.Personal;
            }
            else if (TrendValue(lastTrend) == max)
            {
                // TEMP tie breaker: the last-used tendency among tied values wins.
                winner = lastTrend;
            }
            else if (personalTrend == max)
            {
                winner = TrendType.Personal;
            }
            else if (socialTrend == max)
            {
                winner = TrendType.Social;
            }
            else
            {
                winner = TrendType.Technical;
            }

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

            // TEMP formula for Prototype 0.1: missing 1 stat = 1 additional Willpower.
            return gap;
        }

        private string SoftRequirementPreview(SampleEncounter encounter)
        {
            if (encounter.SoftRequirement <= 0)
                return string.Empty;

            return $" / {TrendName(encounter.SoftStat)} {encounter.SoftRequirement}";
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
                string soft = $"Soft: {TrendName(encounter.SoftStat)} {encounter.SoftRequirement}";
                if (gap > 0)
                    soft += $" (부족 {gap} → 의지 {gap}로 강행 가능 / TEMP 공식)";
                parts.Add(soft);
            }

            switch (encounter.HardRequirement)
            {
                case HardRequirement.TechnicianCompanion:
                    parts.Add($"Hard: 기술자 동료 필요 — {(technicianCompanion ? "충족" : "미충족")}");
                    break;
                case HardRequirement.BenjaminPersuasionReady:
                    parts.Add($"Hard: 벤자민 호감도 1 이상 + 수질 위험 정보 — {((benjaminAffinity >= 1 && waterRiskKnown) ? "충족" : "미충족")}");
                    break;
            }

            return string.Join("\n", parts);
        }

        private string EncounterAvailabilityText(SampleEncounter encounter)
        {
            if (!IsRepeatAvailable(encounter))
                return encounter.RepeatDaily ? "오늘 이미 실행함" : "완료됨";

            if (!HardRequirementMet(encounter))
                return RequirementDescription(encounter);

            int extra = SoftRequirementExtraWill(encounter);
            if (!HasResources(encounter.TimeCost, encounter.WillCost + extra))
                return "현재 시간 또는 의지 부족";

            return string.Empty;
        }

        private bool IsEncounterSelectable(SampleEncounter encounter)
        {
            return IsRepeatAvailable(encounter)
                   && HardRequirementMet(encounter)
                   && HasResources(encounter.TimeCost, encounter.WillCost + SoftRequirementExtraWill(encounter));
        }

        private bool IsRepeatAvailable(SampleEncounter encounter)
        {
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
            statusMessage = "DAY 1. 3D 행성에서 장소를 선택하거나 행성 인카운터를 확인하세요.";
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
            dayGrowthMessage = string.Empty;
            statusMessage = "시작 능력치를 배분하세요.";
            mode = ScreenMode.Setup;
        }

        private void BuildPrototypeContent()
        {
            encounters.Clear();

            encounters.Add(new SampleEncounter
            {
                Id = "global_smoke",
                IsGlobal = true,
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

        private void EnsureStyles()
        {
            if (titleStyle != null)
                return;

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 27,
                fontStyle = FontStyle.Bold,
                wordWrap = true
            };

            headingStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 19,
                fontStyle = FontStyle.Bold,
                wordWrap = true
            };

            bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                wordWrap = true
            };

            smallStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                wordWrap = true
            };

            centeredStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 15,
                wordWrap = true
            };
        }
    }
}
