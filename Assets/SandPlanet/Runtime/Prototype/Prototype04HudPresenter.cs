using System.Collections.Generic;
using System.Linq;
using SandPlanet.Prototype.DataDriven;
using UnityEngine;
using UnityEngine.UI;

namespace SandPlanet.Prototype
{
    public sealed class Prototype04HudPresenter : MonoBehaviour
    {
        private Prototype04RuntimeFacade runtime;
        private Text contextText;
        private Prototype04ProgressBar timeBar;
        private Prototype04ProgressBar willBar;
        private Prototype04ProgressBar personalBar;
        private Prototype04ProgressBar socialBar;
        private Prototype04ProgressBar technicalBar;

        public void Initialize(Prototype04RuntimeFacade runtimeFacade)
        {
            runtime = runtimeFacade;
            ConfigureLayout();
            CreateContextCard();
            CreateProgressBars();
            runtime.Changed += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            if (runtime != null) runtime.Changed -= Refresh;
        }

        private void ConfigureLayout()
        {
            Text hud = runtime.HudText;
            if (hud != null)
            {
                RectTransform panel = hud.transform.parent as RectTransform;
                if (panel != null)
                {
                    panel.anchorMin = new Vector2(0f, 1f);
                    panel.anchorMax = new Vector2(1f, 1f);
                    panel.pivot = new Vector2(.5f, 1f);
                    panel.anchoredPosition = Vector2.zero;
                    panel.sizeDelta = new Vector2(0f, 146f);
                }
                RectTransform rect = hud.rectTransform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = new Vector2(18f, 48f);
                rect.offsetMax = new Vector2(-570f, -8f);
                hud.fontSize = 18;
                hud.alignment = TextAnchor.MiddleLeft;
                hud.lineSpacing = 1.02f;
                hud.supportRichText = true;
            }
            if (runtime.EndDayButton != null)
            {
                RectTransform rect = runtime.EndDayButton.GetComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one;
                rect.anchoredPosition = new Vector2(-18f, -13f);
                rect.sizeDelta = new Vector2(165f, 46f);
                SetButtonText(runtime.EndDayButton, "하루 종료");
            }
            if (runtime.BackButton != null)
            {
                runtime.BackButton.GetComponent<RectTransform>().sizeDelta = new Vector2(165f, 48f);
                SetButtonText(runtime.BackButton, "허브로  [ESC]");
            }
            if (runtime.QuestTrackerText != null && runtime.QuestTrackerText.transform.parent is RectTransform questPanel)
                questPanel.anchoredPosition = new Vector2(16f, -158f);
        }

        private void CreateContextCard()
        {
            Canvas canvas = runtime.HudText != null ? runtime.HudText.GetComponentInParent<Canvas>() : null;
            if (canvas == null) return;
            GameObject card = new GameObject("UX04_ContextCard", typeof(RectTransform), typeof(Image));
            card.transform.SetParent(canvas.transform, false);
            RectTransform rect = card.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one;
            rect.anchoredPosition = new Vector2(-198f, -13f);
            rect.sizeDelta = new Vector2(350f, 86f);
            Image image = card.GetComponent<Image>();
            image.color = new Color(.07f, .085f, .10f, .96f);
            image.raycastTarget = false;
            GameObject textObject = new GameObject("ContextText", typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(card.transform, false);
            contextText = textObject.GetComponent<Text>();
            contextText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            contextText.fontSize = 15;
            contextText.color = Color.white;
            contextText.alignment = TextAnchor.MiddleLeft;
            contextText.supportRichText = true;
            contextText.raycastTarget = false;
            Stretch(contextText.rectTransform, 12f, 12f, 7f, 7f);
        }

        private void CreateProgressBars()
        {
            if (runtime.HudText == null || runtime.HudText.transform.parent == null) return;
            GameObject holder = new GameObject("UX04_ProgressBars", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            holder.transform.SetParent(runtime.HudText.transform.parent, false);
            RectTransform rect = holder.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(.5f, 0f);
            rect.offsetMin = new Vector2(18f, 9f);
            rect.offsetMax = new Vector2(-570f, 39f);
            HorizontalLayoutGroup layout = holder.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = true;
            timeBar = CreateBar(holder.transform, "Progress_Time", new Color(.82f,.56f,.25f));
            willBar = CreateBar(holder.transform, "Progress_Will", new Color(.76f,.36f,.31f));
            personalBar = CreateBar(holder.transform, "Progress_Personal", new Color(.70f,.40f,.50f));
            socialBar = CreateBar(holder.transform, "Progress_Social", new Color(.34f,.66f,.46f));
            technicalBar = CreateBar(holder.transform, "Progress_Technical", new Color(.33f,.55f,.79f));
        }

        private void Refresh()
        {
            if (runtime == null) return;
            string slot = runtime.Hour < 12 ? "오전" : runtime.Hour < 17 ? "오후" : "저녁";
            string world = runtime.Day >= 15 ? "수송선 내부" : "모래 행성";
            if (runtime.HudText != null)
                runtime.HudText.text =
                    $"DAY {runtime.Day:00} / 21     {runtime.Hour:00}:00     의지 {runtime.Will}/{runtime.MaxWill}     시간대 {slot}     <color=#B7C2CC>{world}</color>\n" +
                    $"개인 Lv{runtime.PersonalLevel}        대인 Lv{runtime.SocialLevel}        기술 Lv{runtime.TechnicalLevel}        <color=#9AA6B2>각 6XP → 즉시 Lv +1</color>";
            SetProgress(timeBar, Mathf.InverseLerp(8f, 22f, runtime.Hour), $"시간 {runtime.Hour:00}:00");
            SetProgress(willBar, (float)runtime.Will / Mathf.Max(1, runtime.MaxWill), $"의지 {runtime.Will}/{runtime.MaxWill}");
            SetProgress(personalBar, runtime.PersonalXp / 6f, $"개인 XP {runtime.PersonalXp}/6");
            SetProgress(socialBar, runtime.SocialXp / 6f, $"대인 XP {runtime.SocialXp}/6");
            SetProgress(technicalBar, runtime.TechnicalXp / 6f, $"기술 XP {runtime.TechnicalXp}/6");
            RefreshContext();
        }

        private void RefreshContext()
        {
            if (contextText == null) return;
            if (string.IsNullOrEmpty(runtime.CurrentLocationId))
            {
                string main = runtime.Content.Quests.Values
                    .Where(q => q.Type == "MAIN" && runtime.QuestStatus(q.Id) == "ACTIVE")
                    .OrderBy(q => q.LogOrder).ThenBy(q => q.Title).Select(q => q.Title).FirstOrDefault();
                contextText.text = $"<b>{(runtime.Day >= 15 ? "수송선 내부 허브" : "행성 허브")}</b>\n현재 Main: {(string.IsNullOrEmpty(main) ? "없음" : main)}";
                return;
            }
            string location = runtime.Content.Locations.TryGetValue(runtime.CurrentLocationId, out var definition) ? definition.Name : runtime.CurrentLocationId;
            if (string.IsNullOrEmpty(runtime.SelectedTargetId))
            {
                int targets = runtime.CurrentTargets().Count;
                int questTargets = runtime.CurrentTargets().Count(t => runtime.AvailableInteractions(t.Type, t.Id).Any(i => !string.IsNullOrEmpty(i.QuestId)));
                contextText.text = $"<b>{location}</b>\n대상 {targets}개 · Quest 연계 대상 {questTargets}개";
                return;
            }
            string targetName = runtime.TargetName(runtime.SelectedTargetType, runtime.SelectedTargetId);
            string kind = runtime.SelectedTargetType == "CHARACTER" ? "인물" : "사물";
            List<SandPlanetInteraction04> linked = runtime.AvailableInteractions(runtime.SelectedTargetType, runtime.SelectedTargetId)
                .Where(i => !string.IsNullOrEmpty(i.QuestId) && runtime.Content.Quests.ContainsKey(i.QuestId))
                .OrderBy(i => QuestTypeOrder(runtime.Content.Quests[i.QuestId].Type))
                .ThenBy(i => Prototype04RuntimeFacade.NormalizeQuestRole(i) == "OFFER" ? 0 : 1)
                .ThenBy(i => runtime.Content.Quests[i.QuestId].Title)
                .GroupBy(i => i.QuestId + "#" + Prototype04RuntimeFacade.NormalizeQuestRole(i))
                .Select(g => g.First())
                .Take(2)
                .ToList();
            string questText = linked.Count == 0
                ? "현재 Quest 연계 없음"
                : string.Join("   ", linked.Select(i => QuestBadge(runtime.Content.Quests[i.QuestId].Type, Prototype04RuntimeFacade.NormalizeQuestRole(i)) + " " + runtime.Content.Quests[i.QuestId].Title));
            contextText.text = $"<b>{location}  ›  {targetName}</b>  <color=#AAB4BE>{kind}</color>\n{questText}";
        }

        private static int QuestTypeOrder(string type) => type == "MAIN" ? 0 : type == "CHARACTER" ? 1 : 2;

        private static string QuestBadge(string type, string role)
        {
            string shortType = type == "CHARACTER" ? "CHAR" : type == "SIDE" ? "SIDE" : "MAIN";
            string marker = role == "OFFER" ? "?" : role == "PROGRESS" ? "!" : string.Empty;
            string color = Prototype04RuntimeFacade.QuestColorHex(type);
            return $"<color={color}><b>[{shortType}{(string.IsNullOrEmpty(marker) ? string.Empty : " " + marker)}]</b></color>";
        }

        private static Prototype04ProgressBar CreateBar(Transform parent, string name, Color color)
        {
            GameObject root = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(LayoutElement), typeof(Prototype04ProgressBar));
            root.transform.SetParent(parent, false);
            root.GetComponent<Image>().color = new Color(.13f,.15f,.17f,1f);
            LayoutElement layout = root.GetComponent<LayoutElement>();
            layout.minWidth = 130f;
            layout.preferredHeight = 28f;
            layout.flexibleWidth = 1f;
            GameObject fillObject = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillObject.transform.SetParent(root.transform, false);
            Image fill = fillObject.GetComponent<Image>();
            fill.color = color;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;
            fill.raycastTarget = false;
            Stretch(fill.rectTransform, 0f,0f,0f,0f);
            GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelObject.transform.SetParent(root.transform, false);
            Text label = labelObject.GetComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 13;
            label.color = Color.white;
            label.alignment = TextAnchor.MiddleCenter;
            label.raycastTarget = false;
            Stretch(label.rectTransform, 5f,5f,1f,1f);
            Prototype04ProgressBar bar = root.GetComponent<Prototype04ProgressBar>();
            bar.Fill = fill;
            bar.Label = label;
            return bar;
        }

        private static void SetProgress(Prototype04ProgressBar bar, float value, string label)
        {
            if (bar == null) return;
            bar.Fill.fillAmount = Mathf.Clamp01(value);
            bar.Label.text = label;
        }

        private static void SetButtonText(Button button, string value)
        {
            Text text = button.GetComponentInChildren<Text>(true);
            if (text == null) return;
            text.text = value;
            text.color = Color.white;
            text.fontSize = 17;
            text.alignment = TextAnchor.MiddleCenter;
        }

        private static void Stretch(RectTransform rect, float left, float right, float bottom, float top)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }
    }

    public sealed class Prototype04ProgressBar : MonoBehaviour
    {
        public Image Fill;
        public Text Label;
    }
}
