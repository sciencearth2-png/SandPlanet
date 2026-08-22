using System.Reflection;
using UnityEngine;

namespace SandPlanet.Prototype
{
    /// <summary>
    /// Keeps the location-map composition unchanged while an interaction dialogue is open.
    /// The map may already be zoomed/panned during target selection; opening the narrative
    /// must not apply an additional scale or position change. Only the right narrative UI
    /// should change between selection and dialogue.
    /// </summary>
    [DefaultExecutionOrder(60000)]
    public sealed class SandPlanetPrototype04DialogueSceneTransformLock : MonoBehaviour
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

        private SandPlanetPrototype04Controller controller;
        private FieldInfo currentLocationField;
        private FieldInfo modalBusyField;
        private FieldInfo activeInteractionField;
        private FieldInfo targetRootField;

        private RectTransform targetRoot;
        private bool wasInteractionDialogueOpen;
        private bool locked;
        private Vector2 lockedPosition;
        private Vector3 lockedScale;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            SandPlanetPrototype04Controller found = Object.FindFirstObjectByType<SandPlanetPrototype04Controller>();
            if (found != null && found.GetComponent<SandPlanetPrototype04DialogueSceneTransformLock>() == null)
                found.gameObject.AddComponent<SandPlanetPrototype04DialogueSceneTransformLock>();
        }

        private void Awake()
        {
            controller = GetComponent<SandPlanetPrototype04Controller>();
            if (controller == null)
            {
                enabled = false;
                return;
            }

            System.Type t = controller.GetType();
            currentLocationField = t.GetField("currentLocationId", PrivateInstance);
            modalBusyField = t.GetField("modalBusy", PrivateInstance);
            activeInteractionField = t.GetField("activeInteraction", PrivateInstance);
            targetRootField = t.GetField("targetRoot", PrivateInstance);
        }

        private void Start()
        {
            targetRoot = targetRootField?.GetValue(controller) as RectTransform;
            if (targetRoot == null) enabled = false;
        }

        private void Update()
        {
            if (targetRoot == null) return;

            bool interactionDialogueOpen = IsInteractionDialogueOpen();

            // Capture the exact map composition from the previous target-selection frame
            // before LateUpdate zoom/pan layers get a chance to recalculate it for the modal.
            if (interactionDialogueOpen && !wasInteractionDialogueOpen)
            {
                lockedPosition = targetRoot.anchoredPosition;
                lockedScale = targetRoot.localScale;
                locked = true;
            }

            if (!interactionDialogueOpen)
                locked = false;

            wasInteractionDialogueOpen = interactionDialogueOpen;
        }

        private void LateUpdate()
        {
            if (!locked || targetRoot == null) return;

            // Run after all existing location polish layers and restore the selection-frame
            // transform so only the right narrative panel changes visually.
            targetRoot.anchoredPosition = lockedPosition;
            targetRoot.localScale = lockedScale;
        }

        private bool IsInteractionDialogueOpen()
        {
            string locationId = currentLocationField?.GetValue(controller) as string;
            if (string.IsNullOrEmpty(locationId)) return false;

            bool modalBusy;
            try { modalBusy = modalBusyField != null && (bool)modalBusyField.GetValue(controller); }
            catch { modalBusy = false; }
            if (!modalBusy) return false;

            // World Events may use the same modal panel but are not a continuation of a
            // selected map target. Lock only normal target interactions.
            return activeInteractionField?.GetValue(controller) != null;
        }
    }
}
