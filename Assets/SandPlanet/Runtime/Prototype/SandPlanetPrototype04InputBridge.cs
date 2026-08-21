using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace SandPlanet.Prototype
{
    /// <summary>
    /// Input-System-safe world click bridge for Prototype 0.4/v1.5.
    /// Hub raycasts live here so the data/narrative controller can stay input-system agnostic.
    /// </summary>
    [DefaultExecutionOrder(10000)]
    public sealed class SandPlanetPrototype04InputBridge : MonoBehaviour
    {
        private SandPlanetPrototype04Controller controller;
        private FieldInfo modalBusyField;
        private FieldInfo currentLocationIdField;
        private FieldInfo hubCameraField;
        private MethodInfo openLocationMethod;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            SandPlanetPrototype04Controller found = FindFirstObjectByType<SandPlanetPrototype04Controller>();
            if (found == null || found.GetComponent<SandPlanetPrototype04InputBridge>() != null)
                return;
            found.gameObject.AddComponent<SandPlanetPrototype04InputBridge>();
        }

        private void Awake()
        {
            controller = GetComponent<SandPlanetPrototype04Controller>();
            if (controller == null) return;

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            Type type = typeof(SandPlanetPrototype04Controller);
            modalBusyField = type.GetField("modalBusy", flags);
            currentLocationIdField = type.GetField("currentLocationId", flags);
            hubCameraField = type.GetField("hubCamera", flags);
            openLocationMethod = type.GetMethod("OpenLocation", flags);
        }

        private void Update()
        {
            if (controller == null || openLocationMethod == null) return;
            if (IsModalBusy() || HasOpenLocation()) return;
            if (!TryGetPointerDown(out Vector2 screenPosition)) return;
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

            Camera cam = hubCameraField?.GetValue(controller) as Camera;
            if (cam == null) cam = Camera.main;
            if (cam == null) return;

            Ray ray = cam.ScreenPointToRay(screenPosition);
            if (!Physics.Raycast(ray, out RaycastHit hit, 500f)) return;
            PrototypeLocationNode node = hit.collider.GetComponentInParent<PrototypeLocationNode>();
            if (node == null) return;
            openLocationMethod.Invoke(controller, new object[] { node.LocationId });
        }

        private bool IsModalBusy()
        {
            return modalBusyField != null && modalBusyField.GetValue(controller) is bool value && value;
        }

        private bool HasOpenLocation()
        {
            return currentLocationIdField != null && currentLocationIdField.GetValue(controller) is string id && !string.IsNullOrEmpty(id);
        }

        private static bool TryGetPointerDown(out Vector2 screenPosition)
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                screenPosition = Mouse.current.position.ReadValue();
                return true;
            }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetMouseButtonDown(0))
            {
                screenPosition = Input.mousePosition;
                return true;
            }
#endif
            screenPosition = default;
            return false;
        }
    }
}
