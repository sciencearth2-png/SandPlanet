using System;
using System.Collections.Generic;
using System.Linq;
using SandPlanet.Prototype.DataDriven;

namespace SandPlanet.Prototype
{
    internal sealed class Prototype04FlowRuntime
    {
        private readonly SandPlanetContent04 content;
        private readonly Prototype04GameState state;
        private readonly Prototype04ConditionEvaluator conditions;
        private readonly Prototype04QuestService quests;
        private readonly Prototype04EventService events;
        private readonly Func<string> currentLocationId;
        private readonly Action<string> log;

        private SandPlanetInteraction04 activeInteraction;
        private SandPlanetInteractionFlow04 activeInteractionFlow;
        private SandPlanetEventFlow04 activeEventFlow;
        private string activeNodeId;
        private bool flowCommitted;
        private Prototype04PendingEffects pendingEffects;

        public Prototype04FlowRuntime(
            SandPlanetContent04 runtimeContent,
            Prototype04GameState gameState,
            Prototype04ConditionEvaluator conditionEvaluator,
            Prototype04QuestService questService,
            Prototype04EventService eventService,
            Func<string> currentLocation,
            Action<string> logAction)
        {
            content = runtimeContent;
            state = gameState;
            conditions = conditionEvaluator;
            quests = questService;
            events = eventService;
            currentLocationId = currentLocation;
            log = logAction;
        }

        public event Action Changed;
        public event Action Completed;
        public event Action Cancelled;

        public bool IsActive => activeInteractionFlow != null || activeEventFlow != null;
        public bool HasActiveInteractionFlow => activeInteractionFlow != null;
        public bool HasActiveEventFlow => activeEventFlow != null;
        public bool FlowCommitted => flowCommitted;
        public SandPlanetInteraction04 ActiveInteraction => activeInteraction;
        public SandPlanetEventFlow04 ActiveEventFlow => activeEventFlow;

        public void BeginInteraction(SandPlanetInteraction04 interaction)
        {
            if (interaction == null || interaction.Flow == null) return;
            activeInteraction = interaction;
            activeInteractionFlow = interaction.Flow;
            activeEventFlow = null;
            activeNodeId = interaction.Flow.StartNodeId;
            flowCommitted = false;
            pendingEffects = new Prototype04PendingEffects();
            Changed?.Invoke();
        }

        public void BeginEvent(SandPlanetEventFlow04 flow)
        {
            if (flow == null) return;
            activeInteraction = null;
            activeInteractionFlow = null;
            activeEventFlow = flow;
            activeNodeId = flow.StartNodeId;
            flowCommitted = false;
            pendingEffects = new Prototype04PendingEffects();
            Changed?.Invoke();
        }

        public IReadOnlyList<SandPlanetFlowNode04> CurrentRows()
        {
            return Rows(activeNodeId).Where(node => node.Active).ToList();
        }

        public Prototype04NodeCheck CheckNode(SandPlanetFlowNode04 node)
        {
            if (node == null || !node.Active) return new Prototype04NodeCheck(false, 0, "비활성");
            int stagedTime = pendingEffects?.TimeDelta ?? 0;
            if (state.Hour + stagedTime + node.TimeCost > Prototype04GameState.DayEndHour)
                return new Prototype04NodeCheck(false, 0, "오늘 남은 시간 부족");
            if (!conditions.EvaluatePair(node.HardConditionLogic, node.HardCondition1, node.HardCondition2))
                return new Prototype04NodeCheck(false, 0, "조건 미충족");

            int extraWill = 0;
            if (!string.IsNullOrEmpty(node.SoftStat) && node.SoftStat != "NONE" && node.SoftRequirement > 0)
                extraWill = Math.Max(0, node.SoftRequirement - state.GetStatLevel(node.SoftStat));

            int stagedWill = pendingEffects?.WillDelta ?? 0;
            if (state.Will + stagedWill + node.WillDelta - extraWill < 0)
                return new Prototype04NodeCheck(false, extraWill, "의지 부족");
            return new Prototype04NodeCheck(true, extraWill, string.Empty);
        }

        public void ExecuteNode(SandPlanetFlowNode04 node)
        {
            Prototype04NodeCheck check = CheckNode(node);
            if (!check.CanExecute) return;
            flowCommitted = true;
            StageNode(pendingEffects, node, check.ExtraWillCost, activeInteractionFlow, activeEventFlow);
            if (!string.IsNullOrEmpty(node.NextNodeId))
            {
                activeNodeId = node.NextNodeId;
                Changed?.Invoke();
                return;
            }
            FinalizeActiveFlow();
        }

        public void FinishEmptyFlow() => FinalizeActiveFlow();

        public void Cancel()
        {
            if (!IsActive) return;
            ClearActiveFlow();
            Cancelled?.Invoke();
        }

        public void HandleBack()
        {
            if (activeInteractionFlow != null)
            {
                if (!flowCommitted)
                {
                    Cancel();
                    return;
                }
                SkipLinearRemainderAndFinish();
                return;
            }
            if (activeEventFlow != null)
            {
                if (Rows(activeNodeId).Count() > 1) return;
                SkipLinearRemainderAndFinish();
            }
        }

        public bool IsForcedEventChoice() => activeEventFlow != null && CurrentRows().Count > 1;

        public void ResolveSilentEvent(SandPlanetEventFlow04 flow)
        {
            Prototype04PendingEffects local = new Prototype04PendingEffects();
            string nodeId = flow.StartNodeId;
            HashSet<string> visited = new HashSet<string>(StringComparer.Ordinal);
            while (!string.IsNullOrEmpty(nodeId) && visited.Add(nodeId))
            {
                SandPlanetFlowNode04 node = flow.GetNodeRows(nodeId).FirstOrDefault();
                if (node == null) break;
                StageNode(local, node, 0, null, flow);
                nodeId = node.NextNodeId;
            }
            ApplyPendingEffects(local);
            if (!string.IsNullOrEmpty(local.ResultText)) log(local.ResultText);
        }

        public void AbandonActiveFlow() => ClearActiveFlow();

        private void SkipLinearRemainderAndFinish()
        {
            HashSet<string> visited = new HashSet<string>(StringComparer.Ordinal);
            string nodeId = activeNodeId;
            while (!string.IsNullOrEmpty(nodeId) && visited.Add(nodeId))
            {
                List<SandPlanetFlowNode04> rows = Rows(nodeId).Where(node => node.Active).ToList();
                if (rows.Count != 1) return;
                Prototype04NodeCheck check = CheckNode(rows[0]);
                if (!check.CanExecute) return;
                StageNode(pendingEffects, rows[0], check.ExtraWillCost, activeInteractionFlow, activeEventFlow);
                nodeId = rows[0].NextNodeId;
            }
            FinalizeActiveFlow();
        }

        private void FinalizeActiveFlow()
        {
            Prototype04PendingEffects effects = pendingEffects ?? new Prototype04PendingEffects();
            SandPlanetInteraction04 finishedInteraction = activeInteraction;
            bool wasInteraction = activeInteractionFlow != null;
            ClearActiveFlow();
            ApplyPendingEffects(effects);

            if (wasInteraction && finishedInteraction != null)
            {
                state.MarkInteractionUsed(finishedInteraction.Id);
                events.ProcessTriggers("INTERACTION", currentLocationId());
            }
            if (!string.IsNullOrEmpty(effects.ResultText)) log(effects.ResultText);
            Completed?.Invoke();
        }

        private void ApplyPendingEffects(Prototype04PendingEffects effects)
        {
            state.ApplyClockAndWill(effects.TimeDelta, effects.WillDelta);
            state.AddXp("PERSONAL", effects.PersonalXpDelta);
            state.AddXp("SOCIAL", effects.SocialXpDelta);
            state.AddXp("TECHNICAL", effects.TechnicalXpDelta);
            foreach (KeyValuePair<string, int> pair in effects.AffinityDelta)
                state.SetAffinity(pair.Key, state.GetAffinity(pair.Key) + pair.Value);
            foreach (SandPlanetStateChange04 change in effects.StateChanges)
                events.ApplyStateChange(change, currentLocationId());
            foreach (SandPlanetQuestActionMeta04 action in effects.QuestActions)
                quests.ApplyAction(action);
            foreach (string eventId in effects.EmitEvents)
                events.FireEvent(eventId);
        }

        private void StageNode(
            Prototype04PendingEffects effects,
            SandPlanetFlowNode04 node,
            int extraWillCost,
            SandPlanetInteractionFlow04 interactionFlow,
            SandPlanetEventFlow04 eventFlow)
        {
            effects.TimeDelta += node.TimeCost;
            effects.WillDelta += node.WillDelta - extraWillCost;
            effects.PersonalXpDelta += node.PersonalXpDelta;
            effects.SocialXpDelta += node.SocialXpDelta;
            effects.TechnicalXpDelta += node.TechnicalXpDelta;
            foreach (SandPlanetAffinityChange04 affinity in node.AffinityChanges) effects.AddAffinity(affinity.CharacterId, affinity.Delta);
            foreach (SandPlanetStateChange04 stateChange in node.StateChanges) effects.StateChanges.Add(stateChange);
            if (!string.IsNullOrEmpty(node.EmitEventId)) effects.EmitEvents.Add(node.EmitEventId);
            if (!string.IsNullOrEmpty(node.ResultTextOverride)) effects.ResultText = node.ResultTextOverride;

            if (interactionFlow != null && content.InteractionNodeMeta.TryGetValue(node, out SandPlanetQuestActionMeta04 interactionMeta) && !string.IsNullOrEmpty(interactionMeta.QuestAction))
                effects.QuestActions.Add(interactionMeta);
            if (eventFlow != null && content.EventNodeMeta.TryGetValue(node, out SandPlanetQuestActionMeta04 eventMeta) && !string.IsNullOrEmpty(eventMeta.QuestAction))
                effects.QuestActions.Add(eventMeta);
        }

        private IEnumerable<SandPlanetFlowNode04> Rows(string nodeId)
        {
            if (activeInteractionFlow != null) return activeInteractionFlow.GetNodeRows(nodeId);
            if (activeEventFlow != null) return activeEventFlow.GetNodeRows(nodeId);
            return Enumerable.Empty<SandPlanetFlowNode04>();
        }

        private void ClearActiveFlow()
        {
            activeInteraction = null;
            activeInteractionFlow = null;
            activeEventFlow = null;
            activeNodeId = null;
            flowCommitted = false;
            pendingEffects = null;
        }
    }

    internal sealed class Prototype04PendingEffects
    {
        public int TimeDelta;
        public int WillDelta;
        public int PersonalXpDelta;
        public int SocialXpDelta;
        public int TechnicalXpDelta;
        public readonly Dictionary<string, int> AffinityDelta = new Dictionary<string, int>(StringComparer.Ordinal);
        public readonly List<SandPlanetStateChange04> StateChanges = new List<SandPlanetStateChange04>();
        public readonly List<SandPlanetQuestActionMeta04> QuestActions = new List<SandPlanetQuestActionMeta04>();
        public readonly List<string> EmitEvents = new List<string>();
        public string ResultText;

        public void AddAffinity(string characterId, int delta)
        {
            if (string.IsNullOrEmpty(characterId) || delta == 0) return;
            AffinityDelta[characterId] = AffinityDelta.TryGetValue(characterId, out int current) ? current + delta : delta;
        }
    }

    internal readonly struct Prototype04NodeCheck
    {
        public readonly bool CanExecute;
        public readonly int ExtraWillCost;
        public readonly string Reason;

        public Prototype04NodeCheck(bool canExecute, int extraWillCost, string reason)
        {
            CanExecute = canExecute;
            ExtraWillCost = extraWillCost;
            Reason = reason;
        }
    }
}
