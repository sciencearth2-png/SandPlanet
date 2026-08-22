using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace SandPlanet.Prototype
{
    /// <summary>
    /// Visual-only polish for the Prototype04 location viewport.
    ///
    /// IMPORTANT:
    /// This component must never write target positions, sizes, anchors, or scale.
    /// SandPlanetPrototype04SceneDialogueLayout is the single authority for
    /// character/world-target layout. Keeping that ownership singular prevents
    /// render-order overrides and the repeated placement regressions we saw before.
    ///
    /// Responsibilities here are limited to:
    /// - invisible viewport clipping,
    /// - muted per-location background colors,
    /// - panel draw order,
    /// - location/interaction/dialogue typography and button sizing.
    /// </summary>
    [DefaultExecutionOrder(50000)]
    public sealed class SandPlanetPrototype04MapViewportPolish : MonoBehaviour
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

        private SandPlanetPrototype04Controller controller;
        private FieldInfo currentLocationField;
        private FieldInfo locationPanelField;
        private FieldInfo locationTitleField;
        private FieldInfo interactionRootField;
        private FieldInfo interactionTemplateField;
        private FieldInfo modalPanelField;
        private FieldInfo modalTitleField;
        private FieldInfo modalBodyField;
        private FieldInfo modalButtonRootField;
        private FieldInfo modalButtonTemplateField;

        private GameObject locationPanel;
        private Text locationTitle;
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
            currentLocationField = t.GetField("currentLocationId", PrivateInstance);
            locationPanelField = t.GetField("locationPanel", PrivateInstance);
            locationTitleField = t.GetField("locationTitleText", PrivateInstance);
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
            locationPanel = locationPanelField?.GetValue(controller) as GameObject;
            locationTitle = locationTitleField?.GetValue(controller) as Text;
            interactionRoot = interactionRootField?.GetValue(controller) as Transform;
            interactionTemplate = interactionTemplateField?.GetValue(controller) as Button;
            modalPanel = modalPanelField?.GetValue(controller) as GameObject;
            modalTitle = modalTitleField?.GetValue(controller) as Text;
            modalBody = modalBodyField?.GetValue(controller) as Text;
            modalButtonRoot = modalButtonRootField?.GetValue(controller) as Transform;
            modalButtonTemplate = modalButtonTemplateField?.GetValue(controller) as Button;

            if (locationPanel == null || interactionRoot == null || modalPanel == null || modalButtonRoot == null)
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

            // Layering only. Target transforms are intentionally untouched here.
            locationPanel.transform.SetAsLastSibling();
            if (interactionRoot.parent != null && interactionRoot.parent.gameObject.activeInHierarchy)
                interactionRoot.parent.SetAsLastSibling();
            if (modalPanel.activeInHierarchy)
                modalPanel.transform.SetAsLastSibling();

            ApplyStaticTypography();
            StyleDynamicInteractionButtons();
            StyleDynamicModalButtons();
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
