using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace SandPlanet.Prototype
{
    /// <summary>
    /// Prototype 0.4 opening setup.
    /// Pauses the data-driven controller before Start(), lets the player allocate the
    /// agreed starting total of 6 levels, then resumes GAME_START.
    ///
    /// Narrative intent: this does not rewrite Jay's past. He is still a geologist;
    /// the allocation expresses which capabilities feel most available after cryosleep.
    /// </summary>
    public sealed class SandPlanetPrototype04OpeningSetup : MonoBehaviour
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private const int TotalStartingLevels = 6;

        private SandPlanetPrototype04Controller controller;
        private Type controllerType;

        private FieldInfo personalLevelField;
        private FieldInfo socialLevelField;
        private FieldInfo technicalLevelField;
        private FieldInfo personalXpField;
        private FieldInfo socialXpField;
        private FieldInfo technicalXpField;
        private FieldInfo statesField;
        private FieldInfo eventsOccurredField;

        private GameObject overlay;
        private Text personalValue;
        private Text socialValue;
        private Text technicalValue;
        private Text remainingValue;
        private Button confirmButton;

        private int personal = 2;
        private int social = 2;
        private int technical = 2;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            SandPlanetPrototype04Controller found = UnityEngine.Object.FindFirstObjectByType<SandPlanetPrototype04Controller>();
            if (found == null || found.GetComponent<SandPlanetPrototype04OpeningSetup>() != null) return;

            // Awake() has already loaded the CSV state. Disabling here prevents Start()
            // from firing GAME_START until the player confirms the allocation.
            found.enabled = false;
            found.gameObject.AddComponent<SandPlanetPrototype04OpeningSetup>();
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
            personalLevelField = controllerType.GetField("personalLevel", PrivateInstance);
            socialLevelField = controllerType.GetField("socialLevel", PrivateInstance);
            technicalLevelField = controllerType.GetField("technicalLevel", PrivateInstance);
            personalXpField = controllerType.GetField("personalXp", PrivateInstance);
            socialXpField = controllerType.GetField("socialXp", PrivateInstance);
            technicalXpField = controllerType.GetField("technicalXp", PrivateInstance);
            statesField = controllerType.GetField("states", PrivateInstance);
            eventsOccurredField = controllerType.GetField("eventsOccurred", PrivateInstance);
        }

        private void Start()
        {
            // Safe fallback for entering a scene after the game has already started.
            HashSet<string> occurred = eventsOccurredField?.GetValue(controller) as HashSet<string>;
            if (occurred != null && occurred.Count > 0)
            {
                ResumeController();
                return;
            }

            Canvas canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogWarning("[SandPlanet 0.4] Opening stat setup skipped: Canvas not found.");
                ResumeController();
                return;
            }

            BuildUi(canvas.transform);
            RefreshUi();
        }

        private void BuildUi(Transform canvas)
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            overlay = new GameObject("UX04_OpeningSetup", typeof(RectTransform), typeof(Image));
            overlay.transform.SetParent(canvas, false);
            RectTransform root = overlay.GetComponent<RectTransform>();
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;
            overlay.GetComponent<Image>().color = new Color(.025f, .03f, .035f, .985f);
            overlay.transform.SetAsLastSibling();

            GameObject card = Panel(overlay.transform, "SetupCard", new Color(.07f, .08f, .09f, 1f));
            RectTransform cr = card.GetComponent<RectTransform>();
            cr.anchorMin = new Vector2(.20f, .13f);
            cr.anchorMax = new Vector2(.80f, .87f);
            cr.offsetMin = Vector2.zero;
            cr.offsetMax = Vector2.zero;

            Text title = MakeText(card.transform, "Title", font, 34, TextAnchor.MiddleCenter);
            Anchor(title.rectTransform, new Vector2(.05f, .82f), new Vector2(.95f, .96f));
            title.text = "동면 해제 — 제이의 현재 역량";

            Text body = MakeText(card.transform, "Body", font, 19, TextAnchor.UpperLeft);
            Anchor(body.rectTransform, new Vector2(.08f, .61f), new Vector2(.92f, .82f));
            body.text =
                "기억은 남아 있지만 몸과 감각은 아직 현재에 적응하는 중이다.\n" +
                "제이는 여전히 지질학자다. 아래 배분은 과거를 다시 만드는 것이 아니라,\n" +
                "지금 이 순간 어떤 능력에 가장 의지할 수 있는지를 정한다.";

            CreateStatRow(card.transform, font, "개인", "혼자 상황을 정리하고 버티는 힘", .50f,
                () => Change(ref personal, -1), () => Change(ref personal, 1), out personalValue);
            CreateStatRow(card.transform, font, "대인", "사람의 상태를 읽고 함께 움직이는 힘", .38f,
                () => Change(ref social, -1), () => Change(ref social, 1), out socialValue);
            CreateStatRow(card.transform, font, "기술", "지식과 분석으로 문제를 풀어내는 힘", .26f,
                () => Change(ref technical, -1), () => Change(ref technical, 1), out technicalValue);

            remainingValue = MakeText(card.transform, "Remaining", font, 17, TextAnchor.MiddleCenter);
            Anchor(remainingValue.rectTransform, new Vector2(.22f, .14f), new Vector2(.78f, .22f));

            confirmButton = MakeButton(card.transform, "Confirm", font, "이 역량으로 깨어난다", new Color(.48f, .31f, .16f));
            RectTransform br = confirmButton.GetComponent<RectTransform>();
            br.anchorMin = new Vector2(.34f, .045f);
            br.anchorMax = new Vector2(.66f, .13f);
            br.offsetMin = Vector2.zero;
            br.offsetMax = Vector2.zero;
            confirmButton.onClick.AddListener(Confirm);
        }

        private void CreateStatRow(
            Transform parent, Font font, string label, string description, float centerY,
            Action minus, Action plus, out Text valueText)
        {
            GameObject row = Panel(parent, "Stat_" + label, new Color(.10f, .115f, .13f, 1f));
            RectTransform rr = row.GetComponent<RectTransform>();
            rr.anchorMin = new Vector2(.08f, centerY - .05f);
            rr.anchorMax = new Vector2(.92f, centerY + .05f);
            rr.offsetMin = Vector2.zero;
            rr.offsetMax = Vector2.zero;

            Text name = MakeText(row.transform, "Name", font, 22, TextAnchor.MiddleLeft);
            Anchor(name.rectTransform, new Vector2(.03f, .12f), new Vector2(.22f, .88f));
            name.text = "<b>" + label + "</b>";

            Text desc = MakeText(row.transform, "Description", font, 15, TextAnchor.MiddleLeft);
            Anchor(desc.rectTransform, new Vector2(.22f, .12f), new Vector2(.68f, .88f));
            desc.color = new Color(.78f, .82f, .85f, 1f);
            desc.text = description;

            Button minusButton = MakeButton(row.transform, "Minus", font, "−", new Color(.20f, .24f, .28f));
            RectTransform mr = minusButton.GetComponent<RectTransform>();
            mr.anchorMin = new Vector2(.71f, .18f);
            mr.anchorMax = new Vector2(.79f, .82f);
            mr.offsetMin = Vector2.zero;
            mr.offsetMax = Vector2.zero;
            minusButton.onClick.AddListener(() => minus());

            valueText = MakeText(row.transform, "Value", font, 24, TextAnchor.MiddleCenter);
            Anchor(valueText.rectTransform, new Vector2(.80f, .12f), new Vector2(.88f, .88f));

            Button plusButton = MakeButton(row.transform, "Plus", font, "+", new Color(.29f, .37f, .28f));
            RectTransform pr = plusButton.GetComponent<RectTransform>();
            pr.anchorMin = new Vector2(.89f, .18f);
            pr.anchorMax = new Vector2(.97f, .82f);
            pr.offsetMin = Vector2.zero;
            pr.offsetMax = Vector2.zero;
            plusButton.onClick.AddListener(() => plus());
        }

        private void Change(ref int stat, int delta)
        {
            if (delta < 0)
            {
                stat = Mathf.Max(0, stat - 1);
            }
            else
            {
                int used = personal + social + technical;
                if (used >= TotalStartingLevels) return;
                stat++;
            }
            RefreshUi();
        }

        private void RefreshUi()
        {
            if (personalValue != null) personalValue.text = personal.ToString();
            if (socialValue != null) socialValue.text = social.ToString();
            if (technicalValue != null) technicalValue.text = technical.ToString();

            int remaining = TotalStartingLevels - personal - social - technical;
            if (remainingValue != null)
                remainingValue.text = remaining == 0
                    ? "총 6 Lv 배분 완료"
                    : "남은 Lv  " + remaining;

            if (confirmButton != null) confirmButton.interactable = remaining == 0;
        }

        private void Confirm()
        {
            if (personal + social + technical != TotalStartingLevels) return;

            personalLevelField?.SetValue(controller, personal);
            socialLevelField?.SetValue(controller, social);
            technicalLevelField?.SetValue(controller, technical);
            personalXpField?.SetValue(controller, 0);
            socialXpField?.SetValue(controller, 0);
            technicalXpField?.SetValue(controller, 0);

            // Agreed starting future preferences.
            // The Master sheet should carry the same defaults; this runtime assignment keeps
            // Prototype 0.4 correct even before the next authoring-data sync.
            Dictionary<string, string> states = statesField?.GetValue(controller) as Dictionary<string, string>;
            if (states != null)
            {
                states["STA_STANCE_BENJAMIN"] = "STAY";
                states["STA_STANCE_FAYE"] = "UNDECIDED";
            }

            if (overlay != null) Destroy(overlay);
            ResumeController();
        }

        private void ResumeController()
        {
            if (controller != null) controller.enabled = true;
            Destroy(this);
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
            return text;
        }

        private static Button MakeButton(Transform parent, string name, Font font, string label, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            Button button = go.GetComponent<Button>();

            Text text = MakeText(go.transform, "Label", font, 18, TextAnchor.MiddleCenter);
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
    }
}
