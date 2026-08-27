using System;
using System.Collections.Generic;
using System.Linq;
using SandPlanet.Prototype.DataDriven;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SandPlanet.Prototype
{
    /// <summary>
    /// Quest tracker presentation for Prototype 0.4 / authoring v1.7+.
    ///
    /// - Replaces the single tracker Text with individual hoverable Quest rows.
    /// - Keeps the Day1~2 reunion checklist visible inside the Quest panel.
    /// - Reads PlayerDescription directly from Quests.csv so the core v1.6 data model
    ///   stays backward-compatible; if the column is absent, Summary is used as fallback.
    /// </summary>
    public sealed class SandPlanetPrototype04W1QuestTracker : MonoBehaviour
    {
        private const string IntroQuestId = "QST_W1_MAIN_01_AWAKE";
        private const string MainColor = "#F0A24A";
        private const string CharacterColor = "#69C77C";
        private const string SideColor = "#F1D784";

        private static readonly Entry[] ReunionEntries =
        {
            new Entry("STA_W1_MET_BENJAMIN", "벤자민"),
            new Entry("STA_W1_MET_SAM", "샘"),
            new Entry("STA_W1_MET_JINA", "지나"),
            new Entry("STA_W1_MET_FAYE", "페이"),
            new Entry("STA_W1_MET_BORICHI", "보리치"),
            new Entry("STA_W1_MET_DIYA", "디야")
        };

        private Prototype04RuntimeFacade runtime;
        private Prototype04NavigationController navigation;
        private SandPlanetContent04 content;

        private Text baseTracker;
        private RectTransform rowsRoot;
        private GameObject hoverCard;
        private Text hoverTitle;
        private Text hoverDescription;
        private Text hoverObjective;

        private readonly Dictionary<string, string> playerDescriptions = new Dictionary<string, string>(StringComparer.Ordinal);
        private string lastSignature = string.Empty;
        private string currentHoverQuestId = string.Empty;
        public bool IsDetailOpen => hoverCard != null && hoverCard.activeSelf;

        public void Initialize(Prototype04RuntimeFacade runtimeFacade, Prototype04NavigationController navigationController)
        {
            runtime = runtimeFacade;
            navigation = navigationController;
            content = runtime.Content;
            baseTracker = runtime.QuestTrackerText;
            if (content == null || baseTracker == null || baseTracker.transform.parent == null)
            {
                enabled = false;
                return;
            }

            LoadPlayerDescriptions();
            BuildQuestRowsRoot(baseTracker.transform.parent);
            BuildHoverCard(baseTracker.transform.parent);

            // Controller still writes the legacy tracker text, but this component owns
            // the visible Quest presentation from now on.
            baseTracker.enabled = false;
            baseTracker.raycastTarget = false;

            RefreshTracker(true);
            runtime.Changed += HandleRuntimeChanged;
        }

        private void OnDestroy()
        {
            if (runtime != null) runtime.Changed -= HandleRuntimeChanged;
        }

        private void HandleRuntimeChanged() => RefreshTracker(false);

        private void LoadPlayerDescriptions()
        {
            playerDescriptions.Clear();
            TextAsset[] assets = runtime.CsvAssets;
            TextAsset questAsset = assets?.FirstOrDefault(a => a != null && string.Equals(a.name, "Quests", StringComparison.OrdinalIgnoreCase));
            if (questAsset == null) return;

            foreach (Dictionary<string, string> row in SandPlanetCsv04.Parse(questAsset.text))
            {
                if (!row.TryGetValue("QuestID", out string questId) || string.IsNullOrEmpty(questId)) continue;
                row.TryGetValue("PlayerDescription", out string description);
                if (string.IsNullOrWhiteSpace(description)) row.TryGetValue("Summary", out description);
                if (!string.IsNullOrWhiteSpace(description)) playerDescriptions[questId] = description.Trim();
            }
        }

        private void BuildQuestRowsRoot(Transform parent)
        {
            Transform existing = parent.Find("UX17_QuestRows");
            if (existing != null)
            {
                rowsRoot = existing as RectTransform;
                return;
            }

            GameObject root = new GameObject("UX17_QuestRows", typeof(RectTransform), typeof(VerticalLayoutGroup));
            root.transform.SetParent(parent, false);
            rowsRoot = root.GetComponent<RectTransform>();
            rowsRoot.anchorMin = new Vector2(.035f, .05f);
            rowsRoot.anchorMax = new Vector2(.965f, .95f);
            rowsRoot.offsetMin = Vector2.zero;
            rowsRoot.offsetMax = Vector2.zero;

            VerticalLayoutGroup layout = root.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.spacing = 7f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;
        }

        private void BuildHoverCard(Transform questPanel)
        {
            Transform existing = questPanel.Find("UX17_QuestHoverCard");
            if (existing != null)
            {
                hoverCard = existing.gameObject;
                hoverTitle = FindNamedText(existing, "Title");
                hoverDescription = FindNamedText(existing, "Description");
                hoverObjective = FindNamedText(existing, "Objective");
                hoverCard.SetActive(false);
                return;
            }

            hoverCard = new GameObject("UX17_QuestHoverCard", typeof(RectTransform), typeof(Image));
            hoverCard.transform.SetParent(questPanel, false);
            RectTransform cardRect = hoverCard.GetComponent<RectTransform>();
            cardRect.anchorMin = Vector2.one;
            cardRect.anchorMax = Vector2.one;
            cardRect.pivot = new Vector2(0f, 1f);
            cardRect.anchoredPosition = new Vector2(12f, 0f);
            cardRect.sizeDelta = new Vector2(440f, 220f);

            Image image = hoverCard.GetComponent<Image>();
            image.color = new Color(.035f, .045f, .055f, .98f);
            image.raycastTarget = false;

            Font font = baseTracker.font != null ? baseTracker.font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            hoverTitle = MakeText(hoverCard.transform, "Title", font, 21, FontStyle.Bold, TextAnchor.UpperLeft);
            SetRect(hoverTitle.rectTransform, new Vector2(.05f, .72f), new Vector2(.95f, .94f));

            hoverDescription = MakeText(hoverCard.transform, "Description", font, 16, FontStyle.Normal, TextAnchor.UpperLeft);
            hoverDescription.color = new Color(.91f, .93f, .95f, 1f);
            hoverDescription.lineSpacing = 1.08f;
            SetRect(hoverDescription.rectTransform, new Vector2(.05f, .31f), new Vector2(.95f, .72f));

            hoverObjective = MakeText(hoverCard.transform, "Objective", font, 15, FontStyle.Normal, TextAnchor.UpperLeft);
            hoverObjective.color = new Color(.76f, .82f, .87f, 1f);
            SetRect(hoverObjective.rectTransform, new Vector2(.05f, .07f), new Vector2(.95f, .29f));

            hoverCard.SetActive(false);
        }

        private void RefreshTracker(bool force)
        {
            IReadOnlyDictionary<string, string> status = runtime.QuestStatuses;
            IReadOnlyDictionary<string, string> steps = runtime.QuestSteps;
            IReadOnlyDictionary<string, string> states = runtime.States;

            List<SandPlanetQuest04> active = content.Quests.Values
                .Where(q => q.Active && q.TrackerVisible && status.TryGetValue(q.Id, out string s) && string.Equals(s, "ACTIVE", StringComparison.OrdinalIgnoreCase))
                .OrderBy(q => TypeOrder(q.Type))
                .ThenBy(q => q.LogOrder)
                .ThenBy(q => q.Title)
                .ToList();

            string signature = string.Join("|", active.Select(q => q.Id + ":" + (steps.TryGetValue(q.Id, out string stepId) ? stepId : string.Empty)));
            if (active.Any(q => q.Id == IntroQuestId))
            {
                foreach (Entry entry in ReunionEntries)
                    signature += ";" + entry.StateId + "=" + (states.TryGetValue(entry.StateId, out string value) ? value : string.Empty);
            }

            if (!force && string.Equals(signature, lastSignature, StringComparison.Ordinal)) return;
            lastSignature = signature;
            RebuildRows(active, steps, states);

            if (!string.IsNullOrEmpty(currentHoverQuestId) && active.All(q => q.Id != currentHoverQuestId))
                HideQuest(currentHoverQuestId);
        }

        private void RebuildRows(List<SandPlanetQuest04> active, IReadOnlyDictionary<string, string> steps, IReadOnlyDictionary<string, string> states)
        {
            for (int i = rowsRoot.childCount - 1; i >= 0; i--)
                Destroy(rowsRoot.GetChild(i).gameObject);

            Font font = baseTracker.font != null ? baseTracker.font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Text header = MakeText(rowsRoot, "Header", font, 16, FontStyle.Bold, TextAnchor.MiddleLeft);
            header.text = "[진행 중 Quest]";
            header.color = Color.white;
            LayoutElement headerLayout = header.gameObject.AddComponent<LayoutElement>();
            headerLayout.preferredHeight = 25f;

            if (active.Count == 0)
            {
                Text none = MakeText(rowsRoot, "None", font, 15, FontStyle.Normal, TextAnchor.MiddleLeft);
                none.text = "• 없음";
                none.color = new Color(.68f, .72f, .76f, 1f);
                none.gameObject.AddComponent<LayoutElement>().preferredHeight = 28f;
                return;
            }

            foreach (SandPlanetQuest04 quest in active)
            {
                string stepId = steps.TryGetValue(quest.Id, out string sid) ? sid : string.Empty;
                string tracker = content.QuestSteps.TryGetValue(stepId, out SandPlanetQuestStep04 step) ? step.TrackerText : string.Empty;
                CreateQuestRow(quest, tracker, font);

                if (quest.Id == IntroQuestId)
                    CreateReunionChecklist(states, font);
            }
        }

        private void CreateQuestRow(SandPlanetQuest04 quest, string tracker, Font font)
        {
            GameObject row = new GameObject("QuestRow_" + quest.Id, typeof(RectTransform), typeof(Image), typeof(LayoutElement), typeof(QuestHoverBinding));
            row.transform.SetParent(rowsRoot, false);

            Color color = QuestColor(quest.Type);
            Image image = row.GetComponent<Image>();
            image.color = new Color(color.r, color.g, color.b, .12f);
            image.raycastTarget = true;

            LayoutElement layout = row.GetComponent<LayoutElement>();
            layout.preferredHeight = string.IsNullOrEmpty(tracker) ? 42f : 58f;

            GameObject stripe = new GameObject("Stripe", typeof(RectTransform), typeof(Image));
            stripe.transform.SetParent(row.transform, false);
            RectTransform sr = stripe.GetComponent<RectTransform>();
            sr.anchorMin = new Vector2(0f, 0f);
            sr.anchorMax = new Vector2(0f, 1f);
            sr.pivot = new Vector2(0f, .5f);
            sr.anchoredPosition = Vector2.zero;
            sr.sizeDelta = new Vector2(5f, 0f);
            Image stripeImage = stripe.GetComponent<Image>();
            stripeImage.color = color;
            stripeImage.raycastTarget = false;

            Text label = MakeText(row.transform, "Label", font, 15, FontStyle.Normal, TextAnchor.MiddleLeft);
            SetRect(label.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(12f, 4f), new Vector2(-8f, -4f));
            string colorHex = ColorUtility.ToHtmlStringRGB(color);
            label.text = $"<color=#{colorHex}><b>{quest.Title}</b></color>" +
                         (string.IsNullOrEmpty(tracker) ? string.Empty : $"\n<size=13><color=#C5CDD4>{tracker}</color></size>");

            QuestHoverBinding binding = row.GetComponent<QuestHoverBinding>();
            binding.Owner = this;
            binding.QuestId = quest.Id;
        }

        private void CreateReunionChecklist(IReadOnlyDictionary<string, string> states, Font font)
        {
            GameObject block = new GameObject("ReunionChecklist", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            block.transform.SetParent(rowsRoot, false);
            Image bg = block.GetComponent<Image>();
            bg.color = new Color(.06f, .075f, .09f, .72f);
            bg.raycastTarget = false;
            block.GetComponent<LayoutElement>().preferredHeight = 120f;

            Text checklist = MakeText(block.transform, "Checklist", font, 14, FontStyle.Normal, TextAnchor.UpperLeft);
            SetRect(checklist.rectTransform, Vector2.zero, Vector2.one, new Vector2(12f, 8f), new Vector2(-8f, -8f));
            checklist.lineSpacing = 1.04f;

            string text = "<color=#8EC5E8>다시 만날 사람들</color>\n";
            foreach (Entry entry in ReunionEntries)
            {
                bool met = states.TryGetValue(entry.StateId, out string raw) && string.Equals(raw, "TRUE", StringComparison.OrdinalIgnoreCase);
                text += met
                    ? $"<color=#69C77C><b>✓</b></color> {entry.Name}   "
                    : $"<color=#AAB4BE>□</color> {entry.Name}   ";
            }
            checklist.text = text.TrimEnd();
        }

        public void ShowQuest(string questId)
        {
            if (hoverCard == null || string.IsNullOrEmpty(questId) || !content.Quests.TryGetValue(questId, out SandPlanetQuest04 quest)) return;
            currentHoverQuestId = questId;

            IReadOnlyDictionary<string, string> steps = runtime.QuestSteps;
            string stepId = steps.TryGetValue(questId, out string sid) ? sid : string.Empty;
            string objective = content.QuestSteps.TryGetValue(stepId, out SandPlanetQuestStep04 step) ? step.TrackerText : string.Empty;
            string description = playerDescriptions.TryGetValue(questId, out string value) && !string.IsNullOrWhiteSpace(value)
                ? value
                : quest.Summary;

            hoverTitle.color = QuestColor(quest.Type);
            hoverTitle.text = quest.Title;
            hoverDescription.text = string.IsNullOrWhiteSpace(description) ? "설명이 아직 작성되지 않았습니다." : description;
            hoverObjective.text = string.IsNullOrWhiteSpace(objective)
                ? "<b>현재 목표</b>\n현재 단계의 목표를 확인 중입니다."
                : "<b>현재 목표</b>\n" + objective;
            hoverCard.SetActive(true);
            hoverCard.transform.SetAsLastSibling();
            navigation.NotifyOverlayChanged();
        }

        public void HideQuest(string questId)
        {
            if (hoverCard == null) return;
            if (!string.IsNullOrEmpty(questId) && !string.Equals(currentHoverQuestId, questId, StringComparison.Ordinal)) return;
            currentHoverQuestId = string.Empty;
            hoverCard.SetActive(false);
            navigation.NotifyOverlayChanged();
        }

        public void CloseDetailFromNavigation() => HideQuest(currentHoverQuestId);

        private static int TypeOrder(string type)
        {
            if (string.Equals(type, "MAIN", StringComparison.OrdinalIgnoreCase)) return 0;
            if (string.Equals(type, "CHARACTER", StringComparison.OrdinalIgnoreCase)) return 1;
            return 2;
        }

        private static Color QuestColor(string type)
        {
            string hex = string.Equals(type, "MAIN", StringComparison.OrdinalIgnoreCase)
                ? MainColor
                : string.Equals(type, "CHARACTER", StringComparison.OrdinalIgnoreCase)
                    ? CharacterColor
                    : SideColor;
            return ColorUtility.TryParseHtmlString(hex, out Color color) ? color : Color.white;
        }

        private static Text MakeText(Transform parent, string name, Font font, int fontSize, FontStyle style, TextAnchor alignment)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            Text text = go.GetComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.color = Color.white;
            text.alignment = alignment;
            text.supportRichText = true;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        private static Text FindNamedText(Transform root, string name)
        {
            Transform child = root.Find(name);
            return child != null ? child.GetComponent<Text>() : null;
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
        {
            SetRect(rect, anchorMin, anchorMax, Vector2.zero, Vector2.zero);
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
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

    public sealed class QuestHoverBinding : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public SandPlanetPrototype04W1QuestTracker Owner;
        public string QuestId;

        public void OnPointerEnter(PointerEventData eventData)
        {
            Owner?.ShowQuest(QuestId);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            Owner?.HideQuest(QuestId);
        }
    }
}
