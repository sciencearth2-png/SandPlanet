using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace SandPlanet.Prototype
{
    /// <summary>
    /// Small visual hotfix for Prototype 0.3 UX v2.
    ///
    /// Keeps the existing playtest UI intact while fixing two layout issues:
    /// 1) the new main-quest tracker overlapped the legacy planet-hub info panel;
    /// 2) CHARACTER / narrative badges were clipped on location encounter cards.
    ///
    /// This component is intentionally presentation-only and does not change game state.
    /// </summary>
    public sealed class SandPlanetPrototype03LayoutHotfix : MonoBehaviour
    {
        private const BindingFlags InstancePrivate = BindingFlags.Instance | BindingFlags.NonPublic;
        private const BindingFlags NestedFields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private const float HudHeight = 136f;

        private SandPlanetPrototype03Controller controller;
        private SandPlanetPrototype03PlaytestUXV2 uxV2;
        private FieldInfo modeField;
        private FieldInfo currentLocationIdField;
        private FieldInfo locationScrollField;
        private MethodInfo getAvailableEncountersMethod;

        private GUIStyle badgeTextStyle;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Attach()
        {
            SandPlanetPrototype03Controller found = UnityEngine.Object.FindFirstObjectByType<SandPlanetPrototype03Controller>();
            if (found == null)
                return;

            if (found.GetComponent<SandPlanetPrototype03LayoutHotfix>() == null)
                found.gameObject.AddComponent<SandPlanetPrototype03LayoutHotfix>();
        }

        private void Awake()
        {
            controller = GetComponent<SandPlanetPrototype03Controller>();
            if (controller == null)
                controller = UnityEngine.Object.FindFirstObjectByType<SandPlanetPrototype03Controller>();

            if (controller == null)
            {
                enabled = false;
                return;
            }

            uxV2 = GetComponent<SandPlanetPrototype03PlaytestUXV2>();
            if (uxV2 == null)
                uxV2 = UnityEngine.Object.FindFirstObjectByType<SandPlanetPrototype03PlaytestUXV2>();

            Type controllerType = controller.GetType();
            modeField = controllerType.GetField("mode", InstancePrivate);
            currentLocationIdField = controllerType.GetField("currentLocationId", InstancePrivate);

            foreach (MethodInfo method in controllerType.GetMethods(InstancePrivate))
            {
                if (method.Name == "GetAvailableEncounters" && method.GetParameters().Length == 2)
                {
                    getAvailableEncountersMethod = method;
                    break;
                }
            }

            if (uxV2 != null)
                locationScrollField = uxV2.GetType().GetField("locationScroll", InstancePrivate);
        }

        private void OnGUI()
        {
            if (controller == null)
                return;

            EnsureStyles();
            string mode = modeField != null ? modeField.GetValue(controller)?.ToString() ?? string.Empty : string.Empty;

            if (mode == "Hub")
            {
                // Controller hub UI is depth 0, UX v2 tracker is depth -2000.
                // Draw this matte at -1500 so it hides the old panel but remains behind the new tracker.
                GUI.depth = -1500;
                DrawHubSidebarMatte();
            }
            else if (mode == "Location")
            {
                // UX v2 cards are depth -2000. Draw corrected badges above them.
                GUI.depth = -3000;
                DrawLocationBadgeFixes();
            }
        }

        private void DrawHubSidebarMatte()
        {
            float width = 412f;
            Rect sidebar = new Rect(Screen.width - width, HudHeight, width, Screen.height - HudHeight);
            FillRect(sidebar, new Color(0.038f, 0.032f, 0.030f, 0.995f));
            FillRect(new Rect(sidebar.x, sidebar.y, 1f, sidebar.height), new Color(0.22f, 0.20f, 0.18f, 1f));
        }

        private void DrawLocationBadgeFixes()
        {
            if (currentLocationIdField == null || getAvailableEncountersMethod == null)
                return;

            string locationId = currentLocationIdField.GetValue(controller) as string;
            if (string.IsNullOrEmpty(locationId))
                return;

            IEnumerable available;
            try
            {
                available = getAvailableEncountersMethod.Invoke(controller, new object[] { locationId, false }) as IEnumerable;
            }
            catch
            {
                return;
            }

            if (available == null)
                return;

            List<object> items = new List<object>();
            foreach (object item in available)
            {
                if (item != null)
                    items.Add(item);
            }

            if (items.Count == 0)
                return;

            Vector2 scroll = Vector2.zero;
            if (uxV2 != null && locationScrollField != null)
            {
                object value = locationScrollField.GetValue(uxV2);
                if (value is Vector2)
                    scroll = (Vector2)value;
            }

            Rect grid = new Rect(24f, HudHeight + 112f, Screen.width - 48f, Screen.height - HudHeight - 136f);
            int columns = Screen.width >= 1400 ? 2 : 1;
            float gap = 18f;
            float scrollbarReserve = 22f;
            float cardWidth = (grid.width - scrollbarReserve - gap * (columns - 1)) / columns;
            const float cardHeight = 178f;

            for (int i = 0; i < items.Count; i++)
            {
                int row = i / columns;
                int col = i % columns;
                float cardX = grid.x + col * (cardWidth + gap);
                float cardY = grid.y + row * (cardHeight + gap) - scroll.y;
                Rect card = new Rect(cardX, cardY, cardWidth, cardHeight);

                if (card.yMax < grid.y || card.y > grid.yMax)
                    continue;

                string narrative = ReadNestedObject(items[i], "NarrativeType")?.ToString() ?? "World";
                DrawCorrectedBadge(card, narrative);
            }
        }

        private void DrawCorrectedBadge(Rect card, string narrative)
        {
            Color color = NarrativeColor(narrative);
            string label = NarrativeLabel(narrative);

            // Wider and slightly taller than the UX v2 badge so larger fonts never clip.
            Rect badge = new Rect(card.x + 14f, card.y + 12f, 154f, 34f);
            FillRect(badge, color);
            GUI.Label(new Rect(badge.x + 10f, badge.y + 2f, badge.width - 20f, badge.height - 4f), label, badgeTextStyle);
        }

        private static object ReadNestedObject(object owner, string fieldName)
        {
            if (owner == null)
                return null;

            FieldInfo field = owner.GetType().GetField(fieldName, NestedFields);
            return field != null ? field.GetValue(owner) : null;
        }

        private static Color NarrativeColor(string narrative)
        {
            switch (narrative)
            {
                case "Main": return new Color(0.50f, 0.28f, 0.16f, 1f);
                case "Character": return new Color(0.22f, 0.34f, 0.48f, 1f);
                case "Activity": return new Color(0.28f, 0.43f, 0.31f, 1f);
                default: return new Color(0.36f, 0.33f, 0.30f, 1f);
            }
        }

        private static string NarrativeLabel(string narrative)
        {
            switch (narrative)
            {
                case "Main": return "◆ MAIN";
                case "Character": return "● CHARACTER";
                case "Activity": return "▲ ACTIVITY";
                default: return "■ WORLD";
            }
        }

        private void EnsureStyles()
        {
            if (badgeTextStyle != null)
                return;

            badgeTextStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Overflow,
                normal = { textColor = Color.white }
            };
        }

        private static void FillRect(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }
    }
}
