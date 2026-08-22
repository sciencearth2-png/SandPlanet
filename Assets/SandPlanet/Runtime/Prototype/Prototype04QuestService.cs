using System;
using SandPlanet.Prototype.DataDriven;

namespace SandPlanet.Prototype
{
    internal sealed class Prototype04QuestService
    {
        private readonly SandPlanetContent04 content;
        private readonly Prototype04GameState state;
        private readonly Action<string> log;

        public Prototype04QuestService(SandPlanetContent04 runtimeContent, Prototype04GameState gameState, Action<string> logAction)
        {
            content = runtimeContent;
            state = gameState;
            log = logAction;
        }

        public string GetStatus(string questId) => state.GetQuestStatus(questId);
        public string GetStep(string questId) => state.GetQuestStep(questId);

        public void ApplyAction(SandPlanetQuestActionMeta04 action)
        {
            if (action == null || string.IsNullOrEmpty(action.QuestAction)) return;
            switch (action.QuestAction.ToUpperInvariant())
            {
                case "ACTIVATE_QUEST": Activate(action.QuestId); break;
                case "COMPLETE_QUEST": Complete(action.QuestId); break;
                case "FAIL_QUEST": Fail(action.QuestId); break;
                case "SET_QUEST_STEP": SetStep(action.QuestId, action.QuestStepId); break;
            }
        }

        public void ProgressFromEvent(string eventId)
        {
            foreach (SandPlanetQuest04 quest in content.Quests.Values)
            {
                if (GetStatus(quest.Id) != "ACTIVE") continue;
                string stepId = GetStep(quest.Id);
                if (!content.QuestSteps.TryGetValue(stepId, out SandPlanetQuestStep04 step) || step.ProgressEventId != eventId) continue;
                if (step.OnProgressEvent == "SET_STEP" && !string.IsNullOrEmpty(step.NextStepId)) SetStep(quest.Id, step.NextStepId);
                else if (step.OnProgressEvent == "COMPLETE_QUEST") Complete(quest.Id);
            }
        }

        private void Activate(string questId)
        {
            if (!content.Quests.TryGetValue(questId, out SandPlanetQuest04 quest) || GetStatus(questId) != "LOCKED") return;
            state.SetQuestStatus(questId, "ACTIVE");
            state.SetQuestStep(questId, quest.InitialStepId);
            log("Quest 수락: " + quest.Title);
        }

        private void Complete(string questId)
        {
            if (!content.Quests.TryGetValue(questId, out SandPlanetQuest04 quest) || GetStatus(questId) != "ACTIVE") return;
            state.SetQuestStatus(questId, "COMPLETED");
            log("Quest 완료: " + quest.Title);
        }

        private void Fail(string questId)
        {
            if (!content.Quests.TryGetValue(questId, out SandPlanetQuest04 quest) || GetStatus(questId) != "ACTIVE") return;
            state.SetQuestStatus(questId, "FAILED");
            log("Quest 실패: " + quest.Title);
        }

        private void SetStep(string questId, string stepId)
        {
            if (string.IsNullOrEmpty(questId) || string.IsNullOrEmpty(stepId) || GetStatus(questId) != "ACTIVE") return;
            if (!content.QuestSteps.TryGetValue(stepId, out SandPlanetQuestStep04 step) || step.QuestId != questId) return;
            state.SetQuestStep(questId, stepId);
            if (content.Quests.TryGetValue(questId, out SandPlanetQuest04 quest))
                log("Quest 진행: " + quest.Title + " — " + step.Title);
        }
    }
}
