using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SandPlanet.Prototype.DataDriven;
using UnityEngine;
using UnityEngine.UI;

namespace SandPlanet.Prototype
{
    /// <summary>
    /// Shows QuestAction information on narrative choice buttons without covering
    /// the actual player choice. Quest-linked buttons become a real two-tier layout:
    /// a small quest strip on top and the original choice text below.
    /// </summary>
    [DefaultExecutionOrder(15000)]
    public sealed class SandPlanetPrototype04QuestActionBadge : MonoBehaviour
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private const string MainColor = "#F0A24A";
        private const string CharacterColor = "#69C77C";
        private const string SideColor = "#F1D784";
        private const float NormalHeight = 58f;
        private const float QuestHeight = 88f;

        private SandPlanetPrototype04Controller controller;
        private SandPlanetContent04 content;
        private FieldInfo contentField;
        private FieldInfo activeInteractionFlowField;
        private FieldInfo activeEventFlowField;
        private FieldInfo activeNodeIdField;
        private FieldInfo modalButtonRootField;
        private MethodInfo getQuestStatusMethod;
        private MethodInfo getQuestStepMethod;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            SandPlanetPrototype04Controller found = UnityEngine.Object.FindFirstObjectByType<SandPlanetPrototype04Controller>();
            if (found != null && found.GetComponent<SandPlanetPrototype04QuestActionBadge>() == null)
                found.gameObject.AddComponent<SandPlanetPrototype04QuestActionBadge>();
        }

        private void Awake()
        {
            controller = GetComponent<SandPlanetPrototype04Controller>();
            if (controller == null)
            {
                enabled = false;
                return;
            }

            Type type = controller.GetType();
            contentField = type.GetField("content", PrivateInstance);
            activeInteractionFlowField = type.GetField("activeInteractionFlow", PrivateInstance);
            activeEventFlowField = type.GetField("activeEventFlow", PrivateInstance);
            activeNodeIdField = type.GetField("activeNodeId", PrivateInstance);
            modalButtonRootField = type.GetField("modalButtonRoot", PrivateInstance);
            getQuestStatusMethod = type.GetMethod("GetQuestStatus", PrivateInstance);
            getQuestStepMethod = type.GetMethod("GetQuestStep", PrivateInstance);
        }

        private void OnEnable() => Canvas.willRenderCanvases += Refresh;
        private void OnDisable() => Canvas.willRenderCanvases -= Refresh;

        private void Refresh()
        {
            if (controller == null) return;
            if (content == null && contentField != null)
                content = contentField.GetValue(controller) as SandPlanetContent04;
            if (content == null || modalButtonRootField == null || activeNodeIdField == null) return;

            Transform root = modalButtonRootField.GetValue(controller) as Transform;
            string nodeId = activeNodeIdField.GetValue(controller) as string;
            if (root == null || string.IsNullOrEmpty(nodeId)) return;

            SandPlanetInteractionFlow04 interactionFlow = activeInteractionFlowField?.GetValue(controller) as SandPlanetInteractionFlow04;
            SandPlanetEventFlow04 eventFlow = activeEventFlowField?.GetValue(controller) as SandPlanetEventFlow04;
            if (interactionFlow == null && eventFlow == null) return;

            List<SandPlanetFlowNode04> rows = interactionFlow != null
                ? interactionFlow.GetNodeRows(nodeId).Where(n => n.Active).ToList()
                : eventFlow.GetNodeRows(nodeId).Where(n => n.Active).ToList();
            if (rows.Count == 0) return;

            List<Button> buttons = new List<Button>();
            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (!child.gameObject.activeSelf) continue;
                Button button = child.GetComponent<Button>();
                Text main = FindMainLabel(button);
                if (button == null || main == null || string.Equals(main.text, "취소", StringComparison.Ordinal)) continue;
                buttons.Add(button);
            }

            int count = Mathf.Min(rows.Count, buttons.Count);
            for (int i = 0; i < count; i++)
            {
                Button button = buttons[i];
                QuestActionBadgeBinding binding = button.GetComponent<QuestActionBadgeBinding>();
                if (binding == null) binding = button.gameObject.AddComponent<QuestActionBadgeBinding>();
                ApplyChip(button, binding, BuildBadge(rows[i], interactionFlow != null));
            }
        }

        private void ApplyChip(Button button, QuestActionBadgeBinding binding, BadgeInfo badge)
        {
            EnsureBinding(button, binding);
            if (!badge.Visible)
            {
                if (binding.ChipRoot != null) binding.ChipRoot.SetActive(false);
                ApplyHeight(button, NormalHeight);
                LayoutMainLabel(binding.MainText, false);
                return;
            }

            EnsureChip(button, binding);
            ApplyHeight(button, QuestHeight);
            LayoutMainLabel(binding.MainText, true);
            binding.ChipRoot.SetActive(true);
            binding.ChipText.text = badge.Label;
            binding.ChipText.color = badge.Color;
            binding.ChipImage.color = new Color(badge.Color.r, badge.Color.g, badge.Color.b, .27f);
            binding.ChipRoot.transform.SetAsLastSibling();
        }

        private static void EnsureBinding(Button button, QuestActionBadgeBinding binding)
        {
            if (binding.MainText == null) binding.MainText = FindMainLabel(button);
        }

        private static void EnsureChip(Button button, QuestActionBadgeBinding binding)
        {
            if (binding.ChipRoot != null && binding.ChipText != null && binding.ChipImage != null) return;

            Transform existing = button.transform.Find("QuestActionChip");
            GameObject chip = existing != null
                ? existing.gameObject
                : new GameObject("QuestActionChip", typeof(RectTransform), typeof(Image));
            if (existing == null) chip.transform.SetParent(button.transform, false);

            RectTransform rect = chip.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(.025f, .66f);
            rect.anchorMax = new Vector2(.975f, .96f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = chip.GetComponent<Image>();
            image.raycastTarget = false;

            Transform labelTransform = chip.transform.Find("Label");
            Text label;
            if (labelTransform != null)
            {
                label = labelTransform.GetComponent<Text>();
            }
            else
            {
                GameObject labelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
                labelGo.transform.SetParent(chip.transform, false);
                label = labelGo.GetComponent<Text>();
            }

            label.font = binding.MainText != null && binding.MainText.font != null
                ? binding.MainText.font
                : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 13;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.raycastTarget = false;
            label.supportRichText = true;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            RectTransform lr = label.rectTransform;
            lr.anchorMin = Vector2.zero;
            lr.anchorMax = Vector2.one;
            lr.offsetMin = new Vector2(8f, 1f);
            lr.offsetMax = new Vector2(-8f, -1f);

            binding.ChipRoot = chip;
            binding.ChipImage = image;
            binding.ChipText = label;
        }

        private static void LayoutMainLabel(Text text, bool questLinked)
        {
            if (text == null) return;
            RectTransform r = text.rectTransform;
            r.anchorMin = questLinked ? new Vector2(.035f, .05f) : new Vector2(.035f, .08f);
            r.anchorMax = questLinked ? new Vector2(.965f, .62f) : new Vector2(.965f, .92f);
            r.offsetMin = Vector2.zero;
            r.offsetMax = Vector2.zero;
            text.fontSize = 15;
            text.fontStyle = FontStyle.Normal;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
        }

        private static void ApplyHeight(Button button, float height)
        {
            LayoutElement le = button.GetComponent<LayoutElement>();
            if (le == null) le = button.gameObject.AddComponent<LayoutElement>();
            le.minHeight = height;
            le.preferredHeight = height;
        }

        private static Text FindMainLabel(Button button)
        {
            if (button == null) return null;
            Text[] labels = button.GetComponentsInChildren<Text>(true);
            foreach (Text label in labels)
            {
                if (label == null) continue;
                if (label.transform.IsChildOf(button.transform.Find("QuestActionChip"))) continue;
                return label;
            }
            return null;
        }

        private BadgeInfo BuildBadge(SandPlanetFlowNode04 node, bool interaction)
        {
            SandPlanetQuestActionMeta04 meta = null;
            if (interaction) content.InteractionNodeMeta.TryGetValue(node, out meta);
            else content.EventNodeMeta.TryGetValue(node, out meta);

            if (meta == null || string.IsNullOrEmpty(meta.QuestAction) || string.IsNullOrEmpty(meta.QuestId)) return BadgeInfo.Hidden;
            if (!content.Quests.TryGetValue(meta.QuestId, out SandPlanetQuest04 quest)) return BadgeInfo.Hidden;
            if (!WillQuestActionApply(meta)) return BadgeInfo.Hidden;

            string action;
            switch (meta.QuestAction.ToUpperInvariant())
            {
                case "ACTIVATE_QUEST": action = "수락"; break;
                case "SET_QUEST_STEP": action = "진행"; break;
                case "COMPLETE_QUEST": action = "완료"; break;
                case "FAIL_QUEST": action = "실패"; break;
                default: return BadgeInfo.Hidden;
            }

            return new BadgeInfo(true, quest.Title + "  ·  " + action, QuestColor(quest.Type));
        }

        private bool WillQuestActionApply(SandPlanetQuestActionMeta04 meta)
        {
            string status = InvokeString(getQuestStatusMethod, meta.QuestId, "LOCKED");
            switch ((meta.QuestAction ?? string.Empty).ToUpperInvariant())
            {
                case "ACTIVATE_QUEST": return status == "LOCKED";
                case "SET_QUEST_STEP":
                    if (status != "ACTIVE") return false;
                    string currentStep = InvokeString(getQuestStepMethod, meta.QuestId, string.Empty);
                    return !string.IsNullOrEmpty(meta.QuestStepId) && currentStep != meta.QuestStepId;
                case "COMPLETE_QUEST":
                case "FAIL_QUEST": return status == "ACTIVE";
                default: return false;
            }
        }

        private string InvokeString(MethodInfo method, string argument, string fallback)
        {
            if (method == null) return fallback;
            try { return method.Invoke(controller, new object[] { argument }) as string ?? fallback; }
            catch { return fallback; }
        }

        private static Color QuestColor(string type)
        {
            string hex = string.Equals(type, "MAIN", StringComparison.OrdinalIgnoreCase)
                ? MainColor
                : string.Equals(type, "CHARACTER", StringComparison.OrdinalIgnoreCase)
                    ? CharacterColor
                    : SideColor;
            return ColorUtility.TryParseHtmlString(hex, out Color color) ? color : Color.white;
        }

        private readonly struct BadgeInfo
        {
            public static readonly BadgeInfo Hidden = new BadgeInfo(false, string.Empty, Color.clear);
            public readonly bool Visible;
            public readonly string Label;
            public readonly Color Color;
            public BadgeInfo(bool visible, string label, Color color)
            {
                Visible = visible;
                Label = label;
                Color = color;
            }
        }
    }

    public sealed class QuestActionBadgeBinding : MonoBehaviour
    {
        public GameObject ChipRoot;
        public Image ChipImage;
        public Text ChipText;
        public Text MainText;
    }
}
