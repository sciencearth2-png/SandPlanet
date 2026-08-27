using System;
using System.Collections.Generic;
using System.Linq;
using SandPlanet.Prototype.DataDriven;
using UnityEngine;
using UnityEngine.UI;

namespace SandPlanet.Prototype
{
    public enum Prototype04UiMode
    {
        Hub,
        LocationBrowse,
        TargetSelected,
        Narrative,
        ForcedChoice,
        People,
        QuestDetail
    }

    public readonly struct Prototype04TargetViewModel
    {
        public readonly string Type;
        public readonly string Id;
        public readonly string Name;

        public Prototype04TargetViewModel(string type, string id, string name)
        {
            Type = type;
            Id = id;
            Name = name;
        }
    }

    public sealed class Prototype04RuntimeFacade
    {
        private readonly SandPlanetPrototype04Controller controller;

        public Prototype04RuntimeFacade(SandPlanetPrototype04Controller controller)
        {
            this.controller = controller ?? throw new ArgumentNullException(nameof(controller));
        }

        public event Action Changed
        {
            add => controller.PresentationChanged += value;
            remove => controller.PresentationChanged -= value;
        }

        public SandPlanetContent04 Content => controller.Content;
        public Camera HubCamera => controller.HubCamera;
        public GameObject LocationPanel => controller.LocationPanelObject;
        public Text LocationTitle => controller.LocationTitleText;
        public Text LocationHint => controller.LocationHintText;
        public RectTransform TargetRoot => controller.TargetRoot;
        public Button TargetTemplate => controller.TargetTemplate;
        public Transform InteractionRoot => controller.InteractionRoot;
        public Button InteractionTemplate => controller.InteractionTemplate;
        public Button BackButton => controller.BackButton;
        public GameObject ModalPanel => controller.ModalPanelObject;
        public Text ModalTitle => controller.ModalTitleText;
        public Text ModalBody => controller.ModalBodyText;
        public Transform ModalButtonRoot => controller.ModalButtonRoot;
        public Button ModalButtonTemplate => controller.ModalButtonTemplate;
        public Text HudText => controller.HudText;
        public Text QuestTrackerText => controller.QuestTrackerText;
        public Text LogText => controller.LogText;
        public Button EndDayButton => controller.EndDayButton;
        public TextAsset[] CsvAssets => controller.CsvAssets;
        public int Day => controller.Day;
        public int Hour => controller.Hour;
        public int Will => controller.Will;
        public int MaxWill => controller.MaxWill;
        public int PersonalLevel => controller.PersonalLevel;
        public int PersonalXp => controller.PersonalXp;
        public int SocialLevel => controller.SocialLevel;
        public int SocialXp => controller.SocialXp;
        public int TechnicalLevel => controller.TechnicalLevel;
        public int TechnicalXp => controller.TechnicalXp;
        public string CurrentLocationId => controller.CurrentLocationId;
        public string SelectedTargetType => controller.SelectedTargetType;
        public string SelectedTargetId => controller.SelectedTargetId;
        public bool ModalBusy => controller.ModalBusy;
        public bool HasActiveInteractionFlow => controller.HasActiveInteractionFlow;
        public bool HasActiveEventFlow => controller.HasActiveEventFlow;
        public bool FlowCommitted => controller.FlowCommitted;
        public SandPlanetInteraction04 ActiveInteraction => controller.ActiveInteraction;
        public IReadOnlyDictionary<string, string> States => controller.States;
        public IReadOnlyDictionary<string, int> Affinity => controller.Affinity;
        public IReadOnlyDictionary<string, string> QuestStatuses => controller.QuestStatuses;
        public IReadOnlyDictionary<string, string> QuestSteps => controller.QuestSteps;
        public IReadOnlyCollection<string> EventsOccurred => controller.EventsOccurred;

        public IReadOnlyList<Prototype04TargetViewModel> CurrentTargets() => controller.CurrentTargets();
        public List<SandPlanetInteraction04> AvailableInteractions(string type, string id) => controller.GetAvailableInteractions(type, id);
        public string CharacterLocation(string id) => controller.GetCharacterLocation(id);
        public string QuestStatus(string id) => controller.GetQuestStatus(id);
        public string QuestStep(string id) => controller.GetQuestStep(id);
        public string State(string id) => controller.GetState(id);
        public int AffinityValue(string id) => controller.GetAffinity(id);
        public IReadOnlyList<SandPlanetFlowNode04> CurrentFlowRows() => controller.CurrentFlowRows();
        public string CurrentFlowTitle() => controller.CurrentFlowTitle();
        public string CurrentFlowBody() => controller.CurrentFlowBody();
        public string CurrentSpeakerCharacterId() => controller.CurrentSpeakerCharacterId();
        public bool CanExecuteNode(SandPlanetFlowNode04 node, out string reason, out string cost) => controller.CanExecuteNode(node, out reason, out cost);
        public bool IsForcedEventChoice() => controller.IsForcedEventChoice();

        public void OpenLocation(string id) => controller.OpenLocation(id);
        public void CloseLocation() => controller.CloseLocation();
        public void SelectTarget(string type, string id) => controller.SelectTarget(type, id);
        public void DismissTargetSelection() => controller.DismissTargetSelection();
        public void BeginInteraction(SandPlanetInteraction04 interaction) => controller.BeginInteraction(interaction);
        public void ExecuteNode(SandPlanetFlowNode04 node) => controller.ExecuteNode(node);
        public void CancelFlow() => controller.CancelActiveFlow();
        public void BackFromNarrative() => controller.HandleBackFromNavigation();
        public void CloseSimpleModal() => controller.CloseSimpleModal();
        public void ApplyStartingStats(int personal, int social, int technical) => controller.ApplyStartingStats(personal, social, technical);

        public string TargetName(string type, string id)
        {
            if (type == "CHARACTER" && Content.Characters.TryGetValue(id, out SandPlanetCharacter04 character)) return character.Name;
            if (type == "WORLD_TARGET" && Content.WorldTargets.TryGetValue(id, out SandPlanetWorldTarget04 target)) return target.Name;
            return id ?? string.Empty;
        }

        public static string QuestColorHex(string type)
        {
            if (string.Equals(type, "MAIN", StringComparison.OrdinalIgnoreCase)) return "#F0A24A";
            if (string.Equals(type, "CHARACTER", StringComparison.OrdinalIgnoreCase)) return "#69C77C";
            return "#F1D784";
        }

        public static string NormalizeQuestRole(SandPlanetInteraction04 interaction)
        {
            return Prototype04InteractionService.NormalizeQuestRole(interaction);
        }
    }
}
