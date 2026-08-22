using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SandPlanet.Prototype.DataDriven;

namespace SandPlanet.Prototype
{
    internal sealed class Prototype04EventService
    {
        private readonly SandPlanetContent04 content;
        private readonly Prototype04GameState state;
        private readonly Prototype04QuestService quests;
        private readonly Prototype04ConditionEvaluator conditions;
        private readonly Action<string> log;
        private readonly Queue<string> eventQueue = new Queue<string>();
        private Prototype04FlowRuntime flowRuntime;
        private bool evaluatingStateTriggers;

        public Prototype04EventService(
            SandPlanetContent04 runtimeContent,
            Prototype04GameState gameState,
            Prototype04QuestService questService,
            Prototype04ConditionEvaluator conditionEvaluator,
            Action<string> logAction)
        {
            content = runtimeContent;
            state = gameState;
            quests = questService;
            conditions = conditionEvaluator;
            log = logAction;
        }

        public event Action SilentEventResolved;

        public void BindFlowRuntime(Prototype04FlowRuntime runtime) => flowRuntime = runtime;

        public void ProcessTriggers(string timing, string currentLocationId)
        {
            foreach (SandPlanetTrigger04 trigger in content.Triggers
                         .Where(item => item.Active && item.TriggerTiming == timing)
                         .OrderByDescending(item => item.Priority)
                         .ToList())
            {
                if (!state.IsTriggerRepeatAvailable(trigger.Id, trigger.RepeatRule)) continue;
                if (!string.IsNullOrEmpty(trigger.LocationId) && trigger.LocationId != currentLocationId) continue;
                if (!conditions.EvaluatePair(trigger.ConditionLogic, trigger.Condition1, trigger.Condition2)) continue;
                state.MarkTriggerUsed(trigger.Id);
                FireEvent(trigger.EventId);
            }
        }

        public void FireEvent(string eventId)
        {
            if (string.IsNullOrEmpty(eventId) || !content.EventFlows.TryGetValue(eventId, out SandPlanetEventFlow04 flow) || !flow.Active) return;
            state.MarkEventOccurred(eventId);
            quests.ProgressFromEvent(eventId);
            log("Event: " + flow.Name);

            if (string.Equals(flow.PresentationMode, "SILENT", StringComparison.OrdinalIgnoreCase) && CanAutoResolve(flow))
            {
                flowRuntime.ResolveSilentEvent(flow);
                SilentEventResolved?.Invoke();
                return;
            }
            eventQueue.Enqueue(eventId);
        }

        public bool TryDequeue(out SandPlanetEventFlow04 flow)
        {
            flow = null;
            if (eventQueue.Count == 0) return false;
            string id = eventQueue.Dequeue();
            return content.EventFlows.TryGetValue(id, out flow);
        }

        public void ApplyStateChange(SandPlanetStateChange04 change, string currentLocationId)
        {
            if (change == null || string.IsNullOrEmpty(change.StateId)) return;
            string value = change.Operation == "ADD"
                ? (ParseInt(state.GetState(change.StateId)) + ParseInt(change.Value)).ToString(CultureInfo.InvariantCulture)
                : change.Value;
            state.SetStateValue(change.StateId, value);
            if (evaluatingStateTriggers) return;
            evaluatingStateTriggers = true;
            ProcessTriggers("STATE_CHANGE", currentLocationId);
            evaluatingStateTriggers = false;
        }

        private static bool CanAutoResolve(SandPlanetEventFlow04 flow)
        {
            string nodeId = flow.StartNodeId;
            HashSet<string> visited = new HashSet<string>(StringComparer.Ordinal);
            while (!string.IsNullOrEmpty(nodeId) && visited.Add(nodeId))
            {
                List<SandPlanetFlowNode04> rows = flow.GetNodeRows(nodeId).ToList();
                if (rows.Count != 1) return false;
                nodeId = rows[0].NextNodeId;
            }
            return true;
        }

        private static int ParseInt(string value) => int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed) ? parsed : 0;
    }
}
