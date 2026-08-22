using SandPlanet.Prototype.DataDriven;

namespace SandPlanet.Prototype
{
    internal sealed class Prototype04ScheduleService
    {
        private readonly SandPlanetContent04 content;
        private readonly Prototype04GameState state;
        private readonly Prototype04ConditionEvaluator conditions;

        public Prototype04ScheduleService(SandPlanetContent04 runtimeContent, Prototype04GameState gameState, Prototype04ConditionEvaluator conditionEvaluator)
        {
            content = runtimeContent;
            state = gameState;
            conditions = conditionEvaluator;
        }

        public string GetCharacterLocation(string characterId)
        {
            if (!content.Characters.TryGetValue(characterId, out SandPlanetCharacter04 character)) return string.Empty;
            string bestLocation = character.DefaultLocationId;
            int bestPriority = int.MinValue;
            foreach (SandPlanetSchedule04 schedule in content.Schedules)
            {
                if (!schedule.Active || schedule.CharacterId != characterId || state.Day < schedule.OpenDay || state.Day > schedule.CloseDay || !TimeSlotAllowed(schedule)) continue;
                if (!conditions.EvaluatePair(schedule.ConditionLogic, schedule.Condition1, schedule.Condition2)) continue;
                if (schedule.Priority >= bestPriority)
                {
                    bestPriority = schedule.Priority;
                    bestLocation = schedule.LocationId;
                }
            }
            return bestLocation;
        }

        private bool TimeSlotAllowed(SandPlanetSchedule04 schedule)
        {
            if (state.Hour < 12) return schedule.Morning;
            if (state.Hour < 17) return schedule.Afternoon;
            return schedule.Evening;
        }
    }
}
