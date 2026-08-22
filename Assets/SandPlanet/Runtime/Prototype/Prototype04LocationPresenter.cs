using System;
using System.Collections.Generic;
using System.Linq;
using SandPlanet.Prototype.DataDriven;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SandPlanet.Prototype
{
    public sealed class Prototype04LocationPresenter : MonoBehaviour
    {
        private const float BrowseScale = 1.5f;
        private const float FocusScale = 2f;
        private const float MotionSpeed = 9.5f;
        private static readonly Vector2 LocationMin = new Vector2(.019f, .026f);
        private static readonly Vector2 LocationMax = new Vector2(.728f, .837f);
        private static readonly Vector2[] CharacterSlots =
        {
            new Vector2(.27f,.72f), new Vector2(.50f,.56f), new Vector2(.69f,.72f),
            new Vector2(.30f,.43f), new Vector2(.59f,.42f), new Vector2(.46f,.64f)
        };
        private static readonly Vector2[] ObjectSlots =
        {
            new Vector2(.23f,.37f), new Vector2(.50f,.26f), new Vector2(.71f,.39f),
            new Vector2(.34f,.55f), new Vector2(.66f,.53f), new Vector2(.44f,.19f)
        };

        private readonly Dictionary<string, Sprite> portraitCache = new Dictionary<string, Sprite>(StringComparer.Ordinal);
        private Prototype04RuntimeFacade runtime;
        private GameObject panel;
        private Text title;
        private RectTransform targetRoot;
        private Button template;
        private Vector2 basePosition;
        private Vector3 baseScale;
        private Vector2 goalPosition;
        private float goalMultiplier = BrowseScale;
        private string renderedLocation = string.Empty;
        private string renderedTargets = string.Empty;
        private string focusedType = string.Empty;
        private string focusedId = string.Empty;

        public void Initialize(Prototype04RuntimeFacade runtimeFacade)
        {
            runtime = runtimeFacade;
            panel = runtime.LocationPanel;
            title = runtime.LocationTitle;
            targetRoot = runtime.TargetRoot;
            template = runtime.TargetTemplate;
            if (panel == null || targetRoot == null || template == null)
            {
                enabled = false;
                return;
            }

            basePosition = targetRoot.anchoredPosition;
            baseScale = targetRoot.localScale;
            goalPosition = basePosition;
            ConfigureStaticLayout();
            runtime.Changed += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            if (runtime != null) runtime.Changed -= Refresh;
        }

        private void Update()
        {
            if (runtime == null || targetRoot == null || string.IsNullOrEmpty(runtime.CurrentLocationId)) return;
            float t = 1f - Mathf.Exp(-MotionSpeed * Time.unscaledDeltaTime);
            targetRoot.anchoredPosition = Vector2.Lerp(targetRoot.anchoredPosition, goalPosition, t);
            targetRoot.localScale = Vector3.Lerp(targetRoot.localScale, baseScale * goalMultiplier, t);
        }

        private void ConfigureStaticLayout()
        {
            SetRect(panel.GetComponent<RectTransform>(), LocationMin, LocationMax);
            Image image = panel.GetComponent<Image>();
            if (image != null)
            {
                image.raycastTarget = true;
                image.maskable = true;
            }
            RectMask2D mask = panel.GetComponent<RectMask2D>();
            if (mask == null) mask = panel.AddComponent<RectMask2D>();
            mask.padding = Vector4.zero;

            if (title != null)
            {
                title.fontSize = 29;
                title.fontStyle = FontStyle.Bold;
                title.alignment = TextAnchor.UpperLeft;
                title.horizontalOverflow = HorizontalWrapMode.Wrap;
                title.verticalOverflow = VerticalWrapMode.Truncate;
                title.raycastTarget = false;
                SetRect(title.rectTransform, new Vector2(.028f, .885f), new Vector2(.52f, .975f));
            }
            if (runtime.LocationHint != null)
            {
                runtime.LocationHint.enabled = false;
                runtime.LocationHint.raycastTarget = false;
            }

            Transform targetPanel = targetRoot.parent;
            if (targetPanel != null)
            {
                SetRect(targetPanel as RectTransform, new Vector2(.22f, .12f), new Vector2(.975f, .91f));
                Image background = targetPanel.GetComponent<Image>();
                if (background != null)
                {
                    background.color = Color.clear;
                    background.raycastTarget = false;
                }
            }
            SetRect(targetRoot, Vector2.zero, Vector2.one);
            VerticalLayoutGroup layout = targetRoot.GetComponent<VerticalLayoutGroup>();
            if (layout != null) layout.enabled = false;
            ContentSizeFitter fitter = targetRoot.GetComponent<ContentSizeFitter>();
            if (fitter != null) fitter.enabled = false;
            template.gameObject.SetActive(false);
        }

        private void Refresh()
        {
            if (!enabled || runtime == null) return;
            string locationId = runtime.CurrentLocationId;
            if (string.IsNullOrEmpty(locationId))
            {
                renderedLocation = string.Empty;
                renderedTargets = string.Empty;
                focusedType = string.Empty;
                focusedId = string.Empty;
                ClearTargets();
                targetRoot.anchoredPosition = basePosition;
                targetRoot.localScale = baseScale;
                panel.SetActive(false);
                return;
            }

            bool newlyOpened = !string.Equals(renderedLocation, locationId, StringComparison.Ordinal);
            renderedLocation = locationId;
            panel.SetActive(true);
            panel.transform.SetAsLastSibling();
            if (runtime.Content.Locations.TryGetValue(locationId, out SandPlanetLocation04 location) && title != null)
                title.text = location.Name;
            Image image = panel.GetComponent<Image>();
            if (image != null) image.color = LocationBackground(locationId);

            IReadOnlyList<Prototype04TargetViewModel> targets = runtime.CurrentTargets();
            string signature = string.Join("|", targets.Select(t => t.Type + ":" + t.Id + ":" + BuildBadge(t.Type, t.Id)));
            if (newlyOpened || !string.Equals(signature, renderedTargets, StringComparison.Ordinal))
            {
                renderedTargets = signature;
                RenderTargets(locationId, targets);
            }

            string type = runtime.SelectedTargetType;
            string id = runtime.SelectedTargetId;
            if (!string.Equals(type, focusedType, StringComparison.Ordinal) || !string.Equals(id, focusedId, StringComparison.Ordinal) || newlyOpened)
            {
                focusedType = type;
                focusedId = id;
                CalculateFocusGoal();
            }
            if (newlyOpened)
            {
                targetRoot.anchoredPosition = basePosition;
                targetRoot.localScale = baseScale * BrowseScale;
            }
        }

        private void RenderTargets(string locationId, IReadOnlyList<Prototype04TargetViewModel> targets)
        {
            ClearTargets();
            int characterIndex = 0;
            int objectIndex = 0;
            foreach (Prototype04TargetViewModel model in targets)
            {
                Button button = Instantiate(template, targetRoot);
                button.gameObject.SetActive(true);
                button.interactable = true;
                Prototype04TargetBinding binding = button.gameObject.AddComponent<Prototype04TargetBinding>();
                binding.TargetType = model.Type;
                binding.TargetId = model.Id;
                Text label = button.GetComponentInChildren<Text>(true);
                if (label != null)
                {
                    label.supportRichText = true;
                    label.text = model.Name + BuildBadge(model.Type, model.Id);
                    label.raycastTarget = false;
                }
                string type = model.Type;
                string id = model.Id;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => runtime.SelectTarget(type, id));

                if (model.Type == "CHARACTER")
                    StyleCharacter(button, model, CharacterSlotFor(locationId, model.Id, characterIndex++));
                else
                    StyleObject(button, ObjectSlotFor(locationId, model.Id, objectIndex++));
            }
        }

        private string BuildBadge(string targetType, string targetId)
        {
            List<SandPlanetInteraction04> available = runtime.AvailableInteractions(targetType, targetId);
            List<string> badges = new List<string>();
            AddQuestMarker(badges, available, "MAIN", "OFFER", "?", "#F0A24A");
            AddQuestMarker(badges, available, "MAIN", "PROGRESS", "!", "#F0A24A");
            AddQuestMarker(badges, available, "CHARACTER", "OFFER", "?", "#69C77C");
            AddQuestMarker(badges, available, "CHARACTER", "PROGRESS", "!", "#69C77C");
            AddQuestMarker(badges, available, "SIDE", "OFFER", "?", "#F1D784");
            AddQuestMarker(badges, available, "SIDE", "PROGRESS", "!", "#F1D784");
            if (badges.Count == 0 && available.Any(i => Prototype04RuntimeFacade.NormalizeQuestRole(i) == "NONE"))
                badges.Add("<color=#AAB4BE>[일상]</color>");
            return badges.Count == 0 ? string.Empty : "   " + string.Join(" ", badges);
        }

        private void AddQuestMarker(List<string> output, List<SandPlanetInteraction04> available, string questType, string role, string marker, string color)
        {
            bool exists = available.Any(i =>
                Prototype04RuntimeFacade.NormalizeQuestRole(i) == role &&
                !string.IsNullOrEmpty(i.QuestId) &&
                runtime.Content.Quests.TryGetValue(i.QuestId, out SandPlanetQuest04 quest) &&
                string.Equals(quest.Type, questType, StringComparison.OrdinalIgnoreCase));
            if (exists) output.Add($"<color={color}><b>{marker}</b></color>");
        }

        private void StyleCharacter(Button button, Prototype04TargetViewModel model, Vector2 slot)
        {
            RectTransform rect = button.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = slot;
            rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(120f, 158f);
            Image background = button.GetComponent<Image>();
            if (background != null) background.color = new Color(.03f, .035f, .04f, .62f);
            Text label = button.GetComponentInChildren<Text>(true);
            if (label != null)
            {
                label.fontSize = 14;
                label.fontStyle = FontStyle.Bold;
                label.alignment = TextAnchor.MiddleCenter;
                label.horizontalOverflow = HorizontalWrapMode.Wrap;
                label.verticalOverflow = VerticalWrapMode.Truncate;
                SetRect(label.rectTransform, new Vector2(.02f, 0f), new Vector2(.98f, .22f));
            }
            GameObject portraitObject = new GameObject("UX18_ScenePortrait", typeof(RectTransform), typeof(Image));
            portraitObject.transform.SetParent(button.transform, false);
            portraitObject.transform.SetAsFirstSibling();
            Image portrait = portraitObject.GetComponent<Image>();
            portrait.preserveAspect = true;
            portrait.raycastTarget = false;
            SetRect(portrait.rectTransform, new Vector2(.06f, .23f), new Vector2(.94f, .96f));
            if (runtime.Content.Characters.TryGetValue(model.Id, out SandPlanetCharacter04 character))
            {
                portrait.sprite = GetPortraitSprite(character);
                portrait.color = portrait.sprite != null ? Color.white : new Color(.22f, .25f, .28f, 1f);
            }
        }

        private static void StyleObject(Button button, Vector2 slot)
        {
            RectTransform rect = button.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = slot;
            rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(172f, 46f);
            Image background = button.GetComponent<Image>();
            if (background != null) background.color = new Color(.045f, .055f, .065f, .72f);
            Text label = button.GetComponentInChildren<Text>(true);
            if (label != null)
            {
                label.fontSize = 13;
                label.fontStyle = FontStyle.Normal;
                label.alignment = TextAnchor.MiddleCenter;
                label.horizontalOverflow = HorizontalWrapMode.Wrap;
                label.verticalOverflow = VerticalWrapMode.Truncate;
                SetRect(label.rectTransform, new Vector2(.03f, .05f), new Vector2(.97f, .95f));
            }
        }

        private void CalculateFocusGoal()
        {
            if (string.IsNullOrEmpty(focusedId))
            {
                goalPosition = basePosition;
                goalMultiplier = BrowseScale;
                return;
            }
            Prototype04TargetBinding selected = targetRoot.GetComponentsInChildren<Prototype04TargetBinding>(true)
                .FirstOrDefault(v => v.TargetType == focusedType && v.TargetId == focusedId);
            RectTransform rect = selected != null ? selected.GetComponent<RectTransform>() : null;
            if (rect == null)
            {
                goalPosition = basePosition;
                goalMultiplier = BrowseScale;
                return;
            }
            Vector3 worldCenter = rect.TransformPoint(rect.rect.center);
            Vector3 localCenter3 = targetRoot.InverseTransformPoint(worldCenter);
            Vector2 offset = -new Vector2(localCenter3.x, localCenter3.y) * .42f;
            offset.x = Mathf.Clamp(offset.x, -175f, 175f);
            offset.y = Mathf.Clamp(offset.y, -105f, 105f);
            goalPosition = basePosition + offset;
            goalMultiplier = FocusScale;
        }

        private void ClearTargets()
        {
            for (int i = targetRoot.childCount - 1; i >= 0; i--)
            {
                Transform child = targetRoot.GetChild(i);
                if (child == template.transform) continue;
                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }
        }

        private Sprite GetPortraitSprite(SandPlanetCharacter04 character)
        {
            if (portraitCache.TryGetValue(character.Id, out Sprite cached)) return cached;
#if UNITY_EDITOR
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/SandPlanet/Art/Portraits/" + character.Name + ".png");
            if (texture != null)
            {
                Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f), 100f);
                portraitCache[character.Id] = sprite;
                return sprite;
            }
#endif
            portraitCache[character.Id] = null;
            return null;
        }

        private static Vector2 CharacterSlotFor(string locationId, string id, int fallback)
        {
            if (locationId == "LOC_01_SETTLEMENT")
            {
                if (id == "CHA_DIYA") return new Vector2(.25f, .72f);
                if (id == "CHA_JINA") return new Vector2(.48f, .57f);
                if (id == "CHA_SAM") return new Vector2(.70f, .72f);
            }
            if (locationId == "LOC_02_SHIP")
            {
                if (id == "CHA_BENJAMIN") return new Vector2(.28f, .72f);
                if (id == "CHA_FAYE") return new Vector2(.62f, .61f);
            }
            if (locationId == "LOC_04_OASIS" && id == "CHA_BORICHI") return new Vector2(.30f, .70f);
            return CharacterSlots[fallback % CharacterSlots.Length];
        }

        private static Vector2 ObjectSlotFor(string locationId, string id, int fallback)
        {
            if (locationId == "LOC_01_SETTLEMENT")
            {
                if (id == "OBJ_01_SETTLEMENT_BOARD") return new Vector2(.23f, .38f);
                if (id == "OBJ_01_SETTLEMENT_SHELTER") return new Vector2(.48f, .25f);
                if (id == "OBJ_01_SETTLEMENT_REST") return new Vector2(.72f, .39f);
            }
            if (locationId == "LOC_02_SHIP")
            {
                if (id == "OBJ_02_SHIP_OUTER_PANEL") return new Vector2(.26f, .35f);
                if (id == "OBJ_02_SHIP_CONSOLE") return new Vector2(.64f, .30f);
            }
            if (locationId == "LOC_03_GRAVEYARD")
            {
                if (id == "OBJ_03_GRAVE_MEMORIAL") return new Vector2(.31f, .56f);
                if (id == "OBJ_03_GRAVE_BARRIER") return new Vector2(.66f, .32f);
            }
            if (locationId == "LOC_04_OASIS")
            {
                if (id == "OBJ_04_OASIS_PUMP") return new Vector2(.65f, .48f);
                if (id == "OBJ_04_OASIS_WATER") return new Vector2(.48f, .27f);
            }
            return ObjectSlots[fallback % ObjectSlots.Length];
        }

        private static Color LocationBackground(string id)
        {
            switch (id)
            {
                case "LOC_01_SETTLEMENT": return new Color(.105f,.079f,.058f,1f);
                case "LOC_02_SHIP": return new Color(.052f,.069f,.079f,1f);
                case "LOC_03_GRAVEYARD": return new Color(.061f,.062f,.069f,1f);
                case "LOC_04_OASIS": return new Color(.049f,.078f,.070f,1f);
                case "LOC_05_COMMAND": return new Color(.046f,.058f,.075f,1f);
                case "LOC_06_SUPPLY": return new Color(.070f,.071f,.053f,1f);
                case "LOC_07_TECH": return new Color(.061f,.054f,.075f,1f);
                case "LOC_08_HABIT": return new Color(.078f,.057f,.054f,1f);
                default: return new Color(.035f,.039f,.043f,1f);
            }
        }

        private static void SetRect(RectTransform rect, Vector2 min, Vector2 max)
        {
            if (rect == null) return;
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }

    public sealed class Prototype04TargetBinding : MonoBehaviour
    {
        public string TargetType;
        public string TargetId;
    }
}
