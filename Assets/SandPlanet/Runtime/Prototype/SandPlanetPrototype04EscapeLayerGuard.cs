using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SandPlanet.Prototype
{
    /// <summary>
    /// Owns the pre-Update ESC interception for the location interaction layer.
    /// InputSystem.onAfterUpdate runs before MonoBehaviour.Update, so the legacy UX
    /// handler cannot receive the same ESC frame after this layer consumes it.
    ///
    /// Order while a location is open:
    /// 1) interaction-selection panel only
    /// 2) location panel on the next ESC (handled by the legacy UX layer)
    /// Modal dialogue/event ESC rules remain owned by the controller.
    /// </summary>
    [DefaultExecutionOrder(-30000)]
    public sealed class SandPlanetPrototype04EscapeLayerGuard : MonoBehaviour
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private const BindingFlags PrivateStatic = BindingFlags.Static | BindingFlags.NonPublic;

        private SandPlanetPrototype04Controller controller;
        private SandPlanetPrototype04UxEnhancer uxEnhancer;

        private FieldInfo currentLocationField;
        private FieldInfo modalBusyField;
        private FieldInfo locationHintField;
        private FieldInfo interactionRootField;
        private FieldInfo interactionTemplateField;
        private MethodInfo clearDynamicMethod;

        private FieldInfo uxSelectedTargetTypeField;
        private FieldInfo uxSelectedTargetIdField;
        private int handledFrame = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            SandPlanetPrototype04Controller found = UnityEngine.Object.FindFirstObjectByType<SandPlanetPrototype04Controller>();
            if (found != null && found.GetComponent<SandPlanetPrototype04EscapeLayerGuard>() == null)
                found.gameObject.AddComponent<SandPlanetPrototype04EscapeLayerGuard>();
        }

        private void Awake()
        {
            controller = GetComponent<SandPlanetPrototype04Controller>();
            if (controller == null)
            {
                enabled = false;
                return;
            }

            Type controllerType = controller.GetType();
            currentLocationField = controllerType.GetField("currentLocationId", PrivateInstance);
            modalBusyField = controllerType.GetField("modalBusy", PrivateInstance);
            locationHintField = controllerType.GetField("locationHintText", PrivateInstance);
            interactionRootField = controllerType.GetField("interactionRoot", PrivateInstance);
            interactionTemplateField = controllerType.GetField("interactionTemplate", PrivateInstance);
            clearDynamicMethod = controllerType.GetMethod("ClearDynamic", PrivateStatic);

            CacheUxEnhancer();
        }

        private void OnEnable()
        {
            InputSystem.onAfterUpdate += OnAfterInputUpdate;
        }

        private void OnDisable()
        {
            InputSystem.onAfterUpdate -= OnAfterInputUpdate;
        }

        private void OnAfterInputUpdate()
        {
            if (!enabled || Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame) return;
            if (handledFrame == Time.frameCount) return;
            if (GetBool(modalBusyField)) return; // dialogue/event ESC remains controller-owned

            string locationId = currentLocationField?.GetValue(controller) as string;
            if (string.IsNullOrEmpty(locationId)) return;

            Transform root = interactionRootField?.GetValue(controller) as Transform;
            Button template = interactionTemplateField?.GetValue(controller) as Button;
            if (!HasDynamicChoice(root, template)) return;

            handledFrame = Time.frameCount;
            CacheUxEnhancer();

            // Disable the later legacy Update before the Update loop begins.
            bool reenableUx = uxEnhancer != null && uxEnhancer.enabled;
            if (reenableUx) uxEnhancer.enabled = false;

            // Hide immediately so the right panel cannot be re-enabled during this render.
            if (root != null)
            {
                for (int i = root.childCount - 1; i >= 0; i--)
                {
                    Transform child = root.GetChild(i);
                    if (template != null && child == template.transform) continue;
                    child.gameObject.SetActive(false);
                }
                if (root.parent != null) root.parent.gameObject.SetActive(false);
            }

            // Then destroy the authored dynamic choices through the controller's own helper.
            if (clearDynamicMethod != null)
            {
                try { clearDynamicMethod.Invoke(null, new object[] { root, template }); }
                catch { }
            }

            Text hint = locationHintField?.GetValue(controller) as Text;
            if (hint != null)
                hint.text = "사람/사물을 선택하면 현재 가능한 상호작용이 표시됩니다.";

            if (uxEnhancer != null)
            {
                try { uxSelectedTargetTypeField?.SetValue(uxEnhancer, null); } catch { }
                try { uxSelectedTargetIdField?.SetValue(uxEnhancer, null); } catch { }
            }

            if (reenableUx) StartCoroutine(ReenableUxNextFrame());
        }

        private void CacheUxEnhancer()
        {
            if (uxEnhancer == null) uxEnhancer = GetComponent<SandPlanetPrototype04UxEnhancer>();
            if (uxEnhancer == null) return;

            Type uxType = uxEnhancer.GetType();
            uxSelectedTargetTypeField = uxType.GetField("selectedTargetType", PrivateInstance);
            uxSelectedTargetIdField = uxType.GetField("selectedTargetId", PrivateInstance);
        }

        private IEnumerator ReenableUxNextFrame()
        {
            yield return null;
            if (uxEnhancer != null) uxEnhancer.enabled = true;
        }

        private static bool HasDynamicChoice(Transform root, Button template)
        {
            if (root == null) return false;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (template != null && child == template.transform) continue;
                if (child.gameObject.activeSelf && child.GetComponent<Button>() != null) return true;
            }
            return false;
        }

        private bool GetBool(FieldInfo field)
        {
            if (field == null) return false;
            try { return (bool)field.GetValue(controller); }
            catch { return false; }
        }
    }
}
