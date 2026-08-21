using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SandPlanet.Prototype.DataDriven
{
    [Serializable]
    public sealed class SandPlanetCondition04
    {
        public string Type;
        public string Key;
        public string Operator;
        public string Value;
    }

    [Serializable]
    public sealed class SandPlanetResult04
    {
        public string Type;
        public string TargetType;
        public string TargetId;
        public string Value;
    }

    public sealed class SandPlanetLocation04
    {
        public string Id;
        public string Name;
        public string SceneKey;
        public bool Active;
    }

    public sealed class SandPlanetCharacter04
    {
        public string Id;
        public string Name;
        public string PortraitKey;
        public string DefaultLocationId;
        public string RoleCode;
        public bool Active;
    }

    public sealed class SandPlanetWorldTarget04
    {
        public string Id;
        public string Name;
        public string LocationId;
        public string Category;
        public string SceneObjectKey;
        public bool Clickable;
        public bool Active;
    }

    public sealed class SandPlanetQuest04
    {
        public string Id;
        public string Type;
        public string Title;
        public string Summary;
        public string InitialStepId;
        public bool TrackerVisible;
        public int LogOrder;
        public bool Active;
    }

    public sealed class SandPlanetQuestStep04
    {
        public string Id;
        public string QuestId;
        public int StepOrder;
        public string Title;
        public string Body;
        public string TrackerText;
        public string ProgressEventId;
        public string OnProgressEvent;
        public string NextStepId;
        public bool Active;
    }

    public sealed class SandPlanetInteraction04
    {
        public string Id;
        public string VariantGroupId;
        public string TargetType;
        public string TargetId;
        public string InteractionType;
        public string QuestId;
        public string QuestStepId;
        public string EntryMode;
        public string DisplayText;
        public string ChoiceSetId;
        public int OpenDay;
        public int CloseDay;
        public bool Morning;
        public bool Afternoon;
        public bool Evening;
        public string RepeatRule;
        public string ConditionLogic;
        public SandPlanetCondition04 Condition1;
        public SandPlanetCondition04 Condition2;
        public int Priority;
        public bool Active;
    }

    public sealed class SandPlanetChoice04
    {
        public string ChoiceSetId;
        public string Id;
        public int Order;
        public string Text;
        public string ConfirmText;
        public string ResultText;
        public int TimeCost;
        public int WillCost;
        public string SoftStat;
        public int SoftRequirement;
        public string HardConditionLogic;
        public SandPlanetCondition04 HardCondition1;
        public SandPlanetCondition04 HardCondition2;
        public SandPlanetResult04 Result1;
        public SandPlanetResult04 Result2;
        public SandPlanetResult04 Result3;
        public bool Active;
    }

    public sealed class SandPlanetChoiceBeat04
    {
        public string Id;
        public string ChoiceId;
        public int Order;
        public string PresentationType;
        public string SpeakerType;
        public string SpeakerId;
        public string SpeakerNameOverride;
        public string BodyText;
        public string AdvanceText;
        public string ResultHint;
        public bool Active;
    }

    public sealed class SandPlanetStateDefinition04
    {
        public string Id;
        public string DataType;
        public string DefaultValue;
        public string Scope;
        public string Description;
    }

    public sealed class SandPlanetEvent04
    {
        public string Id;
        public string Name;
        public string ExperiencePurpose;
        public string PlayerPerceivedChange;
        public string PresentationMode;
        public string Title;
        public string Body;
        public string QuestId;
        public string QuestStepId;
        public string ChoiceSetId;
        public SandPlanetResult04 Result1;
        public SandPlanetResult04 Result2;
        public SandPlanetResult04 Result3;
        public bool Active;
    }

    public sealed class SandPlanetTrigger04
    {
        public string Id;
        public string EventId;
        public string TriggerTiming;
        public string TriggerMomentText;
        public string LocationId;
        public string DetectorType;
        public string DetectorId;
        public string ConditionLogic;
        public SandPlanetCondition04 Condition1;
        public SandPlanetCondition04 Condition2;
        public string RepeatRule;
        public int Priority;
        public bool Active;
    }

    public sealed class SandPlanetSchedule04
    {
        public string Id;
        public string CharacterId;
        public int OpenDay;
        public int CloseDay;
        public bool Morning;
        public bool Afternoon;
        public bool Evening;
        public string LocationId;
        public string ConditionLogic;
        public SandPlanetCondition04 Condition1;
        public SandPlanetCondition04 Condition2;
        public int Priority;
        public bool Active;
    }

    /// <summary>
    /// Runtime content database for Prototype 0.4. The authoritative source is SandPlanet_Master.xlsx;
    /// these objects are reconstructed from generated CSV TextAssets every play session.
    /// </summary>
    public sealed class SandPlanetContent04
    {
        public readonly Dictionary<string, SandPlanetLocation04> Locations = new Dictionary<string, SandPlanetLocation04>(StringComparer.Ordinal);
        public readonly Dictionary<string, SandPlanetCharacter04> Characters = new Dictionary<string, SandPlanetCharacter04>(StringComparer.Ordinal);
        public readonly Dictionary<string, SandPlanetWorldTarget04> WorldTargets = new Dictionary<string, SandPlanetWorldTarget04>(StringComparer.Ordinal);
        public readonly Dictionary<string, SandPlanetQuest04> Quests = new Dictionary<string, SandPlanetQuest04>(StringComparer.Ordinal);
        public readonly Dictionary<string, SandPlanetQuestStep04> QuestSteps = new Dictionary<string, SandPlanetQuestStep04>(StringComparer.Ordinal);
        public readonly Dictionary<string, SandPlanetStateDefinition04> States = new Dictionary<string, SandPlanetStateDefinition04>(StringComparer.Ordinal);
        public readonly Dictionary<string, SandPlanetEvent04> Events = new Dictionary<string, SandPlanetEvent04>(StringComparer.Ordinal);
        public readonly List<SandPlanetInteraction04> Interactions = new List<SandPlanetInteraction04>();
        public readonly List<SandPlanetChoice04> Choices = new List<SandPlanetChoice04>();
        public readonly List<SandPlanetChoiceBeat04> ChoiceBeats = new List<SandPlanetChoiceBeat04>();
        public readonly List<SandPlanetTrigger04> Triggers = new List<SandPlanetTrigger04>();
        public readonly List<SandPlanetSchedule04> Schedules = new List<SandPlanetSchedule04>();

        private readonly Dictionary<string, List<SandPlanetChoice04>> choicesBySet = new Dictionary<string, List<SandPlanetChoice04>>(StringComparer.Ordinal);
        private readonly Dictionary<string, List<SandPlanetChoiceBeat04>> beatsByChoice = new Dictionary<string, List<SandPlanetChoiceBeat04>>(StringComparer.Ordinal);

        public static SandPlanetContent04 Load(IEnumerable<TextAsset> csvAssets)
        {
            SandPlanetContent04 db = new SandPlanetContent04();
            Dictionary<string, TextAsset> byName = new Dictionary<string, TextAsset>(StringComparer.OrdinalIgnoreCase);
            foreach (TextAsset asset in csvAssets ?? Array.Empty<TextAsset>())
            {
                if (asset != null)
                    byName[asset.name] = asset;
            }

            db.LoadLocations(GetRows(byName, "Locations"));
            db.LoadCharacters(GetRows(byName, "Characters"));
            db.LoadWorldTargets(GetRows(byName, "WorldTargets"));
            db.LoadQuests(GetRows(byName, "Quests"));
            db.LoadQuestSteps(GetRows(byName, "QuestSteps"));
            db.LoadInteractions(GetRows(byName, "Interactions"));
            db.LoadChoices(GetRows(byName, "Choices"));
            db.LoadChoiceBeats(GetRows(byName, "ChoiceBeats"));
            db.LoadStates(GetRows(byName, "States"));
            db.LoadEvents(GetRows(byName, "Events"));
            db.LoadTriggers(GetRows(byName, "EventTriggers"));
            db.LoadSchedules(GetRows(byName, "NpcSchedules"));

            foreach (IGrouping<string, SandPlanetChoice04> group in db.Choices.Where(c => c.Active).GroupBy(c => c.ChoiceSetId))
                db.choicesBySet[group.Key] = group.OrderBy(c => c.Order).ToList();
            foreach (IGrouping<string, SandPlanetChoiceBeat04> group in db.ChoiceBeats.Where(b => b.Active).GroupBy(b => b.ChoiceId))
                db.beatsByChoice[group.Key] = group.OrderBy(b => b.Order).ThenBy(b => b.Id).ToList();

            return db;
        }

        public IReadOnlyList<SandPlanetChoice04> GetChoices(string choiceSetId)
        {
            if (!string.IsNullOrEmpty(choiceSetId) && choicesBySet.TryGetValue(choiceSetId, out List<SandPlanetChoice04> list))
                return list;
            return Array.Empty<SandPlanetChoice04>();
        }

        public IReadOnlyList<SandPlanetChoiceBeat04> GetChoiceBeats(string choiceId)
        {
            if (!string.IsNullOrEmpty(choiceId) && beatsByChoice.TryGetValue(choiceId, out List<SandPlanetChoiceBeat04> list))
                return list;
            return Array.Empty<SandPlanetChoiceBeat04>();
        }

        private static List<Dictionary<string, string>> GetRows(Dictionary<string, TextAsset> byName, string name)
        {
            if (!byName.TryGetValue(name, out TextAsset asset) || asset == null)
            {
                Debug.LogError("[SandPlanet 0.4] Missing generated CSV TextAsset: " + name + ".csv");
                return new List<Dictionary<string, string>>();
            }
            return SandPlanetCsv04.Parse(asset.text);
        }

        private void LoadLocations(List<Dictionary<string, string>> rows)
        {
            foreach (Dictionary<string, string> r in rows)
            {
                SandPlanetLocation04 d = new SandPlanetLocation04
                {
                    Id = G(r, "LocationID"), Name = G(r, "Name"), SceneKey = G(r, "SceneKey"), Active = B(r, "Active", true)
                };
                if (!string.IsNullOrEmpty(d.Id)) Locations[d.Id] = d;
            }
        }

        private void LoadCharacters(List<Dictionary<string, string>> rows)
        {
            foreach (Dictionary<string, string> r in rows)
            {
                SandPlanetCharacter04 d = new SandPlanetCharacter04
                {
                    Id = G(r, "CharacterID"), Name = G(r, "Name"), PortraitKey = G(r, "PortraitKey"),
                    DefaultLocationId = G(r, "DefaultLocationID"), RoleCode = G(r, "RoleCode"), Active = B(r, "Active", true)
                };
                if (!string.IsNullOrEmpty(d.Id)) Characters[d.Id] = d;
            }
        }

        private void LoadWorldTargets(List<Dictionary<string, string>> rows)
        {
            foreach (Dictionary<string, string> r in rows)
            {
                SandPlanetWorldTarget04 d = new SandPlanetWorldTarget04
                {
                    Id = G(r, "WorldTargetID"), Name = G(r, "Name"), LocationId = G(r, "LocationID"), Category = G(r, "TargetCategory"),
                    SceneObjectKey = G(r, "SceneObjectKey"), Clickable = B(r, "Clickable", true), Active = B(r, "Active", true)
                };
                if (!string.IsNullOrEmpty(d.Id)) WorldTargets[d.Id] = d;
            }
        }

        private void LoadQuests(List<Dictionary<string, string>> rows)
        {
            foreach (Dictionary<string, string> r in rows)
            {
                SandPlanetQuest04 d = new SandPlanetQuest04
                {
                    Id = G(r, "QuestID"), Type = G(r, "QuestType"), Title = G(r, "Title"), Summary = G(r, "Summary"),
                    InitialStepId = G(r, "InitialStepID"), TrackerVisible = B(r, "TrackerVisible", true), LogOrder = I(r, "LogOrder"), Active = B(r, "Active", true)
                };
                if (!string.IsNullOrEmpty(d.Id)) Quests[d.Id] = d;
            }
        }

        private void LoadQuestSteps(List<Dictionary<string, string>> rows)
        {
            foreach (Dictionary<string, string> r in rows)
            {
                SandPlanetQuestStep04 d = new SandPlanetQuestStep04
                {
                    Id = G(r, "QuestStepID"), QuestId = G(r, "QuestID"), StepOrder = I(r, "StepOrder"), Title = G(r, "StepTitle"),
                    Body = G(r, "StepBody"), TrackerText = G(r, "TrackerText"), ProgressEventId = G(r, "ProgressEventID"),
                    OnProgressEvent = G(r, "OnProgressEvent"), NextStepId = G(r, "NextStepID"), Active = B(r, "Active", true)
                };
                if (!string.IsNullOrEmpty(d.Id)) QuestSteps[d.Id] = d;
            }
        }

        private void LoadInteractions(List<Dictionary<string, string>> rows)
        {
            foreach (Dictionary<string, string> r in rows)
            {
                Interactions.Add(new SandPlanetInteraction04
                {
                    Id = G(r, "InteractionID"), VariantGroupId = G(r, "VariantGroupID"), TargetType = G(r, "TargetType"), TargetId = G(r, "TargetID"),
                    InteractionType = G(r, "InteractionType"), QuestId = G(r, "QuestID"), QuestStepId = G(r, "QuestStepID"), EntryMode = G(r, "EntryMode"),
                    DisplayText = G(r, "DisplayText"), ChoiceSetId = G(r, "ChoiceSetID"), OpenDay = I(r, "OpenDay", 1), CloseDay = I(r, "CloseDay", 21),
                    Morning = B(r, "Morning", true), Afternoon = B(r, "Afternoon", true), Evening = B(r, "Evening", true), RepeatRule = G(r, "RepeatRule", "ONCE"),
                    ConditionLogic = G(r, "ExtraConditionLogic", "AND"), Condition1 = C(r, "ExtraCond1"), Condition2 = C(r, "ExtraCond2"),
                    Priority = I(r, "Priority"), Active = B(r, "Active", true)
                });
            }
        }

        private void LoadChoices(List<Dictionary<string, string>> rows)
        {
            foreach (Dictionary<string, string> r in rows)
            {
                Choices.Add(new SandPlanetChoice04
                {
                    ChoiceSetId = G(r, "ChoiceSetID"), Id = G(r, "ChoiceID"), Order = I(r, "Order"), Text = G(r, "ChoiceText"),
                    ConfirmText = G(r, "ConfirmText"), ResultText = G(r, "ResultText"), TimeCost = I(r, "TimeCost"), WillCost = I(r, "WillCost"),
                    SoftStat = G(r, "SoftStat", "NONE"), SoftRequirement = I(r, "SoftRequirement"), HardConditionLogic = G(r, "HardConditionLogic", "AND"),
                    HardCondition1 = C(r, "HardCond1"), HardCondition2 = C(r, "HardCond2"),
                    Result1 = R(r, "Result1"), Result2 = R(r, "Result2"), Result3 = R(r, "Result3"), Active = B(r, "Active", true)
                });
            }
        }

        private void LoadChoiceBeats(List<Dictionary<string, string>> rows)
        {
            foreach (Dictionary<string, string> r in rows)
            {
                ChoiceBeats.Add(new SandPlanetChoiceBeat04
                {
                    Id = G(r, "BeatID"), ChoiceId = G(r, "ChoiceID"), Order = I(r, "BeatOrder"),
                    PresentationType = G(r, "PresentationType", "DIALOGUE"), SpeakerType = G(r, "SpeakerType", "NONE"),
                    SpeakerId = G(r, "SpeakerID"), SpeakerNameOverride = G(r, "SpeakerNameOverride"), BodyText = G(r, "BodyText"),
                    AdvanceText = G(r, "AdvanceText"), ResultHint = G(r, "ResultHint"), Active = B(r, "Active", true)
                });
            }
        }

        private void LoadStates(List<Dictionary<string, string>> rows)
        {
            foreach (Dictionary<string, string> r in rows)
            {
                SandPlanetStateDefinition04 d = new SandPlanetStateDefinition04
                {
                    Id = G(r, "StateID"), DataType = G(r, "DataType"), DefaultValue = G(r, "DefaultValue"), Scope = G(r, "Scope"), Description = G(r, "Description")
                };
                if (!string.IsNullOrEmpty(d.Id)) States[d.Id] = d;
            }
        }

        private void LoadEvents(List<Dictionary<string, string>> rows)
        {
            foreach (Dictionary<string, string> r in rows)
            {
                SandPlanetEvent04 d = new SandPlanetEvent04
                {
                    Id = G(r, "EventID"), Name = G(r, "EventName"), ExperiencePurpose = G(r, "ExperiencePurpose"), PlayerPerceivedChange = G(r, "PlayerPerceivedChange"),
                    PresentationMode = G(r, "PresentationMode", "SILENT"), Title = G(r, "Title"), Body = G(r, "Body"), QuestId = G(r, "QuestID"),
                    QuestStepId = G(r, "QuestStepID"), ChoiceSetId = G(r, "ChoiceSetID"), Result1 = R(r, "EventResult1"), Result2 = R(r, "EventResult2"),
                    Result3 = R(r, "EventResult3"), Active = B(r, "Active", true)
                };
                if (!string.IsNullOrEmpty(d.Id)) Events[d.Id] = d;
            }
        }

        private void LoadTriggers(List<Dictionary<string, string>> rows)
        {
            foreach (Dictionary<string, string> r in rows)
            {
                Triggers.Add(new SandPlanetTrigger04
                {
                    Id = G(r, "TriggerID"), EventId = G(r, "EventID"), TriggerTiming = G(r, "TriggerTiming"), TriggerMomentText = G(r, "TriggerMomentText"),
                    LocationId = G(r, "LocationID"), DetectorType = G(r, "DetectorType"), DetectorId = G(r, "DetectorID"), ConditionLogic = G(r, "ConditionLogic", "AND"),
                    Condition1 = C(r, "Cond1"), Condition2 = C(r, "Cond2"), RepeatRule = G(r, "RepeatRule", "ONCE"), Priority = I(r, "Priority"), Active = B(r, "Active", true)
                });
            }
        }

        private void LoadSchedules(List<Dictionary<string, string>> rows)
        {
            foreach (Dictionary<string, string> r in rows)
            {
                Schedules.Add(new SandPlanetSchedule04
                {
                    Id = G(r, "ScheduleID"), CharacterId = G(r, "CharacterID"), OpenDay = I(r, "OpenDay", 1), CloseDay = I(r, "CloseDay", 21),
                    Morning = B(r, "Morning", true), Afternoon = B(r, "Afternoon", true), Evening = B(r, "Evening", true), LocationId = G(r, "LocationID"),
                    ConditionLogic = G(r, "ScheduleConditionLogic", "AND"), Condition1 = C(r, "ScheduleCond1"), Condition2 = C(r, "ScheduleCond2"),
                    Priority = I(r, "Priority"), Active = B(r, "Active", true)
                });
            }
        }

        private static SandPlanetCondition04 C(Dictionary<string, string> r, string prefix)
        {
            return new SandPlanetCondition04
            {
                Type = G(r, prefix + "Type"), Key = G(r, prefix + "Key"), Operator = G(r, prefix + "Operator"), Value = G(r, prefix + "Value")
            };
        }

        private static SandPlanetResult04 R(Dictionary<string, string> r, string prefix)
        {
            return new SandPlanetResult04
            {
                Type = G(r, prefix + "Type"), TargetType = G(r, prefix + "TargetType"), TargetId = G(r, prefix + "TargetID"), Value = G(r, prefix + "Value")
            };
        }

        private static string G(Dictionary<string, string> r, string key, string fallback = "") => SandPlanetCsv04.Get(r, key, fallback);
        private static int I(Dictionary<string, string> r, string key, int fallback = 0) => SandPlanetCsv04.GetInt(r, key, fallback);
        private static bool B(Dictionary<string, string> r, string key, bool fallback = false) => SandPlanetCsv04.GetBool(r, key, fallback);
    }
}
