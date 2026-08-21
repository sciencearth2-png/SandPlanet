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
    /// Prototype 0.4 convenience/UI layer.
    /// It intentionally leaves the CSV-driven game controller unchanged.
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
        private MethodInfo getCharacterLocationMethod;

        private Canvas canvas;
        private Text hudText;
        private Text contextText;
        private Button endDayButton;
        private Button backButton;
        private Transform targetRoot;
        private Transform modalButtonRoot;

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
            modalButtonRoot = FindNamed<Transform>(canvas.transform, "ModalButtons");

            RestyleHudAndPanels();
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
            RefreshHud();

            if (Time.unscaledTime < nextRefreshTime) return;
            nextRefreshTime = Time.unscaledTime + 0.08f;
            RefreshAll(false);
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
            getCharacterLocationMethod = controllerType.GetMethod("GetCharacterLocation", PrivateInstance);
        }

        private void RestyleHudAndPanels()
        {
            RectTransform hudPanel = GetRect("HUDPanel");
            if (hudPanel != null)
            {
                hudPanel.anchorMin = new Vector2(0f, 1f);
                hudPanel.anchorMax = new Vector2(1f, 1f);
                hudPanel.pivot = new Vector2(.5f, 1f);
                hudPanel.anchoredPosition = Vector2.zero;
                hudPanel.sizeDelta = new Vector2(0f, 104f);
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
                hudText.supportRichText = true;
            }

            if (endDayButton != null)
            {
                RectTransform r = endDayButton.GetComponent<RectTransform>();
                r.anchorMin = r.anchorMax = r.pivot = Vector2.one;
                r.anchoredPosition = new Vector2(-18f, -13f);
                r.sizeDelta = new Vector2(165f, 46f);
                SetButtonText(endDayButton, "하루 종료");
            }

            if (backButton != null)
            {
                backButton.GetComponent<RectTransform>().sizeDelta = new Vector2(165f, 48f);
                SetButtonText(backButton, "허브로  [ESC]");
            }

            RectTransform quest = GetRect("QuestTrackerPanel");
            if (quest != null)
                quest.anchoredPosition = new Vector2(16f, -118f);

            RectTransform location = GetRect("LocationPanel");
            if (location != null)
            {
                location.anchorMin = new Vector2(.53f, .06f);
                location.anchorMax = new Vector2(.99f, .88f);
            }
        }

        private void CreateContextCard()
        {
            Transform existing = FindChildRecursive(canvas.transform, "UX04_ContextCard");
            if (existing != null)
            {
                contextText = existing.GetComponentInChildren<Text>(true);
                return;
            }

            GameObject card = new GameObject("UX04_ContextCard", typeof(RectTransform), typeof(Image));
            card.transform.SetParent(canvas.transform, false);
            RectTransform r = card.GetComponent<RectTransform>();
            r.anchorMin = r.anchorMax = r.pivot = Vector2.one;
            r.anchoredPosition = new Vector2(-198f, -13f);
            r.sizeDelta = new Vector2(350f, 78f);
            Image image = card.GetComponent<Image>();
            image.color = new Color(.07f, .085f, .10f, .96f);
            image.raycastTarget = false;

            GameObject textGo = new GameObject("ContextText", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(card.transform, false);
            contextText = textGo.GetComponent<Text>();
            contextText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            contextText.fontSize = 15;
            contextText.color = Color.white;
            contextText.alignment = TextAnchor.MiddleLeft;
            contextText.supportRichText = true;
            contextText.raycastTarget = false;
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
                selectedTargetType = null;
                selectedTargetId = null;
            }

            RefreshTargetBadges(locationId);
            RefreshContext(locationId);
            SetButtonText(endDayButton, "하루 종료");
            SetButtonText(backButton, "허브로  [ESC]");
        }

        private void RefreshHud()
        {
            if (hudText == null) return;

            int day = GetInt(dayField);
            int hour = GetInt(hourField);
            string slot = hour < 12 ? "오전" : hour < 17 ? "오후" : "저녁";
            string world = day >= 15 ? "수송선 내부" : "모래 행성";

            hudText.text =
                $"DAY {day:00} / 21     {hour:00}:00     의지 {GetInt(willField)}/{GetInt(maxWillField)}     시간대 {slot}     <color=#B7C2CC>{world}</color>\n" +
                $"개인 Lv{GetInt(personalLevelField)}  {GetInt(personalXpField)}/6        " +
                $"대인 Lv{GetInt(socialLevelField)}  {GetInt(socialXpField)}/6        " +
                $"기술 Lv{GetInt(technicalLevelField)}  {GetInt(technicalXpField)}/6        <color=#9AA6B2>6XP → 즉시 Lv +1</color>";
        }

        private void RefreshTargetBadges(string locationId)
        {
            if (targetRoot == null || string.IsNullOrEmpty(locationId) || content == null) return;

            List<TargetInfo> expected = new List<TargetInfo>();
            expected.AddRange(content.Characters.Values
                .Where(c => c.Active && GetActualCharacterLocation(c.Id) == locationId)
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
                Button button = child.GetComponent<Button>();
                if (button != null && child.gameObject.activeSelf)
                    buttons.Add(button);
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
                    SandPlanetPrototype04UxTargetBinding captured = binding;
                    button.onClick.AddListener(() =>
                    {
                        selectedTargetType = captured.TargetType;
                        selectedTargetId = captured.TargetId;
                        RefreshContext(GetString(currentLocationField));
                    });
                }

                binding.TargetType = info.Type;
                binding.TargetId = info.Id;
                binding.TargetName = info.Name;

                Text text = button.GetComponentInChildren<Text>(true);
                if (text == null) continue;
                text.supportRichText = true;
                text.text = $"{info.Kind}  {info.Name}   {BuildBadgeText(info.Type, info.Id)}";
            }
        }

        private string GetActualCharacterLocation(string characterId)
        {
            if (getCharacterLocationMethod == null) return string.Empty;
            try { return getCharacterLocationMethod.Invoke(controller, new object[] { characterId }) as string ?? string.Empty; }
            catch { return string.Empty; }
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
            List<SandPlanetInteraction04> result = new List<SandPlanetInteraction04>();
            if (content == null || isInteractionAvailableMethod == null) return result;

            foreach (SandPlanetInteraction04 interaction in content.Interactions)
            {
                if (!interaction.Active || interaction.TargetType != targetType || interaction.TargetId != targetId) continue;
                try
                {
                    if ((bool)isInteractionAvailableMethod.Invoke(controller, new object[] { interaction }))
                        result.Add(interaction);
                }
                catch { }
            }
            return result;
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
                contextText.text = $"<b>{locationName}</b>\n대상 {CountTargets(locationId)}개 · 활성 Quest 대상 {CountQuestTargets(locationId)}개";
                return;
            }

            string targetName = GetTargetName(selectedTargetType, selectedTargetId);
            string kind = selectedTargetType == "CHARACTER" ? "인물" : "사물";
            List<SandPlanetQuest04> quests = GetAvailableInteractions(selectedTargetType, selectedTargetId)
                .Where(i => i.InteractionType == "QUEST" && !string.IsNullOrEmpty(i.QuestId) && content.Quests.ContainsKey(i.QuestId))
                .Select(i => content.Quests[i.QuestId])
                .GroupBy(q => q.Id)
                .Select(g => g.First())
                .OrderBy(q => QuestTypeOrder(q.Type))
                .ThenBy(q => q.Title)
                .Take(2)
                .ToList();

            string questText = quests.Count == 0
                ? "활성 Quest 없음"
                : string.Join("   ", quests.Select(q => Badge(q.Type) + " " + q.Title));

            contextText.text = $"<b>{locationName}  ›  {targetName}</b>  <color=#AAB4BE>{kind}</color>\n{questText}";
        }

        private string ActiveMainQuestTitle()
        {
            if (getQuestStatusMethod == null || content == null) return string.Empty;
            foreach (SandPlanetQuest04 quest in content.Quests.Values.Where(q => q.Type == "MAIN").OrderBy(q => q.LogOrder).ThenBy(q => q.Title))
            {
                try
                {
                    if ((getQuestStatusMethod.Invoke(controller, new object[] { quest.Id }) as string) == "ACTIVE")
                        return quest.Title;
                }
                catch { }
            }
            return string.Empty;
        }

        private int CountTargets(string locationId)
        {
            int characters = content.Characters.Values.Count(c => c.Active && GetActualCharacterLocation(c.Id) == locationId);
            int objects = content.WorldTargets.Values.Count(w => w.Active && w.Clickable && w.LocationId == locationId);
            return characters + objects;
        }

        private int CountQuestTargets(string locationId)
        {
            int count = 0;
            foreach (SandPlanetCharacter04 c in content.Characters.Values.Where(c => c.Active && GetActualCharacterLocation(c.Id) == locationId))
                if (GetAvailableInteractions("CHARACTER", c.Id).Any(i => i.InteractionType == "QUEST")) count++;
            foreach (SandPlanetWorldTarget04 w in content.WorldTargets.Values.Where(w => w.Active && w.Clickable && w.LocationId == locationId))
                if (GetAvailableInteractions("WORLD_TARGET", w.Id).Any(i => i.InteractionType == "QUEST")) count++;
            return count;
        }

        private void HandleEscape()
        {
            bool modalBusy = modalBusyField != null && (bool)modalBusyField.GetValue(controller);
            if (modalBusy)
            {
                // Forced Event choices deliberately have a disabled Cancel button. Do not let ESC bypass them.
                if (IsForcedEventChoice()) return;
                if (closeModalMethod != null) closeModalMethod.Invoke(controller, null);
                return;
            }

            string locationId = GetString(currentLocationField);
            if (!string.IsNullOrEmpty(locationId) && closeLocationMethod != null)
            {
                selectedTargetType = null;
                selectedTargetId = null;
                closeLocationMethod.Invoke(controller, null);
            }
        }

        private bool IsForcedEventChoice()
        {
            if (modalButtonRoot == null) return false;
            foreach (Button button in modalButtonRoot.GetComponentsInChildren<Button>(true))
            {
                Text text = button.GetComponentInChildren<Text>(true);
                if (text != null && text.text == "취소" && !button.interactable)
                    return true;
            }
            return false;
        }

        private string GetTargetName(string type, string id)
        {
            if (type == "CHARACTER" && content.Characters.TryGetValue(id, out SandPlanetCharacter04 c)) return c.Name;
            if (type == "WORLD_TARGET" && content.WorldTargets.TryGetValue(id, out SandPlanetWorldTarget04 w)) return w.Name;
            return id;
        }

        private static int QuestTypeOrder(string type) => type == "MAIN" ? 0 : type == "CHARACTER" ? 1 : 2;

        private static string Badge(string type)
        {
            if (type == "MAIN") return "<color=#F0B15E>[MAIN]</color>";
            if (type == "CHARACTER") return "<color=#74C0FC>[CHAR]</color>";
            return "<color=#91C788>[SUB]</color>";
        }

        private int GetInt(FieldInfo field)
        {
            if (field == null) return 0;
            object value = field.GetValue(controller);
            return value is int i ? i : 0;
        }

        private string GetString(FieldInfo field) => field != null ? field.GetValue(controller) as string : null;

        private RectTransform GetRect(string name)
        {
            Transform t = FindChildRecursive(canvas.transform, name);
            return t != null ? t.GetComponent<RectTransform>() : null;
        }

        private static void SetButtonText(Button button, string textValue)
        {
            if (button == null) return;
            Text text = button.GetComponentInChildren<Text>(true);
            if (text == null) return;
            text.text = textValue;
            text.color = Color.white;
            text.fontSize = 17;
            text.alignment = TextAnchor.MiddleCenter;
            text.supportRichText = true;
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
    }
}
