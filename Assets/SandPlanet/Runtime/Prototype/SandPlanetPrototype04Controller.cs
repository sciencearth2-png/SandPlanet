using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SandPlanet.Prototype.DataDriven;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SandPlanet.Prototype
{
    /// <summary>
    /// Prototype 0.4 — CSV driven 21-day vertical slice.
    /// Content is static CSV data; only runtime state lives in this component.
    /// </summary>
    public sealed class SandPlanetPrototype04Controller : MonoBehaviour
    {
        private const int DayStartHour = 8;
        private const int DayEndHour = 22;
        private const int MaxWillDefault = 5;
        private const int XpPerLevel = 6;

        [Header("Generated CSV")]
        [SerializeField] private TextAsset[] csvAssets;

        [Header("World")]
        [SerializeField] private Camera hubCamera;
        [SerializeField] private GameObject planetHubRoot;
        [SerializeField] private GameObject shipInteriorHubRoot;

        [Header("HUD")]
        [SerializeField] private Text hudText;
        [SerializeField] private Text questTrackerText;
        [SerializeField] private Text logText;
        [SerializeField] private Button endDayButton;

        [Header("Location UI")]
        [SerializeField] private GameObject locationPanel;
        [SerializeField] private Text locationTitleText;
        [SerializeField] private Text locationHintText;
        [SerializeField] private Transform targetRoot;
        [SerializeField] private Transform interactionRoot;
        [SerializeField] private Button targetTemplate;
        [SerializeField] private Button interactionTemplate;
        [SerializeField] private Button backButton;

        [Header("Modal UI")]
        [SerializeField] private GameObject modalPanel;
        [SerializeField] private Text modalTitleText;
        [SerializeField] private Text modalBodyText;
        [SerializeField] private Transform modalButtonRoot;
        [SerializeField] private Button modalButtonTemplate;

        private SandPlanetContent04 content;
        private int day = 1;
        private int hour = DayStartHour;
        private int will = MaxWillDefault;
        private int maxWill = MaxWillDefault;
        private int personalLevel = 2, personalXp;
        private int socialLevel = 2, socialXp;
        private int technicalLevel = 2, technicalXp;

        private readonly Dictionary<string, string> states = new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly Dictionary<string, int> affinity = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly Dictionary<string, string> questStatus = new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly Dictionary<string, string> questStep = new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly HashSet<string> eventsOccurred = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> interactionsUsed = new HashSet<string>(StringComparer.Ordinal);
        private readonly Dictionary<string, int> interactionLastDay = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly HashSet<string> triggersUsed = new HashSet<string>(StringComparer.Ordinal);
        private readonly Dictionary<string, int> triggerLastDay = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly Queue<SandPlanetEvent04> eventQueue = new Queue<SandPlanetEvent04>();
        private readonly List<string> recentLog = new List<string>();

        private string currentLocationId;
        private SandPlanetInteraction04 activeInteraction;
        private bool modalBusy;
        private bool evaluatingStateTriggers;

        public void Configure(
            TextAsset[] dataFiles, Camera camera, GameObject planetRoot, GameObject shipRoot,
            Text hud, Text questTracker, Text log, Button endDay,
            GameObject locationPanelObject, Text locationTitle, Text locationHint,
            Transform targetButtonRoot, Transform interactionButtonRoot,
            Button targetButtonPrefab, Button interactionButtonPrefab, Button backToHub,
            GameObject modalPanelObject, Text modalTitle, Text modalBody,
            Transform choiceButtonRoot, Button choiceButtonPrefab)
        {
            csvAssets = dataFiles;
            hubCamera = camera;
            planetHubRoot = planetRoot;
            shipInteriorHubRoot = shipRoot;
            hudText = hud;
            questTrackerText = questTracker;
            logText = log;
            endDayButton = endDay;
            locationPanel = locationPanelObject;
            locationTitleText = locationTitle;
            locationHintText = locationHint;
            targetRoot = targetButtonRoot;
            interactionRoot = interactionButtonRoot;
            targetTemplate = targetButtonPrefab;
            interactionTemplate = interactionButtonPrefab;
            backButton = backToHub;
            modalPanel = modalPanelObject;
            modalTitleText = modalTitle;
            modalBodyText = modalBody;
            modalButtonRoot = choiceButtonRoot;
            modalButtonTemplate = choiceButtonPrefab;
        }

        private void Awake()
        {
            content = SandPlanetContent04.Load(csvAssets);
            InitializeState();
            WireButtons();
            SetVisible(locationPanel, false);
            SetVisible(modalPanel, false);
            RefreshHub();
            RefreshUi();
        }

        private void Start()
        {
            ProcessTriggers("GAME_START");
            ProcessTriggers("DAY_START");
            RefreshUi();
            ShowQueuedEvent();
        }

        private void Update()
        {
            if (modalBusy || !string.IsNullOrEmpty(currentLocationId) || !Input.GetMouseButtonDown(0))
                return;
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;
            Camera cam = hubCamera != null ? hubCamera : Camera.main;
            if (cam == null) return;
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out RaycastHit hit, 500f)) return;
            PrototypeLocationNode node = hit.collider.GetComponentInParent<PrototypeLocationNode>();
            if (node != null) OpenLocation(node.LocationId);
        }

        private void InitializeState()
        {
            foreach (SandPlanetStateDefinition04 def in content.States.Values)
                states[def.Id] = NormalizeState(def.DefaultValue, def.DataType);
            foreach (SandPlanetCharacter04 character in content.Characters.Values)
                affinity[character.Id] = 0;
            foreach (SandPlanetQuest04 quest in content.Quests.Values)
            {
                questStatus[quest.Id] = "LOCKED";
                questStep[quest.Id] = string.Empty;
            }
            Log($"CSV 로드: 장소 {content.Locations.Count}, 퀘스트 {content.Quests.Count}, 이벤트 {content.Events.Count}");
        }

        private void WireButtons()
        {
            if (endDayButton != null) { endDayButton.onClick.RemoveAllListeners(); endDayButton.onClick.AddListener(EndDay); }
            if (backButton != null) { backButton.onClick.RemoveAllListeners(); backButton.onClick.AddListener(CloseLocation); }
            if (targetTemplate != null) targetTemplate.gameObject.SetActive(false);
            if (interactionTemplate != null) interactionTemplate.gameObject.SetActive(false);
            if (modalButtonTemplate != null) modalButtonTemplate.gameObject.SetActive(false);
        }

        private void OpenLocation(string locationId)
        {
            if (!content.Locations.TryGetValue(locationId, out SandPlanetLocation04 location) || !location.Active) return;
            currentLocationId = locationId;
            SetVisible(locationPanel, true);
            if (locationTitleText != null) locationTitleText.text = location.Name;
            if (locationHintText != null) locationHintText.text = "사람/사물을 선택하면 현재 가능한 행동만 표시됩니다.";
            RefreshTargets();
            ClearDynamic(interactionRoot, interactionTemplate);
            Log("장소 진입: " + location.Name);
            RefreshUi();
        }

        private void CloseLocation()
        {
            currentLocationId = null;
            activeInteraction = null;
            SetVisible(locationPanel, false);
            ClearDynamic(targetRoot, targetTemplate);
            ClearDynamic(interactionRoot, interactionTemplate);
            RefreshUi();
            ShowQueuedEvent();
        }

        private void RefreshTargets()
        {
            ClearDynamic(targetRoot, targetTemplate);
            if (string.IsNullOrEmpty(currentLocationId)) return;

            foreach (SandPlanetCharacter04 c in content.Characters.Values.Where(c => c.Active && GetCharacterLocation(c.Id) == currentLocationId).OrderBy(c => c.Name))
            {
                SandPlanetCharacter04 captured = c;
                CreateButton(targetTemplate, targetRoot, "인물  " + c.Name, true, () => SelectTarget("CHARACTER", captured.Id));
            }
            foreach (SandPlanetWorldTarget04 w in content.WorldTargets.Values.Where(w => w.Active && w.Clickable && w.LocationId == currentLocationId).OrderBy(w => w.Name))
            {
                SandPlanetWorldTarget04 captured = w;
                CreateButton(targetTemplate, targetRoot, "사물  " + w.Name, true, () => SelectTarget("WORLD_TARGET", captured.Id));
            }
        }

        private void SelectTarget(string type, string id)
        {
            List<SandPlanetInteraction04> list = content.Interactions
                .Where(i => i.Active && i.TargetType == type && i.TargetId == id && IsInteractionAvailable(i))
                .OrderByDescending(i => i.Priority).ThenBy(i => i.DisplayText).ToList();

            SandPlanetInteraction04 direct = list.FirstOrDefault(i => i.EntryMode == "DIRECT_CLICK");
            if (direct != null)
            {
                BeginInteraction(direct);
                return;
            }

            ClearDynamic(interactionRoot, interactionTemplate);
            if (locationHintText != null) locationHintText.text = GetTargetName(type, id) + " — 가능한 행동";
            if (list.Count == 0)
                CreateButton(interactionTemplate, interactionRoot, "현재 가능한 행동 없음", false, null);
            foreach (SandPlanetInteraction04 interaction in list)
            {
                SandPlanetInteraction04 captured = interaction;
                string prefix = interaction.EntryMode == "WORLD_MARKER" ? "◆ " : string.Empty;
                CreateButton(interactionTemplate, interactionRoot, prefix + interaction.DisplayText, true, () => BeginInteraction(captured));
            }
        }

        private void BeginInteraction(SandPlanetInteraction04 interaction)
        {
            activeInteraction = interaction;
            ShowChoiceSet(interaction.DisplayText, interaction.ChoiceSetId, false);
        }

        private void ShowChoiceSet(string title, string choiceSetId, bool eventChoice)
        {
            IReadOnlyList<SandPlanetChoice04> choices = content.GetChoices(choiceSetId);
            modalBusy = true;
            SetVisible(modalPanel, true);
            ClearDynamic(modalButtonRoot, modalButtonTemplate);
            if (modalTitleText != null) modalTitleText.text = title;
            if (modalBodyText != null) modalBodyText.text = eventChoice ? "즉시 판단이 필요한 사건입니다." : "시간과 결과를 확인하고 선택하세요.";

            foreach (SandPlanetChoice04 choice in choices)
            {
                ChoiceCheck check = CheckChoice(choice);
                SandPlanetChoice04 captured = choice;
                string cost = choice.TimeCost > 0 ? $"  [{choice.TimeCost}h" : "  [0h";
                if (check.TotalWill > 0) cost += $" / 의지 {check.TotalWill}";
                cost += "]";
                string label = choice.Text + cost + (check.CanExecute ? string.Empty : "\n<" + check.Reason + ">");
                CreateButton(modalButtonTemplate, modalButtonRoot, label, check.CanExecute, () => ExecuteChoice(captured));
            }
            CreateButton(modalButtonTemplate, modalButtonRoot, "취소", !eventChoice, CloseModal);
        }

        private ChoiceCheck CheckChoice(SandPlanetChoice04 choice)
        {
            if (choice == null || !choice.Active) return new ChoiceCheck(false, 0, "비활성");
            if (hour + choice.TimeCost > DayEndHour) return new ChoiceCheck(false, 0, "오늘 남은 시간 부족");
            if (!EvaluatePair(choice.HardConditionLogic, choice.HardCondition1, choice.HardCondition2)) return new ChoiceCheck(false, 0, "조건 미충족");
            int extraWill = 0;
            if (!string.IsNullOrEmpty(choice.SoftStat) && choice.SoftStat != "NONE" && choice.SoftRequirement > 0)
                extraWill = Math.Max(0, choice.SoftRequirement - GetStatLevel(choice.SoftStat)); // TEMP 0.4: Lv 1 부족 = 의지 1.
            int total = choice.WillCost + extraWill;
            if (will < total) return new ChoiceCheck(false, total, "의지 부족");
            return new ChoiceCheck(true, total, string.Empty);
        }

        private void ExecuteChoice(SandPlanetChoice04 choice)
        {
            ChoiceCheck check = CheckChoice(choice);
            if (!check.CanExecute) return;
            hour += choice.TimeCost;
            will = Mathf.Max(0, will - check.TotalWill);
            ApplyResult(choice.Result1);
            ApplyResult(choice.Result2);
            ApplyResult(choice.Result3);

            if (activeInteraction != null)
            {
                interactionsUsed.Add(activeInteraction.Id);
                interactionLastDay[activeInteraction.Id] = day;
                ProcessTriggers("INTERACTION");
            }

            string result = string.IsNullOrEmpty(choice.ResultText) ? "선택 완료" : choice.ResultText;
            Log(result);
            activeInteraction = null;
            CloseModal();
            RefreshUi();
            if (!string.IsNullOrEmpty(currentLocationId)) RefreshTargets();
            ShowQueuedEvent();
        }

        private void ApplyResult(SandPlanetResult04 result)
        {
            if (result == null || string.IsNullOrEmpty(result.Type)) return;
            switch (result.Type)
            {
                case "ADD_XP": AddXp(result.TargetId, ParseInt(result.Value)); break;
                case "ADD_AFFINITY": affinity[result.TargetId] = Mathf.Clamp(GetAffinity(result.TargetId) + ParseInt(result.Value), 0, 5); break;
                case "ADD_WILL": will = Mathf.Clamp(will + ParseInt(result.Value), 0, maxWill); break;
                case "SET_STATE": SetState(result.TargetId, result.Value); break;
                case "ADD_STATE": SetState(result.TargetId, (ParseInt(GetState(result.TargetId)) + ParseInt(result.Value)).ToString(CultureInfo.InvariantCulture)); break;
                case "EMIT_EVENT": FireEvent(result.TargetId); break;
                case "ACTIVATE_QUEST": ActivateQuest(result.TargetId); break;
                case "COMPLETE_QUEST": CompleteQuest(result.TargetId); break;
                case "FAIL_QUEST": SetQuestStatus(result.TargetId, "FAILED"); break;
                case "SET_QUEST_STEP": SetQuestStep(result.TargetId, result.Value); break;
                default: Debug.LogWarning("[SandPlanet 0.4] Unsupported result type: " + result.Type); break;
            }
        }

        private void AddXp(string stat, int amount)
        {
            if (stat == "PERSONAL") AddXpTo(ref personalLevel, ref personalXp, amount);
            else if (stat == "SOCIAL" || stat == "INTERPERSONAL") AddXpTo(ref socialLevel, ref socialXp, amount);
            else if (stat == "TECHNICAL") AddXpTo(ref technicalLevel, ref technicalXp, amount);
        }

        private static void AddXpTo(ref int level, ref int xp, int amount)
        {
            xp += Math.Max(0, amount);
            while (xp >= XpPerLevel && level < 20) { xp -= XpPerLevel; level++; }
            if (level >= 20) xp = Mathf.Min(xp, XpPerLevel - 1);
        }

        private void FireEvent(string eventId)
        {
            if (string.IsNullOrEmpty(eventId) || !content.Events.TryGetValue(eventId, out SandPlanetEvent04 evt) || !evt.Active) return;
            eventsOccurred.Add(eventId);
            ApplyResult(evt.Result1);
            ApplyResult(evt.Result2);
            ApplyResult(evt.Result3);
            ProgressQuestsFromEvent(eventId);
            if (evt.PresentationMode != "SILENT" || !string.IsNullOrEmpty(evt.ChoiceSetId)) eventQueue.Enqueue(evt);
            Log("Event: " + evt.Name);
        }

        private void ProgressQuestsFromEvent(string eventId)
        {
            foreach (SandPlanetQuest04 quest in content.Quests.Values)
            {
                if (GetQuestStatus(quest.Id) != "ACTIVE") continue;
                string stepId = GetQuestStep(quest.Id);
                if (!content.QuestSteps.TryGetValue(stepId, out SandPlanetQuestStep04 step) || step.ProgressEventId != eventId) continue;
                if (step.OnProgressEvent == "SET_STEP" && !string.IsNullOrEmpty(step.NextStepId)) SetQuestStep(quest.Id, step.NextStepId);
                else if (step.OnProgressEvent == "COMPLETE_QUEST") CompleteQuest(quest.Id);
            }
        }

        private void ActivateQuest(string questId)
        {
            if (!content.Quests.TryGetValue(questId, out SandPlanetQuest04 quest)) return;
            if (GetQuestStatus(questId) == "ACTIVE" || GetQuestStatus(questId) == "COMPLETED") return;
            questStatus[questId] = "ACTIVE";
            questStep[questId] = quest.InitialStepId;
            Log("Quest 시작: " + quest.Title);
        }

        private void CompleteQuest(string questId)
        {
            if (!content.Quests.TryGetValue(questId, out SandPlanetQuest04 quest)) return;
            questStatus[questId] = "COMPLETED";
            Log("Quest 완료: " + quest.Title);
        }

        private void SetQuestStatus(string questId, string status)
        {
            if (questStatus.ContainsKey(questId)) questStatus[questId] = status;
        }

        private void SetQuestStep(string questId, string stepId)
        {
            if (questStatus.ContainsKey(questId) && content.QuestSteps.ContainsKey(stepId)) questStep[questId] = stepId;
        }

        private void ProcessTriggers(string timing)
        {
            foreach (SandPlanetTrigger04 trigger in content.Triggers.Where(t => t.Active && t.TriggerTiming == timing).OrderByDescending(t => t.Priority).ToList())
            {
                if (!RepeatAvailable(trigger.Id, trigger.RepeatRule, triggersUsed, triggerLastDay)) continue;
                if (!string.IsNullOrEmpty(trigger.LocationId) && trigger.LocationId != currentLocationId) continue;
                if (!EvaluatePair(trigger.ConditionLogic, trigger.Condition1, trigger.Condition2)) continue;
                triggersUsed.Add(trigger.Id);
                triggerLastDay[trigger.Id] = day;
                FireEvent(trigger.EventId);
            }
        }

        private bool IsInteractionAvailable(SandPlanetInteraction04 i)
        {
            if (day < i.OpenDay || day > i.CloseDay || !TimeSlotAllowed(i)) return false;
            if (!RepeatAvailable(i.Id, i.RepeatRule, interactionsUsed, interactionLastDay)) return false;
            if (i.InteractionType == "QUEST")
            {
                if (GetQuestStatus(i.QuestId) != "ACTIVE") return false;
                if (!string.IsNullOrEmpty(i.QuestStepId) && GetQuestStep(i.QuestId) != i.QuestStepId) return false;
            }
            return EvaluatePair(i.ConditionLogic, i.Condition1, i.Condition2);
        }

        private bool TimeSlotAllowed(SandPlanetInteraction04 i)
        {
            if (hour < 12) return i.Morning;
            if (hour < 17) return i.Afternoon;
            return i.Evening;
        }

        private bool RepeatAvailable(string id, string rule, HashSet<string> used, Dictionary<string, int> lastDay)
        {
            string r = string.IsNullOrEmpty(rule) ? "ONCE" : rule.ToUpperInvariant();
            if (r == "UNLIMITED") return true;
            if (r == "DAILY") return !lastDay.TryGetValue(id, out int d) || d != day;
            return !used.Contains(id);
        }

        private bool EvaluatePair(string logic, SandPlanetCondition04 a, SandPlanetCondition04 b)
        {
            bool hasA = a != null && !string.IsNullOrEmpty(a.Type);
            bool hasB = b != null && !string.IsNullOrEmpty(b.Type);
            if (!hasA && !hasB) return true;
            bool av = !hasA || EvaluateCondition(a);
            bool bv = !hasB || EvaluateCondition(b);
            return string.Equals(logic, "OR", StringComparison.OrdinalIgnoreCase) ? (hasA && av) || (hasB && bv) : av && bv;
        }

        private bool EvaluateCondition(SandPlanetCondition04 c)
        {
            if (c == null || string.IsNullOrEmpty(c.Type)) return true;
            string left;
            switch (c.Type)
            {
                case "STATE": left = GetState(c.Key); break;
                case "DAY": left = day.ToString(CultureInfo.InvariantCulture); break;
                case "TIME": left = hour.ToString(CultureInfo.InvariantCulture); break;
                case "AFFINITY": left = GetAffinity(c.Key).ToString(CultureInfo.InvariantCulture); break;
                case "QUEST_STATUS": left = GetQuestStatus(c.Key); break;
                case "QUEST_STEP": left = GetQuestStep(c.Key); break;
                case "EVENT_OCCURRED": left = eventsOccurred.Contains(c.Key) ? "TRUE" : "FALSE"; break;
                case "INTERACTION_DONE": left = interactionsUsed.Contains(c.Key) ? "TRUE" : "FALSE"; break;
                case "STAT_LEVEL": left = GetStatLevel(c.Key).ToString(CultureInfo.InvariantCulture); break;
                default: return false;
            }
            return Compare(left, c.Operator, c.Value);
        }

        private static bool Compare(string left, string op, string right)
        {
            if (int.TryParse(left, out int li) && int.TryParse(right, out int ri))
            {
                switch (op) { case "NE": return li != ri; case "GT": return li > ri; case "GE": return li >= ri; case "LT": return li < ri; case "LE": return li <= ri; default: return li == ri; }
            }
            return op == "NE" ? !string.Equals(left, right, StringComparison.OrdinalIgnoreCase) : string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
        }

        private void SetState(string id, string value)
        {
            if (string.IsNullOrEmpty(id)) return;
            states[id] = value ?? string.Empty;
            if (evaluatingStateTriggers) return;
            evaluatingStateTriggers = true;
            ProcessTriggers("STATE_CHANGE");
            evaluatingStateTriggers = false;
        }

        private string GetCharacterLocation(string characterId)
        {
            if (!content.Characters.TryGetValue(characterId, out SandPlanetCharacter04 character)) return string.Empty;
            string best = character.DefaultLocationId;
            int bestPriority = int.MinValue;
            foreach (SandPlanetSchedule04 s in content.Schedules)
            {
                if (!s.Active || s.CharacterId != characterId || day < s.OpenDay || day > s.CloseDay || !ScheduleTimeAllowed(s)) continue;
                if (!EvaluatePair(s.ConditionLogic, s.Condition1, s.Condition2)) continue;
                if (s.Priority >= bestPriority) { bestPriority = s.Priority; best = s.LocationId; }
            }
            return best;
        }

        private bool ScheduleTimeAllowed(SandPlanetSchedule04 s)
        {
            if (hour < 12) return s.Morning;
            if (hour < 17) return s.Afternoon;
            return s.Evening;
        }

        private void EndDay()
        {
            if (modalBusy) return;
            if (day >= 21)
            {
                ShowMessage("21일 결과", BuildDay21Summary());
                return;
            }
            ProcessTriggers("DAY_END");
            day++;
            hour = DayStartHour;
            will = Mathf.Min(maxWill, will + 2);
            CloseLocation();
            RefreshHub();
            Log($"Day {day} 시작 / 수면 회복 후 의지 {will}/{maxWill}");
            ProcessTriggers("DAY_START");
            RefreshUi();
            ShowQueuedEvent();
        }

        private void RefreshHub()
        {
            bool week3 = day >= 15;
            if (planetHubRoot != null) planetHubRoot.SetActive(!week3);
            if (shipInteriorHubRoot != null) shipInteriorHubRoot.SetActive(week3);
        }

        private void ShowQueuedEvent()
        {
            if (modalBusy || !string.IsNullOrEmpty(currentLocationId) || eventQueue.Count == 0) return;
            SandPlanetEvent04 evt = eventQueue.Dequeue();
            if (!string.IsNullOrEmpty(evt.ChoiceSetId))
            {
                ShowChoiceSet(string.IsNullOrEmpty(evt.Title) ? evt.Name : evt.Title, evt.ChoiceSetId, true);
                if (modalBodyText != null) modalBodyText.text = evt.Body;
                return;
            }
            ShowMessage(string.IsNullOrEmpty(evt.Title) ? evt.Name : evt.Title, evt.Body);
        }

        private void ShowMessage(string title, string body)
        {
            modalBusy = true;
            SetVisible(modalPanel, true);
            ClearDynamic(modalButtonRoot, modalButtonTemplate);
            if (modalTitleText != null) modalTitleText.text = title;
            if (modalBodyText != null) modalBodyText.text = body;
            CreateButton(modalButtonTemplate, modalButtonRoot, "계속", true, () => { CloseModal(); ShowQueuedEvent(); });
        }

        private void CloseModal()
        {
            modalBusy = false;
            SetVisible(modalPanel, false);
            ClearDynamic(modalButtonRoot, modalButtonTemplate);
        }

        private string BuildDay21Summary()
        {
            return "Excel 데이터 기준 Day 21 상태\n\n"
                   + $"거주지 보존 {GetState("STA_PRESERVE_SETTLEMENT")}\n"
                   + $"오아시스 보존 {GetState("STA_PRESERVE_OASIS")}\n"
                   + $"묘지 보존 {GetState("STA_PRESERVE_GRAVEYARD")}\n"
                   + $"수송선 외부 보존 {GetState("STA_PRESERVE_SHIP")}\n\n"
                   + $"지휘·항해 안정 {GetState("STA_W3_NAV_STABILITY")} / 보급 안정 {GetState("STA_W3_SUPPLY_STABILITY")}\n"
                   + $"기관·연구 안정 {GetState("STA_W3_TECH_STABILITY")} / 거주 안정 {GetState("STA_W3_HABIT_STABILITY")}\n\n"
                   + "최종 엔딩 판정식은 아직 TEMP/TBD입니다. 0.4는 Excel → CSV → 21일 상태 연결을 검증합니다.";
        }

        private void RefreshUi()
        {
            if (hudText != null)
                hudText.text = $"DAY {day:00}  {hour:00}:00   |   의지 {will}/{maxWill}   |   개인 Lv{personalLevel} {personalXp}/6   대인 Lv{socialLevel} {socialXp}/6   기술 Lv{technicalLevel} {technicalXp}/6   |   {(day >= 15 ? "수송선 내부" : "모래 행성")}";
            if (questTrackerText != null)
            {
                List<string> lines = new List<string> { "[진행 중 Quest]" };
                foreach (SandPlanetQuest04 q in content.Quests.Values.Where(q => q.TrackerVisible && GetQuestStatus(q.Id) == "ACTIVE").OrderBy(q => TypeOrder(q.Type)).ThenBy(q => q.LogOrder).ThenBy(q => q.Title))
                {
                    string sid = GetQuestStep(q.Id);
                    string tracker = content.QuestSteps.TryGetValue(sid, out SandPlanetQuestStep04 step) ? step.TrackerText : string.Empty;
                    lines.Add("• " + q.Title + (string.IsNullOrEmpty(tracker) ? string.Empty : " — " + tracker));
                }
                if (lines.Count == 1) lines.Add("• 없음");
                questTrackerText.text = string.Join("\n", lines);
            }
            if (logText != null) logText.text = RecentLogText();
        }

        private static int TypeOrder(string type) { if (type == "MAIN") return 0; if (type == "CHARACTER") return 1; return 2; }
        private string GetState(string id) => !string.IsNullOrEmpty(id) && states.TryGetValue(id, out string v) ? v : string.Empty;
        private int GetAffinity(string id) => !string.IsNullOrEmpty(id) && affinity.TryGetValue(id, out int v) ? v : 0;
        private string GetQuestStatus(string id) => !string.IsNullOrEmpty(id) && questStatus.TryGetValue(id, out string v) ? v : "LOCKED";
        private string GetQuestStep(string id) => !string.IsNullOrEmpty(id) && questStep.TryGetValue(id, out string v) ? v : string.Empty;
        private int GetStatLevel(string stat) { if (stat == "PERSONAL") return personalLevel; if (stat == "SOCIAL" || stat == "INTERPERSONAL") return socialLevel; if (stat == "TECHNICAL") return technicalLevel; return 0; }
        private string GetTargetName(string type, string id) { if (type == "CHARACTER" && content.Characters.TryGetValue(id, out SandPlanetCharacter04 c)) return c.Name; if (type == "WORLD_TARGET" && content.WorldTargets.TryGetValue(id, out SandPlanetWorldTarget04 w)) return w.Name; return id; }
        private static int ParseInt(string value) => int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v) ? v : 0;
        private static string NormalizeState(string value, string type) => type == "BOOL" ? (string.Equals(value, "TRUE", StringComparison.OrdinalIgnoreCase) ? "TRUE" : "FALSE") : value ?? string.Empty;

        private void Log(string message)
        {
            recentLog.Add($"D{day:00} {hour:00}:00  {message}");
            while (recentLog.Count > 40) recentLog.RemoveAt(0);
            if (logText != null) logText.text = RecentLogText();
        }

        private string RecentLogText()
        {
            int count = Math.Min(8, recentLog.Count);
            return count == 0 ? string.Empty : string.Join("\n", recentLog.GetRange(recentLog.Count - count, count));
        }

        private static void SetVisible(GameObject go, bool visible) { if (go != null) go.SetActive(visible); }
        private static void ClearDynamic(Transform root, Button template)
        {
            if (root == null) return;
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                Transform child = root.GetChild(i);
                if (template != null && child == template.transform) continue;
                Destroy(child.gameObject);
            }
        }

        private static Button CreateButton(Button template, Transform root, string label, bool enabled, UnityEngine.Events.UnityAction click)
        {
            if (template == null || root == null) return null;
            Button button = Instantiate(template, root);
            button.gameObject.SetActive(true);
            button.interactable = enabled;
            Text text = button.GetComponentInChildren<Text>();
            if (text != null) text.text = label;
            button.onClick.RemoveAllListeners();
            if (click != null) button.onClick.AddListener(click);
            return button;
        }

        private readonly struct ChoiceCheck
        {
            public readonly bool CanExecute;
            public readonly int TotalWill;
            public readonly string Reason;
            public ChoiceCheck(bool canExecute, int totalWill, string reason) { CanExecute = canExecute; TotalWill = totalWill; Reason = reason; }
        }
    }
}
