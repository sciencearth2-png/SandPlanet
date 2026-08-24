using UnityEngine;

namespace SandPlanet.Prototype
{
    public sealed class Prototype04CompositionRoot : MonoBehaviour
    {
        private SandPlanetPrototype04Controller controller;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            SandPlanetPrototype04Controller found = FindAnyObjectByType<SandPlanetPrototype04Controller>();
            if (found != null && found.GetComponent<Prototype04CompositionRoot>() == null)
                found.gameObject.AddComponent<Prototype04CompositionRoot>();
        }

        private void Awake()
        {
            controller = GetComponent<SandPlanetPrototype04Controller>();
            if (controller == null)
            {
                enabled = false;
                return;
            }

            // Controller.Awake has loaded content. Pause Start until the opening allocation is confirmed.
            controller.enabled = false;
            Prototype04RuntimeFacade runtime = new Prototype04RuntimeFacade(controller);

            Prototype04NavigationController navigation = gameObject.AddComponent<Prototype04NavigationController>();
            Prototype04LocationPresenter location = gameObject.AddComponent<Prototype04LocationPresenter>();
            Prototype04NarrativeLogPresenter narrative = gameObject.AddComponent<Prototype04NarrativeLogPresenter>();
            Prototype04HudPresenter hud = gameObject.AddComponent<Prototype04HudPresenter>();
            SandPlanetPrototype04W1QuestTracker quests = gameObject.AddComponent<SandPlanetPrototype04W1QuestTracker>();
            SandPlanetPrototype04PeoplePanel people = gameObject.AddComponent<SandPlanetPrototype04PeoplePanel>();
            SandPlanetPrototype04OpeningSetup opening = gameObject.AddComponent<SandPlanetPrototype04OpeningSetup>();

            navigation.Initialize(runtime);
            location.Initialize(runtime);
            narrative.Initialize(runtime);
            hud.Initialize(runtime);
            quests.Initialize(runtime, navigation);
            people.Initialize(runtime, navigation);
            navigation.RegisterOverlays(people, quests);
            opening.Initialize(runtime, controller);

            controller.EnableConsolidatedPresentation();
        }
    }
}
