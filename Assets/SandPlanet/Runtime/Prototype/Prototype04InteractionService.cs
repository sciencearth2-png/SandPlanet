using System;
using System.Collections.Generic;
using System.Linq;
using SandPlanet.Prototype.DataDriven;

namespace SandPlanet.Prototype
{
    internal sealed class Prototype04InteractionService
    {
        private readonly SandPlanetContent04 content;
        private readonly Prototype04GameState state;
        private readonly Prototype04QuestService quests;
        private readonly Prototype04ConditionEvaluator conditions;

        public Prototype04InteractionService(
            SandPlanetContent04 runtimeContent,
            Prototype04GameState gameState,
            Prototype04QuestService questService,
            Prototype04ConditionEvaluator conditionEvaluator)
        {
            content = runtimeContent;
            state = gameState;
            quests = questService;
            conditions = conditionEvaluator;
        }

        public List<SandPlanetInteraction04> GetAvailable(string targetType, string targetId)
        {
            return content.Interactions
                .Where(interaction => interaction.Active && interaction.TargetType == targetType && interaction.TargetId == targetId && IsAvailable(interaction))
                .ToList();
        }

        public bool IsAvailable(SandPlanetInteraction04 interaction)
        {
            if (interaction == null || !interaction.Active) return false;
            if (state.Day < interaction.OpenDay || state.Day > interaction.CloseDay || !TimeSlotAllowed(interaction)) return false;
            if (!state.IsInteractionRepeatAvailable(interaction.Id, interaction.RepeatRule)) return false;

            string role = NormalizeQuestRole(interaction);
            if (role == "OFFER")
            {
                if (string.IsNullOrEmpty(interaction.QuestId) || quests.GetStatus(interaction.QuestId) != "LOCKED") return false;
            }
            else if (role == "PROGRESS")
            {
                if (string.IsNullOrEmpty(interaction.QuestId) || quests.GetStatus(interaction.QuestId) != "ACTIVE") return false;
                if (!string.IsNullOrEmpty(interaction.QuestStepId) && quests.GetStep(interaction.QuestId) != interaction.QuestStepId) return false;
            }
            else if (!string.IsNullOrEmpty(interaction.QuestId))
            {
                if (quests.GetStatus(interaction.QuestId) != "ACTIVE") return false;
                if (!string.IsNullOrEmpty(interaction.QuestStepId) && quests.GetStep(interaction.QuestId) != interaction.QuestStepId) return false;
            }

            return conditions.EvaluatePair(interaction.ConditionLogic, interaction.Condition1, interaction.Condition2);
        }

        public static string NormalizeQuestRole(SandPlanetInteraction04 interaction)
        {
            if (interaction == null) return "NONE";
            if (!string.IsNullOrEmpty(interaction.QuestRole)) return interaction.QuestRole.ToUpperInvariant();
            return string.IsNullOrEmpty(interaction.QuestId) ? "NONE" : "PROGRESS";
        }

        private bool TimeSlotAllowed(SandPlanetInteraction04 interaction)
        {
            if (state.Hour < 12) return interaction.Morning;
            if (state.Hour < 17) return interaction.Afternoon;
            return interaction.Evening;
        }
    }
}
