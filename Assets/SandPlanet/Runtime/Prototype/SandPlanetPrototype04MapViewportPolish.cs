using System;
using System.Collections.Generic;
using System.Reflection;
using SandPlanet.Prototype.DataDriven;
using UnityEngine;
using UnityEngine.UI;

namespace SandPlanet.Prototype
{
    /// <summary>
    /// Final visual pass for the Prototype04 location viewport.
    /// Keeps gameplay/data untouched and only owns presentation values that must win
    /// after the older SceneDialogueLayout / density layers have refreshed.
    ///
    /// Responsibilities:
    /// - Clip the location scene like a real map viewport.
    /// - Apply the requested redistributed character/object layout from the very first frame.
    /// - Scatter world objects naturally instead of pinning them to the bottom row.
    /// - Give each location a subdued, opaque background tone.
    /// - Move the location title to the upper-left.
    /// - Improve interaction/dialogue readability with slightly larger type and controls.
    /// </summary>
    [DefaultExecutionOrder(50000)]
    public sealed class SandPlanetPrototype04MapViewportPolish : MonoBehaviour
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

        // Fallback slots. The currently authored targets use explicit ID-based slots below so
        // the browse-scale composition is stable regardless of list/order changes.
        private static readonly Vector2[] CharacterSlots =
        {
            new Vector2(.30f, .69f),
            new Vector2(.48f, .55f),
            new Vector2(.65f, .68f),
            new Vector2(.34f, .45f),
            new Vector2(.58f, .48f),
            new Vector2(.43f, .63f)
        };

        // These are deliberately higher than the previous values. The whole TargetRoot is shown
        // at 1.5x in browse mode, so low authored Y values visually collapsed back into a bottom row.
        private static readonly Vector2[] ObjectSlots =
        {
            new Vector2(.32f, .42f),
            new Vector2(.57f, .35f),
            new Vector2(.63f, .54f),
            new Vector2(.38f, .58f),
            new Vector2(.58f, .63f),
            new Vector2(.46f, .31f)
        };

        private SandPlanetPrototype04Controller controller;
        private SandPlanetContent04 content;
        private FieldInfo contentField;
        private FieldInfo currentLocationField;
        private FieldInfo locationPanelField;
        private FieldInfo locationTitleField;
        private FieldInfo targetRootField;
        private FieldInfo targetTemplateField;
        private FieldInfo interactionRootField;
        private FieldInfo interactionTemplateField;
        private FieldInfo modalPanelField;
        private FieldInfo modalTitleField;
        private FieldInfo modalBodyField;
        private FieldInfo modalButtonRootField;
        private FieldInfo modalButtonTemplateField;

        private GameObject locationPanel;
        private Text locationTitle;
        private Transform targetRoot;
        private Button targetTemplate;
        private Transform interactionRoot;
        private Button interactionTemplate;
        private GameObject modalPanel;
        private Text modalTitle;
        private Text modalBody;
        private Transform modalButtonRoot;
        private Button modalButtonTemplate;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            SandPlanetPrototype04Controller found = UnityEngine.Object.FindFirstObjectByType<SandPlanetPrototype04Controller>();
            if (found != null && found.GetComponent<SandPlanetPrototype04MapViewportPolish>() == null)
                found.gameObject.AddComponent<SandPlanetPrototype04MapViewportPolish>();
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
            currentLocationField = t.GetField("currentLocationId", PrivateInstance);
            locationPanelField = t.GetField("locationPanel", PrivateInstance);
            locationTitleField = t.GetField("locationTitleText", PrivateInstance);
            targetRootField = t.GetField("targetRoot", PrivateInstance);
            targetTemplateField = t.GetField("targetTemplate", PrivateInstance);
            interactionRootField = t.GetField("interactionRoot", PrivateInstance);
            interactionTemplateField = t.GetField("interactionTemplate", PrivateInstance);
            modalPanelField = t.GetField("modalPanel", PrivateInstance);
            modalTitleField = t.GetField("modalTitleText", PrivateInstance);
            modalBodyField = t.GetField("modalBodyText", PrivateInstance);
            modalButtonRootField = t.GetField("modalButtonRoot", PrivateInstance);
            modalButtonTemplateField = t.GetField("modalButtonTemplate", PrivateInstance);
        }

        private void Start()
        {
            content = contentField?.GetValue(controller) as SandPlanetContent04;
            locationPanel = locationPanelField?.GetValue(controller) as GameObject;
            locationTitle = locationTitleField?.GetValue(controller) as Text;
            targetRoot = targetRootField?.GetValue(controller) as Transform;
            targetTemplate = targetTemplateField?.GetValue(controller) as Button;
            interactionRoot = interactionRootField?.GetValue(controller) as Transform;
            interactionTemplate = interactionTemplateField?.GetValue(controller) as Button;
            modalPanel = modalPanelField?.GetValue(controller) as GameObject;
            modalTitle = modalTitleField?.GetValue(controller) as Text;
            modalBody = modalBodyField?.GetValue(controller) as Text;
            modalButtonRoot = modalButtonRootField?.GetValue(controller) as Transform;
            modalButtonTemplate = modalButtonTemplateField?.GetValue(controller) as Button;

            if (content == null || locationPanel == null || targetRoot == null || interactionRoot == null || modalPanel == null || modalButtonRoot == null)
            {
                enabled = false;
                return;
            }

            EnsureViewportClip();
            ApplyStaticTypography();
            Canvas.willRenderCanvases += FinalizePresentation;
            FinalizePresentation();
        }

        private void OnDestroy()
        {
            Canvas.willRenderCanvases -= FinalizePresentation;
        }

        private void LateUpdate()
        {
            FinalizePresentation();
        }

        private void EnsureViewportClip()
        {
            // RectMask2D clips portraits/cards that move outside the location rectangle.
            // There is intentionally no visible border: the viewport edge itself is the boundary.
            RectMask2D mask = locationPanel.GetComponent<RectMask2D>();
            if (mask == null) mask = locationPanel.AddComponent<RectMask2D>();
            mask.padding = Vector4.zero;

            Image image = locationPanel.GetComponent<Image>();
            if (image != null)
            {
                image.raycastTarget = true;
                image.maskable = true;
            }
        }

        private void ApplyStaticTypography()
        {
            if (locationTitle != null)
            {
                locationTitle.fontSize = 29;
                locationTitle.fontStyle = FontStyle.Bold;
                locationTitle.alignment = TextAnchor.UpperLeft;
                locationTitle.horizontalOverflow = HorizontalWrapMode.Wrap;
                locationTitle.verticalOverflow = VerticalWrapMode.Truncate;
                locationTitle.raycastTarget = false;
                SetRect(locationTitle.rectTransform, new Vector2(.028f, .885f), new Vector2(.52f, .975f));
            }

            Text interactionHeader = FindNamed<Text>(interactionRoot.parent, "UX18_InteractionHeader");
            if (interactionHeader != null)
            {
                interactionHeader.fontSize = 24;
                interactionHeader.lineSpacing = 1.06f;
                SetRect(interactionHeader.rectTransform, new Vector2(.065f, .875f), new Vector2(.935f, .97f));
            }

            if (modalTitle != null)
            {
                modalTitle.fontSize = 25;
                modalTitle.lineSpacing = 1.05f;
                SetRect(modalTitle.rectTransform, new Vector2(.065f, .865f), new Vector2(.935f, .965f));
            }

            if (modalBody != null)
            {
                modalBody.fontSize = 19;
                modalBody.lineSpacing = 1.13f;
                modalBody.alignment = TextAnchor.UpperLeft;
                modalBody.horizontalOverflow = HorizontalWrapMode.Wrap;
                modalBody.verticalOverflow = VerticalWrapMode.Overflow;
                SetRect(modalBody.rectTransform, new Vector2(.065f, .365f), new Vector2(.935f, .845f));
            }

            RectTransform choiceRoot = modalButtonRoot as RectTransform;
            if (choiceRoot != null)
                SetRect(choiceRoot, new Vector2(.055f, .025f), new Vector2(.945f, .34f));

            StyleTemplate(modalButtonTemplate, 66f, 17, TextAnchor.MiddleCenter);
            StyleTemplate(interactionTemplate, 84f, 17, TextAnchor.MiddleLeft);
        }

        private void FinalizePresentation()
        {
            if (!enabled || locationPanel == null) return;

            string locationId = GetString(currentLocationField);
            bool locationOpen = !string.IsNullOrEmpty(locationId) && locationPanel.activeInHierarchy;
            if (!locationOpen) return;

            Image scene = locationPanel.GetComponent<Image>();
            if (scene != null)
                scene.color = LocationBackground(locationId);

            // Scene panel remains above Quest/Log as requested; right interaction/modal panels can still sit above it.
            locationPanel.transform.SetAsLastSibling();
            if (interactionRoot.parent != null && interactionRoot.parent.gameObject.activeInHierarchy)
                interactionRoot.parent.SetAsLastSibling();
            if (modalPanel.activeInHierarchy)
                modalPanel.transform.SetAsLastSibling();

            ApplySafeSceneSlots();
            ApplyStaticTypography();
            StyleDynamicInteractionButtons();
            StyleDynamicModalButtons();
        }

        private void ApplySafeSceneSlots()
        {
            List<SceneTargetView04> characters = new List<SceneTargetView04>();
            List<SceneTargetView04> objects = new List<SceneTargetView04>();

            for (int i = 0; i < targetRoot.childCount; i++)
            {
                Transform child = targetRoot.GetChild(i);
                if (targetTemplate != null && child == targetTemplate.transform) continue;
                if (!child.gameObject.activeSelf) continue;

                Button button = child.GetComponent<Button>();
                Text label = child.GetComponentInChildren<Text>(true);
                if (button == null || label == null) continue;

                // Do not wait for the older layout layer to identify targets. The first visible
                // location frame must already use the redistributed composition.
                SceneTargetView04 view = child.GetComponent<SceneTargetView04>();
                if (view == null) view = child.gameObject.AddComponent<SceneTargetView04>();
                if (string.IsNullOrEmpty(view.TargetId)) IdentifyTarget(label.text, view);
                if (string.IsNullOrEmpty(view.TargetId)) continue;

                if (string.Equals(view.TargetType, "CHARACTER", StringComparison.Ordinal)) characters.Add(view);
                else if (string.Equals(view.TargetType, "WORLD_TARGET", StringComparison.Ordinal)) objects.Add(view);
            }

            characters.Sort((a, b) => string.CompareOrdinal(a.TargetId, b.TargetId));
            objects.Sort((a, b) => string.CompareOrdinal(a.TargetId, b.TargetId));

            for (int i = 0; i < characters.Count; i++)
                PlaceTarget(characters[i], CharacterSlot(characters[i].TargetId, i), new Vector2(120f, 158f));

            for (int i = 0; i < objects.Count; i++)
                PlaceTarget(objects[i], ObjectSlot(objects[i].TargetId, i), new Vector2(172f, 50f));
        }

        private static Vector2 CharacterSlot(string targetId, int fallbackIndex)
        {
            // Explicit slots make the initial 1.5x browse view safe and deterministic.
            // In particular, Jina is kept near the center instead of the right clipping edge.
            switch (targetId)
            {
                case "CHA_DIYA":     return new Vector2(.30f, .69f);
                case "CHA_JINA":     return new Vector2(.48f, .55f);
                case "CHA_SAM":      return new Vector2(.65f, .68f);
                case "CHA_BENJAMIN": return new Vector2(.32f, .68f);
                case "CHA_FAYE":     return new Vector2(.59f, .53f);
                case "CHA_BORICHI":  return new Vector2(.33f, .68f);
                default: return CharacterSlots[fallbackIndex % CharacterSlots.Length];
            }
        }

        private static Vector2 ObjectSlot(string targetId, int fallbackIndex)
        {
            // Objects intentionally occupy different heights so they read as map elements rather
            // than a toolbar. Positions are authored for the 1.5x browse scale.
            switch (targetId)
            {
                case "OBJ_01_SETTLEMENT_BOARD":   return new Vector2(.32f, .42f);
                case "OBJ_01_SETTLEMENT_REST":    return new Vector2(.55f, .34f);
                case "OBJ_01_SETTLEMENT_SHELTER": return new Vector2(.63f, .54f);

                case "OBJ_02_SHIP_OUTER_PANEL": return new Vector2(.36f, .35f);
                case "OBJ_02_SHIP_CONSOLE":     return new Vector2(.62f, .44f);

                case "OBJ_03_GRAVE_MEMORIAL": return new Vector2(.39f, .49f);
                case "OBJ_03_GRAVE_BARRIER":  return new Vector2(.62f, .34f);

                case "OBJ_04_OASIS_PUMP":  return new Vector2(.60f, .44f);
                case "OBJ_04_OASIS_WATER": return new Vector2(.48f, .31f);

                default: return ObjectSlots[fallbackIndex % ObjectSlots.Length];
            }
        }

        private void IdentifyTarget(string rawLabel, SceneTargetView04 view)
        {
            if (content == null || view == null) return;
            string raw = rawLabel ?? string.Empty;

            foreach (SandPlanetCharacter04 character in content.Characters.Values)
            {
                if (!character.Active || raw.IndexOf(character.Name, StringComparison.Ordinal) < 0) continue;
                view.TargetType = "CHARACTER";
                view.TargetId = character.Id;
                return;
            }

            foreach (SandPlanetWorldTarget04 target in content.WorldTargets.Values)
            {
                if (!target.Active || raw.IndexOf(target.Name, StringComparison.Ordinal) < 0) continue;
                view.TargetType = "WORLD_TARGET";
                view.TargetId = target.Id;
                return;
            }
        }

        private static void PlaceTarget(SceneTargetView04 view, Vector2 slot, Vector2 size)
        {
            RectTransform rect = view.GetComponent<RectTransform>();
            if (rect == null) return;

            // These insets are deliberately stricter than before because TargetRoot is already
            // scaled to 1.5x in browse mode. Focus zoom may clip naturally; browse mode must not.
            slot.x = Mathf.Clamp(slot.x, .28f, .66f);
            slot.y = Mathf.Clamp(slot.y, .29f, .72f);
            rect.anchorMin = rect.anchorMax = slot;
            rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = size;
        }

        private void StyleDynamicInteractionButtons()
        {
            for (int i = 0; i < interactionRoot.childCount; i++)
            {
                Transform child = interactionRoot.GetChild(i);
                if (interactionTemplate != null && child == interactionTemplate.transform) continue;
                Button button = child.GetComponent<Button>();
                if (button == null || !child.gameObject.activeSelf) continue;

                LayoutElement le = child.GetComponent<LayoutElement>();
                if (le == null) le = child.gameObject.AddComponent<LayoutElement>();
                le.minHeight = 84f;
                le.preferredHeight = 84f;

                Text text = button.GetComponentInChildren<Text>(true);
                if (text != null)
                {
                    text.fontSize = 17;
                    text.lineSpacing = 1.08f;
                    text.alignment = TextAnchor.MiddleLeft;
                    text.horizontalOverflow = HorizontalWrapMode.Wrap;
                    text.verticalOverflow = VerticalWrapMode.Truncate;
                }
            }
        }

        private void StyleDynamicModalButtons()
        {
            for (int i = 0; i < modalButtonRoot.childCount; i++)
            {
                Transform child = modalButtonRoot.GetChild(i);
                if (modalButtonTemplate != null && child == modalButtonTemplate.transform) continue;
                Button button = child.GetComponent<Button>();
                if (button == null || !child.gameObject.activeSelf) continue;

                LayoutElement le = child.GetComponent<LayoutElement>();
                if (le == null) le = child.gameObject.AddComponent<LayoutElement>();
                le.minHeight = 66f;
                le.preferredHeight = 66f;

                Text[] texts = button.GetComponentsInChildren<Text>(true);
                foreach (Text text in texts)
                {
                    Transform chip = button.transform.Find("QuestActionChip");
                    if (chip != null && text.transform.IsChildOf(chip)) continue;
                    text.fontSize = 17;
                    text.lineSpacing = 1.08f;
                    text.alignment = TextAnchor.MiddleCenter;
                    text.horizontalOverflow = HorizontalWrapMode.Wrap;
                    text.verticalOverflow = VerticalWrapMode.Truncate;
                    break;
                }
            }
        }

        private static Color LocationBackground(string id)
        {
            // Muted, low-saturation, fully opaque tones. These are placeholders for future scene art.
            switch (id)
            {
                case "LOC_01_SETTLEMENT": return new Color(.105f, .079f, .058f, 1f);
                case "LOC_02_SHIP":       return new Color(.052f, .069f, .079f, 1f);
                case "LOC_03_GRAVEYARD":  return new Color(.061f, .062f, .069f, 1f);
                case "LOC_04_OASIS":      return new Color(.049f, .078f, .070f, 1f);
                case "LOC_05_COMMAND":    return new Color(.046f, .058f, .075f, 1f);
                case "LOC_06_SUPPLY":     return new Color(.070f, .071f, .053f, 1f);
                case "LOC_07_TECH":       return new Color(.061f, .054f, .075f, 1f);
                case "LOC_08_HABIT":      return new Color(.078f, .057f, .054f, 1f);
                default:                    return new Color(.035f, .039f, .043f, 1f);
            }
        }

        private static void StyleTemplate(Button template, float height, int fontSize, TextAnchor alignment)
        {
            if (template == null) return;
            LayoutElement le = template.GetComponent<LayoutElement>();
            if (le == null) le = template.gameObject.AddComponent<LayoutElement>();
            le.minHeight = height;
            le.preferredHeight = height;

            Text text = template.GetComponentInChildren<Text>(true);
            if (text != null)
            {
                text.fontSize = fontSize;
                text.lineSpacing = 1.08f;
                text.alignment = alignment;
                text.horizontalOverflow = HorizontalWrapMode.Wrap;
                text.verticalOverflow = VerticalWrapMode.Truncate;
            }
        }

        private static T FindNamed<T>(Transform root, string name) where T : Component
        {
            if (root == null) return null;
            Transform found = FindChildRecursive(root, name);
            return found != null ? found.GetComponent<T>() : null;
        }

        private static Transform FindChildRecursive(Transform root, string name)
        {
            if (root == null) return null;
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindChildRecursive(root.GetChild(i), name);
                if (found != null) return found;
            }
            return null;
        }

        private string GetString(FieldInfo field)
        {
            if (field == null) return string.Empty;
            try { return field.GetValue(controller) as string ?? string.Empty; }
            catch { return string.Empty; }
        }

        private static void SetRect(RectTransform rect, Vector2 min, Vector2 max)
        {
            if (rect == null) return;
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
