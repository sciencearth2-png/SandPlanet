using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SandPlanet.Prototype
{
    /// <summary>
    /// Runs before the legacy UX Escape handler so a selected target's interaction list
    /// closes first. A second ESC then reaches the legacy handler and closes the location.
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
            uxEnhancer = GetComponent<SandPlanetPrototype04UxEnhancer>();
            if (controller == null)
            {
                enabled = false;
                return;
            }

            Type t = controller.GetType();
            currentLocationField = t.GetField("currentLocationId", PrivateInstance);
            modalBusyField = t.GetField("modalBusy", PrivateInstance);
            locationHintField = t.GetField("locationHintText", PrivateInstance);
            interactionRootField = t.GetField("interactionRoot", PrivateInstance);
            interactionTemplateField = t.GetField("interactionTemplate", PrivateInstance);
            clearDynamicMethod = t.GetMethod("ClearDynamic", PrivateStatic);
        }

        private void Update()
        {
            if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame) return;
            if (GetBool(modalBusyField)) return;

            string locationId = currentLocationField?.GetValue(controller) as string;
            if (string.IsNullOrEmpty(locationId)) return;

            Transform root = interactionRootField?.GetValue(controller) as Transform;
            Button template = interactionTemplateField?.GetValue(controller) as Button;
            if (!HasDynamicChoice(root, template)) return;

            if (clearDynamicMethod != null)
            {
                try { clearDynamicMethod.Invoke(null, new object[] { root, template }); }
                catch { }
            }

            Text hint = locationHintField?.GetValue(controller) as Text;
            if (hint != null)
                hint.text = "사람/사물을 선택하면 현재 가능한 상호작용이 표시됩니다.";

            // Prevent the legacy UX Update from receiving this same key-down frame.
            if (uxEnhancer != null && uxEnhancer.enabled)
            {
                uxEnhancer.enabled = false;
                StartCoroutine(ReenableNextFrame());
            }
        }

        private IEnumerator ReenableNextFrame()
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
