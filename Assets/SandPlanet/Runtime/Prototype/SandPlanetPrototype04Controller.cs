using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SandPlanet.Prototype.DataDriven;
using UnityEngine;
using UnityEngine.UI;

namespace SandPlanet.Prototype
{
    /// <summary>
    /// Prototype 0.4 runtime using the v1.6 integrated-flow workbook.
    /// Interaction Flow starts from a player click; Event Flow starts from the world/trigger system.
    /// Quest state changes are legal only as explicit Interaction/Event results.
    /// </summary>
    public sealed class SandPlanetPrototype04Controller : MonoBehaviour
    {
        private const int DayStartHour = 8;
        private const int DayEndHour = 22;
        private const int MaxWillDefault = 5;
        private const int XpPerLevel = 6;

        private const string MainColor = "#F0A24A";
        private const string CharacterColor = "#69C77C";
        private const string SideColor = "#F1D784";
        private const string BasicColor = "#AAB4BE";

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
        private readonly Queue<string> eventQueue = new Queue<string>();
        private readonly List<string> recentLog = new List<string>();

        private string currentLocationId;
        private SandPlanetInteraction04 activeInteraction;
        private SandPlanetInteractionFlow04 activeInteractionFlow;
        private SandPlanetEventFlow04 activeEventFlow;
        private string activeNodeId;
        private bool flowCommitted;
        private PendingEffects pendingEffects;
        private bool modalBusy;
        private bool evaluatingStateTriggers;
        private Action simpleModalCloseAction;

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
            Log($"v1.6 CSV 로드: 장소 {content.Locations.Count}, 상호작용 플로우 {content.InteractionFlows.Count}, 이벤트 플로우 {content.EventFlows.Count}");
        }

        private void WireButtons()
        {
            if (endDayButton != null)
            {
                endDayButton.onClick.RemoveAllListeners();
                endDayButton.onClick.AddListener(EndDay);
            }
            if (backButton != null)
            {
                backButton.onClick.RemoveAllListeners();
                backButton.onClick.AddListener(CloseLocation);
            }
            if (targetTemplate != null) targetTemplate.gameObject.SetActive(false);
            if (interactionTemplate != null) interactionTemplate.gameObject.SetActive(false);
            if (modalButtonTemplate != null) modalButtonTemplate.gameObject.SetActive(false);
        }

        private void OpenLocation(string locationId)
        {
            if (modalBusy) return;
            if (!content.Locations.TryGetValue(locationId, out SandPlanetLocation04 location) || !location.Active) return;
            currentLocationId = locationId;
            SetVisible(locationPanel, true);
            if (locationTitleText != null) locationTitleText.text = location.Name;
            if (locationHintText != null) locationHintText.text = "사람/사물을 선택하면 현재 가능한 상호작용이 표시됩니다.";
            RefreshTargets();
            ClearDynamic(interactionRoot, interactionTemplate);
            Log("장소 진입: " + location.Name);
            RefreshUi();
        }

        private void CloseLocation()
        {
            if (modalBusy) return;
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
                string badge = BuildTargetBadge("CHARACTER", c.Id);
                CreateButton(targetTemplate, targetRoot, "인물  " + c.Name + badge, true, () => SelectTarget("CHARACTER", captured.Id));
            }
            foreach (SandPlanetWorldTarget04 w in content.WorldTargets.Values.Where(w => w.Active && w.Clickable && w.LocationId == currentLocationId).OrderBy(w => w.Name))
            {
                SandPlanetWorldTarget04 captured = w;
                string badge = BuildTargetBadge("WORLD_TARGET", w.Id);
                CreateButton(targetTemplate, targetRoot, "사물  " + w.Name + badge, true, () => SelectTarget("WORLD_TARGET", captured.Id));
            }
        }

        private string BuildTargetBadge(string targetType, string targetId)
        {
            List<SandPlanetInteraction04> available = GetAvailableInteractions(targetType, targetId);
            List<string> badges = new List<string>();

            AddTargetQuestMarker(badges, available, "MAIN", "OFFER", "?", MainColor);
            AddTargetQuestMarker(badges, available, "MAIN", "PROGRESS", "!", MainColor);
            AddTargetQuestMarker(badges, available, "CHARACTER", "OFFER", "?", CharacterColor);
            AddTargetQuestMarker(badges, available, "CHARACTER", "PROGRESS", "!", CharacterColor);
            AddTargetQuestMarker(badges, available, "SIDE", "OFFER", "?", SideColor);
            AddTargetQuestMarker(badges, available, "SIDE", "PROGRESS", "!", SideColor);

            if (badges.Count == 0 && available.Any(i => string.IsNullOrEmpty(i.QuestId) || string.Equals(i.QuestRole, "NONE", StringComparison.OrdinalIgnoreCase)))
                badges.Add($"<color={BasicColor}>[일상]</color>");

            return badges.Count == 0 ? string.Empty : "   " + string.Join(" ", badges);
        }

        private void AddTargetQuestMarker(List<string> output, List<SandPlanetInteraction04> available, string questType, string role, string marker, string color)
        {
            bool exists = available.Any(i =>
                string.Equals(NormalizeQuestRole(i), role, StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrEmpty(i.QuestId) &&
                content.Quests.TryGetValue(i.QuestId, out SandPlanetQuest04 q) &&
                string.Equals(q.Type, questType, StringComparison.OrdinalIgnoreCase));
            if (exists) output.Add($"<color={color}><b>{marker}</b></color>");
        }

        private void SelectTarget(string type, string id)
        {
            List<SandPlanetInteraction04> list = GetAvailableInteractions(type, id)
                .OrderByDescending(i => QuestRoleOrder(NormalizeQuestRole(i)))
                .ThenByDescending(i => i.Priority)
                .ThenBy(i => i.DisplayText)
                .ToList();

            SandPlanetInteraction04 direct = list.FirstOrDefault(i => i.EntryMode == "DIRECT_CLICK");
            if (direct != null)
            {
                BeginInteraction(direct);
                return;
            }

            ClearDynamic(interactionRoot, interactionTemplate);
            if (locationHintText != null) locationHintText.text = GetTargetName(type, id) + " — 가능한 상호작용";
            if (list.Count == 0)
            {
                CreateButton(interactionTemplate, interactionRoot, "현재 가능한 상호작용 없음", false, null);
                return;
            }

            foreach (SandPlanetInteraction04 interaction in list)
            {
                SandPlanetInteraction04 captured = interaction;
                string worldMarker = interaction.EntryMode == "WORLD_MARKER" ? "◆ " : string.Empty;
                string badge = InteractionBadge(interaction);
                CreateButton(interactionTemplate, interactionRoot, badge + worldMarker + interaction.DisplayText, true, () => BeginInteraction(captured));
            }
        }

        private string InteractionBadge(SandPlanetInteraction04 interaction)
        {
            string role = NormalizeQuestRole(interaction);
            if (string.IsNullOrEmpty(interaction.QuestId) || role == "NONE")
                return $"<color={BasicColor}>[일상]</color>  ";
            if (!content.Quests.TryGetValue(interaction.QuestId, out SandPlanetQuest04 q)) return string.Empty;

            string marker = role == "OFFER" ? "?" : role == "PROGRESS" ? "!" : string.Empty;
            string typeLabel = q.Type == "CHARACTER" ? "CHAR" : q.Type == "SIDE" ? "SIDE" : "MAIN";
            string color = QuestColor(q.Type);
            return $"<color={color}><b>[{typeLabel}{(string.IsNullOrEmpty(marker) ? string.Empty : " " + marker)}]</b></color>  ";
        }

        private static int QuestRoleOrder(string role)
        {
            if (role == "OFFER") return 3;
            if (role == "PROGRESS") return 2;
            return 1;
        }

        private static string NormalizeQuestRole(SandPlanetInteraction04 interaction)
        {
            if (interaction == null) return "NONE";
            if (!string.IsNullOrEmpty(interaction.QuestRole)) return interaction.QuestRole.ToUpperInvariant();
            return string.IsNullOrEmpty(interaction.QuestId) ? "NONE" : "PROGRESS";
        }

        private static string QuestColor(string type)
        {
            if (string.Equals(type, "MAIN", StringComparison.OrdinalIgnoreCase)) return MainColor;
            if (string.Equals(type, "CHARACTER", StringComparison.OrdinalIgnoreCase)) return CharacterColor;
            return SideColor;
        }

        private List<SandPlanetInteraction04> GetAvailableInteractions(string targetType, string targetId)
        {
            return content.Interactions
                .Where(i => i.Active && i.TargetType == targetType && i.TargetId == targetId && IsInteractionAvailable(i))
                .ToList();
        }

        private void BeginInteraction(SandPlanetInteraction04 interaction)
        {
            if (interaction == null || interaction.Flow == null) return;
            activeInteraction = interaction;
            activeInteractionFlow = interaction.Flow;
            activeEventFlow = null;
            activeNodeId = interaction.Flow.StartNodeId;
            flowCommitted = false;
            pendingEffects = new PendingEffects();
            simpleModalCloseAction = null;
            modalBusy = true;
            SetVisible(modalPanel, true);
            ShowCurrentFlowNode();
        }

        private void BeginEventFlow(SandPlanetEventFlow04 flow)
        {
            if (flow == null) return;
            activeInteraction = null;
            activeInteractionFlow = null;
            activeEventFlow = flow;
            activeNodeId = flow.StartNodeId;
            flowCommitted = false;
            pendingEffects = new PendingEffects();
            simpleModalCloseAction = null;
            modalBusy = true;
            SetVisible(modalPanel, true);
            ShowCurrentFlowNode();
        }

        private void ShowCurrentFlowNode()
        {
            List<SandPlanetFlowNode04> rows = CurrentNodeRows().Where(n => n.Active).ToList();
            if (rows.Count == 0)
            {
                FinalizeActiveFlow();
                return;
            }

            ClearDynamic(modalButtonRoot, modalButtonTemplate);
            if (modalTitleText != null)
                modalTitleText.text = activeInteractionFlow != null ? activeInteraction.DisplayText : activeEventFlow.Name;

            bool sameBody = rows.Select(r => r.BodyText ?? string.Empty).Distinct().Count() <= 1;
            string body;
            if (rows.Count > 1 && !sameBody)
                body = activeInteractionFlow != null ? activeInteraction.DisplayText : activeEventFlow.PlayerPerceivedChange;
            else
                body = FormatNodeBody(rows[0]);
            if (modalBodyText != null) modalBodyText.text = body;

            foreach (SandPlanetFlowNode04 row in rows)
            {
                NodeCheck check = CheckNode(row);
                SandPlanetFlowNode04 captured = row;
                string label = string.IsNullOrEmpty(row.ChoiceText) ? (string.IsNullOrEmpty(row.NextNodeId) ? "종료" : "계속") : row.ChoiceText;
                string cost = BuildCostLabel(row, check);
                if (!check.CanExecute) label += "\n<" + check.Reason + ">";
                CreateButton(modalButtonTemplate, modalButtonRoot, label + cost, check.CanExecute, () => ChooseNode(captured));
            }

            if (activeInteractionFlow != null && !flowCommitted)
                CreateButton(modalButtonTemplate, modalButtonRoot, "취소", true, CancelActiveFlow);
        }

        private string FormatNodeBody(SandPlanetFlowNode04 row)
        {
            string body = row.BodyText ?? string.Empty;
            string speaker = SpeakerName(row.Speaker);
            if (row.PresentationType == "DIALOGUE" && !string.IsNullOrEmpty(speaker))
                body = "<b>" + speaker + "</b>\n\n" + body;
            if (string.IsNullOrEmpty(row.ChoiceId) && !string.IsNullOrEmpty(row.ResultTextOverride))
                body += "\n\n<color=#B9C4CD>" + row.ResultTextOverride + "</color>";
            return body;
        }

        private string SpeakerName(string speaker)
        {
            if (string.IsNullOrEmpty(speaker) || speaker == "NARRATOR" || speaker == "NONE") return string.Empty;
            if (speaker == "PLAYER") return "제이";
            return content.Characters.TryGetValue(speaker, out SandPlanetCharacter04 c) ? c.Name : speaker;
        }

        private string BuildCostLabel(SandPlanetFlowNode04 row, NodeCheck check)
        {
            List<string> parts = new List<string>();
            if (row.TimeCost != 0) parts.Add(row.TimeCost + "h");
            int willCost = Math.Max(0, -row.WillDelta) + check.ExtraWillCost;
            if (willCost > 0) parts.Add("의지 " + willCost);
            return parts.Count == 0 ? string.Empty : "  [" + string.Join(" / ", parts) + "]";
        }

        private NodeCheck CheckNode(SandPlanetFlowNode04 node)
        {
            if (node == null || !node.Active) return new NodeCheck(false, 0, "비활성");
            int stagedTime = pendingEffects?.TimeDelta ?? 0;
            if (hour + stagedTime + node.TimeCost > DayEndHour) return new NodeCheck(false, 0, "오늘 남은 시간 부족");
            if (!EvaluatePair(node.HardConditionLogic, node.HardCondition1, node.HardCondition2)) return new NodeCheck(false, 0, "조건 미충족");

            int extraWill = 0;
            if (!string.IsNullOrEmpty(node.SoftStat) && node.SoftStat != "NONE" && node.SoftRequirement > 0)
                extraWill = Math.Max(0, node.SoftRequirement - GetStatLevel(node.SoftStat)); // TEMP: 부족 Lv 1 = 의지 1.

            int stagedWill = pendingEffects?.WillDelta ?? 0;
            if (will + stagedWill + node.WillDelta - extraWill < 0)
                return new NodeCheck(false, extraWill, "의지 부족");
            return new NodeCheck(true, extraWill, string.Empty);
        }

        private void ChooseNode(SandPlanetFlowNode04 node)
        {
            NodeCheck check = CheckNode(node);
            if (!check.CanExecute) return;
            flowCommitted = true;
            StageNode(node, check.ExtraWillCost);
            if (!string.IsNullOrEmpty(node.NextNodeId))
            {
                activeNodeId = node.NextNodeId;
                ShowCurrentFlowNode();
                return;
            }
            FinalizeActiveFlow();
        }

        private void StageNode(SandPlanetFlowNode04 node, int extraWillCost)
        {
            if (pendingEffects == null) pendingEffects = new PendingEffects();
            pendingEffects.TimeDelta += node.TimeCost;
            pendingEffects.WillDelta += node.WillDelta - extraWillCost;
            pendingEffects.PersonalXpDelta += node.PersonalXpDelta;
            pendingEffects.SocialXpDelta += node.SocialXpDelta;
            pendingEffects.TechnicalXpDelta += node.TechnicalXpDelta;
            foreach (SandPlanetAffinityChange04 a in node.AffinityChanges) pendingEffects.AddAffinity(a.CharacterId, a.Delta);
            foreach (SandPlanetStateChange04 s in node.StateChanges) pendingEffects.StateChanges.Add(s);
            if (!string.IsNullOrEmpty(node.EmitEventId)) pendingEffects.EmitEvents.Add(node.EmitEventId);
            if (!string.IsNullOrEmpty(node.ResultTextOverride)) pendingEffects.ResultText = node.ResultTextOverride;

            if (activeInteractionFlow != null && content.InteractionNodeMeta.TryGetValue(node, out SandPlanetQuestActionMeta04 interactionMeta) && !string.IsNullOrEmpty(interactionMeta.QuestAction))
                pendingEffects.QuestActions.Add(interactionMeta);
            if (activeEventFlow != null && content.EventNodeMeta.TryGetValue(node, out SandPlanetQuestActionMeta04 eventMeta) && !string.IsNullOrEmpty(eventMeta.QuestAction))
                pendingEffects.QuestActions.Add(eventMeta);
        }

        private void FinalizeActiveFlow()
        {
            PendingEffects effects = pendingEffects ?? new PendingEffects();
            SandPlanetInteraction04 finishedInteraction = activeInteraction;
            bool wasInteraction = activeInteractionFlow != null;

            CloseModalInternal();
            ApplyPendingEffects(effects);

            if (wasInteraction && finishedInteraction != null)
            {
                interactionsUsed.Add(finishedInteraction.Id);
                interactionLastDay[finishedInteraction.Id] = day;
                ProcessTriggers("INTERACTION");
            }

            if (!string.IsNullOrEmpty(effects.ResultText)) Log(effects.ResultText);
            if (!string.IsNullOrEmpty(currentLocationId))
            {
                RefreshTargets();
                ClearDynamic(interactionRoot, interactionTemplate);
            }
            RefreshUi();
            ShowQueuedEvent();
        }

        private void ApplyPendingEffects(PendingEffects effects)
        {
            hour = Mathf.Clamp(hour + effects.TimeDelta, DayStartHour, DayEndHour);
            will = Mathf.Clamp(will + effects.WillDelta, 0, maxWill);
            AddXp("PERSONAL", effects.PersonalXpDelta);
            AddXp("SOCIAL", effects.SocialXpDelta);
            AddXp("TECHNICAL", effects.TechnicalXpDelta);

            foreach (KeyValuePair<string, int> pair in effects.AffinityDelta)
                affinity[pair.Key] = Mathf.Clamp(GetAffinity(pair.Key) + pair.Value, 0, 5);
            foreach (SandPlanetStateChange04 change in effects.StateChanges)
                ApplyStateChange(change);
            foreach (SandPlanetQuestActionMeta04 action in effects.QuestActions)
                ApplyQuestAction(action);
            foreach (string eventId in effects.EmitEvents)
                FireEvent(eventId);
        }

        private void ApplyStateChange(SandPlanetStateChange04 change)
        {
            if (change == null || string.IsNullOrEmpty(change.StateId)) return;
            if (change.Operation == "ADD")
                SetState(change.StateId, (ParseInt(GetState(change.StateId)) + ParseInt(change.Value)).ToString(CultureInfo.InvariantCulture));
            else
                SetState(change.StateId, change.Value);
        }

        private void ApplyQuestAction(SandPlanetQuestActionMeta04 action)
        {
            if (action == null || string.IsNullOrEmpty(action.QuestAction)) return;
            switch (action.QuestAction.ToUpperInvariant())
            {
                case "ACTIVATE_QUEST": ActivateQuest(action.QuestId); break;
                case "COMPLETE_QUEST": CompleteQuest(action.QuestId); break;
                case "FAIL_QUEST": FailQuest(action.QuestId); break;
                case "SET_QUEST_STEP": SetQuestStep(action.QuestId, action.QuestStepId); break;
            }
        }

        private void AddXp(string stat, int amount)
        {
            if (amount == 0) return;
            if (stat == "PERSONAL") AddXpTo(ref personalLevel, ref personalXp, amount);
            else if (stat == "SOCIAL" || stat == "INTERPERSONAL") AddXpTo(ref socialLevel, ref socialXp, amount);
            else if (stat == "TECHNICAL") AddXpTo(ref technicalLevel, ref technicalXp, amount);
        }

        private static void AddXpTo(ref int level, ref int xp, int amount)
        {
            if (amount < 0)
            {
                xp = Mathf.Max(0, xp + amount);
                return;
            }
            xp += amount;
            while (xp >= XpPerLevel && level < 20)
            {
                xp -= XpPerLevel;
                level++;
            }
            if (level >= 20) xp = Mathf.Min(xp, XpPerLevel - 1);
        }

        private void FireEvent(string eventId)
        {
            if (string.IsNullOrEmpty(eventId) || !content.EventFlows.TryGetValue(eventId, out SandPlanetEventFlow04 flow) || !flow.Active) return;
            eventsOccurred.Add(eventId);
            ProgressQuestsFromEvent(eventId);
            Log("Event: " + flow.Name);

            if (string.Equals(flow.PresentationMode, "SILENT", StringComparison.OrdinalIgnoreCase) && CanAutoResolve(flow))
            {
                ResolveSilentEvent(flow);
                return;
            }
            eventQueue.Enqueue(eventId);
        }

        private bool CanAutoResolve(SandPlanetEventFlow04 flow)
        {
            string nodeId = flow.StartNodeId;
            HashSet<string> visited = new HashSet<string>(StringComparer.Ordinal);
            while (!string.IsNullOrEmpty(nodeId) && visited.Add(nodeId))
            {
                List<SandPlanetFlowNode04> rows = flow.GetNodeRows(nodeId).ToList();
                if (rows.Count != 1) return false;
                nodeId = rows[0].NextNodeId;
            }
            return true;
        }

        private void ResolveSilentEvent(SandPlanetEventFlow04 flow)
        {
            PendingEffects local = new PendingEffects();
            string nodeId = flow.StartNodeId;
            HashSet<string> visited = new HashSet<string>(StringComparer.Ordinal);
            while (!string.IsNullOrEmpty(nodeId) && visited.Add(nodeId))
            {
                SandPlanetFlowNode04 node = flow.GetNodeRows(nodeId).FirstOrDefault();
                if (node == null) break;
                PendingEffects previous = pendingEffects;
                SandPlanetEventFlow04 previousEvent = activeEventFlow;
                pendingEffects = local;
                activeEventFlow = flow;
                StageNode(node, 0);
                activeEventFlow = previousEvent;
                pendingEffects = previous;
                nodeId = node.NextNodeId;
            }
            ApplyPendingEffects(local);
            if (!string.IsNullOrEmpty(local.ResultText)) Log(local.ResultText);
            if (!string.IsNullOrEmpty(currentLocationId)) RefreshTargets();
            RefreshUi();
            ShowQueuedEvent();
        }

        private void ShowQueuedEvent()
        {
            if (modalBusy || eventQueue.Count == 0) return;
            string id = eventQueue.Dequeue();
            if (!content.EventFlows.TryGetValue(id, out SandPlanetEventFlow04 flow)) return;
            BeginEventFlow(flow);
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
            if (GetQuestStatus(questId) != "LOCKED") return;
            questStatus[questId] = "ACTIVE";
            questStep[questId] = quest.InitialStepId;
            Log("Quest 수락: " + quest.Title);
        }

        private void CompleteQuest(string questId)
        {
            if (!content.Quests.TryGetValue(questId, out SandPlanetQuest04 quest)) return;
            if (GetQuestStatus(questId) != "ACTIVE") return;
            questStatus[questId] = "COMPLETED";
            Log("Quest 완료: " + quest.Title);
        }

        private void FailQuest(string questId)
        {
            if (!content.Quests.TryGetValue(questId, out SandPlanetQuest04 quest)) return;
            if (GetQuestStatus(questId) != "ACTIVE") return;
            questStatus[questId] = "FAILED";
            Log("Quest 실패: " + quest.Title);
        }

        private void SetQuestStatus(string questId, string status)
        {
            if (!string.IsNullOrEmpty(questId) && questStatus.ContainsKey(questId)) questStatus[questId] = status;
        }

        private void SetQuestStep(string questId, string stepId)
        {
            if (string.IsNullOrEmpty(questId) || string.IsNullOrEmpty(stepId)) return;
            if (GetQuestStatus(questId) != "ACTIVE") return;
            if (!content.QuestSteps.TryGetValue(stepId, out SandPlanetQuestStep04 step) || step.QuestId != questId) return;
            questStep[questId] = stepId;
            if (content.Quests.TryGetValue(questId, out SandPlanetQuest04 quest))
                Log("Quest 진행: " + quest.Title + " — " + step.Title);
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
            if (i == null || !i.Active) return false;
            if (day < i.OpenDay || day > i.CloseDay || !TimeSlotAllowed(i)) return false;
            if (!RepeatAvailable(i.Id, i.RepeatRule, interactionsUsed, interactionLastDay)) return false;

            string role = NormalizeQuestRole(i);
            if (role == "OFFER")
            {
                if (string.IsNullOrEmpty(i.QuestId) || GetQuestStatus(i.QuestId) != "LOCKED") return false;
            }
            else if (role == "PROGRESS")
            {
                if (string.IsNullOrEmpty(i.QuestId) || GetQuestStatus(i.QuestId) != "ACTIVE") return false;
                if (!string.IsNullOrEmpty(i.QuestStepId) && GetQuestStep(i.QuestId) != i.QuestStepId) return false;
            }
            else if (!string.IsNullOrEmpty(i.QuestId))
            {
                // Backward-compatible rule for old authored rows without QuestRole.
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
                switch (op)
                {
                    case "NE": return li != ri;
                    case "GT": return li > ri;
                    case "GE": return li >= ri;
                    case "LT": return li < ri;
                    case "LE": return li <= ri;
                    default: return li == ri;
                }
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
                if (s.Priority >= bestPriority)
                {
                    bestPriority = s.Priority;
                    best = s.LocationId;
                }
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
            currentLocationId = null;
            SetVisible(locationPanel, false);
            ClearDynamic(targetRoot, targetTemplate);
            ClearDynamic(interactionRoot, interactionTemplate);
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

        private void ShowMessage(string title, string body)
        {
            activeInteraction = null;
            activeInteractionFlow = null;
            activeEventFlow = null;
            activeNodeId = null;
            pendingEffects = null;
            modalBusy = true;
            simpleModalCloseAction = CloseModalInternal;
            SetVisible(modalPanel, true);
            ClearDynamic(modalButtonRoot, modalButtonTemplate);
            if (modalTitleText != null) modalTitleText.text = title;
            if (modalBodyText != null) modalBodyText.text = body;
            CreateButton(modalButtonTemplate, modalButtonRoot, "닫기", true, () =>
            {
                CloseModalInternal();
                ShowQueuedEvent();
            });
        }

        private void CancelActiveFlow()
        {
            CloseModalInternal();
            RefreshUi();
        }

        private void CloseModalInternal()
        {
            modalBusy = false;
            SetVisible(modalPanel, false);
            ClearDynamic(modalButtonRoot, modalButtonTemplate);
            activeInteraction = null;
            activeInteractionFlow = null;
            activeEventFlow = null;
            activeNodeId = null;
            flowCommitted = false;
            pendingEffects = null;
            simpleModalCloseAction = null;
        }

        /// <summary>Called reflectively by the UX enhancer when ESC is pressed.</summary>
        private void HandleEscapeFromUx()
        {
            if (!modalBusy) return;
            if (activeInteractionFlow != null)
            {
                if (!flowCommitted)
                {
                    CancelActiveFlow();
                    return;
                }
                SkipLinearRemainderAndFinish();
                return;
            }
            if (activeEventFlow != null)
            {
                if (CurrentNodeRows().Count() > 1) return; // forced Event choice cannot be bypassed.
                SkipLinearRemainderAndFinish();
                return;
            }
            simpleModalCloseAction?.Invoke();
        }

        private void SkipLinearRemainderAndFinish()
        {
            HashSet<string> visited = new HashSet<string>(StringComparer.Ordinal);
            string nodeId = activeNodeId;
            while (!string.IsNullOrEmpty(nodeId) && visited.Add(nodeId))
            {
                List<SandPlanetFlowNode04> rows = CurrentNodeRows(nodeId).Where(n => n.Active).ToList();
                if (rows.Count != 1) return;
                NodeCheck check = CheckNode(rows[0]);
                if (!check.CanExecute) return;
                StageNode(rows[0], check.ExtraWillCost);
                nodeId = rows[0].NextNodeId;
            }
            FinalizeActiveFlow();
        }

        private IEnumerable<SandPlanetFlowNode04> CurrentNodeRows() => CurrentNodeRows(activeNodeId);

        private IEnumerable<SandPlanetFlowNode04> CurrentNodeRows(string nodeId)
        {
            if (activeInteractionFlow != null) return activeInteractionFlow.GetNodeRows(nodeId);
            if (activeEventFlow != null) return activeEventFlow.GetNodeRows(nodeId);
            return Enumerable.Empty<SandPlanetFlowNode04>();
        }

        private string BuildDay21Summary()
        {
            return "Excel v1.6 데이터 기준 Day 21 상태\n\n"
                   + $"거주지 보존 {GetState("STA_PRESERVE_SETTLEMENT")}\n"
                   + $"오아시스 보존 {GetState("STA_PRESERVE_OASIS")}\n"
                   + $"묘지 보존 {GetState("STA_PRESERVE_GRAVEYARD")}\n"
                   + $"수송선 외부 보존 {GetState("STA_PRESERVE_SHIP")}\n\n"
                   + $"지휘·항해 안정 {GetState("STA_W3_NAV_STABILITY")} / 보급 안정 {GetState("STA_W3_SUPPLY_STABILITY")}\n"
                   + $"기관·연구 안정 {GetState("STA_W3_TECH_STABILITY")} / 거주 안정 {GetState("STA_W3_HABIT_STABILITY")}\n\n"
                   + "최종 엔딩 판정식은 아직 TEMP/TBD입니다.";
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
                    string color = QuestColor(q.Type);
                    string type = q.Type == "CHARACTER" ? "CHAR" : q.Type;
                    lines.Add($"• <color={color}>[{type}]</color> " + q.Title + (string.IsNullOrEmpty(tracker) ? string.Empty : " — " + tracker));
                }
                if (lines.Count == 1) lines.Add("• 없음");
                questTrackerText.supportRichText = true;
                questTrackerText.text = string.Join("\n", lines);
            }
            if (logText != null) logText.text = RecentLogText();
        }

        private static int TypeOrder(string type) => type == "MAIN" ? 0 : type == "CHARACTER" ? 1 : 2;
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
            Text text = button.GetComponentInChildren<Text>(true);
            if (text != null)
            {
                text.supportRichText = true;
                text.text = label;
            }
            button.onClick.RemoveAllListeners();
            if (click != null) button.onClick.AddListener(click);
            return button;
        }

        private sealed class PendingEffects
        {
            public int TimeDelta;
            public int WillDelta;
            public int PersonalXpDelta;
            public int SocialXpDelta;
            public int TechnicalXpDelta;
            public readonly Dictionary<string, int> AffinityDelta = new Dictionary<string, int>(StringComparer.Ordinal);
            public readonly List<SandPlanetStateChange04> StateChanges = new List<SandPlanetStateChange04>();
            public readonly List<SandPlanetQuestActionMeta04> QuestActions = new List<SandPlanetQuestActionMeta04>();
            public readonly List<string> EmitEvents = new List<string>();
            public string ResultText;

            public void AddAffinity(string characterId, int delta)
            {
                if (string.IsNullOrEmpty(characterId) || delta == 0) return;
                AffinityDelta[characterId] = AffinityDelta.TryGetValue(characterId, out int current) ? current + delta : delta;
            }
        }

        private readonly struct NodeCheck
        {
            public readonly bool CanExecute;
            public readonly int ExtraWillCost;
            public readonly string Reason;
            public NodeCheck(bool canExecute, int extraWillCost, string reason)
            {
                CanExecute = canExecute;
                ExtraWillCost = extraWillCost;
                Reason = reason;
            }
        }
    }
}
