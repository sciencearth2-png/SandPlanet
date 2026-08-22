using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using SandPlanet.Prototype.DataDriven;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SandPlanet.Prototype
{
    /// <summary>
    /// Final interaction-stage polish layered on top of Prototype04 UI.
    /// - Location stage is fully opaque and sits above Quest/Log overlays.
    /// - ESC closes target interaction choices first, then the location on the next press.
    /// - Character portrait is 1.5x larger and also appears while choosing an interaction.
    /// - The location target group behaves like a light virtual camera: selecting a target
    ///   zooms/pans the whole scene group, then eases back after the interaction closes.
    ///
    /// This changes presentation only; authoring data and gameplay rules are untouched.
    /// </summary>
    [DefaultExecutionOrder(30000)]
    public sealed class SandPlanetPrototype04SceneInteractionPolish : MonoBehaviour
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private const BindingFlags PrivateStatic = BindingFlags.Static | BindingFlags.NonPublic;
        private const float FocusScale = 1.30f;
        private const float MotionSpeed = 8.5f;

        private SandPlanetPrototype04Controller controller;
        private SandPlanetPrototype04UxEnhancer uxEnhancer;
        private SandPlanetContent04 content;
        private Canvas canvas;

        private FieldInfo contentField;
        private FieldInfo currentLocationField;
        private FieldInfo modalBusyField;
        private FieldInfo locationPanelField;
        private FieldInfo locationHintField;
        private FieldInfo targetRootField;
        private FieldInfo targetTemplateField;
        private FieldInfo interactionRootField;
        private FieldInfo interactionTemplateField;
        private FieldInfo modalPanelField;
        private FieldInfo activeInteractionField;
        private MethodInfo clearDynamicMethod;

        private GameObject locationPanel;
        private Text locationHint;
        private RectTransform targetRoot;
        private Button targetTemplate;
        private Transform interactionRoot;
        private Button interactionTemplate;
        private GameObject interactionPanel;
        private GameObject modalPanel;

        private GameObject portraitFrame;
        private Image portraitImage;
        private Text portraitName;

        private Vector2 targetBasePosition;
        private Vector3 targetBaseScale = Vector3.one;
        private Vector2 targetGoalPosition;
        private Vector3 targetGoalScale = Vector3.one;
        private string focusedTargetId = string.Empty;
        private string focusedTargetType = string.Empty;
        private bool escapeConsumed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            SandPlanetPrototype04Controller found = UnityEngine.Object.FindFirstObjectByType<SandPlanetPrototype04Controller>();
            if (found != null && found.GetComponent<SandPlanetPrototype04SceneInteractionPolish>() == null)
                found.gameObject.AddComponent<SandPlanetPrototype04SceneInteractionPolish>();
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
            modalBusyField = t.GetField("modalBusy", PrivateInstance);
            locationPanelField = t.GetField("locationPanel", PrivateInstance);
            locationHintField = t.GetField("locationHintText", PrivateInstance);
            targetRootField = t.GetField("targetRoot", PrivateInstance);
            targetTemplateField = t.GetField("targetTemplate", PrivateInstance);
            interactionRootField = t.GetField("interactionRoot", PrivateInstance);
            interactionTemplateField = t.GetField("interactionTemplate", PrivateInstance);
            modalPanelField = t.GetField("modalPanel", PrivateInstance);
            activeInteractionField = t.GetField("activeInteraction", PrivateInstance);
            clearDynamicMethod = t.GetMethod("ClearDynamic", PrivateStatic);
        }

        private void Start()
        {
            content = contentField?.GetValue(controller) as SandPlanetContent04;
            canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
            uxEnhancer = GetComponent<SandPlanetPrototype04UxEnhancer>();
            locationPanel = locationPanelField?.GetValue(controller) as GameObject;
            locationHint = locationHintField?.GetValue(controller) as Text;
            targetRoot = targetRootField?.GetValue(controller) as RectTransform;
            targetTemplate = targetTemplateField?.GetValue(controller) as Button;
            interactionRoot = interactionRootField?.GetValue(controller) as Transform;
            interactionTemplate = interactionTemplateField?.GetValue(controller) as Button;
            modalPanel = modalPanelField?.GetValue(controller) as GameObject;
            interactionPanel = interactionRoot != null && interactionRoot.parent != null ? interactionRoot.parent.gameObject : null;

            if (content == null || canvas == null || locationPanel == null || targetRoot == null || interactionRoot == null || modalPanel == null)
            {
                enabled = false;
                return;
            }

            targetBasePosition = targetRoot.anchoredPosition;
            targetBaseScale = targetRoot.localScale;
            targetGoalPosition = targetBasePosition;
            targetGoalScale = targetBaseScale;

            PrepareSharedPortrait();
            EnforcePresentation();
            Canvas.willRenderCanvases += EnforcePresentation;
        }

        private void OnDestroy()
        {
            Canvas.willRenderCanvases -= EnforcePresentation;
        }

        private void Update()
        {
            if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame) return;
            if (GetBool(modalBusyField)) return; // Existing modal/event ESC rules remain authoritative.
            if (!IsLocationOpen()) return;
            if (!HasInteractionSelection()) return;

            CloseInteractionSelectionOnly();
            if (uxEnhancer != null && uxEnhancer.enabled)
            {
                uxEnhancer.enabled = false;
                escapeConsumed = true;
                StartCoroutine(ReenableUxEnhancerNextFrame());
            }
        }

        private IEnumerator ReenableUxEnhancerNextFrame()
        {
            yield return null;
            if (uxEnhancer != null) uxEnhancer.enabled = true;
            escapeConsumed = false;
        }

        private void LateUpdate()
        {
            RefreshFocusTarget();
            AnimateSceneCamera();
            RefreshPortraitPresentation();
            EnforcePresentation();
        }

        private void EnforcePresentation()
        {
            if (!enabled || locationPanel == null) return;

            Image locationImage = locationPanel.GetComponent<Image>();
            if (locationImage != null)
                locationImage.color = new Color(.025f, .030f, .035f, 1f); // fully opaque

            if (IsLocationOpen())
            {
                // Location stage intentionally hides Quest/Log while open.
                locationPanel.transform.SetAsLastSibling();
                if (interactionPanel != null && interactionPanel.activeInHierarchy)
                    interactionPanel.transform.SetAsLastSibling();
                if (modalPanel != null && modalPanel.activeInHierarchy)
                    modalPanel.transform.SetAsLastSibling();
                if (portraitFrame != null && portraitFrame.activeInHierarchy)
                    portraitFrame.transform.SetAsLastSibling();
            }
        }

        private void PrepareSharedPortrait()
        {
            Transform existing = FindChildRecursive(canvas.transform, "UX18_SpeakerPortraitFrame");
            if (existing == null) return;

            portraitFrame = existing.gameObject;
            portraitImage = existing.Find("Portrait")?.GetComponent<Image>();
            portraitName = existing.Find("Name")?.GetComponent<Text>();

            // Reparent to the Canvas so the same portrait can appear before the modal opens.
            portraitFrame.transform.SetParent(canvas.transform, false);
            RectTransform frame = portraitFrame.GetComponent<RectTransform>();
            frame.anchorMin = frame.anchorMax = new Vector2(.739f, .655f);
            frame.pivot = new Vector2(1f, .5f);
            frame.anchoredPosition = new Vector2(-14f, 0f);
            frame.sizeDelta = new Vector2(225f, 315f); // ~1.5x previous 150x210

            Image frameImage = portraitFrame.GetComponent<Image>();
            if (frameImage != null)
                frameImage.color = new Color(.045f, .050f, .055f, .98f);

            if (portraitImage != null)
            {
                SetRect(portraitImage.rectTransform, new Vector2(.045f, .145f), new Vector2(.955f, .97f));
                portraitImage.preserveAspect = true;
            }
            if (portraitName != null)
            {
                SetRect(portraitName.rectTransform, new Vector2(.04f, .015f), new Vector2(.96f, .145f));
                portraitName.fontSize = 17;
            }
        }

        private void RefreshPortraitPresentation()
        {
            if (portraitFrame == null || portraitImage == null || portraitName == null) return;

            bool modalBusy = GetBool(modalBusyField);
            if (modalBusy)
            {
                // SceneDialogueLayout already chooses the active dialogue speaker.
                // We only keep the larger shared frame above the panels.
                if (portraitFrame.activeSelf) portraitFrame.transform.SetAsLastSibling();
                return;
            }

            if (!HasInteractionSelection() || !string.Equals(focusedTargetType, "CHARACTER", StringComparison.Ordinal))
            {
                portraitFrame.SetActive(false);
                return;
            }

            SceneTargetView04 view = FindTargetView(focusedTargetType, focusedTargetId);
            if (view == null)
            {
                portraitFrame.SetActive(false);
                return;
            }

            Image source = view.transform.Find("UX18_ScenePortrait")?.GetComponent<Image>();
            if (source == null || source.sprite == null)
            {
                portraitFrame.SetActive(false);
                return;
            }

            portraitImage.sprite = source.sprite;
            portraitImage.color = Color.white;
            portraitName.text = TargetDisplayName(focusedTargetType, focusedTargetId);
            portraitFrame.SetActive(true);
            portraitFrame.transform.SetAsLastSibling();
        }

        private void RefreshFocusTarget()
        {
            string type = string.Empty;
            string id = string.Empty;

            if (GetBool(modalBusyField))
            {
                SandPlanetInteraction04 interaction = activeInteractionField?.GetValue(controller) as SandPlanetInteraction04;
                if (interaction != null)
                {
                    type = interaction.TargetType ?? string.Empty;
                    id = interaction.TargetId ?? string.Empty;
                }
            }
            else if (HasInteractionSelection())
            {
                string name = SelectedTargetNameFromHint();
                if (!string.IsNullOrEmpty(name))
                {
                    SandPlanetCharacter04 c = content.Characters.Values.FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.Ordinal));
                    if (c != null)
                    {
                        type = "CHARACTER";
                        id = c.Id;
                    }
                    else
                    {
                        SandPlanetWorldTarget04 w = content.WorldTargets.Values.FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.Ordinal));
                        if (w != null)
                        {
                            type = "WORLD_TARGET";
                            id = w.Id;
                        }
                    }
                }
            }

            if (string.Equals(type, focusedTargetType, StringComparison.Ordinal) && string.Equals(id, focusedTargetId, StringComparison.Ordinal))
                return;

            focusedTargetType = type;
            focusedTargetId = id;
            CalculateFocusGoal();
        }

        private void CalculateFocusGoal()
        {
            if (string.IsNullOrEmpty(focusedTargetId))
            {
                targetGoalPosition = targetBasePosition;
                targetGoalScale = targetBaseScale;
                return;
            }

            SceneTargetView04 view = FindTargetView(focusedTargetType, focusedTargetId);
            RectTransform selected = view != null ? view.GetComponent<RectTransform>() : null;
            if (selected == null)
            {
                targetGoalPosition = targetBasePosition;
                targetGoalScale = targetBaseScale;
                return;
            }

            Vector3 worldCenter = selected.TransformPoint(selected.rect.center);
            Vector3 localCenter3 = targetRoot.InverseTransformPoint(worldCenter);
            Vector2 localCenter = new Vector2(localCenter3.x, localCenter3.y);

            // Controlled pan: enough to feel like a camera move without losing the rest of the scene.
            Vector2 offset = -localCenter * .42f;
            offset.x = Mathf.Clamp(offset.x, -175f, 175f);
            offset.y = Mathf.Clamp(offset.y, -105f, 105f);
            targetGoalPosition = targetBasePosition + offset;
            targetGoalScale = targetBaseScale * FocusScale;
        }

        private void AnimateSceneCamera()
        {
            if (targetRoot == null) return;
            float t = 1f - Mathf.Exp(-MotionSpeed * Time.unscaledDeltaTime);
            targetRoot.anchoredPosition = Vector2.Lerp(targetRoot.anchoredPosition, targetGoalPosition, t);
            targetRoot.localScale = Vector3.Lerp(targetRoot.localScale, targetGoalScale, t);
        }

        private void CloseInteractionSelectionOnly()
        {
            if (clearDynamicMethod != null)
            {
                try { clearDynamicMethod.Invoke(null, new object[] { interactionRoot, interactionTemplate }); }
                catch { }
            }

            if (interactionPanel != null) interactionPanel.SetActive(false);
            if (locationHint != null)
                locationHint.text = "사람/사물을 선택하면 현재 가능한 상호작용이 표시됩니다.";

            focusedTargetType = string.Empty;
            focusedTargetId = string.Empty;
            targetGoalPosition = targetBasePosition;
            targetGoalScale = targetBaseScale;
            if (portraitFrame != null) portraitFrame.SetActive(false);
        }

        private bool HasInteractionSelection()
        {
            if (interactionRoot == null || interactionPanel == null || !interactionPanel.activeInHierarchy) return false;
            for (int i = 0; i < interactionRoot.childCount; i++)
            {
                Transform child = interactionRoot.GetChild(i);
                if (interactionTemplate != null && child == interactionTemplate.transform) continue;
                if (child.gameObject.activeSelf && child.GetComponent<Button>() != null) return true;
            }
            return false;
        }

        private string SelectedTargetNameFromHint()
        {
            string raw = locationHint != null ? locationHint.text ?? string.Empty : string.Empty;
            int separator = raw.IndexOf(" — ", StringComparison.Ordinal);
            if (separator >= 0) raw = raw.Substring(0, separator);
            return raw.Trim();
        }

        private SceneTargetView04 FindTargetView(string type, string id)
        {
            if (targetRoot == null || string.IsNullOrEmpty(id)) return null;
            SceneTargetView04[] views = targetRoot.GetComponentsInChildren<SceneTargetView04>(true);
            return views.FirstOrDefault(v => string.Equals(v.TargetType, type, StringComparison.Ordinal) && string.Equals(v.TargetId, id, StringComparison.Ordinal));
        }

        private string TargetDisplayName(string type, string id)
        {
            if (type == "CHARACTER" && content.Characters.TryGetValue(id, out SandPlanetCharacter04 c)) return c.Name;
            if (type == "WORLD_TARGET" && content.WorldTargets.TryGetValue(id, out SandPlanetWorldTarget04 w)) return w.Name;
            return id;
        }

        private bool IsLocationOpen()
        {
            string id = currentLocationField?.GetValue(controller) as string;
            return !string.IsNullOrEmpty(id) && locationPanel != null && locationPanel.activeInHierarchy;
        }

        private bool GetBool(FieldInfo field)
        {
            if (field == null) return false;
            try { return (bool)field.GetValue(controller); }
            catch { return false; }
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
