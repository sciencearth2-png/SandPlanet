using System;
using System.Collections.Generic;
using System.Reflection;
using SandPlanet.Prototype.DataDriven;
using UnityEngine;
using UnityEngine.UI;

namespace SandPlanet.Prototype
{
    /// <summary>
    /// Prototype 0.4/v1.7 presentation patch for quest-offer buttons.
    /// Quest type is already communicated by color, so OFFER entries display
    /// the actual quest title instead of [MAIN ?] / [CHAR ?] / [SIDE ?].
    /// Progress entries keep the existing marker presentation.
    /// </summary>
    [DefaultExecutionOrder(12000)]
    public sealed class SandPlanetPrototype04QuestOfferLabels : MonoBehaviour
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private const string MainColor = "#F0A24A";
        private const string CharacterColor = "#69C77C";
        private const string SideColor = "#F1D784";

        private SandPlanetPrototype04Controller controller;
        private SandPlanetContent04 content;
        private FieldInfo contentField;
        private FieldInfo interactionRootField;
        private Transform interactionRoot;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            SandPlanetPrototype04Controller found = UnityEngine.Object.FindFirstObjectByType<SandPlanetPrototype04Controller>();
            if (found != null && found.GetComponent<SandPlanetPrototype04QuestOfferLabels>() == null)
                found.gameObject.AddComponent<SandPlanetPrototype04QuestOfferLabels>();
        }

        private void Awake()
        {
            controller = GetComponent<SandPlanetPrototype04Controller>();
            if (controller == null)
            {
                enabled = false;
                return;
            }

            Type t = controller.GetType();
            contentField = t.GetField("content", PrivateInstance);
            interactionRootField = t.GetField("interactionRoot", PrivateInstance);
        }

        private void Start()
        {
            content = contentField?.GetValue(controller) as SandPlanetContent04;
            interactionRoot = interactionRootField?.GetValue(controller) as Transform;
            if (content == null || interactionRoot == null)
            {
                enabled = false;
                return;
            }

            Canvas.willRenderCanvases += RefreshLabels;
            RefreshLabels();
        }

        private void OnDestroy()
        {
            Canvas.willRenderCanvases -= RefreshLabels;
        }

        private void LateUpdate()
        {
            RefreshLabels();
        }

        private void RefreshLabels()
        {
            if (content == null || interactionRoot == null) return;

            Button[] buttons = interactionRoot.GetComponentsInChildren<Button>(true);
            foreach (Button button in buttons)
            {
                if (button == null || !button.gameObject.activeInHierarchy) continue;
                Text label = button.GetComponentInChildren<Text>(true);
                if (label == null || string.IsNullOrEmpty(label.text)) continue;

                foreach (SandPlanetInteraction04 interaction in content.Interactions)
                {
                    if (interaction == null ||
                        !string.Equals(interaction.QuestRole, "OFFER", StringComparison.OrdinalIgnoreCase) ||
                        string.IsNullOrEmpty(interaction.QuestId) ||
                        string.IsNullOrEmpty(interaction.DisplayText) ||
                        !label.text.EndsWith(interaction.DisplayText, StringComparison.Ordinal))
                        continue;

                    if (!content.Quests.TryGetValue(interaction.QuestId, out SandPlanetQuest04 quest)) break;
                    label.supportRichText = true;
                    label.text = $"<color={QuestColor(quest.Type)}><b>[{quest.Title}] 수락</b></color>";
                    break;
                }
            }
        }

        private static string QuestColor(string type)
        {
            if (string.Equals(type, "MAIN", StringComparison.OrdinalIgnoreCase)) return MainColor;
            if (string.Equals(type, "CHARACTER", StringComparison.OrdinalIgnoreCase)) return CharacterColor;
            return SideColor;
        }
    }
}
