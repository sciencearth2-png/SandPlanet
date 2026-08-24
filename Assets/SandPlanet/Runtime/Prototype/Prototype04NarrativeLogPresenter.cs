using System;
using System.Collections.Generic;
using System.Linq;
using SandPlanet.Prototype.DataDriven;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SandPlanet.Prototype
{
    /// <summary>
    /// Consolidated right-side narrative presenter for the readable-log vertical slice.
    /// Gameplay authority remains in Prototype04FlowRuntime; this component only owns presentation.
    /// </summary>
    public sealed class Prototype04NarrativeLogPresenter : MonoBehaviour
    {
        private static readonly Vector2 RightMin = new Vector2(.739f, .018f);
        private static readonly Vector2 RightMax = new Vector2(.995f, .837f);
        private const string HoverHint = "선택지에 마우스를 올리면 즉시 바뀌는 결과를 확인할 수 있습니다.";

        private readonly Dictionary<string, Sprite> portraitCache = new Dictionary<string, Sprite>(StringComparer.Ordinal);
        private readonly List<string> transcript = new List<string>();

        private Prototype04RuntimeFacade runtime;
        private Canvas canvas;
        private GameObject interactionPanel;
        private Transform interactionRoot;
        private Button interactionTemplate;
        private Text interactionHeader;
        private GameObject modalPanel;
        private Text modalTitle;
        private Text modalBody;
        private Transform modalRoot;
        private Button modalTemplate;
        private ScrollRect narrativeScroll;
        private GameObject hoverPanel;
        private Text hoverText;
        private GameObject portraitFrame;
        private Image portraitImage;
        private Text portraitName;
        private string interactionSignature = string.Empty;
        private string narrativeSignature = string.Empty;
        private string activeFlowKey = string.Empty;
        private string lastNodeSignature = string.Empty;

        public void Initialize(Prototype04RuntimeFacade runtimeFacade)
        {
            runtime = runtimeFacade;
            interactionRoot = runtime.InteractionRoot;
            interactionTemplate = runtime.InteractionTemplate;
            modalPanel = runtime.ModalPanel;
            modalTitle = runtime.ModalTitle;
            modalBody = runtime.ModalBody;
            modalRoot = runtime.ModalButtonRoot;
            modalTemplate = runtime.ModalButtonTemplate;
            interactionPanel = interactionRoot != null && interactionRoot.parent != null ? interactionRoot.parent.gameObject : null;
            canvas = runtime.LocationPanel != null ? runtime.LocationPanel.GetComponentInParent<Canvas>() : null;

            if (canvas == null || interactionPanel == null || interactionTemplate == null || modalPanel == null || modalBody == null || modalRoot == null || modalTemplate == null)
            {
                enabled = false;
                return;
            }

            ConfigureStaticLayout();
            CreatePortrait();
            runtime.Changed += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            if (runtime != null) runtime.Changed -= Refresh;
        }

        private void ConfigureStaticLayout()
        {
            Font font = runtime.LocationTitle != null && runtime.LocationTitle.font != null
                ? runtime.LocationTitle.font
                : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            interactionPanel.transform.SetParent(canvas.transform, false);
            SetRect(interactionPanel.GetComponent<RectTransform>(), RightMin, RightMax);
            Image interactionImage = interactionPanel.GetComponent<Image>();
            if (interactionImage != null)
            {
                interactionImage.color = new Color(.035f, .043f, .055f, .98f);
                interactionImage.raycastTarget = true;
            }

            GameObject headerObject = new GameObject("UX_NarrativeInteractionHeader", typeof(RectTransform), typeof(Text));
            headerObject.transform.SetParent(interactionPanel.transform, false);
            interactionHeader = headerObject.GetComponent<Text>();
            interactionHeader.font = font;
            interactionHeader.fontSize = 24;
            interactionHeader.fontStyle = FontStyle.Bold;
            interactionHeader.color = Color.white;
            interactionHeader.alignment = TextAnchor.MiddleLeft;
            interactionHeader.raycastTarget = false;
            SetRect(interactionHeader.rectTransform, new Vector2(.065f, .875f), new Vector2(.935f, .97f));

            SetRect(interactionRoot as RectTransform, new Vector2(.06f, .055f), new Vector2(.94f, .865f));
            ConfigureVerticalRoot(interactionRoot, 10f, TextAnchor.UpperCenter);
            StyleTemplate(interactionTemplate, 84f, 17, TextAnchor.MiddleLeft);

            SetRect(modalPanel.GetComponent<RectTransform>(), RightMin, RightMax);
            Image modalImage = modalPanel.GetComponent<Image>();
            if (modalImage != null)
            {
                modalImage.color = new Color(.02f, .027f, .035f, .985f);
                modalImage.raycastTarget = true;
            }

            if (modalTitle != null)
            {
                modalTitle.fontSize = 25;
                modalTitle.fontStyle = FontStyle.Bold;
                modalTitle.alignment = TextAnchor.MiddleLeft;
                modalTitle.horizontalOverflow = HorizontalWrapMode.Wrap;
                modalTitle.verticalOverflow = VerticalWrapMode.Truncate;
                SetRect(modalTitle.rectTransform, new Vector2(.065f, .865f), new Vector2(.935f, .965f));
            }

            CreateNarrativeScroll(font);
            CreateHoverPanel(font);

            SetRect(modalRoot as RectTransform, new Vector2(.055f, .025f), new Vector2(.945f, .285f));
            ConfigureVerticalRoot(modalRoot, 7f, TextAnchor.LowerCenter);
            StyleTemplate(modalTemplate, 64f, 16, TextAnchor.MiddleLeft);

            interactionTemplate.gameObject.SetActive(false);
            modalTemplate.gameObject.SetActive(false);
        }

        private void CreateNarrativeScroll(Font font)
        {
            Transform oldParent = modalBody.transform.parent;
            GameObject scrollObject = new GameObject("UX_NarrativeLogScroll", typeof(RectTransform), typeof(ScrollRect));
            scrollObject.transform.SetParent(oldParent, false);
            SetRect(scrollObject.GetComponent<RectTransform>(), new Vector2(.055f, .39f), new Vector2(.945f, .85f));

            GameObject viewportObject = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewportObject.transform.SetParent(scrollObject.transform, false);
            RectTransform viewport = viewportObject.GetComponent<RectTransform>();
            SetRect(viewport, Vector2.zero, Vector2.one);
            Image viewportImage = viewportObject.GetComponent<Image>();
            viewportImage.color = new Color(0f, 0f, 0f, .01f);
            viewportImage.raycastTarget = true;
            viewportObject.GetComponent<Mask>().showMaskGraphic = false;

            modalBody.transform.SetParent(viewportObject.transform, false);
            RectTransform bodyRect = modalBody.rectTransform;
            bodyRect.anchorMin = new Vector2(0f, 1f);
            bodyRect.anchorMax = new Vector2(1f, 1f);
            bodyRect.pivot = new Vector2(.5f, 1f);
            bodyRect.anchoredPosition = Vector2.zero;
            bodyRect.sizeDelta = Vector2.zero;
            modalBody.font = font;
            modalBody.fontSize = 18;
            modalBody.fontStyle = FontStyle.Normal;
            modalBody.lineSpacing = 1.14f;
            modalBody.color = new Color(.91f, .93f, .95f, 1f);
            modalBody.alignment = TextAnchor.UpperLeft;
            modalBody.horizontalOverflow = HorizontalWrapMode.Wrap;
            modalBody.verticalOverflow = VerticalWrapMode.Overflow;
            modalBody.supportRichText = true;
            modalBody.raycastTarget = false;

            ContentSizeFitter fitter = modalBody.GetComponent<ContentSizeFitter>();
            if (fitter == null) fitter = modalBody.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            narrativeScroll = scrollObject.GetComponent<ScrollRect>();
            narrativeScroll.viewport = viewport;
            narrativeScroll.content = bodyRect;
            narrativeScroll.horizontal = false;
            narrativeScroll.vertical = true;
            narrativeScroll.inertia = true;
            narrativeScroll.decelerationRate = .12f;
            narrativeScroll.scrollSensitivity = 24f;
            narrativeScroll.movementType = ScrollRect.MovementType.Clamped;
        }

        private void CreateHoverPanel(Font font)
        {
            hoverPanel = new GameObject("UX_NarrativeChoicePreview", typeof(RectTransform), typeof(Image));
            hoverPanel.transform.SetParent(modalPanel.transform, false);
            SetRect(hoverPanel.GetComponent<RectTransform>(), new Vector2(.055f, .295f), new Vector2(.945f, .38f));
            Image image = hoverPanel.GetComponent<Image>();
            image.color = new Color(.075f, .095f, .12f, .98f);
            image.raycastTarget = false;

            GameObject textObject = new GameObject("PreviewText", typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(hoverPanel.transform, false);
            SetRect(textObject.GetComponent<RectTransform>(), new Vector2(.035f, .08f), new Vector2(.965f, .92f));
            hoverText = textObject.GetComponent<Text>();
            hoverText.font = font;
            hoverText.fontSize = 14;
            hoverText.color = new Color(.80f, .86f, .91f, 1f);
            hoverText.alignment = TextAnchor.MiddleLeft;
            hoverText.horizontalOverflow = HorizontalWrapMode.Wrap;
            hoverText.verticalOverflow = VerticalWrapMode.Truncate;
            hoverText.supportRichText = true;
            hoverText.raycastTarget = false;
            hoverText.text = HoverHint;
        }

        private void Refresh()
        {
            if (!enabled || runtime == null) return;

            if (runtime.ModalBusy)
            {
                interactionPanel.SetActive(false);
                modalPanel.SetActive(true);
                modalPanel.transform.SetAsLastSibling();
                RenderNarrative();
                RefreshPortrait(runtime.CurrentSpeakerCharacterId());
                return;
            }

            modalPanel.SetActive(false);
            ResetNarrativeState();
            ClearDynamic(modalRoot, modalTemplate);

            if (!string.IsNullOrEmpty(runtime.SelectedTargetId))
            {
                RenderInteractions();
                interactionPanel.SetActive(true);
                interactionPanel.transform.SetAsLastSibling();
                RefreshPortrait(runtime.SelectedTargetType == "CHARACTER" ? runtime.SelectedTargetId : string.Empty);
            }
            else
            {
                interactionPanel.SetActive(false);
                interactionSignature = string.Empty;
                ClearDynamic(interactionRoot, interactionTemplate);
                RefreshPortrait(string.Empty);
            }
        }

        private void RenderInteractions()
        {
            List<SandPlanetInteraction04> interactions = runtime.AvailableInteractions(runtime.SelectedTargetType, runtime.SelectedTargetId)
                .Where(i => !string.Equals(i.EntryMode, "DIRECT_CLICK", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(i => RoleOrder(Prototype04RuntimeFacade.NormalizeQuestRole(i)))
                .ThenByDescending(i => i.Priority)
                .ThenBy(i => i.DisplayText)
                .ToList();

            string signature = runtime.SelectedTargetType + ":" + runtime.SelectedTargetId + "|" + string.Join("|", interactions.Select(i => i.Id));
            if (signature == interactionSignature) return;
            interactionSignature = signature;
            ClearDynamic(interactionRoot, interactionTemplate);
            interactionHeader.text = runtime.TargetName(runtime.SelectedTargetType, runtime.SelectedTargetId);

            if (interactions.Count == 0)
            {
                CreateInteractionButton("현재 가능한 상호작용 없음", false, null);
                return;
            }

            foreach (SandPlanetInteraction04 interaction in interactions)
            {
                SandPlanetInteraction04 captured = interaction;
                CreateInteractionButton(InteractionLabel(interaction), true, () => runtime.BeginInteraction(captured));
            }
        }

        private string InteractionLabel(SandPlanetInteraction04 interaction)
        {
            string world = string.Equals(interaction.EntryMode, "WORLD_MARKER", StringComparison.OrdinalIgnoreCase) ? "◆ " : string.Empty;
            string role = Prototype04RuntimeFacade.NormalizeQuestRole(interaction);
            if (string.IsNullOrEmpty(interaction.QuestId) || (role != "OFFER" && role != "PROGRESS") ||
                !runtime.Content.Quests.TryGetValue(interaction.QuestId, out SandPlanetQuest04 quest))
                return world + interaction.DisplayText;

            string marker = role == "OFFER" ? "?" : "!";
            return world + interaction.DisplayText + "\n<size=12><color=" + Prototype04RuntimeFacade.QuestColorHex(quest.Type) + ">" + marker + " " + quest.Title + "</color></size>";
        }

        private void RenderNarrative()
        {
            IReadOnlyList<SandPlanetFlowNode04> rows = runtime.CurrentFlowRows();
            if (!runtime.HasActiveInteractionFlow && !runtime.HasActiveEventFlow)
            {
                RenderSimpleModal();
                return;
            }

            EnsureFlowTranscript();
            AppendCurrentNode(rows);

            string signature = CurrentFlowKey() + "|" + lastNodeSignature + "|" + string.Join("|", rows.Select(r => r.NodeId + ":" + r.ChoiceId + ":" + r.NextNodeId));
            if (signature == narrativeSignature) return;
            narrativeSignature = signature;

            if (modalTitle != null) modalTitle.text = runtime.CurrentFlowTitle();
            modalBody.text = string.Join("\n\n", transcript);
            ClearDynamic(modalRoot, modalTemplate);
            SetHover(HoverHint);

            if (rows.Count == 0)
            {
                CreateModalButton("닫기", true, runtime.CloseSimpleModal, null, null);
                ScrollToBottom();
                return;
            }

            foreach (SandPlanetFlowNode04 row in rows)
            {
                bool canExecute = runtime.CanExecuteNode(row, out string reason, out _);
                string label = ChoiceLabel(row, canExecute, reason);
                SandPlanetFlowNode04 captured = row;
                CreateModalButton(label, canExecute, () => ExecuteChoice(captured), QuestActionFor(row), PreviewFor(row, canExecute, reason));
            }

            if (runtime.HasActiveInteractionFlow && !runtime.FlowCommitted)
                CreateModalButton("취소", true, runtime.CancelFlow, null, "아직 선택한 결과를 확정하지 않고 대화를 닫습니다.");

            ScrollToBottom();
        }

        private void RenderSimpleModal()
        {
            string signature = "SIMPLE|" + runtime.CurrentFlowTitle() + "|" + runtime.CurrentFlowBody();
            if (signature == narrativeSignature) return;
            ResetNarrativeState();
            narrativeSignature = signature;
            if (modalTitle != null) modalTitle.text = runtime.CurrentFlowTitle();
            if (modalBody != null) modalBody.text = runtime.CurrentFlowBody();
            ClearDynamic(modalRoot, modalTemplate);
            SetHover(HoverHint);
            CreateModalButton("닫기", true, runtime.CloseSimpleModal, null, null);
            ScrollToBottom();
        }

        private void EnsureFlowTranscript()
        {
            string key = CurrentFlowKey();
            if (key == activeFlowKey) return;
            activeFlowKey = key;
            transcript.Clear();
            lastNodeSignature = string.Empty;
            narrativeSignature = string.Empty;
        }

        private string CurrentFlowKey()
        {
            if (runtime.HasActiveInteractionFlow && runtime.ActiveInteraction != null)
                return "I:" + runtime.ActiveInteraction.Id;
            if (runtime.HasActiveEventFlow)
                return "E:" + runtime.CurrentFlowTitle();
            return "S:" + runtime.CurrentFlowTitle();
        }

        private void AppendCurrentNode(IReadOnlyList<SandPlanetFlowNode04> rows)
        {
            if (rows == null || rows.Count == 0) return;
            SandPlanetFlowNode04 first = rows[0];
            string body = FormatNodeBody(first);
            string signature = first.NodeId + "|" + first.Speaker + "|" + body;
            if (signature == lastNodeSignature) return;
            lastNodeSignature = signature;
            if (!string.IsNullOrWhiteSpace(body)) transcript.Add(body);
        }

        private string FormatNodeBody(SandPlanetFlowNode04 row)
        {
            string body = row?.BodyText ?? string.Empty;
            if (row == null) return body;
            string speaker = SpeakerName(row.Speaker);
            if (string.Equals(row.PresentationType, "DIALOGUE", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(speaker))
                return "<b>" + speaker + "</b> - " + body;
            return body;
        }

        private void ExecuteChoice(SandPlanetFlowNode04 row)
        {
            AppendChosenText(row);
            modalBody.text = string.Join("\n\n", transcript);
            ScrollToBottom();
            runtime.ExecuteNode(row);
        }

        private void AppendChosenText(SandPlanetFlowNode04 row)
        {
            string choice = row?.ChoiceText ?? string.Empty;
            if (string.IsNullOrWhiteSpace(choice)) return;
            if (string.Equals(choice, "계속", StringComparison.OrdinalIgnoreCase) || string.Equals(choice, "종료", StringComparison.OrdinalIgnoreCase)) return;

            bool dialogue = string.Equals(row.PresentationType, "DIALOGUE", StringComparison.OrdinalIgnoreCase);
            string prefix = dialogue ? "<color=#CFE3F3><b>제이</b> - " : "<color=#CFE3F3>▶ ";
            transcript.Add(prefix + choice + "</color>");
        }

        private string ChoiceLabel(SandPlanetFlowNode04 row, bool canExecute, string reason)
        {
            string label = string.IsNullOrEmpty(row.ChoiceText)
                ? (string.IsNullOrEmpty(row.NextNodeId) ? "종료" : "계속")
                : row.ChoiceText;
            string requirement = RequirementLabel(row);
            if (!string.IsNullOrEmpty(requirement))
                label += "\n<size=13><color=#AFC4D6>" + requirement + "</color></size>";
            if (!canExecute)
                label += "\n<size=12><color=#E7A0A0>잠김 · " + reason + "</color></size>";
            return label;
        }

        private string RequirementLabel(SandPlanetFlowNode04 row)
        {
            List<string> parts = new List<string>();
            AddVisibleHardCondition(parts, row.HardCondition1);
            AddVisibleHardCondition(parts, row.HardCondition2);

            int extraWill = ExtraWillCost(row);
            if (!string.IsNullOrEmpty(row.SoftStat) && !string.Equals(row.SoftStat, "NONE", StringComparison.OrdinalIgnoreCase) && row.SoftRequirement > 0)
            {
                int current = StatLevel(row.SoftStat);
                string stat = StatLabel(row.SoftStat);
                parts.Add(extraWill > 0
                    ? stat + " " + current + "/" + row.SoftRequirement + " + 의지 " + extraWill + " 보완"
                    : stat + " ≥ " + row.SoftRequirement);
            }

            int explicitWillCost = Math.Max(0, -row.WillDelta);
            if (explicitWillCost > 0) parts.Add("의지 -" + explicitWillCost);
            if (row.TimeCost > 0) parts.Add(row.TimeCost + "시간");
            return parts.Count == 0 ? string.Empty : "[" + string.Join(" / ", parts) + "]";
        }

        private void AddVisibleHardCondition(List<string> output, SandPlanetCondition04 condition)
        {
            if (condition == null || string.IsNullOrEmpty(condition.Type)) return;
            string op = OperatorLabel(condition.Operator);
            switch ((condition.Type ?? string.Empty).ToUpperInvariant())
            {
                case "AFFINITY":
                    output.Add(runtime.TargetName("CHARACTER", condition.Key) + " 호감도 " + op + " " + condition.Value + " (현재 " + runtime.AffinityValue(condition.Key) + ")");
                    break;
                case "STAT_LEVEL":
                    output.Add(StatLabel(condition.Key) + " " + op + " " + condition.Value + " (현재 " + StatLevel(condition.Key) + ")");
                    break;
                case "DAY":
                    output.Add("Day " + op + " " + condition.Value);
                    break;
                case "TIME":
                    output.Add("시간 " + op + " " + condition.Value + ":00");
                    break;
            }
        }

        private string PreviewFor(SandPlanetFlowNode04 row, bool canExecute, string reason)
        {
            List<string> parts = new List<string>();
            if (row.TimeCost != 0) parts.Add("시간 " + Signed(-row.TimeCost) + "h");

            int extraWill = ExtraWillCost(row);
            int willDelta = row.WillDelta - extraWill;
            if (willDelta != 0) parts.Add("의지 " + Signed(willDelta));
            if (row.PersonalXpDelta != 0) parts.Add("강인함 경험치 " + Signed(row.PersonalXpDelta));
            if (row.SocialXpDelta != 0) parts.Add("교감 경험치 " + Signed(row.SocialXpDelta));
            if (row.TechnicalXpDelta != 0) parts.Add("기술 경험치 " + Signed(row.TechnicalXpDelta));

            foreach (SandPlanetAffinityChange04 affinity in row.AffinityChanges)
            {
                if (affinity == null || affinity.Delta == 0) continue;
                parts.Add(runtime.TargetName("CHARACTER", affinity.CharacterId) + " 호감도 " + Signed(affinity.Delta));
            }

            string text = parts.Count == 0
                ? "<b>예상 결과</b>  수치 변화 없음"
                : "<b>예상 결과</b>  " + string.Join("   ·   ", parts);
            if (!canExecute) text += "\n<color=#E7A0A0>현재 선택 불가 · " + reason + "</color>";
            return text;
        }

        private int ExtraWillCost(SandPlanetFlowNode04 row)
        {
            if (row == null || string.IsNullOrEmpty(row.SoftStat) || string.Equals(row.SoftStat, "NONE", StringComparison.OrdinalIgnoreCase) || row.SoftRequirement <= 0)
                return 0;
            return Math.Max(0, row.SoftRequirement - StatLevel(row.SoftStat));
        }

        private int StatLevel(string stat)
        {
            switch ((stat ?? string.Empty).ToUpperInvariant())
            {
                case "PERSONAL": return runtime.PersonalLevel;
                case "SOCIAL": return runtime.SocialLevel;
                case "TECHNICAL": return runtime.TechnicalLevel;
                default: return 0;
            }
        }

        private static string StatLabel(string stat)
        {
            switch ((stat ?? string.Empty).ToUpperInvariant())
            {
                case "PERSONAL": return "강인함";
                case "SOCIAL": return "교감";
                case "TECHNICAL": return "기술";
                default: return stat ?? string.Empty;
            }
        }

        private static string OperatorLabel(string op)
        {
            switch ((op ?? string.Empty).ToUpperInvariant())
            {
                case "GE": return "≥";
                case "GT": return ">";
                case "LE": return "≤";
                case "LT": return "<";
                case "NE": return "≠";
                default: return "=";
            }
        }

        private static string Signed(int value) => value > 0 ? "+" + value : value.ToString();

        private string SpeakerName(string speaker)
        {
            if (string.IsNullOrEmpty(speaker) || speaker == "NARRATOR" || speaker == "NONE") return string.Empty;
            if (speaker == "PLAYER") return "제이";
            return runtime.Content.Characters.TryGetValue(speaker, out SandPlanetCharacter04 character) ? character.Name : speaker;
        }

        private void ScrollToBottom()
        {
            if (narrativeScroll == null) return;
            Canvas.ForceUpdateCanvases();
            narrativeScroll.verticalNormalizedPosition = 0f;
        }

        private void SetHover(string text)
        {
            if (hoverText != null) hoverText.text = string.IsNullOrEmpty(text) ? HoverHint : text;
        }

        private void ResetNarrativeState()
        {
            activeFlowKey = string.Empty;
            lastNodeSignature = string.Empty;
            narrativeSignature = string.Empty;
            transcript.Clear();
            SetHover(HoverHint);
        }

        private QuestBadge QuestActionFor(SandPlanetFlowNode04 node)
        {
            SandPlanetQuestActionMeta04 meta = null;
            if (runtime.HasActiveInteractionFlow) runtime.Content.InteractionNodeMeta.TryGetValue(node, out meta);
            else runtime.Content.EventNodeMeta.TryGetValue(node, out meta);
            if (meta == null || string.IsNullOrEmpty(meta.QuestAction) || string.IsNullOrEmpty(meta.QuestId) ||
                !runtime.Content.Quests.TryGetValue(meta.QuestId, out SandPlanetQuest04 quest) || !WillApply(meta)) return null;

            string action;
            switch (meta.QuestAction.ToUpperInvariant())
            {
                case "ACTIVATE_QUEST": action = "수락"; break;
                case "SET_QUEST_STEP": action = "진행"; break;
                case "COMPLETE_QUEST": action = "완료"; break;
                case "FAIL_QUEST": action = "실패"; break;
                default: return null;
            }
            ColorUtility.TryParseHtmlString(Prototype04RuntimeFacade.QuestColorHex(quest.Type), out Color color);
            return new QuestBadge(quest.Title + "  ·  " + action, color);
        }

        private bool WillApply(SandPlanetQuestActionMeta04 meta)
        {
            string status = runtime.QuestStatus(meta.QuestId);
            switch (meta.QuestAction.ToUpperInvariant())
            {
                case "ACTIVATE_QUEST": return status == "LOCKED";
                case "SET_QUEST_STEP": return status == "ACTIVE" && !string.IsNullOrEmpty(meta.QuestStepId) && runtime.QuestStep(meta.QuestId) != meta.QuestStepId;
                case "COMPLETE_QUEST":
                case "FAIL_QUEST": return status == "ACTIVE";
                default: return false;
            }
        }

        private void CreateInteractionButton(string label, bool interactable, UnityEngine.Events.UnityAction click)
        {
            Button button = Instantiate(interactionTemplate, interactionRoot);
            button.gameObject.SetActive(true);
            button.interactable = interactable;
            ApplyHeight(button, 84f);
            Text text = button.GetComponentInChildren<Text>(true);
            if (text != null)
            {
                text.text = label;
                text.supportRichText = true;
                text.fontSize = 17;
                text.lineSpacing = 1.08f;
                text.alignment = TextAnchor.MiddleLeft;
                text.horizontalOverflow = HorizontalWrapMode.Wrap;
                text.verticalOverflow = VerticalWrapMode.Truncate;
            }
            button.onClick.RemoveAllListeners();
            if (click != null) button.onClick.AddListener(click);
        }

        private void CreateModalButton(string label, bool interactable, UnityEngine.Events.UnityAction click, QuestBadge badge, string preview)
        {
            Button button = Instantiate(modalTemplate, modalRoot);
            button.gameObject.SetActive(true);
            button.interactable = interactable;
            button.onClick.RemoveAllListeners();
            if (click != null) button.onClick.AddListener(click);

            Text text = button.GetComponentInChildren<Text>(true);
            if (text != null)
            {
                text.text = label;
                text.supportRichText = true;
                text.fontSize = badge == null ? 16 : 15;
                text.alignment = TextAnchor.MiddleLeft;
                text.horizontalOverflow = HorizontalWrapMode.Wrap;
                text.verticalOverflow = VerticalWrapMode.Truncate;
                SetRect(text.rectTransform, badge == null ? new Vector2(.035f,.08f) : new Vector2(.035f,.05f), badge == null ? new Vector2(.965f,.92f) : new Vector2(.965f,.62f));
            }

            ApplyHeight(button, badge == null ? (label.Contains("\n") ? 76f : 64f) : 94f);
            if (badge != null) CreateQuestChip(button, text, badge);
            AttachHover(button, preview);
        }

        private void AttachHover(Button button, string preview)
        {
            EventTrigger trigger = button.GetComponent<EventTrigger>();
            if (trigger == null) trigger = button.gameObject.AddComponent<EventTrigger>();
            trigger.triggers = new List<EventTrigger.Entry>();

            EventTrigger.Entry enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enter.callback.AddListener(_ => SetHover(preview));
            trigger.triggers.Add(enter);

            EventTrigger.Entry exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exit.callback.AddListener(_ => SetHover(HoverHint));
            trigger.triggers.Add(exit);
        }

        private static void CreateQuestChip(Button button, Text main, QuestBadge badge)
        {
            GameObject chip = new GameObject("QuestActionChip", typeof(RectTransform), typeof(Image));
            chip.transform.SetParent(button.transform, false);
            SetRect(chip.GetComponent<RectTransform>(), new Vector2(.025f,.66f), new Vector2(.975f,.96f));
            Image image = chip.GetComponent<Image>();
            image.color = new Color(badge.Color.r, badge.Color.g, badge.Color.b, .27f);
            image.raycastTarget = false;

            GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelObject.transform.SetParent(chip.transform, false);
            Text label = labelObject.GetComponent<Text>();
            label.font = main != null && main.font != null ? main.font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.text = badge.Label;
            label.color = badge.Color;
            label.fontSize = 13;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.raycastTarget = false;
            SetRect(label.rectTransform, Vector2.zero, Vector2.one);
        }

        private void CreatePortrait()
        {
            portraitFrame = new GameObject("UX_NarrativeSpeakerPortraitFrame", typeof(RectTransform), typeof(Image));
            portraitFrame.transform.SetParent(canvas.transform, false);
            RectTransform frame = portraitFrame.GetComponent<RectTransform>();
            frame.anchorMin = frame.anchorMax = new Vector2(.739f, .655f);
            frame.pivot = new Vector2(1f, .5f);
            frame.anchoredPosition = new Vector2(-14f, 0f);
            frame.sizeDelta = new Vector2(225f, 315f);
            portraitFrame.GetComponent<Image>().color = new Color(.045f,.050f,.055f,.98f);

            GameObject imageObject = new GameObject("Portrait", typeof(RectTransform), typeof(Image));
            imageObject.transform.SetParent(portraitFrame.transform, false);
            portraitImage = imageObject.GetComponent<Image>();
            portraitImage.preserveAspect = true;
            portraitImage.raycastTarget = false;
            SetRect(portraitImage.rectTransform, new Vector2(.045f,.145f), new Vector2(.955f,.97f));

            GameObject nameObject = new GameObject("Name", typeof(RectTransform), typeof(Text));
            nameObject.transform.SetParent(portraitFrame.transform, false);
            portraitName = nameObject.GetComponent<Text>();
            portraitName.font = runtime.ModalTitle != null && runtime.ModalTitle.font != null ? runtime.ModalTitle.font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            portraitName.fontSize = 17;
            portraitName.fontStyle = FontStyle.Bold;
            portraitName.color = Color.white;
            portraitName.alignment = TextAnchor.MiddleCenter;
            portraitName.raycastTarget = false;
            SetRect(portraitName.rectTransform, new Vector2(.04f,.015f), new Vector2(.96f,.145f));
            portraitFrame.SetActive(false);
        }

        private void RefreshPortrait(string characterId)
        {
            if (string.IsNullOrEmpty(characterId) || !runtime.Content.Characters.TryGetValue(characterId, out SandPlanetCharacter04 character))
            {
                portraitFrame.SetActive(false);
                return;
            }

            Sprite sprite = GetPortraitSprite(character);
            if (sprite == null)
            {
                portraitFrame.SetActive(false);
                return;
            }

            portraitImage.sprite = sprite;
            portraitImage.color = Color.white;
            portraitName.text = character.Name;
            portraitFrame.SetActive(true);
            portraitFrame.transform.SetAsLastSibling();
        }

        private Sprite GetPortraitSprite(SandPlanetCharacter04 character)
        {
            if (portraitCache.TryGetValue(character.Id, out Sprite cached)) return cached;
            Sprite sprite = Resources.Load<Sprite>("Portraits/" + character.Name);
            portraitCache[character.Id] = sprite;
            return sprite;
        }

        private static void ConfigureVerticalRoot(Transform root, float spacing, TextAnchor alignment)
        {
            ContentSizeFitter fitter = root.GetComponent<ContentSizeFitter>();
            if (fitter != null) fitter.enabled = false;
            VerticalLayoutGroup layout = root.GetComponent<VerticalLayoutGroup>();
            if (layout == null) return;
            layout.enabled = true;
            layout.spacing = spacing;
            layout.childAlignment = alignment;
            layout.padding = new RectOffset(0,0,0,0);
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;
        }

        private static void StyleTemplate(Button template, float height, int fontSize, TextAnchor alignment)
        {
            ApplyHeight(template, height);
            Text text = template.GetComponentInChildren<Text>(true);
            if (text == null) return;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
        }

        private static void ApplyHeight(Button button, float height)
        {
            LayoutElement layout = button.GetComponent<LayoutElement>();
            if (layout == null) layout = button.gameObject.AddComponent<LayoutElement>();
            layout.minHeight = height;
            layout.preferredHeight = height;
        }

        private static void ClearDynamic(Transform root, Button template)
        {
            if (root == null) return;
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                Transform child = root.GetChild(i);
                if (template != null && child == template.transform) continue;
                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }
        }

        private static int RoleOrder(string role) => role == "OFFER" ? 3 : role == "PROGRESS" ? 2 : 1;

        private static void SetRect(RectTransform rect, Vector2 min, Vector2 max)
        {
            if (rect == null) return;
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private sealed class QuestBadge
        {
            public readonly string Label;
            public readonly Color Color;
            public QuestBadge(string label, Color color) { Label = label; Color = color; }
        }
    }
}
