using System;
using System.Globalization;
using SandPlanet.Prototype.DataDriven;

namespace SandPlanet.Prototype
{
    internal sealed class Prototype04ConditionEvaluator
    {
        private readonly Prototype04GameState state;
        private readonly Prototype04QuestService quests;

        public Prototype04ConditionEvaluator(Prototype04GameState gameState, Prototype04QuestService questService)
        {
            state = gameState;
            quests = questService;
        }

        public bool EvaluatePair(string logic, SandPlanetCondition04 first, SandPlanetCondition04 second)
        {
            bool hasFirst = first != null && !string.IsNullOrEmpty(first.Type);
            bool hasSecond = second != null && !string.IsNullOrEmpty(second.Type);
            if (!hasFirst && !hasSecond) return true;
            bool firstValue = !hasFirst || Evaluate(first);
            bool secondValue = !hasSecond || Evaluate(second);
            return string.Equals(logic, "OR", StringComparison.OrdinalIgnoreCase)
                ? (hasFirst && firstValue) || (hasSecond && secondValue)
                : firstValue && secondValue;
        }

        private bool Evaluate(SandPlanetCondition04 condition)
        {
            if (condition == null || string.IsNullOrEmpty(condition.Type)) return true;
            string left;
            switch (condition.Type)
            {
                case "STATE": left = state.GetState(condition.Key); break;
                case "DAY": left = state.Day.ToString(CultureInfo.InvariantCulture); break;
                case "TIME": left = state.Hour.ToString(CultureInfo.InvariantCulture); break;
                case "AFFINITY": left = state.GetAffinity(condition.Key).ToString(CultureInfo.InvariantCulture); break;
                case "QUEST_STATUS": left = quests.GetStatus(condition.Key); break;
                case "QUEST_STEP": left = quests.GetStep(condition.Key); break;
                case "EVENT_OCCURRED": left = state.HasEventOccurred(condition.Key) ? "TRUE" : "FALSE"; break;
                case "INTERACTION_DONE": left = state.HasInteractionOccurred(condition.Key) ? "TRUE" : "FALSE"; break;
                case "STAT_LEVEL": left = state.GetStatLevel(condition.Key).ToString(CultureInfo.InvariantCulture); break;
                default: return false;
            }
            return Compare(left, condition.Operator, condition.Value);
        }

        private static bool Compare(string left, string operation, string right)
        {
            if (int.TryParse(left, out int leftNumber) && int.TryParse(right, out int rightNumber))
            {
                switch (operation)
                {
                    case "NE": return leftNumber != rightNumber;
                    case "GT": return leftNumber > rightNumber;
                    case "GE": return leftNumber >= rightNumber;
                    case "LT": return leftNumber < rightNumber;
                    case "LE": return leftNumber <= rightNumber;
                    default: return leftNumber == rightNumber;
                }
            }
            return operation == "NE"
                ? !string.Equals(left, right, StringComparison.OrdinalIgnoreCase)
                : string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
        }
    }
}
