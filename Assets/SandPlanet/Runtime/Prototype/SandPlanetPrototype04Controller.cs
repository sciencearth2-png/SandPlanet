using System;
using System.Collections.Generic;
using System.Linq;
using SandPlanet.Prototype.DataDriven;
using UnityEngine;
using UnityEngine.UI;

namespace SandPlanet.Prototype
{
    /// <summary>
    /// Unity coordinator for the Prototype 0.4 gameplay services and Phase 1 presentation facade.
    /// </summary>
    public sealed class SandPlanetPrototype04Controller : MonoBehaviour
    {
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
        private Prototype04GameState gameState;
        private Prototype04QuestService questService;
        private Prototype04ConditionEvaluator conditionEvaluator;
        private Prototype04InteractionService interactionService;
        private Prototype04ScheduleService scheduleService;
        private Prototype04EventService eventService;
        private Prototype04FlowRuntime flowRuntime;

        private readonly List<string> recentLog = new List<string>();
        private string currentLocationId;
        private string selectedTargetType;
        private string selectedTargetId;
        private bool simpleModalBusy;
        private Action simpleModalCloseAction;
        private string simpleModalTitle;
        private string simpleModalBody;
        private bool consolidatedPresentation;

        public event Action PresentationChanged;

        public SandPlanetContent04 Content => content;
        public Camera HubCamera => hubCamera;
        public GameObject LocationPanelObject => locationPanel;
        public Text LocationTitleText => locationTitleText;
        public Text LocationHintText => locationHintText;
        public RectTransform TargetRoot => targetRoot as RectTransform;
        public Button TargetTemplate => targetTemplate;
        public Transform InteractionRoot => interactionRoot;
        public Button InteractionTemplate => interactionTemplate;
        public Button BackButton => backButton;
        public GameObject ModalPanelObject => modalPanel;
        public Text ModalTitleText => modalTitleText;
        public Text ModalBodyText => modalBodyText;
        public Transform ModalButtonRoot => modalButtonRoot;
        public Button ModalButtonTemplate => modalButtonTemplate;
        public Text HudText => hudText;
        public Text QuestTrackerText => questTrackerText;
        public Text LogText => logText;
        public Button EndDayButton => endDayButton;
        public TextAsset[] CsvAssets => csvAssets;
        public int Day => gameState.Day;
        public int Hour => gameState.Hour;
        public int Will => gameState.Will;
        public int MaxWill => gameState.MaxWill;
        public int PersonalLevel => gameState.PersonalLevel;
        public int PersonalXp => gameState.PersonalXp;
        public int SocialLevel => gameState.SocialLevel;
        public int SocialXp => gameState.SocialXp;
        public int TechnicalLevel => gameState.TechnicalLevel;
        public int TechnicalXp => gameState.TechnicalXp;
        public string CurrentLocationId => currentLocationId ?? string.Empty;
        public string SelectedTargetType => selectedTargetType ?? string.Empty;
        public string SelectedTargetId => selectedTargetId ?? string.Empty;
        public bool ModalBusy => simpleModalBusy || flowRuntime.IsActive;
        public bool HasActiveInteractionFlow => flowRuntime.HasActiveInteractionFlow;
        public bool HasActiveEventFlow => flowRuntime.HasActiveEventFlow;
        public bool FlowCommitted => flowRuntime.FlowCommitted;
        public SandPlanetInteraction04 ActiveInteraction => flowRuntime.ActiveInteraction;
        public string SimpleModalTitle => simpleModalTitle ?? string.Empty;
        public string SimpleModalBody => simpleModalBody ?? string.Empty;
        public IReadOnlyDictionary<string, int> Affinity => gameState.Affinity;
        public IReadOnlyDictionary<string, string> States => gameState.States;
        public IReadOnlyDictionary<string, string> QuestStatuses => gameState.QuestStatuses;
        public IReadOnlyDictionary<string, string> QuestSteps => gameState.QuestSteps;
        public IReadOnlyCollection<string> EventsOccurred => gameState.EventsOccurred;

        public void EnableConsolidatedPresentation()
        {
            consolidatedPresentation = true;
            NotifyPresentationChanged();
        }

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
            ConstructGameplay();
            WireButtons();
            SetVisible(locationPanel, false);
            SetVisible(modalPanel, false);
            RefreshHub();
            Log($"v1.6 CSV 로드: 장소 {content.Locations.Count}, 상호작용 플로우 {content.InteractionFlows.Count}, 이벤트 플로우 {content.EventFlows.Count}");
            RefreshUi();
        }

        private void Start()
        {
            ProcessTriggers("GAME_START");
            ProcessTriggers("DAY_START");
            RefreshUi();
            ShowQueuedEvent();
        }

        private void ConstructGameplay()
        {
            gameState = new Prototype04GameState();
            gameState.Initialize(content);
            questService = new Prototype04QuestService(content, gameState, Log);
            conditionEvaluator = new Prototype04ConditionEvaluator(gameState, questService);
            interactionService = new Prototype04InteractionService(content, gameState, questService, conditionEvaluator);
            scheduleService = new Prototype04ScheduleService(content, gameState, conditionEvaluator);
            eventService = new Prototype04EventService(content, gameState, questService, conditionEvaluator, Log);
            flowRuntime = new Prototype04FlowRuntime(content, gameState, conditionEvaluator, questService, eventService, () => currentLocationId, Log);
            eventService.BindFlowRuntime(flowRuntime);
            flowRuntime.Changed += ShowCurrentFlowNode;
            flowRuntime.Completed += HandleFlowCompleted;
            flowRuntime.Cancelled += HandleFlowCancelled;
            eventService.SilentEventResolved += HandleSilentEventResolved;
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

        public void OpenLocation(string locationId)
        {
            if (ModalBusy) return;
            if (!content.Locations.TryGetValue(locationId, out SandPlanetLocation04 location) || !location.Active) return;
            currentLocationId = locationId;
            selectedTargetType = null;
            selectedTargetId = null;
            if (!consolidatedPresentation)
            {
                SetVisible(locationPanel, true);
                if (locationTitleText != null) locationTitleText.text = location.Name;
                if (locationHintText != null) locationHintText.text = "사람/사물을 선택하면 현재 가능한 상호작용이 표시됩니다.";
            }
            RefreshTargets();
            if (!consolidatedPresentation) ClearDynamic(interactionRoot, interactionTemplate);
            Log("장소 진입: " + location.Name);
            RefreshUi();
            NotifyPresentationChanged();
        }

        public void CloseLocation()
        {
            if (ModalBusy) return;
            currentLocationId = null;
            selectedTargetType = null;
            selectedTargetId = null;
            if (!consolidatedPresentation) SetVisible(locationPanel, false);
            if (!consolidatedPresentation)
            {
                ClearDynamic(targetRoot, targetTemplate);
                ClearDynamic(interactionRoot, interactionTemplate);
            }
            RefreshUi();
            ShowQueuedEvent();
            NotifyPresentationChanged();
        }

        private void RefreshTargets()
        {
            if (consolidatedPresentation)
            {
                NotifyPresentationChanged();
                return;
            }
            ClearDynamic(targetRoot, targetTemplate);
            if (string.IsNullOrEmpty(currentLocationId)) return;

            foreach (SandPlanetCharacter04 character in content.Characters.Values.Where(item => item.Active && GetCharacterLocation(item.Id) == currentLocationId).OrderBy(item => item.Name))
            {
                SandPlanetCharacter04 captured = character;
                CreateButton(targetTemplate, targetRoot, "인물  " + character.Name + BuildTargetBadge("CHARACTER", character.Id), true, () => SelectTarget("CHARACTER", captured.Id));
            }
            foreach (SandPlanetWorldTarget04 target in content.WorldTargets.Values.Where(item => item.Active && item.Clickable && item.LocationId == currentLocationId).OrderBy(item => item.Name))
            {
                SandPlanetWorldTarget04 captured = target;
                CreateButton(targetTemplate, targetRoot, "사물  " + target.Name + BuildTargetBadge("WORLD_TARGET", target.Id), true, () => SelectTarget("WORLD_TARGET", captured.Id));
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
            if (badges.Count == 0 && available.Any(item => string.IsNullOrEmpty(item.QuestId) || string.Equals(item.QuestRole, "NONE", StringComparison.OrdinalIgnoreCase)))
                badges.Add($"<color={BasicColor}>[일상]</color>");
            return badges.Count == 0 ? string.Empty : "   " + string.Join(" ", badges);
        }

        private void AddTargetQuestMarker(List<string> output, List<SandPlanetInteraction04> available, string questType, string role, string marker, string color)
        {
            bool exists = available.Any(interaction =>
                string.Equals(Prototype04InteractionService.NormalizeQuestRole(interaction), role, StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrEmpty(interaction.QuestId) &&
                content.Quests.TryGetValue(interaction.QuestId, out SandPlanetQuest04 quest) &&
                string.Equals(quest.Type, questType, StringComparison.OrdinalIgnoreCase));
            if (exists) output.Add($"<color={color}><b>{marker}</b></color>");
        }

        public void SelectTarget(string type, string id)
        {
            List<SandPlanetInteraction04> interactions = GetAvailableInteractions(type, id)
                .OrderByDescending(item => QuestRoleOrder(Prototype04InteractionService.NormalizeQuestRole(item)))
                .ThenByDescending(item => item.Priority)
                .ThenBy(item => item.DisplayText)
                .ToList();
            SandPlanetInteraction04 direct = interactions.FirstOrDefault(item => item.EntryMode == "DIRECT_CLICK");
            selectedTargetType = type;
            selectedTargetId = id;
            if (direct != null)
            {
                BeginInteraction(direct);
                return;
            }
            if (consolidatedPresentation)
            {
                NotifyPresentationChanged();
                return;
            }

            ClearDynamic(interactionRoot, interactionTemplate);
            if (locationHintText != null) locationHintText.text = GetTargetName(type, id) + " — 가능한 상호작용";
            if (interactions.Count == 0)
            {
                CreateButton(interactionTemplate, interactionRoot, "현재 가능한 상호작용 없음", false, null);
                return;
            }
            foreach (SandPlanetInteraction04 interaction in interactions)
            {
                SandPlanetInteraction04 captured = interaction;
                string worldMarker = interaction.EntryMode == "WORLD_MARKER" ? "◆ " : string.Empty;
                CreateButton(interactionTemplate, interactionRoot, InteractionBadge(interaction) + worldMarker + interaction.DisplayText, true, () => BeginInteraction(captured));
            }
        }

        public void DismissTargetSelection()
        {
            if (ModalBusy) return;
            selectedTargetType = null;
            selectedTargetId = null;
            if (!consolidatedPresentation)
            {
                ClearDynamic(interactionRoot, interactionTemplate);
                if (locationHintText != null) locationHintText.text = "사람/사물을 선택하면 현재 가능한 상호작용이 표시됩니다.";
            }
            NotifyPresentationChanged();
        }

        public List<SandPlanetInteraction04> GetAvailableInteractions(string targetType, string targetId) => interactionService.GetAvailable(targetType, targetId);
        public bool IsInteractionAvailable(SandPlanetInteraction04 interaction) => interactionService.IsAvailable(interaction);

        public void BeginInteraction(SandPlanetInteraction04 interaction)
        {
            if (interaction == null || interaction.Flow == null) return;
            simpleModalBusy = false;
            simpleModalCloseAction = null;
            if (!consolidatedPresentation) SetVisible(modalPanel, true);
            flowRuntime.BeginInteraction(interaction);
        }

        private void BeginEventFlow(SandPlanetEventFlow04 flow)
        {
            if (flow == null) return;
            simpleModalBusy = false;
            simpleModalCloseAction = null;
            if (!consolidatedPresentation) SetVisible(modalPanel, true);
            flowRuntime.BeginEvent(flow);
        }

        private void ShowCurrentFlowNode()
        {
            List<SandPlanetFlowNode04> rows = flowRuntime.CurrentRows().ToList();
            if (rows.Count == 0)
            {
                flowRuntime.FinishEmptyFlow();
                return;
            }
            if (consolidatedPresentation)
            {
                NotifyPresentationChanged();
                return;
            }

            ClearDynamic(modalButtonRoot, modalButtonTemplate);
            if (modalTitleText != null) modalTitleText.text = CurrentFlowTitle();
            if (modalBodyText != null) modalBodyText.text = CurrentFlowBody();
            foreach (SandPlanetFlowNode04 row in rows)
            {
                Prototype04NodeCheck check = flowRuntime.CheckNode(row);
                SandPlanetFlowNode04 captured = row;
                string label = string.IsNullOrEmpty(row.ChoiceText) ? (string.IsNullOrEmpty(row.NextNodeId) ? "종료" : "계속") : row.ChoiceText;
                if (!check.CanExecute) label += "\n<" + check.Reason + ">";
                CreateButton(modalButtonTemplate, modalButtonRoot, label + BuildCostLabel(row, check), check.CanExecute, () => flowRuntime.ExecuteNode(captured));
            }
            if (flowRuntime.HasActiveInteractionFlow && !flowRuntime.FlowCommitted)
                CreateButton(modalButtonTemplate, modalButtonRoot, "취소", true, CancelActiveFlow);
        }

        private void HandleFlowCompleted()
        {
            // A silent effect can synchronously open the next queued Event before the
            // finishing flow raises Completed. Keep that new flow's modal visible.
            if (!flowRuntime.IsActive) HideFlowPresentation();
            selectedTargetType = null;
            selectedTargetId = null;
            if (!string.IsNullOrEmpty(currentLocationId))
            {
                RefreshTargets();
                if (!consolidatedPresentation) ClearDynamic(interactionRoot, interactionTemplate);
            }
            RefreshUi();
            ShowQueuedEvent();
            NotifyPresentationChanged();
        }

        private void HandleFlowCancelled()
        {
            HideFlowPresentation();
            RefreshUi();
        }

        private void HandleSilentEventResolved()
        {
            if (!string.IsNullOrEmpty(currentLocationId)) RefreshTargets();
            RefreshUi();
            ShowQueuedEvent();
        }

        private void HideFlowPresentation()
        {
            if (!consolidatedPresentation)
            {
                SetVisible(modalPanel, false);
                ClearDynamic(modalButtonRoot, modalButtonTemplate);
            }
            NotifyPresentationChanged();
        }

        private string FormatNodeBody(SandPlanetFlowNode04 row)
        {
            string body = row.BodyText ?? string.Empty;
            string speaker = SpeakerName(row.Speaker);
            if (row.PresentationType == "DIALOGUE" && !string.IsNullOrEmpty(speaker)) body = "<b>" + speaker + "</b>\n\n" + body;
            if (string.IsNullOrEmpty(row.ChoiceId) && !string.IsNullOrEmpty(row.ResultTextOverride))
                body += "\n\n<color=#B9C4CD>" + row.ResultTextOverride + "</color>";
            return body;
        }

        private string SpeakerName(string speaker)
        {
            if (string.IsNullOrEmpty(speaker) || speaker == "NARRATOR" || speaker == "NONE") return string.Empty;
            if (speaker == "PLAYER") return "제이";
            return content.Characters.TryGetValue(speaker, out SandPlanetCharacter04 character) ? character.Name : speaker;
        }

        private static string BuildCostLabel(SandPlanetFlowNode04 row, Prototype04NodeCheck check)
        {
            List<string> parts = new List<string>();
            if (row.TimeCost != 0) parts.Add(row.TimeCost + "h");
            int willCost = Math.Max(0, -row.WillDelta) + check.ExtraWillCost;
            if (willCost > 0) parts.Add("의지 " + willCost);
            return parts.Count == 0 ? string.Empty : "  [" + string.Join(" / ", parts) + "]";
        }

        private void ProcessTriggers(string timing) => eventService.ProcessTriggers(timing, currentLocationId);

        private void ShowQueuedEvent()
        {
            if (ModalBusy || !eventService.TryDequeue(out SandPlanetEventFlow04 flow)) return;
            BeginEventFlow(flow);
        }

        public string GetCharacterLocation(string characterId) => scheduleService.GetCharacterLocation(characterId);

        private void EndDay()
        {
            if (ModalBusy) return;
            if (gameState.Day >= 21)
            {
                ShowMessage("21일 결과", BuildDay21Summary());
                return;
            }

            ProcessTriggers("DAY_END");
            gameState.AdvanceDay();
            currentLocationId = null;
            selectedTargetType = null;
            selectedTargetId = null;
            if (!consolidatedPresentation) SetVisible(locationPanel, false);
            if (!consolidatedPresentation)
            {
                ClearDynamic(targetRoot, targetTemplate);
                ClearDynamic(interactionRoot, interactionTemplate);
            }
            RefreshHub();
            Log($"Day {gameState.Day} 시작 / 수면 회복 후 의지 {gameState.Will}/{gameState.MaxWill}");
            ProcessTriggers("DAY_START");
            RefreshUi();
            ShowQueuedEvent();
            NotifyPresentationChanged();
        }

        private void RefreshHub()
        {
            bool week3 = gameState.Day >= 15;
            if (planetHubRoot != null) planetHubRoot.SetActive(!week3);
            if (shipInteriorHubRoot != null) shipInteriorHubRoot.SetActive(week3);
        }

        private void ShowMessage(string title, string body)
        {
            flowRuntime.AbandonActiveFlow();
            simpleModalBusy = true;
            simpleModalCloseAction = CloseSimpleModalInternal;
            simpleModalTitle = title;
            simpleModalBody = body;
            if (!consolidatedPresentation) SetVisible(modalPanel, true);
            if (consolidatedPresentation)
            {
                NotifyPresentationChanged();
                return;
            }
            ClearDynamic(modalButtonRoot, modalButtonTemplate);
            if (modalTitleText != null) modalTitleText.text = title;
            if (modalBodyText != null) modalBodyText.text = body;
            CreateButton(modalButtonTemplate, modalButtonRoot, "닫기", true, () =>
            {
                CloseSimpleModalInternal();
                ShowQueuedEvent();
            });
        }

        public void CancelActiveFlow() => flowRuntime.Cancel();

        private void CloseSimpleModalInternal()
        {
            simpleModalBusy = false;
            if (!consolidatedPresentation)
            {
                SetVisible(modalPanel, false);
                ClearDynamic(modalButtonRoot, modalButtonTemplate);
            }
            simpleModalCloseAction = null;
            simpleModalTitle = null;
            simpleModalBody = null;
            NotifyPresentationChanged();
        }

        public void HandleBackFromNavigation()
        {
            if (!ModalBusy) return;
            if (flowRuntime.IsActive)
            {
                flowRuntime.HandleBack();
                return;
            }
            simpleModalCloseAction?.Invoke();
        }

        public void CloseSimpleModal()
        {
            if (flowRuntime.IsActive) return;
            simpleModalCloseAction?.Invoke();
            ShowQueuedEvent();
        }

        public bool IsForcedEventChoice() => flowRuntime.IsForcedEventChoice();
        public IReadOnlyList<SandPlanetFlowNode04> CurrentFlowRows() => flowRuntime.CurrentRows();

        public string CurrentFlowTitle()
        {
            if (flowRuntime.HasActiveInteractionFlow && flowRuntime.ActiveInteraction != null) return flowRuntime.ActiveInteraction.DisplayText;
            return flowRuntime.HasActiveEventFlow ? flowRuntime.ActiveEventFlow.Name : SimpleModalTitle;
        }

        public string CurrentFlowBody()
        {
            if (!flowRuntime.IsActive) return SimpleModalBody;
            List<SandPlanetFlowNode04> rows = flowRuntime.CurrentRows().ToList();
            if (rows.Count == 0) return string.Empty;
            bool sameBody = rows.Select(row => row.BodyText ?? string.Empty).Distinct().Count() <= 1;
            if (rows.Count > 1 && !sameBody)
                return flowRuntime.HasActiveInteractionFlow ? flowRuntime.ActiveInteraction.DisplayText : flowRuntime.ActiveEventFlow.PlayerPerceivedChange;
            return FormatNodeBody(rows[0]);
        }

        public string CurrentSpeakerCharacterId()
        {
            SandPlanetFlowNode04 row = flowRuntime.CurrentRows().FirstOrDefault();
            if (row == null || !string.Equals(row.PresentationType, "DIALOGUE", StringComparison.OrdinalIgnoreCase)) return string.Empty;
            return content.Characters.ContainsKey(row.Speaker ?? string.Empty) ? row.Speaker : string.Empty;
        }

        public bool CanExecuteNode(SandPlanetFlowNode04 node, out string reason, out string costLabel)
        {
            Prototype04NodeCheck check = flowRuntime.CheckNode(node);
            reason = check.Reason;
            costLabel = BuildCostLabel(node, check);
            return check.CanExecute;
        }

        public void ExecuteNode(SandPlanetFlowNode04 node) => flowRuntime.ExecuteNode(node);

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
            if (!consolidatedPresentation && hudText != null)
                hudText.text = $"DAY {Day:00}  {Hour:00}:00   |   의지 {Will}/{MaxWill}   |   개인 Lv{PersonalLevel} {PersonalXp}/6   대인 Lv{SocialLevel} {SocialXp}/6   기술 Lv{TechnicalLevel} {TechnicalXp}/6   |   {(Day >= 15 ? "수송선 내부" : "모래 행성")}";
            if (!consolidatedPresentation && questTrackerText != null)
            {
                List<string> lines = new List<string> { "[진행 중 Quest]" };
                foreach (SandPlanetQuest04 quest in content.Quests.Values.Where(item => item.TrackerVisible && GetQuestStatus(item.Id) == "ACTIVE").OrderBy(item => TypeOrder(item.Type)).ThenBy(item => item.LogOrder).ThenBy(item => item.Title))
                {
                    string stepId = GetQuestStep(quest.Id);
                    string tracker = content.QuestSteps.TryGetValue(stepId, out SandPlanetQuestStep04 step) ? step.TrackerText : string.Empty;
                    string type = quest.Type == "CHARACTER" ? "CHAR" : quest.Type;
                    lines.Add($"• <color={QuestColor(quest.Type)}>[{type}]</color> " + quest.Title + (string.IsNullOrEmpty(tracker) ? string.Empty : " — " + tracker));
                }
                if (lines.Count == 1) lines.Add("• 없음");
                questTrackerText.supportRichText = true;
                questTrackerText.text = string.Join("\n", lines);
            }
            if (logText != null) logText.text = RecentLogText();
            NotifyPresentationChanged();
        }

        public string GetState(string id) => gameState.GetState(id);
        public int GetAffinity(string id) => gameState.GetAffinity(id);
        public string GetQuestStatus(string id) => questService.GetStatus(id);
        public string GetQuestStep(string id) => questService.GetStep(id);

        public void ApplyStartingStats(int personal, int social, int technical)
        {
            gameState.ApplyStartingStats(personal, social, technical);
            NotifyPresentationChanged();
        }

        public IReadOnlyList<Prototype04TargetViewModel> CurrentTargets()
        {
            List<Prototype04TargetViewModel> result = new List<Prototype04TargetViewModel>();
            if (string.IsNullOrEmpty(currentLocationId) || content == null) return result;
            foreach (SandPlanetCharacter04 character in content.Characters.Values.Where(item => item.Active && GetCharacterLocation(item.Id) == currentLocationId).OrderBy(item => item.Name))
                result.Add(new Prototype04TargetViewModel("CHARACTER", character.Id, character.Name));
            foreach (SandPlanetWorldTarget04 target in content.WorldTargets.Values.Where(item => item.Active && item.Clickable && item.LocationId == currentLocationId).OrderBy(item => item.Name))
                result.Add(new Prototype04TargetViewModel("WORLD_TARGET", target.Id, target.Name));
            return result;
        }

        private string InteractionBadge(SandPlanetInteraction04 interaction)
        {
            string role = Prototype04InteractionService.NormalizeQuestRole(interaction);
            if (string.IsNullOrEmpty(interaction.QuestId) || role == "NONE") return $"<color={BasicColor}>[일상]</color>  ";
            if (!content.Quests.TryGetValue(interaction.QuestId, out SandPlanetQuest04 quest)) return string.Empty;
            string marker = role == "OFFER" ? "?" : role == "PROGRESS" ? "!" : string.Empty;
            string typeLabel = quest.Type == "CHARACTER" ? "CHAR" : quest.Type == "SIDE" ? "SIDE" : "MAIN";
            return $"<color={QuestColor(quest.Type)}><b>[{typeLabel}{(string.IsNullOrEmpty(marker) ? string.Empty : " " + marker)}]</b></color>  ";
        }

        private static int QuestRoleOrder(string role) => role == "OFFER" ? 3 : role == "PROGRESS" ? 2 : 1;
        private static int TypeOrder(string type) => type == "MAIN" ? 0 : type == "CHARACTER" ? 1 : 2;
        private static string QuestColor(string type) => string.Equals(type, "MAIN", StringComparison.OrdinalIgnoreCase) ? MainColor : string.Equals(type, "CHARACTER", StringComparison.OrdinalIgnoreCase) ? CharacterColor : SideColor;

        private string GetTargetName(string type, string id)
        {
            if (type == "CHARACTER" && content.Characters.TryGetValue(id, out SandPlanetCharacter04 character)) return character.Name;
            if (type == "WORLD_TARGET" && content.WorldTargets.TryGetValue(id, out SandPlanetWorldTarget04 target)) return target.Name;
            return id;
        }

        private void Log(string message)
        {
            recentLog.Add($"D{gameState.Day:00} {gameState.Hour:00}:00  {message}");
            while (recentLog.Count > 40) recentLog.RemoveAt(0);
            if (logText != null) logText.text = RecentLogText();
        }

        private string RecentLogText()
        {
            int count = Math.Min(8, recentLog.Count);
            return count == 0 ? string.Empty : string.Join("\n", recentLog.GetRange(recentLog.Count - count, count));
        }

        private void NotifyPresentationChanged()
        {
            if (consolidatedPresentation) PresentationChanged?.Invoke();
        }

        private static void SetVisible(GameObject gameObject, bool visible)
        {
            if (gameObject != null) gameObject.SetActive(visible);
        }

        private static void ClearDynamic(Transform root, Button template)
        {
            if (root == null) return;
            for (int index = root.childCount - 1; index >= 0; index--)
            {
                Transform child = root.GetChild(index);
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
    }
}
