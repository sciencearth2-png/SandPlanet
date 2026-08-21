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

    public sealed class SandPlanetStateChange04
    {
        public string StateId;
        public string Operation;
        public string Value;
    }

    public sealed class SandPlanetAffinityChange04
    {
        public string CharacterId;
        public int Delta;
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

    public sealed class SandPlanetFlowNode04
    {
        public string NodeId;
        public string ChoiceId;
        public string PresentationType;
        public string Speaker;
        public string BodyText;
        public string ChoiceText;
        public string NextNodeId;

        public int TimeCost;
        public int WillDelta;
        public int PersonalXpDelta;
        public int SocialXpDelta;
        public int TechnicalXpDelta;
        public readonly List<SandPlanetAffinityChange04> AffinityChanges = new List<SandPlanetAffinityChange04>();
        public readonly List<SandPlanetStateChange04> StateChanges = new List<SandPlanetStateChange04>();
        public string EmitEventId;
        public string ResultTextOverride;

        public string SoftStat;
        public int SoftRequirement;
        public string HardConditionLogic;
        public SandPlanetCondition04 HardCondition1;
        public SandPlanetCondition04 HardCondition2;
        public bool Active;
    }

    public sealed class SandPlanetQuestActionMeta04
    {
        public string QuestAction;
        public string QuestId;
        public string QuestStepId;
    }

    public sealed class SandPlanetInteractionFlow04
    {
        public string Id;
        public string TargetType;
        public string TargetId;
        public string EntryText;
        public string QuestId;
        public string QuestStepId;
        public string QuestRole;
        public string EntryMode;
        public int OpenDay;
        public int CloseDay;
        public string AllowedTimeSlots;
        public string RepeatRule;
        public int Priority;
        public string ConditionLogic;
        public SandPlanetCondition04 Condition1;
        public SandPlanetCondition04 Condition2;
        public bool Active;
        public string WriterNote;
        public readonly List<SandPlanetFlowNode04> Nodes = new List<SandPlanetFlowNode04>();

        public IEnumerable<SandPlanetFlowNode04> GetNodeRows(string nodeId)
        {
            return Nodes.Where(n => n.Active && string.Equals(n.NodeId, nodeId, StringComparison.Ordinal));
        }

        public string StartNodeId
        {
            get
            {
                SandPlanetFlowNode04 n01 = Nodes.FirstOrDefault(n => n.Active && n.NodeId == "N01");
                return n01 != null ? n01.NodeId : Nodes.FirstOrDefault(n => n.Active)?.NodeId ?? string.Empty;
            }
        }
    }

    public sealed class SandPlanetInteraction04
    {
        public string Id;
        public string TargetType;
        public string TargetId;
        public string InteractionType;
        public string QuestId;
        public string QuestStepId;
        public string QuestRole;
        public string EntryMode;
        public string DisplayText;
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
        public SandPlanetInteractionFlow04 Flow;
    }

    public sealed class SandPlanetEventFlow04
    {
        public string Id;
        public string Name;
        public string ExperiencePurpose;
        public string PlayerPerceivedChange;
        public string PresentationMode;
        public bool Active;
        public string WriterNote;
        public readonly List<SandPlanetFlowNode04> Nodes = new List<SandPlanetFlowNode04>();

        public IEnumerable<SandPlanetFlowNode04> GetNodeRows(string nodeId)
        {
            return Nodes.Where(n => n.Active && string.Equals(n.NodeId, nodeId, StringComparison.Ordinal));
        }

        public string StartNodeId
        {
            get
            {
                SandPlanetFlowNode04 n01 = Nodes.FirstOrDefault(n => n.Active && n.NodeId == "N01");
                return n01 != null ? n01.NodeId : Nodes.FirstOrDefault(n => n.Active)?.NodeId ?? string.Empty;
            }
        }
    }

    public sealed class SandPlanetStateDefinition04
    {
        public string Id;
        public string DataType;
        public string DefaultValue;
        public string Scope;
        public string Description;
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

    public sealed class SandPlanetContent04
    {
        public readonly Dictionary<string, SandPlanetLocation04> Locations = new Dictionary<string, SandPlanetLocation04>(StringComparer.Ordinal);
        public readonly Dictionary<string, SandPlanetCharacter04> Characters = new Dictionary<string, SandPlanetCharacter04>(StringComparer.Ordinal);
        public readonly Dictionary<string, SandPlanetWorldTarget04> WorldTargets = new Dictionary<string, SandPlanetWorldTarget04>(StringComparer.Ordinal);
        public readonly Dictionary<string, SandPlanetQuest04> Quests = new Dictionary<string, SandPlanetQuest04>(StringComparer.Ordinal);
        public readonly Dictionary<string, SandPlanetQuestStep04> QuestSteps = new Dictionary<string, SandPlanetQuestStep04>(StringComparer.Ordinal);
        public readonly Dictionary<string, SandPlanetStateDefinition04> States = new Dictionary<string, SandPlanetStateDefinition04>(StringComparer.Ordinal);
        public readonly Dictionary<string, SandPlanetInteractionFlow04> InteractionFlows = new Dictionary<string, SandPlanetInteractionFlow04>(StringComparer.Ordinal);
        public readonly Dictionary<string, SandPlanetEventFlow04> EventFlows = new Dictionary<string, SandPlanetEventFlow04>(StringComparer.Ordinal);
        public readonly List<SandPlanetInteraction04> Interactions = new List<SandPlanetInteraction04>();
        public readonly List<SandPlanetTrigger04> Triggers = new List<SandPlanetTrigger04>();
        public readonly List<SandPlanetSchedule04> Schedules = new List<SandPlanetSchedule04>();
        public readonly Dictionary<SandPlanetFlowNode04, SandPlanetQuestActionMeta04> EventNodeMeta = new Dictionary<SandPlanetFlowNode04, SandPlanetQuestActionMeta04>();
        public readonly Dictionary<SandPlanetFlowNode04, SandPlanetQuestActionMeta04> InteractionNodeMeta = new Dictionary<SandPlanetFlowNode04, SandPlanetQuestActionMeta04>();

        public static SandPlanetContent04 Load(IEnumerable<TextAsset> csvAssets)
        {
            SandPlanetContent04 db = new SandPlanetContent04();
            Dictionary<string, TextAsset> byName = new Dictionary<string, TextAsset>(StringComparer.OrdinalIgnoreCase);
            foreach (TextAsset asset in csvAssets ?? Array.Empty<TextAsset>())
                if (asset != null) byName[asset.name] = asset;

            db.LoadLocations(GetRows(byName, "Locations"));
            db.LoadCharacters(GetRows(byName, "Characters"));
            db.LoadWorldTargets(GetRows(byName, "WorldTargets"));
            db.LoadQuests(GetRows(byName, "Quests"));
            db.LoadQuestSteps(GetRows(byName, "QuestSteps"));
            db.LoadStates(GetRows(byName, "States"));
            db.LoadInteractionFlows(GetRows(byName, "Interactions"));
            db.LoadEventFlows(GetRows(byName, "Events"));
            db.LoadTriggers(GetRows(byName, "EventTriggers"));
            db.LoadSchedules(GetRows(byName, "NpcSchedules"));
            db.BuildInteractionCompatibilityEntries();
            return db;
        }

        private static List<Dictionary<string, string>> GetRows(Dictionary<string, TextAsset> byName, string name)
        {
            if (!byName.TryGetValue(name, out TextAsset asset) || asset == null)
            {
                Debug.LogError("[SandPlanet 0.4/v1.6] Missing generated CSV TextAsset: " + name + ".csv");
                return new List<Dictionary<string, string>>();
            }
            return SandPlanetCsv04.Parse(asset.text);
        }

        private void LoadLocations(IEnumerable<Dictionary<string, string>> rows)
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

        private void LoadCharacters(IEnumerable<Dictionary<string, string>> rows)
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

        private void LoadWorldTargets(IEnumerable<Dictionary<string, string>> rows)
        {
            foreach (Dictionary<string, string> r in rows)
            {
                SandPlanetWorldTarget04 d = new SandPlanetWorldTarget04
                {
                    Id = G(r, "WorldTargetID"), Name = G(r, "Name"), LocationId = G(r, "LocationID"),
                    Category = G(r, "TargetCategory"), SceneObjectKey = G(r, "SceneObjectKey"),
                    Clickable = B(r, "Clickable", true), Active = B(r, "Active", true)
                };
                if (!string.IsNullOrEmpty(d.Id)) WorldTargets[d.Id] = d;
            }
        }

        private void LoadQuests(IEnumerable<Dictionary<string, string>> rows)
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

        private void LoadQuestSteps(IEnumerable<Dictionary<string, string>> rows)
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

        private void LoadStates(IEnumerable<Dictionary<string, string>> rows)
        {
            foreach (Dictionary<string, string> r in rows)
            {
                SandPlanetStateDefinition04 d = new SandPlanetStateDefinition04
                {
                    Id = G(r, "StateID"), DataType = G(r, "DataType"), DefaultValue = G(r, "DefaultValue"),
                    Scope = G(r, "Scope"), Description = G(r, "Description")
                };
                if (!string.IsNullOrEmpty(d.Id)) States[d.Id] = d;
            }
        }

        private void LoadInteractionFlows(IEnumerable<Dictionary<string, string>> rows)
        {
            foreach (Dictionary<string, string> r in rows)
            {
                string id = G(r, "FlowID");
                if (string.IsNullOrEmpty(id)) continue;
                if (!InteractionFlows.TryGetValue(id, out SandPlanetInteractionFlow04 flow))
                {
                    flow = new SandPlanetInteractionFlow04
                    {
                        Id = id,
                        TargetId = G(r, "TargetID"), EntryText = G(r, "EntryText"),
                        QuestId = G(r, "QuestID"), QuestStepId = G(r, "QuestStepID"), QuestRole = G(r, "QuestRole", "NONE").ToUpperInvariant(),
                        EntryMode = G(r, "EntryMode", "MENU_OPTION"), OpenDay = I(r, "OpenDay", 1), CloseDay = I(r, "CloseDay", 21),
                        AllowedTimeSlots = G(r, "AllowedTimeSlots", "MORNING|AFTERNOON|EVENING"), RepeatRule = G(r, "RepeatRule", "ONCE"),
                        Priority = I(r, "Priority"), ConditionLogic = G(r, "ShowConditionLogic", "AND"),
                        Condition1 = C(r, "ShowCond1"), Condition2 = C(r, "ShowCond2"), Active = B(r, "Active", true), WriterNote = G(r, "WriterNote")
                    };
                    flow.TargetType = Characters.ContainsKey(flow.TargetId) ? "CHARACTER" : "WORLD_TARGET";
                    InteractionFlows[id] = flow;
                }

                SandPlanetFlowNode04 node = ParseInteractionNode(r);
                string questAction = G(r, "QuestAction");
                if (!string.IsNullOrEmpty(questAction) && !string.Equals(questAction, "NONE", StringComparison.OrdinalIgnoreCase))
                {
                    InteractionNodeMeta[node] = new SandPlanetQuestActionMeta04
                    {
                        QuestAction = questAction,
                        QuestId = flow.QuestId,
                        QuestStepId = G(r, "QuestActionStepID")
                    };
                }
                flow.Nodes.Add(node);
            }
        }

        private SandPlanetFlowNode04 ParseInteractionNode(Dictionary<string, string> r)
        {
            SandPlanetFlowNode04 node = ParseCommonNode(r, "TimeCost");
            AddAffinity(node, G(r, "Affinity1CharacterID"), I(r, "Affinity1Delta"));
            AddAffinity(node, G(r, "Affinity2CharacterID"), I(r, "Affinity2Delta"));
            AddState(node, G(r, "State1ID"), G(r, "State1Change"));
            AddState(node, G(r, "State2ID"), G(r, "State2Change"));
            return node;
        }

        private void LoadEventFlows(IEnumerable<Dictionary<string, string>> rows)
        {
            foreach (Dictionary<string, string> r in rows)
            {
                string id = G(r, "EventID");
                if (string.IsNullOrEmpty(id)) continue;
                if (!EventFlows.TryGetValue(id, out SandPlanetEventFlow04 flow))
                {
                    flow = new SandPlanetEventFlow04
                    {
                        Id = id, Name = G(r, "EventName"), ExperiencePurpose = G(r, "ExperiencePurpose"),
                        PlayerPerceivedChange = G(r, "PlayerPerceivedChange"), PresentationMode = G(r, "PresentationMode", "SILENT"),
                        Active = B(r, "Active", true), WriterNote = G(r, "WriterNote")
                    };
                    EventFlows[id] = flow;
                }

                SandPlanetFlowNode04 node = ParseCommonNode(r, "TimeDelta");
                AddState(node, G(r, "State1ID"), G(r, "State1Change"));
                AddState(node, G(r, "State2ID"), G(r, "State2Change"));
                AddState(node, G(r, "State3ID"), G(r, "State3Change"));
                AddFixedCastAffinity(node, r);
                string questAction = G(r, "QuestAction");
                if (!string.IsNullOrEmpty(questAction) && !string.Equals(questAction, "NONE", StringComparison.OrdinalIgnoreCase))
                {
                    EventNodeMeta[node] = new SandPlanetQuestActionMeta04
                    {
                        QuestAction = questAction,
                        QuestId = G(r, "QuestID"),
                        QuestStepId = G(r, "QuestStepID")
                    };
                }
                flow.Nodes.Add(node);
            }
        }

        private SandPlanetFlowNode04 ParseCommonNode(Dictionary<string, string> r, string timeKey)
        {
            return new SandPlanetFlowNode04
            {
                NodeId = G(r, "NodeID", "N01"), ChoiceId = G(r, "ChoiceID"), PresentationType = G(r, "PresentationType", "NARRATION"),
                Speaker = G(r, "Speaker"), BodyText = G(r, "BodyText"), ChoiceText = G(r, "ChoiceText", "계속"), NextNodeId = G(r, "NextNodeID"),
                TimeCost = I(r, timeKey), WillDelta = I(r, "WillDelta"), PersonalXpDelta = I(r, "PersonalXpDelta"),
                SocialXpDelta = I(r, "SocialXpDelta"), TechnicalXpDelta = I(r, "TechnicalXpDelta"), EmitEventId = G(r, "EmitEventID"),
                ResultTextOverride = G(r, "ResultTextOverride"), SoftStat = G(r, "SoftStat", "NONE"), SoftRequirement = I(r, "SoftRequirement"),
                HardConditionLogic = G(r, "HardConditionLogic", "AND"), HardCondition1 = C(r, "HardCond1"), HardCondition2 = C(r, "HardCond2"),
                Active = B(r, "Active", true)
            };
        }

        private void AddFixedCastAffinity(SandPlanetFlowNode04 node, Dictionary<string, string> r)
        {
            AddAffinity(node, "CHA_SAM", I(r, "SamAffinityDelta"));
            AddAffinity(node, "CHA_JINA", I(r, "JinaAffinityDelta"));
            AddAffinity(node, "CHA_FAYE", I(r, "FayeAffinityDelta"));
            AddAffinity(node, "CHA_BENJAMIN", I(r, "BenjaminAffinityDelta"));
            AddAffinity(node, "CHA_BORICHI", I(r, "BorichiAffinityDelta"));
            AddAffinity(node, "CHA_DIYA", I(r, "DiyaAffinityDelta"));
        }

        private static void AddAffinity(SandPlanetFlowNode04 node, string characterId, int delta)
        {
            if (!string.IsNullOrEmpty(characterId) && delta != 0)
                node.AffinityChanges.Add(new SandPlanetAffinityChange04 { CharacterId = characterId, Delta = delta });
        }

        private static void AddState(SandPlanetFlowNode04 node, string stateId, string change)
        {
            if (string.IsNullOrEmpty(stateId) || string.IsNullOrEmpty(change)) return;
            string[] parts = change.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
            node.StateChanges.Add(new SandPlanetStateChange04
            {
                StateId = stateId,
                Operation = parts.Length > 0 ? parts[0].ToUpperInvariant() : "SET",
                Value = parts.Length > 1 ? parts[1] : string.Empty
            });
        }

        private void BuildInteractionCompatibilityEntries()
        {
            Interactions.Clear();
            foreach (SandPlanetInteractionFlow04 flow in InteractionFlows.Values)
            {
                bool morning = ContainsSlot(flow.AllowedTimeSlots, "MORNING");
                bool afternoon = ContainsSlot(flow.AllowedTimeSlots, "AFTERNOON");
                bool evening = ContainsSlot(flow.AllowedTimeSlots, "EVENING");
                string role = string.IsNullOrEmpty(flow.QuestRole) ? (string.IsNullOrEmpty(flow.QuestId) ? "NONE" : "PROGRESS") : flow.QuestRole.ToUpperInvariant();
                Interactions.Add(new SandPlanetInteraction04
                {
                    Id = flow.Id, TargetType = flow.TargetType, TargetId = flow.TargetId,
                    InteractionType = string.IsNullOrEmpty(flow.QuestId) ? "BASIC" : "QUEST",
                    QuestId = flow.QuestId, QuestStepId = flow.QuestStepId, QuestRole = role,
                    EntryMode = flow.EntryMode, DisplayText = flow.EntryText,
                    OpenDay = flow.OpenDay, CloseDay = flow.CloseDay, Morning = morning, Afternoon = afternoon, Evening = evening,
                    RepeatRule = flow.RepeatRule, ConditionLogic = flow.ConditionLogic, Condition1 = flow.Condition1, Condition2 = flow.Condition2,
                    Priority = flow.Priority, Active = flow.Active, Flow = flow
                });
            }
        }

        private static bool ContainsSlot(string slots, string value)
        {
            return (slots ?? string.Empty).Split('|').Any(s => string.Equals(s.Trim(), value, StringComparison.OrdinalIgnoreCase));
        }

        private void LoadTriggers(IEnumerable<Dictionary<string, string>> rows)
        {
            foreach (Dictionary<string, string> r in rows)
            {
                Triggers.Add(new SandPlanetTrigger04
                {
                    Id = G(r, "TriggerID"), EventId = G(r, "EventID"), TriggerTiming = G(r, "TriggerTiming"), TriggerMomentText = G(r, "TriggerMomentText"),
                    LocationId = G(r, "LocationID"), DetectorType = G(r, "DetectorType"), DetectorId = G(r, "DetectorID"),
                    ConditionLogic = G(r, "ConditionLogic", "AND"), Condition1 = C(r, "Cond1"), Condition2 = C(r, "Cond2"),
                    RepeatRule = G(r, "RepeatRule", "ONCE"), Priority = I(r, "Priority"), Active = B(r, "Active", true)
                });
            }
        }

        private void LoadSchedules(IEnumerable<Dictionary<string, string>> rows)
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

        private static string G(Dictionary<string, string> r, string key, string fallback = "") => SandPlanetCsv04.Get(r, key, fallback);
        private static int I(Dictionary<string, string> r, string key, int fallback = 0) => SandPlanetCsv04.GetInt(r, key, fallback);
        private static bool B(Dictionary<string, string> r, string key, bool fallback = false) => SandPlanetCsv04.GetBool(r, key, fallback);
    }
}
