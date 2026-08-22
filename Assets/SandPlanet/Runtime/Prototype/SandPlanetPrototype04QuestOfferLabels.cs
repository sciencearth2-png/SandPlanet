using System;
using System.Collections.Generic;
using System.Reflection;
using SandPlanet.Prototype.DataDriven;
using UnityEngine;
using UnityEngine.UI;

namespace SandPlanet.Prototype
{
    /// <summary>
    /// Prototype 0.4/v1.7 presentation patch for quest-linked interaction buttons.
    ///
    /// Interaction buttons must describe what Jay actually does or says. Quest metadata
    /// is secondary information only, so the natural EntryText stays on the first line
    /// and the quest title appears as a small colored ?/! hint underneath.
    /// MAIN / CHARACTER / SIDE text labels are intentionally omitted because color
    /// already communicates quest type.
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
                        string.IsNullOrEmpty(interaction.QuestId) ||
                        string.IsNullOrEmpty(interaction.DisplayText))
                        continue;

                    string role = (interaction.QuestRole ?? string.Empty).ToUpperInvariant();
                    if (role != "OFFER" && role != "PROGRESS") continue;

                    // Fresh buttons are created by the controller as "badge + EntryText".
                    // Once rewritten, they already start with EntryText; do not keep rewriting.
                    bool freshControllerLabel = label.text.EndsWith(interaction.DisplayText, StringComparison.Ordinal);
                    bool alreadyNaturalLabel = label.text.StartsWith(interaction.DisplayText + "\n", StringComparison.Ordinal);
                    if (!freshControllerLabel && !alreadyNaturalLabel) continue;

                    if (!content.Quests.TryGetValue(interaction.QuestId, out SandPlanetQuest04 quest)) break;

                    string marker = role == "OFFER" ? "?" : "!";
                    string worldMarker = string.Equals(interaction.EntryMode, "WORLD_MARKER", StringComparison.OrdinalIgnoreCase) ? "◆ " : string.Empty;
                    string hint = $"<size=12><color={QuestColor(quest.Type)}>{marker} {quest.Title}</color></size>";

                    label.supportRichText = true;
                    label.text = worldMarker + interaction.DisplayText + "\n" + hint;
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
