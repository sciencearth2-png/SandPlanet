using System;
using System.Collections.Generic;
using SandPlanet.Prototype.DataDriven;

namespace SandPlanet.Prototype
{
    internal sealed class Prototype04GameState
    {
        public const int DayStartHour = 8;
        public const int DayEndHour = 22;
        public const int DefaultMaxWill = 5;
        private const int XpPerLevel = 6;

        private readonly Dictionary<string, string> states = new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly Dictionary<string, int> affinity = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly Dictionary<string, string> questStatuses = new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly Dictionary<string, string> questSteps = new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly HashSet<string> eventsOccurred = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> interactionsUsed = new HashSet<string>(StringComparer.Ordinal);
        private readonly Dictionary<string, int> interactionLastDay = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly HashSet<string> triggersUsed = new HashSet<string>(StringComparer.Ordinal);
        private readonly Dictionary<string, int> triggerLastDay = new Dictionary<string, int>(StringComparer.Ordinal);

        public int Day { get; private set; } = 1;
        public int Hour { get; private set; } = DayStartHour;
        public int Will { get; private set; } = DefaultMaxWill;
        public int MaxWill { get; private set; } = DefaultMaxWill;
        public int PersonalLevel { get; private set; } = 2;
        public int PersonalXp { get; private set; }
        public int SocialLevel { get; private set; } = 2;
        public int SocialXp { get; private set; }
        public int TechnicalLevel { get; private set; } = 2;
        public int TechnicalXp { get; private set; }

        public IReadOnlyDictionary<string, string> States => states;
        public IReadOnlyDictionary<string, int> Affinity => affinity;
        public IReadOnlyDictionary<string, string> QuestStatuses => questStatuses;
        public IReadOnlyDictionary<string, string> QuestSteps => questSteps;
        public IReadOnlyCollection<string> EventsOccurred => eventsOccurred;

        public void Initialize(SandPlanetContent04 content)
        {
            states.Clear();
            affinity.Clear();
            questStatuses.Clear();
            questSteps.Clear();
            eventsOccurred.Clear();
            interactionsUsed.Clear();
            interactionLastDay.Clear();
            triggersUsed.Clear();
            triggerLastDay.Clear();
            Day = 1;
            Hour = DayStartHour;
            Will = DefaultMaxWill;
            MaxWill = DefaultMaxWill;
            PersonalLevel = SocialLevel = TechnicalLevel = 2;
            PersonalXp = SocialXp = TechnicalXp = 0;

            foreach (SandPlanetStateDefinition04 definition in content.States.Values)
                states[definition.Id] = NormalizeState(definition.DefaultValue, definition.DataType);
            foreach (SandPlanetCharacter04 character in content.Characters.Values)
                affinity[character.Id] = 0;
            foreach (SandPlanetQuest04 quest in content.Quests.Values)
            {
                questStatuses[quest.Id] = "LOCKED";
                questSteps[quest.Id] = string.Empty;
            }
        }

        public void ApplyStartingStats(int personal, int social, int technical)
        {
            PersonalLevel = personal;
            SocialLevel = social;
            TechnicalLevel = technical;
            PersonalXp = SocialXp = TechnicalXp = 0;
            SetStateValue("STA_STANCE_BENJAMIN", "STAY");
            SetStateValue("STA_STANCE_FAYE", "UNDECIDED");
        }

        public void ApplyClockAndWill(int timeDelta, int willDelta)
        {
            Hour = Clamp(Hour + timeDelta, DayStartHour, DayEndHour);
            Will = Clamp(Will + willDelta, 0, MaxWill);
        }

        public void AdvanceDay()
        {
            Day++;
            Hour = DayStartHour;
            Will = Math.Min(MaxWill, Will + 2);
        }

        public void AddXp(string stat, int amount)
        {
            if (amount == 0) return;
            if (stat == "PERSONAL")
            {
                int level = PersonalLevel;
                int xp = PersonalXp;
                AddXpTo(ref level, ref xp, amount);
                PersonalLevel = level;
                PersonalXp = xp;
            }
            else if (stat == "SOCIAL" || stat == "INTERPERSONAL")
            {
                int level = SocialLevel;
                int xp = SocialXp;
                AddXpTo(ref level, ref xp, amount);
                SocialLevel = level;
                SocialXp = xp;
            }
            else if (stat == "TECHNICAL")
            {
                int level = TechnicalLevel;
                int xp = TechnicalXp;
                AddXpTo(ref level, ref xp, amount);
                TechnicalLevel = level;
                TechnicalXp = xp;
            }
        }

        public int GetStatLevel(string stat)
        {
            if (stat == "PERSONAL") return PersonalLevel;
            if (stat == "SOCIAL" || stat == "INTERPERSONAL") return SocialLevel;
            if (stat == "TECHNICAL") return TechnicalLevel;
            return 0;
        }

        public string GetState(string id) => !string.IsNullOrEmpty(id) && states.TryGetValue(id, out string value) ? value : string.Empty;
        public int GetAffinity(string id) => !string.IsNullOrEmpty(id) && affinity.TryGetValue(id, out int value) ? value : 0;
        public string GetQuestStatus(string id) => !string.IsNullOrEmpty(id) && questStatuses.TryGetValue(id, out string value) ? value : "LOCKED";
        public string GetQuestStep(string id) => !string.IsNullOrEmpty(id) && questSteps.TryGetValue(id, out string value) ? value : string.Empty;
        public bool HasEventOccurred(string id) => !string.IsNullOrEmpty(id) && eventsOccurred.Contains(id);
        public bool HasInteractionOccurred(string id) => !string.IsNullOrEmpty(id) && interactionsUsed.Contains(id);

        public void SetStateValue(string id, string value)
        {
            if (!string.IsNullOrEmpty(id)) states[id] = value ?? string.Empty;
        }

        public void SetAffinity(string id, int value)
        {
            if (!string.IsNullOrEmpty(id)) affinity[id] = Clamp(value, 0, 5);
        }

        public void SetQuestStatus(string id, string status)
        {
            if (!string.IsNullOrEmpty(id) && questStatuses.ContainsKey(id)) questStatuses[id] = status;
        }

        public void SetQuestStep(string id, string stepId)
        {
            if (!string.IsNullOrEmpty(id) && questSteps.ContainsKey(id)) questSteps[id] = stepId ?? string.Empty;
        }

        public void MarkEventOccurred(string id)
        {
            if (!string.IsNullOrEmpty(id)) eventsOccurred.Add(id);
        }

        public bool IsInteractionRepeatAvailable(string id, string rule) => RepeatAvailable(id, rule, interactionsUsed, interactionLastDay);
        public bool IsTriggerRepeatAvailable(string id, string rule) => RepeatAvailable(id, rule, triggersUsed, triggerLastDay);

        public void MarkInteractionUsed(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            interactionsUsed.Add(id);
            interactionLastDay[id] = Day;
        }

        public void MarkTriggerUsed(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            triggersUsed.Add(id);
            triggerLastDay[id] = Day;
        }

        private bool RepeatAvailable(string id, string rule, HashSet<string> used, Dictionary<string, int> lastDay)
        {
            string normalized = string.IsNullOrEmpty(rule) ? "ONCE" : rule.ToUpperInvariant();
            if (normalized == "UNLIMITED") return true;
            if (normalized == "DAILY") return !lastDay.TryGetValue(id, out int usedDay) || usedDay != Day;
            return !used.Contains(id);
        }

        private static void AddXpTo(ref int level, ref int xp, int amount)
        {
            if (amount < 0)
            {
                xp = Math.Max(0, xp + amount);
                return;
            }
            xp += amount;
            while (xp >= XpPerLevel && level < 20)
            {
                xp -= XpPerLevel;
                level++;
            }
            if (level >= 20) xp = Math.Min(xp, XpPerLevel - 1);
        }

        private static string NormalizeState(string value, string type) => type == "BOOL"
            ? (string.Equals(value, "TRUE", StringComparison.OrdinalIgnoreCase) ? "TRUE" : "FALSE")
            : value ?? string.Empty;

        private static int Clamp(int value, int min, int max) => Math.Min(max, Math.Max(min, value));
    }
}
