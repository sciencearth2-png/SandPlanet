using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace SandPlanet.Prototype
{
    public sealed class Prototype04NavigationController : MonoBehaviour
    {
        private Prototype04RuntimeFacade runtime;
        private SandPlanetPrototype04PeoplePanel peoplePanel;
        private SandPlanetPrototype04W1QuestTracker questTracker;

        public Prototype04UiMode Mode { get; private set; } = Prototype04UiMode.Hub;

        public void Initialize(Prototype04RuntimeFacade runtimeFacade)
        {
            runtime = runtimeFacade;
            runtime.Changed += RefreshMode;
            if (runtime.BackButton != null)
            {
                runtime.BackButton.onClick.RemoveAllListeners();
                runtime.BackButton.onClick.AddListener(Back);
            }
            RefreshMode();
        }

        public void RegisterOverlays(SandPlanetPrototype04PeoplePanel people, SandPlanetPrototype04W1QuestTracker quests)
        {
            peoplePanel = people;
            questTracker = quests;
            RefreshMode();
        }

        private void OnDestroy()
        {
            if (runtime != null) runtime.Changed -= RefreshMode;
        }

        private void Update()
        {
            if (runtime == null) return;
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                Back();
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.Escape))
                Back();
#endif
            HandleWorldClick();
        }

        public void Back()
        {
            RefreshMode();
            switch (Mode)
            {
                case Prototype04UiMode.People:
                    peoplePanel.CloseFromNavigation();
                    break;
                case Prototype04UiMode.QuestDetail:
                    questTracker.CloseDetailFromNavigation();
                    break;
                case Prototype04UiMode.ForcedChoice:
                    break;
                case Prototype04UiMode.Narrative:
                    runtime.BackFromNarrative();
                    break;
                case Prototype04UiMode.TargetSelected:
                    runtime.DismissTargetSelection();
                    break;
                case Prototype04UiMode.LocationBrowse:
                    runtime.CloseLocation();
                    break;
            }
            RefreshMode();
        }

        public void NotifyOverlayChanged() => RefreshMode();

        private void RefreshMode()
        {
            if (peoplePanel != null && peoplePanel.IsOpen)
            {
                Mode = Prototype04UiMode.People;
                return;
            }
            if (questTracker != null && questTracker.IsDetailOpen)
            {
                Mode = Prototype04UiMode.QuestDetail;
                return;
            }
            if (runtime != null && runtime.ModalBusy)
            {
                Mode = runtime.IsForcedEventChoice() ? Prototype04UiMode.ForcedChoice : Prototype04UiMode.Narrative;
                return;
            }
            if (runtime != null && !string.IsNullOrEmpty(runtime.SelectedTargetId))
            {
                Mode = Prototype04UiMode.TargetSelected;
                return;
            }
            Mode = runtime != null && !string.IsNullOrEmpty(runtime.CurrentLocationId)
                ? Prototype04UiMode.LocationBrowse
                : Prototype04UiMode.Hub;
        }

        private void HandleWorldClick()
        {
            if (Mode != Prototype04UiMode.Hub || !TryGetPointerDown(out Vector2 screenPosition)) return;
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

            Camera camera = runtime.HubCamera != null ? runtime.HubCamera : Camera.main;
            if (camera == null) return;
            Ray ray = camera.ScreenPointToRay(screenPosition);
            if (!Physics.Raycast(ray, out RaycastHit hit, 500f)) return;
            PrototypeLocationNode node = hit.collider.GetComponentInParent<PrototypeLocationNode>();
            if (node != null) runtime.OpenLocation(node.LocationId);
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
