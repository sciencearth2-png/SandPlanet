using System;
using System.Collections.Generic;
using System.Reflection;
using SandPlanet.Prototype.DataDriven;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SandPlanet.Prototype
{
    /// <summary>
    /// Always-available character overview for Prototype 0.4.
    /// Shows portrait, role, short known information, affinity and current leave/stay stance.
    /// Stance is qualitative on purpose: no hidden percentage is exposed.
    /// </summary>
    public sealed class SandPlanetPrototype04PeoplePanel : MonoBehaviour
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

        private static readonly string[] Order =
        {
            "CHA_BENJAMIN", "CHA_FAYE", "CHA_SAM",
            "CHA_JINA", "CHA_BORICHI", "CHA_DIYA"
        };

        private SandPlanetPrototype04Controller controller;
        private SandPlanetContent04 content;
        private FieldInfo contentField;
        private FieldInfo affinityField;
        private FieldInfo statesField;
        private FieldInfo eventsOccurredField;
        private FieldInfo modalBusyField;

        private Canvas canvas;
        private Button tabButton;
        private GameObject panel;
        private readonly Dictionary<string, CardView> cards = new Dictionary<string, CardView>(StringComparer.Ordinal);
        private float nextRefresh;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            SandPlanetPrototype04Controller found = UnityEngine.Object.FindFirstObjectByType<SandPlanetPrototype04Controller>();
            if (found != null && found.GetComponent<SandPlanetPrototype04PeoplePanel>() == null)
                found.gameObject.AddComponent<SandPlanetPrototype04PeoplePanel>();
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
            contentField = t.GetField("content", PrivateInstance);
            affinityField = t.GetField("affinity", PrivateInstance);
            statesField = t.GetField("states", PrivateInstance);
            eventsOccurredField = t.GetField("eventsOccurred", PrivateInstance);
            modalBusyField = t.GetField("modalBusy", PrivateInstance);
        }

        private void Start()
        {
            content = contentField?.GetValue(controller) as SandPlanetContent04;
            canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
            if (content == null || canvas == null)
            {
                enabled = false;
                return;
            }

            CreateTabButton();
            CreatePanel();
            SetOpen(false);
        }

        private void Update()
        {
            if (panel != null && panel.activeSelf &&
                Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                SetOpen(false);
            }
        }

        private void LateUpdate()
        {
            if (panel == null || !panel.activeSelf || Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + .20f;
            RefreshCards();
        }

        private void CreateTabButton()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            tabButton = MakeButton(canvas.transform, "UX04_PeopleTab", font, "인물", new Color(.20f, .29f, .34f));
            RectTransform r = tabButton.GetComponent<RectTransform>();
            r.anchorMin = r.anchorMax = r.pivot = Vector2.one;
            r.anchoredPosition = new Vector2(-18f, -68f);
            r.sizeDelta = new Vector2(165f, 40f);
            tabButton.onClick.AddListener(() => SetOpen(true));
            tabButton.transform.SetAsLastSibling();
        }

        private void CreatePanel()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            panel = Panel(canvas.transform, "UX04_PeoplePanel", new Color(.025f, .03f, .035f, .985f));
            RectTransform root = panel.GetComponent<RectTransform>();
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;

            Text title = MakeText(panel.transform, "Title", font, 34, TextAnchor.MiddleLeft);
            Anchor(title.rectTransform, new Vector2(.045f, .89f), new Vector2(.78f, .97f));
            title.text = "<b>인물</b>   <size=18><color=#AEB9C2>제이와 함께 출발했던 동료들</color></size>";

            Text hint = MakeText(panel.transform, "Hint", font, 16, TextAnchor.MiddleLeft);
            Anchor(hint.rectTransform, new Vector2(.045f, .845f), new Vector2(.82f, .895f));
            hint.color = new Color(.72f, .77f, .81f, 1f);
            hint.text = "호감도와 현재 미래 입장은 사건과 Character Quest에 따라 변한다. 입장은 수치 퍼센트가 아니라 현재 판단으로 표시한다.";

            Button close = MakeButton(panel.transform, "Close", font, "닫기  [ESC]", new Color(.24f, .28f, .32f));
            RectTransform closeRect = close.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(.84f, .90f);
            closeRect.anchorMax = new Vector2(.955f, .955f);
            closeRect.offsetMin = Vector2.zero;
            closeRect.offsetMax = Vector2.zero;
            close.onClick.AddListener(() => SetOpen(false));

            GameObject holder = new GameObject("Cards", typeof(RectTransform), typeof(GridLayoutGroup));
            holder.transform.SetParent(panel.transform, false);
            RectTransform hr = holder.GetComponent<RectTransform>();
            hr.anchorMin = new Vector2(.045f, .08f);
            hr.anchorMax = new Vector2(.955f, .83f);
            hr.offsetMin = Vector2.zero;
            hr.offsetMax = Vector2.zero;

            GridLayoutGroup grid = holder.GetComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;
            grid.spacing = new Vector2(18f, 18f);
            grid.padding = new RectOffset(0, 0, 0, 0);
            grid.childAlignment = TextAnchor.UpperCenter;

            float availableWidth = 1920f * .91f;
            grid.cellSize = new Vector2((availableWidth - 36f) / 3f, 335f);

            foreach (string id in Order)
            {
                if (!content.Characters.TryGetValue(id, out SandPlanetCharacter04 character) || !character.Active) continue;
                cards[id] = CreateCard(holder.transform, font, character);
            }

            panel.transform.SetAsLastSibling();
        }

        private CardView CreateCard(Transform parent, Font font, SandPlanetCharacter04 character)
        {
            GameObject card = Panel(parent, "Card_" + character.Id, new Color(.075f, .088f, .102f, 1f));
            CardView view = new CardView { CharacterId = character.Id, Root = card };

            GameObject portraitFrame = Panel(card.transform, "PortraitFrame", new Color(.12f, .13f, .14f, 1f));
            RectTransform pr = portraitFrame.GetComponent<RectTransform>();
            pr.anchorMin = new Vector2(.035f, .18f);
            pr.anchorMax = new Vector2(.31f, .94f);
            pr.offsetMin = Vector2.zero;
            pr.offsetMax = Vector2.zero;

            GameObject imageGo = new GameObject("Portrait", typeof(RectTransform), typeof(Image));
            imageGo.transform.SetParent(portraitFrame.transform, false);
            Image portrait = imageGo.GetComponent<Image>();
            portrait.color = Color.white;
            portrait.preserveAspect = true;
            portrait.raycastTarget = false;
            Anchor(portrait.rectTransform, Vector2.zero, Vector2.one);
            view.Portrait = portrait;
            LoadPortrait(portrait, character);

            Text name = MakeText(card.transform, "Name", font, 26, TextAnchor.MiddleLeft);
            Anchor(name.rectTransform, new Vector2(.34f, .78f), new Vector2(.96f, .94f));
            name.text = "<b>" + character.Name + "</b>";

            Text role = MakeText(card.transform, "Role", font, 15, TextAnchor.MiddleLeft);
            Anchor(role.rectTransform, new Vector2(.34f, .68f), new Vector2(.96f, .79f));
            role.color = new Color(.65f, .73f, .78f, 1f);
            role.text = RoleLabel(character.RoleCode);

            view.Description = MakeText(card.transform, "Description", font, 15, TextAnchor.UpperLeft);
            Anchor(view.Description.rectTransform, new Vector2(.34f, .34f), new Vector2(.96f, .67f));

            view.Affinity = MakeText(card.transform, "Affinity", font, 16, TextAnchor.MiddleLeft);
            Anchor(view.Affinity.rectTransform, new Vector2(.34f, .21f), new Vector2(.96f, .33f));

            view.Stance = MakeText(card.transform, "Stance", font, 17, TextAnchor.MiddleLeft);
            Anchor(view.Stance.rectTransform, new Vector2(.34f, .08f), new Vector2(.96f, .21f));

            return view;
        }

        private void RefreshCards()
        {
            Dictionary<string, int> affinity = affinityField?.GetValue(controller) as Dictionary<string, int>;
            Dictionary<string, string> states = statesField?.GetValue(controller) as Dictionary<string, string>;
            HashSet<string> occurred = eventsOccurredField?.GetValue(controller) as HashSet<string>;

            foreach (KeyValuePair<string, CardView> pair in cards)
            {
                string id = pair.Key;
                CardView card = pair.Value;
                int value = affinity != null && affinity.TryGetValue(id, out int a) ? Mathf.Clamp(a, 0, 5) : 0;
                card.Affinity.text = "호감도  " + new string('●', value) + new string('○', 5 - value);

                string stateId = StanceStateId(id);
                string raw = states != null && states.TryGetValue(stateId, out string s) ? s : string.Empty;
                card.Stance.text = "현재 생각  " + StanceLabel(id, raw);
                card.Description.text = BaseDescription(id) + KnownDetail(id, occurred);
            }
        }

        private void SetOpen(bool open)
        {
            if (panel == null) return;

            if (open && modalBusyField != null)
            {
                try
                {
                    if ((bool)modalBusyField.GetValue(controller)) return;
                }
                catch { }
            }

            panel.SetActive(open);
            if (open)
            {
                panel.transform.SetAsLastSibling();
                RefreshCards();
            }
        }

        private static string StanceStateId(string characterId)
        {
            switch (characterId)
            {
                case "CHA_SAM": return "STA_STANCE_SAM";
                case "CHA_JINA": return "STA_STANCE_JINA";
                case "CHA_FAYE": return "STA_STANCE_FAYE";
                case "CHA_BENJAMIN": return "STA_STANCE_BENJAMIN";
                case "CHA_BORICHI": return "STA_STANCE_BORICHI";
                case "CHA_DIYA": return "STA_STANCE_DIYA";
                default: return string.Empty;
            }
        }

        private static string StanceLabel(string characterId, string raw)
        {
            if (string.Equals(raw, "LEAVE", StringComparison.OrdinalIgnoreCase))
                return "<color=#E7B56A><b>출항</b></color>";
            if (string.Equals(raw, "STAY", StringComparison.OrdinalIgnoreCase))
                return "<color=#79C996><b>정착</b></color>";
            if (string.Equals(raw, "UNDECIDED", StringComparison.OrdinalIgnoreCase))
            {
                if (characterId == "CHA_FAYE")
                    return "<color=#D7C985><b>갈등 중</b></color>";
                return "<color=#B7C1C9><b>판단 보류</b></color>";
            }
            return "<color=#87929B>알 수 없음</color>";
        }

        private static string RoleLabel(string roleCode)
        {
            switch (roleCode)
            {
                case "LEADER": return "공동체 총책임자";
                case "LIVING_MANAGER": return "생활·자원 관리자";
                case "TECHNICIAN": return "기술자";
                case "METEOROLOGIST": return "기상학자";
                case "URBAN_RESEARCHER": return "도시·공동체 연구자";
                default: return roleCode ?? string.Empty;
            }
        }

        private static string BaseDescription(string id)
        {
            switch (id)
            {
                case "CHA_BENJAMIN": return "제이와 오래 알고 지낸 가까운 동료. 수송선 기술자.";
                case "CHA_FAYE": return "수송선 기술자. 연구자이자 전자음악가였던 시절이 있다.";
                case "CHA_SAM": return "사고 이후 공동체의 책임을 떠안은 총책임자.";
                case "CHA_JINA": return "사람들의 식량·물·생활을 관리하는 책임자.";
                case "CHA_BORICHI": return "이 행성의 바람과 모래폭풍을 관측하는 기상학자.";
                case "CHA_DIYA": return "사람과 장소가 공동체가 되는 과정을 연구하는 연구자.";
                default: return string.Empty;
            }
        }

        private static string KnownDetail(string id, HashSet<string> occurred)
        {
            if (occurred == null) return string.Empty;

            switch (id)
            {
                case "CHA_BENJAMIN" when occurred.Contains("EVT_W1_BEN_NAMES_DONE"):
                    return "\n<color=#AEB9C2>알게 된 것: 불시착 당시 아내와 아이를 잃었고, 남겨진 이름들을 쉽게 떠나지 못한다.</color>";
                case "CHA_FAYE" when occurred.Contains("EVT_W1_FAYE_SHIP_DONE"):
                    return "\n<color=#AEB9C2>알게 된 것: 망가진 수송선의 핵심 손상과 가능한 선택지를 정리하고 있다.</color>";
                case "CHA_SAM" when occurred.Contains("EVT_W1_SAM_BURDEN_DONE"):
                    return "\n<color=#AEB9C2>알게 된 것: 권력보다 사람의 생사를 먼저 계산하며 책임을 혼자 떠안고 있다.</color>";
                case "CHA_JINA" when occurred.Contains("EVT_W1_JINA_RATIONS_DONE"):
                    return "\n<color=#AEB9C2>알게 된 것: 엄격한 배급 기준은 사람을 살리기 위한 돌봄의 방식이다.</color>";
                case "CHA_BORICHI" when occurred.Contains("EVT_W1_BOR_SKY_DONE"):
                    return "\n<color=#AEB9C2>알게 된 것: 작은 이상 징후도 놓치지 않으려 관측을 멈추지 않는다.</color>";
                case "CHA_DIYA" when occurred.Contains("EVT_W1_DIYA_SETTLE_DONE"):
                    return "\n<color=#AEB9C2>알게 된 것: 임시 거처가 이미 누군가에게는 돌아올 집이 되었다고 본다.</color>";
                default:
                    return string.Empty;
            }
        }

        private static void LoadPortrait(Image image, SandPlanetCharacter04 character)
        {
#if UNITY_EDITOR
            string path = "Assets/SandPlanet/Art/Portraits/" + character.Name + ".png";
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture != null)
            {
                image.sprite = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(.5f, .5f),
                    100f);
                return;
            }
#endif
            image.color = new Color(.25f, .28f, .31f, 1f);
        }

        private static GameObject Panel(Transform parent, string name, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            return go;
        }

        private static Text MakeText(Transform parent, string name, Font font, int size, TextAnchor alignment)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            Text text = go.GetComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.color = Color.white;
            text.alignment = alignment;
            text.supportRichText = true;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            return text;
        }

        private static Button MakeButton(Transform parent, string name, Font font, string label, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            Button button = go.GetComponent<Button>();
            Text text = MakeText(go.transform, "Label", font, 17, TextAnchor.MiddleCenter);
            text.text = label;
            Anchor(text.rectTransform, Vector2.zero, Vector2.one);
            return button;
        }

        private static void Anchor(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private sealed class CardView
        {
            public string CharacterId;
            public GameObject Root;
            public Image Portrait;
            public Text Description;
            public Text Affinity;
            public Text Stance;
        }
    }
}
