using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SandPlanet.Prototype.DataDriven;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SandPlanet.Prototype
{
    /// <summary>
    /// Runtime UX layer for Prototype 0.4.
    /// Keeps the CSV/data-driven controller untouched while improving the generated scene UI:
    /// - two-row HUD similar to Prototype 0.3
    /// - always-visible context card
    /// - MAIN / CHAR / SUB badges on target buttons
    /// - ESC closes the top-most modal or returns from a location to the hub
    /// </summary>
    public sealed class SandPlanetPrototype04UxEnhancer : MonoBehaviour
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

        private SandPlanetPrototype04Controller controller;
        private SandPlanetContent04 content;
        private Type controllerType;

        private FieldInfo contentField;
        private FieldInfo dayField;
        private FieldInfo hourField;
        private FieldInfo willField;
        private FieldInfo maxWillField;
        private FieldInfo personalLevelField;
        private FieldInfo personalXpField;
        private FieldInfo socialLevelField;
        private FieldInfo socialXpField;
        private FieldInfo technicalLevelField;
        private FieldInfo technicalXpField;
        private FieldInfo currentLocationField;
        private FieldInfo modalBusyField;

        private MethodInfo isInteractionAvailableMethod;
        private MethodInfo closeModalMethod;
        private MethodInfo closeLocationMethod;
        private MethodInfo getQuestStatusMethod;

        private Canvas canvas;
        private Text hudText;
        private Text contextText;
        private Button endDayButton;
        private Button backButton;
        private Transform targetRoot;
        private RectTransform questPanelRect;
        private RectTransform locationPanelRect;

        private string selectedTargetType;
        private string selectedTargetId;
        private string lastLocationId;
        private float nextRefreshTime;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            SandPlanetPrototype04Controller found = UnityEngine.Object.FindFirstObjectByType<SandPlanetPrototype04Controller>();
            if (found != null && found.GetComponent<SandPlanetPrototype04UxEnhancer>() == null)
                found.gameObject.AddComponent<SandPlanetPrototype04UxEnhancer>();
        }

        private void Awake()
        {
            controller = GetComponent<SandPlanetPrototype04Controller>();
            if (controller == null)
            {
                enabled = false;
                return;
            }

            controllerType = controller.GetType();
            CacheReflection();
        }

        private void Start()
        {
            content = contentField != null ? contentField.GetValue(controller) as SandPlanetContent04 : null;
            canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                enabled = false;
                return;
            }

            hudText = FindNamed<Text>(canvas.transform, "HUD");
            endDayButton = FindNamed<Button>(canvas.transform, "EndDay");
            backButton = FindNamed<Button>(canvas.transform, "Back");
            targetRoot = FindNamed<Transform>(canvas.transform, "TargetButtons");

            Transform questPanel = FindChildRecursive(canvas.transform, "QuestTrackerPanel");
            questPanelRect = questPanel != null ? questPanel.GetComponent<RectTransform>() : null;
            Transform locationPanel = FindChildRecursive(canvas.transform, "LocationPanel");
            locationPanelRect = locationPanel != null ? locationPanel.GetComponent<RectTransform>() : null;

            RestyleExistingUi();
            CreateContextCard();
            RefreshAll(true);
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                HandleEscape();
        }

        private void LateUpdate()
        {
            // The data controller refreshes its HUD during gameplay; overwrite it after that refresh.
            RefreshHud();

            if (Time.unscaledTime >= nextRefreshTime)
            {
                nextRefreshTime = Time.unscaledTime + 0.08f;
                RefreshAll(false);
            }
        }

        private void CacheReflection()
        {
            contentField = controllerType.GetField("content", PrivateInstance);
            dayField = controllerType.GetField("day", PrivateInstance);
            hourField = controllerType.GetField("hour", PrivateInstance);
            willField = controllerType.GetField("will", PrivateInstance);
            maxWillField = controllerType.GetField("maxWill", PrivateInstance);
            personalLevelField = controllerType.GetField("personalLevel", PrivateInstance);
            personalXpField = controllerType.GetField("personalXp", PrivateInstance);
            socialLevelField = controllerType.GetField("socialLevel", PrivateInstance);
            socialXpField = controllerType.GetField("socialXp", PrivateInstance);
            technicalLevelField = controllerType.GetField("technicalLevel", PrivateInstance);
            technicalXpField = controllerType.GetField("technicalXp", PrivateInstance);
            currentLocationField = controllerType.GetField("currentLocationId", PrivateInstance);
            modalBusyField = controllerType.GetField("modalBusy", PrivateInstance);

            isInteractionAvailableMethod = controllerType.GetMethod("IsInteractionAvailable", PrivateInstance);
            closeModalMethod = controllerType.GetMethod("CloseModal", PrivateInstance);
            closeLocationMethod = controllerType.GetMethod("CloseLocation", PrivateInstance);
            getQuestStatusMethod = controllerType.GetMethod("GetQuestStatus", PrivateInstance);
        }

        private void RestyleExistingUi()
        {
            Transform hudPanel = FindChildRecursive(canvas.transform, "HUDPanel");
            if (hudPanel != null)
            {
                RectTransform r = hudPanel.GetComponent<RectTransform>();
                r.anchorMin = new Vector2(0f, 1f);
                r.anchorMax = new Vector2(1f, 1f);
                r.pivot = new Vector2(.5f, 1f);
                r.anchoredPosition = Vector2.zero;
                r.sizeDelta = new Vector2(0f, 104f);
            }

            if (hudText != null)
            {
                RectTransform r = hudText.rectTransform;
                r.anchorMin = Vector2.zero;
                r.anchorMax = Vector2.one;
                r.offsetMin = new Vector2(18f, 8f);
                r.offsetMax = new Vector2(-570f, -8f);
                hudText.fontSize = 19;
                hudText.alignment = TextAnchor.MiddleLeft;
                hudText.lineSpacing = 1.05f;
            }

            if (endDayButton != null)
            {
                RectTransform r = endDayButton.GetComponent<RectTransform>();
                r.anchorMin = r.anchorMax = r.pivot = Vector2.one;
                r.anchoredPosition = new Vector2(-18f, -13f);
                r.sizeDelta = new Vector2(165f, 46f);
                ForceButtonLabel(endDayButton, "하루 종료");
            }

            if (backButton != null)
            {
                ForceButtonLabel(backButton, "허브로  [ESC]");
                RectTransform r = backButton.GetComponent<RectTransform>();
                r.sizeDelta = new Vector2(165f, 48f);
            }

            if (questPanelRect != null)
                questPanelRect.anchoredPosition = new Vector2(16f, -118f);

            // Leave more breathing room below the new two-line HUD.
            if (locationPanelRect != null)
            {
                locationPanelRect.anchorMax = new Vector2(.99f, .88f);
                locationPanelRect.anchorMin = new Vector2(.53f, .06f);
            }
        }

        private void CreateContextCard()
        {
            Transform old = FindChildRecursive(canvas.transform, "UX04_ContextCard");
            if (old != null)
            {
                contextText = old.GetComponentInChildren<Text>(true);
                return;
            }

            GameObject card = new GameObject("UX04_ContextCard", typeof(RectTransform), typeof(Image));
            card.transform.SetParent(canvas.transform, false);
            RectTransform r = card.GetComponent<RectTransform>();
            r.anchorMin = r.anchorMax = r.pivot = Vector2.one;
            r.anchoredPosition = new Vector2(-198f, -13f);
            r.sizeDelta = new Vector2(350f, 78f);
            card.GetComponent<Image>().color = new Color(.07f, .085f, .10f, .96f);

            GameObject textObject = new GameObject("ContextText", typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(card.transform, false);
            contextText = textObject.GetComponent<Text>();
            contextText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            contextText.fontSize = 15;
            contextText.color = Color.white;
            contextText.alignment = TextAnchor.MiddleLeft;
            contextText.supportRichText = true;
            RectTransform tr = contextText.rectTransform;
            tr.anchorMin = Vector2.zero;
            tr.anchorMax = Vector2.one;
            tr.offsetMin = new Vector2(12f, 7f);
            tr.offsetMax = new Vector2(-12f, -7f);
        }

        private void RefreshAll(bool force)
        {
            if (content == null && contentField != null)
                content = contentField.GetValue(controller) as SandPlanetContent04;
            if (content == null) return;

            string locationId = GetString(currentLocationField);
            if (force || !string.Equals(locationId, lastLocationId, StringComparison.Ordinal))
            {
                lastLocationId = locationId;
                selectedTargetId = null;
                selectedTargetType = null;
            }

            RefreshTargetBadges(locationId);
            RefreshContext(locationId);
            RefreshButtonLabels();
        }

        private void RefreshHud()
        {
            if (hudText == null) return;

            int day = GetInt(dayField);
            int hour = GetInt(hourField);
            int will = GetInt(willField);
            int maxWill = GetInt(maxWillField);
            int pLv = GetInt(personalLevelField);
            int pXp = GetInt(personalXpField);
            int sLv = GetInt(socialLevelField);
            int sXp = GetInt(socialXpField);
            int tLv = GetInt(technicalLevelField);
            int tXp = GetInt(technicalXpField);

            string slot = hour < 12 ? "오전" : hour < 17 ? "오후" : "저녁";
            string world = day >= 15 ? "수송선 내부" : "모래 행성";

            hudText.text =
                $"DAY {day:00} / 21     {hour:00}:00     의지 {will}/{maxWill}     시간대 {slot}     <color=#B7C2CC>{world}</color>\n" +
                $"개인 Lv{pLv}  {pXp}/6        대인 Lv{sLv}  {sXp}/6        기술 Lv{tLv}  {tXp}/6        <color=#9AA6B2>6XP → 즉시 Lv +1</color>";
            hudText.supportRichText = true;
        }

        private void RefreshButtonLabels()
        {
            if (endDayButton != null) ForceButtonLabel(endDayButton, "하루 종료");
            if (backButton != null) ForceButtonLabel(backButton, "허브로  [ESC]");
        }

        private void RefreshTargetBadges(string locationId)
        {
            if (targetRoot == null || string.IsNullOrEmpty(locationId) || content == null) return;

            List<TargetInfo> expected = new List<TargetInfo>();
            expected.AddRange(content.Characters.Values
                .Where(c => c.Active && string.Equals(GetCharacterLocation(c), locationId, StringComparison.Ordinal))
                .OrderBy(c => c.Name)
                .Select(c => new TargetInfo("CHARACTER", c.Id, c.Name, "인물")));
            expected.AddRange(content.WorldTargets.Values
                .Where(w => w.Active && w.Clickable && w.LocationId == locationId)
                .OrderBy(w => w.Name)
                .Select(w => new TargetInfo("WORLD_TARGET", w.Id, w.Name, "사물")));

            List<Button> buttons = new List<Button>();
            for (int i = 0; i < targetRoot.childCount; i++)
            {
                Transform child = targetRoot.GetChild(i);
                Button b = child.GetComponent<Button>();
                if (b != null && child.gameObject.activeSelf)
                    buttons.Add(b);
            }

            int count = Mathf.Min(buttons.Count, expected.Count);
            for (int i = 0; i < count; i++)
            {
                Button button = buttons[i];
                TargetInfo info = expected[i];
                SandPlanetPrototype04UxTargetBinding binding = button.GetComponent<SandPlanetPrototype04UxTargetBinding>();
                if (binding == null)
                {
                    binding = button.gameObject.AddComponent<SandPlanetPrototype04UxTargetBinding>();
                    binding.TargetType = info.Type;
                    binding.TargetId = info.Id;
                    binding.TargetName = info.Name;
                    binding.KindLabel = info.Kind;
                    SandPlanetPrototype04UxTargetBinding captured = binding;
                    button.onClick.AddListener(() =>
                    {
                        selectedTargetType = captured.TargetType;
                        selectedTargetId = captured.TargetId;
                        RefreshContext(GetString(currentLocationField));
                    });
                }

                Text text = button.GetComponentInChildren<Text>(true);
                if (text == null) continue;
                text.supportRichText = true;
                text.text = $"{info.Kind}  {info.Name}   {BuildBadgeText(info.Type, info.Id)}";
            }
        }

        private string GetCharacterLocation(SandPlanetCharacter04 character)
        {
            // For visual badges it is enough to follow the same schedule priority rules for the current day/time.
            int day = GetInt(dayField);
            int hour = GetInt(hourField);
            string best = character.DefaultLocationId;
            int bestPriority = int.MinValue;

            foreach (SandPlanetSchedule04 s in content.Schedules)
            {
                if (!s.Active || s.CharacterId != character.Id || day < s.OpenDay || day > s.CloseDay) continue;
                bool timeAllowed = hour < 12 ? s.Morning : hour < 17 ? s.Afternoon : s.Evening;
                if (!timeAllowed) continue;
                if (s.Priority >= bestPriority)
                {
                    bestPriority = s.Priority;
                    best = s.LocationId;
                }
            }
            return best;
        }

        private string BuildBadgeText(string targetType, string targetId)
        {
            List<SandPlanetInteraction04> available = GetAvailableInteractions(targetType, targetId);
            HashSet<string> types = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (SandPlanetInteraction04 i in available)
            {
                if (i.InteractionType != "QUEST" || string.IsNullOrEmpty(i.QuestId)) continue;
                if (content.Quests.TryGetValue(i.QuestId, out SandPlanetQuest04 quest))
                    types.Add(quest.Type ?? string.Empty);
            }

            List<string> badges = new List<string>();
            if (types.Contains("MAIN")) badges.Add("<color=#F0B15E>[MAIN]</color>");
            if (types.Contains("CHARACTER")) badges.Add("<color=#74C0FC>[CHAR]</color>");
            if (types.Contains("SIDE") || types.Contains("WORLD")) badges.Add("<color=#91C788>[SUB]</color>");
            if (badges.Count == 0 && available.Any(i => i.InteractionType == "BASIC"))
                badges.Add("<color=#AAB4BE>[일상]</color>");
            return string.Join("", badges);
        }

        private List<SandPlanetInteraction04> GetAvailableInteractions(string targetType, string targetId)
        {
            if (content == null || isInteractionAvailableMethod == null)
                return new List<SandPlanetInteraction04>();

            List<SandPlanetInteraction04> list = new List<SandPlanetInteraction04>();
            foreach (SandPlanetInteraction04 i in content.Interactions)
            {
                if (!i.Active || i.TargetType != targetType || i.TargetId != targetId) continue;
                try
                {
                    if ((bool)isInteractionAvailableMethod.Invoke(controller, new object[] { i }))
                        list.Add(i);
                }
                catch { }
            }
            return list;
        }

        private void RefreshContext(string locationId)
        {
            if (contextText == null || content == null) return;

            if (string.IsNullOrEmpty(locationId))
            {
                string hub = GetInt(dayField) >= 15 ? "수송선 내부 허브" : "행성 허브";
                string main = ActiveMainQuestTitle();
                contextText.text = $"<b>{hub}</b>\n현재 Main: {(string.IsNullOrEmpty(main) ? "없음" : main)}";
                return;
            }

            string locationName = content.Locations.TryGetValue(locationId, out SandPlanetLocation04 location) ? location.Name : locationId;
            if (string.IsNullOrEmpty(selectedTargetId))
            {
                int targetCount = CountTargetsInLocation(locationId);
                int questTargetCount = CountQuestTargetsInLocation(locationId);
                contextText.text = $"<b>{locationName}</b>\n대상 {targetCount}개 · 활성 Quest 대상 {questTargetCount}개";
                return;
            }

            string targetName = GetTargetName(selectedTargetType, selectedTargetId);
            string kind = selectedTargetType == "CHARACTER" ? "인물" : "사물";
            List<string> questLines = GetAvailableInteractions(selectedTargetType, selectedTargetId)
                .Where(i => i.InteractionType == "QUEST" && !string.IsNullOrEmpty(i.QuestId) && content.Quests.ContainsKey(i.QuestId))
                .Select(i => content.Quests[i.QuestId])
                .GroupBy(q => q.Id)
                .Select(g => g.First())
                .OrderBy(q => QuestTypeOrder(q.Type))
                .ThenBy(q => q.Title)
                .Take(2)
                .Select(q => BadgePlain(q.Type) + " " + q.Title)
                .ToList();

            contextText.text = $"<b>{locationName}  ›  {targetName}</b>  <color=#AAB4BE>{kind}</color>\n" +
                               (questLines.Count == 0 ? "활성 Quest 없음" : string.Join("   ", questLines));
        }

        private string ActiveMainQuestTitle()
        {
            if (content == null || getQuestStatusMethod == null) return string.Empty;
            foreach (SandPlanetQuest04 q in content.Quests.Values.Where(q => q.Type == "MAIN").OrderBy(q => q.LogOrder).ThenBy(q => q.Title))
            {
                try
                {
                    string status = getQuestStatusMethod.Invoke(controller, new object[] { q.Id }) as string;
                    if (status == "ACTIVE") return q.Title;
                }
                catch { }
            }
            return string.Empty;
        }

        private int CountTargetsInLocation(string locationId)
        {
            int chars = content.Characters.Values.Count(c => c.Active && GetCharacterLocation(c) == locationId);
            int worlds = content.WorldTargets.Values.Count(w => w.Active && w.Clickable && w.LocationId == locationId);
            return chars + worlds;
        }

        private int CountQuestTargetsInLocation(string locationId)
        {
            HashSet<string> keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (SandPlanetCharacter04 c in content.Characters.Values.Where(c => c.Active && GetCharacterLocation(c) == locationId))
            {
                if (GetAvailableInteractions("CHARACTER", c.Id).Any(i => i.InteractionType == "QUEST"))
                    keys.Add("C:" + c.Id);
            }
            foreach (SandPlanetWorldTarget04 w in content.WorldTargets.Values.Where(w => w.Active && w.Clickable && w.LocationId == locationId))
            {
                if (GetAvailableInteractions("WORLD_TARGET", w.Id).Any(i => i.InteractionType == "QUEST"))
                    keys.Add("W:" + w.Id);
            }
            return keys.Count;
        }

        private void HandleEscape()
        {
            bool modalBusy = modalBusyField != null && (bool)modalBusyField.GetValue(controller);
            if (modalBusy && closeModalMethod != null)
            {
                closeModalMethod.Invoke(controller, null);
                return;
            }

            string locationId = GetString(currentLocationField);
            if (!string.IsNullOrEmpty(locationId) && closeLocationMethod != null)
            {
                selectedTargetId = null;
                selectedTargetType = null;
                closeLocationMethod.Invoke(controller, null);
            }
        }

        private string GetTargetName(string type, string id)
        {
            if (type == "CHARACTER" && content.Characters.TryGetValue(id, out SandPlanetCharacter04 c)) return c.Name;
            if (type == "WORLD_TARGET" && content.WorldTargets.TryGetValue(id, out SandPlanetWorldTarget04 w)) return w.Name;
            return id;
        }

        private static int QuestTypeOrder(string type)
        {
            if (type == "MAIN") return 0;
            if (type == "CHARACTER") return 1;
            return 2;
        }

        private static string BadgePlain(string type)
        {
            if (type == "MAIN") return "<color=#F0B15E>[MAIN]</color>";
            if (type == "CHARACTER") return "<color=#74C0FC>[CHAR]</color>";
            return "<color=#91C788>[SUB]</color>";
        }

        private static void ForceButtonLabel(Button button, string label)
        {
            if (button == null) return;
            Text text = button.GetComponentInChildren<Text>(true);
            if (text == null) return;
            text.text = label;
            text.color = Color.white;
            text.fontSize = 17;
            text.alignment = TextAnchor.MiddleCenter;
            text.supportRichText = true;
        }

        private int GetInt(FieldInfo field)
        {
            if (field == null) return 0;
            object value = field.GetValue(controller);
            return value is int i ? i : 0;
        }

        private string GetString(FieldInfo field)
        {
            return field != null ? field.GetValue(controller) as string : null;
        }

        private static T FindNamed<T>(Transform root, string name) where T : Component
        {
            Transform t = FindChildRecursive(root, name);
            return t != null ? t.GetComponent<T>() : null;
        }

        private static Transform FindChildRecursive(Transform root, string name)
        {
            if (root == null) return null;
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindChildRecursive(root.GetChild(i), name);
                if (found != null) return found;
            }
            return null;
        }

        private readonly struct TargetInfo
        {
            public readonly string Type;
            public readonly string Id;
            public readonly string Name;
            public readonly string Kind;
            public TargetInfo(string type, string id, string name, string kind)
            {
                Type = type;
                Id = id;
                Name = name;
                Kind = kind;
            }
        }
    }

    public sealed class SandPlanetPrototype04UxTargetBinding : MonoBehaviour
    {
        public string TargetType;
        public string TargetId;
        public string TargetName;
        public string KindLabel;
    }
}
