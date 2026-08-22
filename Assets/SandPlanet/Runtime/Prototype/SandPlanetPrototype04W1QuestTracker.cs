using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace SandPlanet.Prototype
{
    /// <summary>
    /// Prototype 0.4 special tracker for the Day 1~2 reunion Main Quest.
    /// Renders a dedicated six-name checklist inside the quest panel while
    /// QST_W1_MAIN_01_AWAKE is active. This stays a one-off presentation rule,
    /// not a generic checklist-quest framework.
    /// </summary>
    [DefaultExecutionOrder(11000)]
    public sealed class SandPlanetPrototype04W1QuestTracker : MonoBehaviour
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private const string IntroQuestId = "QST_W1_MAIN_01_AWAKE";

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
        private FieldInfo trackerField;
        private FieldInfo questStatusField;
        private FieldInfo statesField;
        private Text checklistText;
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
            Text baseTracker = trackerField?.GetValue(controller) as Text;
            if (baseTracker == null || baseTracker.transform.parent == null)
            {
                enabled = false;
                return;
            }

            Transform parent = baseTracker.transform.parent;
            Transform existing = parent.Find("UX17_ReunionChecklist");
            if (existing != null)
            {
                checklistText = existing.GetComponent<Text>();
            }
            else
            {
                GameObject go = new GameObject("UX17_ReunionChecklist", typeof(RectTransform), typeof(Text));
                go.transform.SetParent(parent, false);
                checklistText = go.GetComponent<Text>();
                checklistText.font = baseTracker.font != null
                    ? baseTracker.font
                    : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                checklistText.fontSize = 15;
                checklistText.color = Color.white;
                checklistText.alignment = TextAnchor.UpperLeft;
                checklistText.supportRichText = true;
                checklistText.horizontalOverflow = HorizontalWrapMode.Wrap;
                checklistText.verticalOverflow = VerticalWrapMode.Overflow;
                checklistText.raycastTarget = false;
                checklistText.lineSpacing = 1.05f;

                RectTransform r = checklistText.rectTransform;
                r.anchorMin = new Vector2(.045f, .05f);
                r.anchorMax = new Vector2(.955f, .63f);
                r.offsetMin = Vector2.zero;
                r.offsetMax = Vector2.zero;
            }

            checklistText.gameObject.SetActive(false);
            RefreshChecklist();
        }

        private void LateUpdate()
        {
            if (checklistText == null || Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + .10f;
            RefreshChecklist();
        }

        private void RefreshChecklist()
        {
            Dictionary<string, string> questStatus = questStatusField?.GetValue(controller) as Dictionary<string, string>;
            Dictionary<string, string> states = statesField?.GetValue(controller) as Dictionary<string, string>;

            bool active = questStatus != null &&
                          questStatus.TryGetValue(IntroQuestId, out string status) &&
                          string.Equals(status, "ACTIVE", StringComparison.OrdinalIgnoreCase);

            checklistText.gameObject.SetActive(active);
            if (!active) return;

            string text = "<color=#8EC5E8><b>다시 만날 사람들</b></color>\n";
            foreach (Entry entry in Entries)
            {
                bool met = states != null &&
                           states.TryGetValue(entry.StateId, out string raw) &&
                           string.Equals(raw, "TRUE", StringComparison.OrdinalIgnoreCase);
                text += met
                    ? $"<color=#69C77C><b>✓</b></color> {entry.Name}\n"
                    : $"<color=#AAB4BE>□</color> {entry.Name}\n";
            }
            checklistText.text = text.TrimEnd();
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
