using System;
using System.Collections.Generic;
using System.Linq;
using SandPlanet.Prototype.DataDriven;
using UnityEngine;
using UnityEngine.UI;

namespace SandPlanet.Prototype
{
    public sealed class Prototype04NarrativePresenter : MonoBehaviour
    {
        private static readonly Vector2 RightMin = new Vector2(.739f, .018f);
        private static readonly Vector2 RightMax = new Vector2(.995f, .837f);
        private readonly Dictionary<string, Sprite> portraitCache = new Dictionary<string, Sprite>(StringComparer.Ordinal);
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
        private GameObject portraitFrame;
        private Image portraitImage;
        private Text portraitName;
        private string interactionSignature = string.Empty;
        private string narrativeSignature = string.Empty;

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
            if (canvas == null || interactionPanel == null || interactionTemplate == null || modalPanel == null || modalRoot == null || modalTemplate == null)
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
            interactionPanel.transform.SetParent(canvas.transform, false);
            SetRect(interactionPanel.GetComponent<RectTransform>(), RightMin, RightMax);
            Image interactionImage = interactionPanel.GetComponent<Image>();
            if (interactionImage != null)
            {
                interactionImage.color = new Color(.035f, .043f, .055f, .98f);
                interactionImage.raycastTarget = true;
            }
            Font font = runtime.LocationTitle != null && runtime.LocationTitle.font != null
                ? runtime.LocationTitle.font
                : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            GameObject headerObject = new GameObject("UX18_InteractionHeader", typeof(RectTransform), typeof(Text));
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
            if (modalBody != null)
            {
                modalBody.fontSize = 19;
                modalBody.fontStyle = FontStyle.Normal;
                modalBody.lineSpacing = 1.13f;
                modalBody.alignment = TextAnchor.UpperLeft;
                modalBody.horizontalOverflow = HorizontalWrapMode.Wrap;
                modalBody.verticalOverflow = VerticalWrapMode.Overflow;
                SetRect(modalBody.rectTransform, new Vector2(.065f, .365f), new Vector2(.935f, .845f));
            }
            SetRect(modalRoot as RectTransform, new Vector2(.055f, .025f), new Vector2(.945f, .34f));
            ConfigureVerticalRoot(modalRoot, 9f, TextAnchor.LowerCenter);
            StyleTemplate(modalTemplate, 66f, 17, TextAnchor.MiddleCenter);
            interactionTemplate.gameObject.SetActive(false);
            modalTemplate.gameObject.SetActive(false);
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
            narrativeSignature = string.Empty;
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
            string signature = runtime.CurrentFlowTitle() + "|" + runtime.CurrentFlowBody() + "|" + string.Join("|", rows.Select(r => r.NodeId + ":" + r.ChoiceId + ":" + r.NextNodeId));
            if (signature == narrativeSignature) return;
            narrativeSignature = signature;
            if (modalTitle != null) modalTitle.text = runtime.CurrentFlowTitle();
            if (modalBody != null) modalBody.text = runtime.CurrentFlowBody();
            ClearDynamic(modalRoot, modalTemplate);

            if (rows.Count == 0)
            {
                CreateModalButton("닫기", true, runtime.CloseSimpleModal, null);
                return;
            }
            foreach (SandPlanetFlowNode04 row in rows)
            {
                bool enabled = runtime.CanExecuteNode(row, out string reason, out string cost);
                string label = string.IsNullOrEmpty(row.ChoiceText) ? (string.IsNullOrEmpty(row.NextNodeId) ? "종료" : "계속") : row.ChoiceText;
                if (!enabled) label += "\n<" + reason + ">";
                SandPlanetFlowNode04 captured = row;
                CreateModalButton(label + cost, enabled, () => runtime.ExecuteNode(captured), QuestActionFor(row));
            }
            if (runtime.HasActiveInteractionFlow && !runtime.FlowCommitted)
                CreateModalButton("취소", true, runtime.CancelFlow, null);
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

        private void CreateModalButton(string label, bool interactable, UnityEngine.Events.UnityAction click, QuestBadge badge)
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
                text.fontSize = badge == null ? 17 : 15;
                text.alignment = TextAnchor.MiddleCenter;
                text.horizontalOverflow = HorizontalWrapMode.Wrap;
                text.verticalOverflow = VerticalWrapMode.Truncate;
                SetRect(text.rectTransform, badge == null ? new Vector2(.035f,.08f) : new Vector2(.035f,.05f), badge == null ? new Vector2(.965f,.92f) : new Vector2(.965f,.62f));
            }
            ApplyHeight(button, badge == null ? 66f : 88f);
            if (badge != null) CreateQuestChip(button, text, badge);
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
            portraitFrame = new GameObject("UX18_SpeakerPortraitFrame", typeof(RectTransform), typeof(Image));
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
