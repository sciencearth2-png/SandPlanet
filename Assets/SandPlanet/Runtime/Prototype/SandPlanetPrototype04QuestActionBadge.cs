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
    /// Adds player-facing Quest update badges to narrative choice buttons.
    /// The badge is derived from v1.6 QuestAction metadata, so writers do not need
    /// a separate UI column in Excel.
    /// </summary>
    public sealed class SandPlanetPrototype04QuestActionBadge : MonoBehaviour
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private const string MainColor = "#F0A24A";
        private const string CharacterColor = "#69C77C";
        private const string SideColor = "#F1D784";

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

        private void OnEnable()
        {
            Canvas.willRenderCanvases += Refresh;
        }

        private void OnDisable()
        {
            Canvas.willRenderCanvases -= Refresh;
        }

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
                Text text = child.GetComponentInChildren<Text>(true);
                if (button == null || text == null || text.text == "취소") continue;
                buttons.Add(button);
            }

            int count = Mathf.Min(rows.Count, buttons.Count);
            for (int i = 0; i < count; i++)
            {
                Button button = buttons[i];
                Text text = button.GetComponentInChildren<Text>(true);
                if (text == null) continue;

                QuestActionBadgeBinding binding = button.GetComponent<QuestActionBadgeBinding>();
                if (binding == null)
                {
                    binding = button.gameObject.AddComponent<QuestActionBadgeBinding>();
                    binding.BaseText = text.text;
                }

                string badge = BuildBadge(rows[i], interactionFlow != null);
                text.supportRichText = true;
                text.text = binding.BaseText + badge;
            }
        }

        private string BuildBadge(SandPlanetFlowNode04 node, bool interaction)
        {
            SandPlanetQuestActionMeta04 meta = null;
            if (interaction)
                content.InteractionNodeMeta.TryGetValue(node, out meta);
            else
                content.EventNodeMeta.TryGetValue(node, out meta);

            if (meta == null || string.IsNullOrEmpty(meta.QuestAction) || string.IsNullOrEmpty(meta.QuestId))
                return string.Empty;
            if (!content.Quests.TryGetValue(meta.QuestId, out SandPlanetQuest04 quest))
                return string.Empty;
            if (!WillQuestActionApply(meta))
                return string.Empty;

            string action;
            switch (meta.QuestAction.ToUpperInvariant())
            {
                case "ACTIVATE_QUEST": action = "퀘스트 수락"; break;
                case "SET_QUEST_STEP": action = "퀘스트 진행"; break;
                case "COMPLETE_QUEST": action = "퀘스트 완료"; break;
                case "FAIL_QUEST": action = "퀘스트 실패"; break;
                default: return string.Empty;
            }

            string type = quest.Type == "CHARACTER" ? "CHAR" : quest.Type == "SIDE" ? "SIDE" : "MAIN";
            string color = QuestColor(quest.Type);
            return $"    <color={color}><b>[{type} {action}]</b></color>";
        }

        private bool WillQuestActionApply(SandPlanetQuestActionMeta04 meta)
        {
            string status = InvokeString(getQuestStatusMethod, meta.QuestId, "LOCKED");
            switch ((meta.QuestAction ?? string.Empty).ToUpperInvariant())
            {
                case "ACTIVATE_QUEST":
                    return status == "LOCKED";
                case "SET_QUEST_STEP":
                    if (status != "ACTIVE") return false;
                    string currentStep = InvokeString(getQuestStepMethod, meta.QuestId, string.Empty);
                    return !string.IsNullOrEmpty(meta.QuestStepId) && currentStep != meta.QuestStepId;
                case "COMPLETE_QUEST":
                case "FAIL_QUEST":
                    return status == "ACTIVE";
                default:
                    return false;
            }
        }

        private string InvokeString(MethodInfo method, string argument, string fallback)
        {
            if (method == null) return fallback;
            try { return method.Invoke(controller, new object[] { argument }) as string ?? fallback; }
            catch { return fallback; }
        }

        private static string QuestColor(string type)
        {
            if (string.Equals(type, "MAIN", StringComparison.OrdinalIgnoreCase)) return MainColor;
            if (string.Equals(type, "CHARACTER", StringComparison.OrdinalIgnoreCase)) return CharacterColor;
            return SideColor;
        }
    }

    public sealed class QuestActionBadgeBinding : MonoBehaviour
    {
        public string BaseText;
    }
}
