using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace SandPlanet.Prototype
{
    /// <summary>
    /// Prototype 0.4 special tracker for the Day 1~2 reunion Main Quest.
    /// Keeps the normal quest tracker intact and appends a six-name checklist while
    /// QST_W1_MAIN_01_AWAKE is active. This is intentionally a one-off presentation
    /// rule rather than a new generic checklist-quest framework.
    /// </summary>
    public sealed class SandPlanetPrototype04W1QuestTracker : MonoBehaviour
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private const string IntroQuestId = "QST_W1_MAIN_01_AWAKE";
        private const string BlockToken = "\n\n<size=16><color=#8EC5E8><b>— 다시 만난 사람들 —</b></color></size>\n";

        private static readonly Entry[] Entries =
        {
            new Entry("STA_W1_MET_BENJAMIN", "벤자민"),
            new Entry("STA_W1_MET_SAM", "샘"),
            new Entry("STA_W1_MET_JINA", "지나"),
            new Entry("STA_W1_MET_FAYE", "페이"),
            new Entry("STA_W1_MET_BORICHI", "보리치"),
            new Entry("STA_W1_MET_DIYA", "디야")
        };

        private SandPlanetPrototype04Controller controller;
        private Text tracker;
        private FieldInfo trackerField;
        private FieldInfo questStatusField;
        private FieldInfo statesField;
        private float nextRefresh;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            SandPlanetPrototype04Controller found = UnityEngine.Object.FindFirstObjectByType<SandPlanetPrototype04Controller>();
            if (found != null && found.GetComponent<SandPlanetPrototype04W1QuestTracker>() == null)
                found.gameObject.AddComponent<SandPlanetPrototype04W1QuestTracker>();
        }

        private void Awake()
        {
            controller = GetComponent<SandPlanetPrototype04Controller>();
            if (controller == null)
            {
                enabled = false;
                return;
            }

            Type t = controller.GetType();
            trackerField = t.GetField("questTrackerText", PrivateInstance);
            questStatusField = t.GetField("questStatus", PrivateInstance);
            statesField = t.GetField("states", PrivateInstance);
        }

        private void Start()
        {
            tracker = trackerField?.GetValue(controller) as Text;
            if (tracker == null) enabled = false;
        }

        private void LateUpdate()
        {
            if (tracker == null || Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + .10f;

            string baseText = StripChecklist(tracker.text ?? string.Empty);
            Dictionary<string, string> questStatus = questStatusField?.GetValue(controller) as Dictionary<string, string>;
            Dictionary<string, string> states = statesField?.GetValue(controller) as Dictionary<string, string>;

            bool active = questStatus != null &&
                          questStatus.TryGetValue(IntroQuestId, out string status) &&
                          string.Equals(status, "ACTIVE", StringComparison.OrdinalIgnoreCase);

            if (!active)
            {
                if (!string.Equals(tracker.text, baseText, StringComparison.Ordinal)) tracker.text = baseText;
                return;
            }

            string block = BlockToken;
            foreach (Entry entry in Entries)
            {
                bool met = states != null &&
                           states.TryGetValue(entry.StateId, out string raw) &&
                           string.Equals(raw, "TRUE", StringComparison.OrdinalIgnoreCase);
                block += met
                    ? $"<color=#69C77C><b>✓</b></color> {entry.Name}\n"
                    : $"<color=#AAB4BE>□</color> {entry.Name}\n";
            }

            string composed = baseText.TrimEnd() + block.TrimEnd();
            if (!string.Equals(tracker.text, composed, StringComparison.Ordinal)) tracker.text = composed;
        }

        private static string StripChecklist(string text)
        {
            int index = text.IndexOf(BlockToken, StringComparison.Ordinal);
            return index >= 0 ? text.Substring(0, index).TrimEnd() : text;
        }

        private readonly struct Entry
        {
            public readonly string StateId;
            public readonly string Name;

            public Entry(string stateId, string name)
            {
                StateId = stateId;
                Name = name;
            }
        }
    }
}
