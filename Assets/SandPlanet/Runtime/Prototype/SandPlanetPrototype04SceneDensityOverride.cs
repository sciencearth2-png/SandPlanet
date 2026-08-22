using System;
using System.Reflection;
using SandPlanet.Prototype.DataDriven;
using UnityEngine;
using UnityEngine.UI;

namespace SandPlanet.Prototype
{
    /// <summary>
    /// Final scene-density scale authority.
    /// The redistributed map composition stays the same, but the browse scale returns
    /// to the stable 1.5x value so the whole scene remains readable at first glance.
    /// Selecting a target eases to 2.0x for the focused interaction shot.
    /// Position/pan continues to come from SceneInteractionPolish.
    /// </summary>
    [DefaultExecutionOrder(40000)]
    public sealed class SandPlanetPrototype04SceneDensityOverride : MonoBehaviour
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private const float BrowseScale = 1.50f;
        private const float FocusScale = 2.00f;
        private const float ScaleSpeed = 9.5f;

        private SandPlanetPrototype04Controller controller;
        private FieldInfo currentLocationField;
        private FieldInfo modalBusyField;
        private FieldInfo activeInteractionField;
        private FieldInfo targetRootField;
        private FieldInfo interactionRootField;
        private FieldInfo interactionTemplateField;

        private RectTransform targetRoot;
        private Transform interactionRoot;
        private Button interactionTemplate;
        private Vector3 originalScale = Vector3.one;
        private float currentMultiplier = 1f;
        private bool wasLocationOpen;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            SandPlanetPrototype04Controller found = UnityEngine.Object.FindFirstObjectByType<SandPlanetPrototype04Controller>();
            if (found != null && found.GetComponent<SandPlanetPrototype04SceneDensityOverride>() == null)
                found.gameObject.AddComponent<SandPlanetPrototype04SceneDensityOverride>();
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
            modalBusyField = t.GetField("modalBusy", PrivateInstance);
            activeInteractionField = t.GetField("activeInteraction", PrivateInstance);
            targetRootField = t.GetField("targetRoot", PrivateInstance);
            interactionRootField = t.GetField("interactionRoot", PrivateInstance);
            interactionTemplateField = t.GetField("interactionTemplate", PrivateInstance);
        }

        private void Start()
        {
            targetRoot = targetRootField?.GetValue(controller) as RectTransform;
            interactionRoot = interactionRootField?.GetValue(controller) as Transform;
            interactionTemplate = interactionTemplateField?.GetValue(controller) as Button;
            if (targetRoot == null)
            {
                enabled = false;
                return;
            }

            // SceneInteractionPolish still owns pan/position. We only remember the authored scale.
            originalScale = targetRoot.localScale;
        }

        private void LateUpdate()
        {
            if (targetRoot == null) return;

            bool locationOpen = IsLocationOpen();
            if (!locationOpen)
            {
                wasLocationOpen = false;
                currentMultiplier = 1f;
                targetRoot.localScale = originalScale;
                return;
            }

            bool focused = HasFocusedTarget();
            float desired = focused ? FocusScale : BrowseScale;

            // A location opens directly at the readable browse scale.
            if (!wasLocationOpen)
            {
                currentMultiplier = BrowseScale;
                wasLocationOpen = true;
            }

            float t = 1f - Mathf.Exp(-ScaleSpeed * Time.unscaledDeltaTime);
            currentMultiplier = Mathf.Lerp(currentMultiplier, desired, t);
            targetRoot.localScale = originalScale * currentMultiplier;
        }

        private bool HasFocusedTarget()
        {
            if (GetBool(modalBusyField))
            {
                // World Events are not tied to a selected map target; interactions are.
                SandPlanetInteraction04 active = activeInteractionField?.GetValue(controller) as SandPlanetInteraction04;
                if (active != null) return true;
            }

            return HasDynamicInteractionChoice();
        }

        private bool HasDynamicInteractionChoice()
        {
            if (interactionRoot == null) return false;
            for (int i = 0; i < interactionRoot.childCount; i++)
            {
                Transform child = interactionRoot.GetChild(i);
                if (interactionTemplate != null && child == interactionTemplate.transform) continue;
                if (child.gameObject.activeSelf && child.GetComponent<Button>() != null) return true;
            }
            return false;
        }

        private bool IsLocationOpen()
        {
            string id = currentLocationField?.GetValue(controller) as string;
            return !string.IsNullOrEmpty(id);
        }

        private bool GetBool(FieldInfo field)
        {
            if (field == null) return false;
            try { return (bool)field.GetValue(controller); }
            catch { return false; }
        }
    }
}
