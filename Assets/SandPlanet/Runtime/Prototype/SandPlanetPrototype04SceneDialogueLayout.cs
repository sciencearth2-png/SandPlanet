using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SandPlanet.Prototype.DataDriven;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SandPlanet.Prototype
{
    /// <summary>
    /// Prototype 0.4 scene/dialogue presentation layer.
    ///
    /// Location view:
    /// - Uses the center/left of the screen as a lightweight scene stage.
    /// - Characters are placed as portrait targets instead of a vertical list.
    /// - World targets are placed as smaller scene labels.
    /// - Player-facing target names never include the old "인물" / "사물" prefixes.
    ///
    /// Right side:
    /// - Target interaction choices appear in one fixed right-side panel.
    /// - Narrative Interaction Flow and Event Flow use the same right-side dialogue panel.
    /// - Character dialogue shows the active speaker portrait slightly outside the panel,
    ///   inspired by portrait-forward CRPG dialogue layouts.
    ///
    /// This component changes presentation only. Existing v1.6 interaction/event/quest rules stay intact.
    /// </summary>
    [DefaultExecutionOrder(14000)]
    public sealed class SandPlanetPrototype04SceneDialogueLayout : MonoBehaviour
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

        private static readonly Vector2[] CharacterSlots =
        {
            new Vector2(.35f, .70f),
            new Vector2(.57f, .63f),
            new Vector2(.80f, .72f),
            new Vector2(.40f, .34f),
            new Vector2(.64f, .31f),
            new Vector2(.84f, .40f)
        };

        private static readonly Vector2[] ObjectSlots =
        {
            new Vector2(.23f, .18f),
            new Vector2(.45f, .17f),
            new Vector2(.67f, .18f),
            new Vector2(.87f, .16f),
            new Vector2(.29f, .49f),
            new Vector2(.72f, .50f)
        };

        private SandPlanetPrototype04Controller controller;
        private SandPlanetContent04 content;
        private Canvas canvas;

        private FieldInfo contentField;
        private FieldInfo currentLocationField;
        private FieldInfo modalBusyField;
        private FieldInfo locationPanelField;
        private FieldInfo locationTitleField;
        private FieldInfo locationHintField;
        private FieldInfo targetRootField;
        private FieldInfo targetTemplateField;
        private FieldInfo interactionRootField;
        private FieldInfo interactionTemplateField;
        private FieldInfo modalPanelField;
        private FieldInfo modalTitleField;
        private FieldInfo modalBodyField;
        private FieldInfo modalButtonRootField;
        private FieldInfo activeInteractionFlowField;
        private FieldInfo activeEventFlowField;
        private FieldInfo activeNodeIdField;

        private GameObject locationPanel;
        private Text locationTitle;
        private Text locationHint;
        private Transform targetRoot;
        private Button targetTemplate;
        private Transform interactionRoot;
        private Button interactionTemplate;
        private GameObject interactionPanel;
        private Text interactionHeader;
        private GameObject modalPanel;
        private Text modalTitle;
        private Text modalBody;
        private Transform modalButtonRoot;

        private GameObject speakerPortraitFrame;
        private Image speakerPortrait;
        private Text speakerPortraitName;
        private string visibleSpeakerId = string.Empty;

        private readonly Dictionary<string, Sprite> portraitCache = new Dictionary<string, Sprite>(StringComparer.Ordinal);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            SandPlanetPrototype04Controller found = UnityEngine.Object.FindFirstObjectByType<SandPlanetPrototype04Controller>();
            if (found != null && found.GetComponent<SandPlanetPrototype04SceneDialogueLayout>() == null)
                found.gameObject.AddComponent<SandPlanetPrototype04SceneDialogueLayout>();
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
            currentLocationField = t.GetField("currentLocationId", PrivateInstance);
            modalBusyField = t.GetField("modalBusy", PrivateInstance);
            locationPanelField = t.GetField("locationPanel", PrivateInstance);
            locationTitleField = t.GetField("locationTitleText", PrivateInstance);
            locationHintField = t.GetField("locationHintText", PrivateInstance);
            targetRootField = t.GetField("targetRoot", PrivateInstance);
            targetTemplateField = t.GetField("targetTemplate", PrivateInstance);
            interactionRootField = t.GetField("interactionRoot", PrivateInstance);
            interactionTemplateField = t.GetField("interactionTemplate", PrivateInstance);
            modalPanelField = t.GetField("modalPanel", PrivateInstance);
            modalTitleField = t.GetField("modalTitleText", PrivateInstance);
            modalBodyField = t.GetField("modalBodyText", PrivateInstance);
            modalButtonRootField = t.GetField("modalButtonRoot", PrivateInstance);
            activeInteractionFlowField = t.GetField("activeInteractionFlow", PrivateInstance);
            activeEventFlowField = t.GetField("activeEventFlow", PrivateInstance);
            activeNodeIdField = t.GetField("activeNodeId", PrivateInstance);
        }

        private void Start()
        {
            content = contentField?.GetValue(controller) as SandPlanetContent04;
            canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
            locationPanel = locationPanelField?.GetValue(controller) as GameObject;
            locationTitle = locationTitleField?.GetValue(controller) as Text;
            locationHint = locationHintField?.GetValue(controller) as Text;
            targetRoot = targetRootField?.GetValue(controller) as Transform;
            targetTemplate = targetTemplateField?.GetValue(controller) as Button;
            interactionRoot = interactionRootField?.GetValue(controller) as Transform;
            interactionTemplate = interactionTemplateField?.GetValue(controller) as Button;
            modalPanel = modalPanelField?.GetValue(controller) as GameObject;
            modalTitle = modalTitleField?.GetValue(controller) as Text;
            modalBody = modalBodyField?.GetValue(controller) as Text;
            modalButtonRoot = modalButtonRootField?.GetValue(controller) as Transform;

            if (content == null || canvas == null || locationPanel == null || targetRoot == null || interactionRoot == null || modalPanel == null)
            {
                enabled = false;
                return;
            }

            RestyleLocationStage();
            RestyleInteractionPanel();
            RestyleDialoguePanel();
            CreateSpeakerPortrait();

            Canvas.willRenderCanvases += RefreshBeforeRender;
            RefreshBeforeRender();
        }

        private void OnDestroy()
        {
            Canvas.willRenderCanvases -= RefreshBeforeRender;
        }

        private void LateUpdate()
        {
            RefreshBeforeRender();
        }

        private void RestyleLocationStage()
        {
            RectTransform locationRect = locationPanel.GetComponent<RectTransform>();
            locationRect.anchorMin = new Vector2(.025f, .06f);
            locationRect.anchorMax = new Vector2(.70f, .84f);
            locationRect.offsetMin = Vector2.zero;
            locationRect.offsetMax = Vector2.zero;

            Image locationBg = locationPanel.GetComponent<Image>();
            if (locationBg != null)
            {
                locationBg.color = new Color(.025f, .03f, .035f, .12f);
                locationBg.raycastTarget = false;
            }

            // Keep the scene stage behind the quest/log overlays when their areas overlap.
            locationPanel.transform.SetSiblingIndex(Mathf.Min(1, locationPanel.transform.parent.childCount - 1));

            if (locationTitle != null)
            {
                locationTitle.fontSize = 28;
                locationTitle.alignment = TextAnchor.MiddleLeft;
                SetRect(locationTitle.rectTransform, new Vector2(.42f, .90f), new Vector2(.80f, .99f), Vector2.zero, Vector2.zero);
            }

            if (locationHint != null)
            {
                locationHint.fontSize = 14;
                locationHint.color = new Color(.78f, .82f, .85f, .92f);
                SetRect(locationHint.rectTransform, new Vector2(.42f, .84f), new Vector2(.92f, .91f), Vector2.zero, Vector2.zero);
                if ((locationHint.text ?? string.Empty).StartsWith("사람/사물", StringComparison.Ordinal))
                    locationHint.text = "장면 속 대상을 선택하세요.";
            }

            Transform targetPanel = targetRoot.parent;
            if (targetPanel != null)
            {
                RectTransform targetPanelRect = targetPanel as RectTransform;
                if (targetPanelRect != null)
                    SetRect(targetPanelRect, new Vector2(.02f, .22f), new Vector2(.98f, .82f), Vector2.zero, Vector2.zero);
                Image bg = targetPanel.GetComponent<Image>();
                if (bg != null)
                {
                    bg.color = Color.clear;
                    bg.raycastTarget = false;
                }
            }

            RectTransform rootRect = targetRoot as RectTransform;
            if (rootRect != null)
                SetRect(rootRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            VerticalLayoutGroup vertical = targetRoot.GetComponent<VerticalLayoutGroup>();
            if (vertical != null) vertical.enabled = false;
            ContentSizeFitter fitter = targetRoot.GetComponent<ContentSizeFitter>();
            if (fitter != null) fitter.enabled = false;
        }

        private void RestyleInteractionPanel()
        {
            Transform oldPanel = interactionRoot.parent;
            if (oldPanel == null) return;

            interactionPanel = oldPanel.gameObject;
            oldPanel.SetParent(canvas.transform, false);
            RectTransform panelRect = oldPanel as RectTransform;
            if (panelRect != null)
                SetRect(panelRect, new Vector2(.715f, .06f), new Vector2(.99f, .84f), Vector2.zero, Vector2.zero);

            Image panelImage = interactionPanel.GetComponent<Image>();
            if (panelImage != null)
                panelImage.color = new Color(.035f, .045f, .055f, .985f);

            Font font = locationTitle != null && locationTitle.font != null
                ? locationTitle.font
                : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            Transform existingHeader = oldPanel.Find("UX18_InteractionHeader");
            if (existingHeader != null)
            {
                interactionHeader = existingHeader.GetComponent<Text>();
            }
            else
            {
                GameObject headerGo = new GameObject("UX18_InteractionHeader", typeof(RectTransform), typeof(Text));
                headerGo.transform.SetParent(oldPanel, false);
                interactionHeader = headerGo.GetComponent<Text>();
                interactionHeader.font = font;
                interactionHeader.fontSize = 24;
                interactionHeader.fontStyle = FontStyle.Bold;
                interactionHeader.color = Color.white;
                interactionHeader.alignment = TextAnchor.MiddleLeft;
                interactionHeader.raycastTarget = false;
                SetRect(interactionHeader.rectTransform, new Vector2(.06f, .88f), new Vector2(.94f, .97f), Vector2.zero, Vector2.zero);
            }

            RectTransform rootRect = interactionRoot as RectTransform;
            if (rootRect != null)
                SetRect(rootRect, new Vector2(.05f, .05f), new Vector2(.95f, .86f), Vector2.zero, Vector2.zero);

            interactionPanel.SetActive(false);
        }

        private void RestyleDialoguePanel()
        {
            RectTransform modalRect = modalPanel.GetComponent<RectTransform>();
            modalRect.anchorMin = new Vector2(.715f, .06f);
            modalRect.anchorMax = new Vector2(.99f, .84f);
            modalRect.offsetMin = Vector2.zero;
            modalRect.offsetMax = Vector2.zero;

            Image modalImage = modalPanel.GetComponent<Image>();
            if (modalImage != null)
                modalImage.color = new Color(.025f, .03f, .04f, .992f);

            if (modalTitle != null)
            {
                modalTitle.fontSize = 25;
                SetRect(modalTitle.rectTransform, new Vector2(.06f, .89f), new Vector2(.94f, .97f), Vector2.zero, Vector2.zero);
            }

            if (modalBody != null)
            {
                modalBody.fontSize = 18;
                modalBody.lineSpacing = 1.08f;
                SetRect(modalBody.rectTransform, new Vector2(.06f, .37f), new Vector2(.94f, .86f), Vector2.zero, Vector2.zero);
            }

            RectTransform choices = modalButtonRoot as RectTransform;
            if (choices != null)
                SetRect(choices, new Vector2(.05f, .04f), new Vector2(.95f, .34f), Vector2.zero, Vector2.zero);
        }

        private void CreateSpeakerPortrait()
        {
            Transform existing = modalPanel.transform.Find("UX18_SpeakerPortraitFrame");
            if (existing != null)
            {
                speakerPortraitFrame = existing.gameObject;
                speakerPortrait = existing.Find("Portrait")?.GetComponent<Image>();
                speakerPortraitName = existing.Find("Name")?.GetComponent<Text>();
                speakerPortraitFrame.SetActive(false);
                return;
            }

            speakerPortraitFrame = new GameObject("UX18_SpeakerPortraitFrame", typeof(RectTransform), typeof(Image));
            speakerPortraitFrame.transform.SetParent(modalPanel.transform, false);
            RectTransform frameRect = speakerPortraitFrame.GetComponent<RectTransform>();
            frameRect.anchorMin = frameRect.anchorMax = new Vector2(0f, .61f);
            frameRect.pivot = new Vector2(1f, .5f);
            frameRect.anchoredPosition = new Vector2(-14f, 0f);
            frameRect.sizeDelta = new Vector2(150f, 218f);

            Image frame = speakerPortraitFrame.GetComponent<Image>();
            frame.color = new Color(.055f, .065f, .075f, .98f);
            frame.raycastTarget = false;

            GameObject portraitGo = new GameObject("Portrait", typeof(RectTransform), typeof(Image));
            portraitGo.transform.SetParent(speakerPortraitFrame.transform, false);
            speakerPortrait = portraitGo.GetComponent<Image>();
            speakerPortrait.color = Color.white;
            speakerPortrait.preserveAspect = true;
            speakerPortrait.raycastTarget = false;
            SetRect(speakerPortrait.rectTransform, new Vector2(.04f, .15f), new Vector2(.96f, .98f), Vector2.zero, Vector2.zero);

            GameObject nameGo = new GameObject("Name", typeof(RectTransform), typeof(Text));
            nameGo.transform.SetParent(speakerPortraitFrame.transform, false);
            speakerPortraitName = nameGo.GetComponent<Text>();
            speakerPortraitName.font = modalTitle != null && modalTitle.font != null
                ? modalTitle.font
                : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            speakerPortraitName.fontSize = 15;
            speakerPortraitName.fontStyle = FontStyle.Bold;
            speakerPortraitName.color = Color.white;
            speakerPortraitName.alignment = TextAnchor.MiddleCenter;
            speakerPortraitName.raycastTarget = false;
            SetRect(speakerPortraitName.rectTransform, new Vector2(.03f, .01f), new Vector2(.97f, .15f), Vector2.zero, Vector2.zero);

            speakerPortraitFrame.SetActive(false);
        }

        private void RefreshBeforeRender()
        {
            if (!enabled || content == null) return;

            bool modalBusy = GetBool(modalBusyField);
            string locationId = GetString(currentLocationField);
            bool locationOpen = !string.IsNullOrEmpty(locationId) && locationPanel.activeInHierarchy;

            if (locationOpen)
            {
                if (locationHint != null && (locationHint.text ?? string.Empty).StartsWith("사람/사물", StringComparison.Ordinal))
                    locationHint.text = "장면 속 대상을 선택하세요.";
                DecorateSceneTargets();
            }

            bool hasInteractionChoices = DynamicButtonCount(interactionRoot, interactionTemplate) > 0;
            if (interactionPanel != null)
            {
                interactionPanel.SetActive(locationOpen && !modalBusy && hasInteractionChoices);
                if (interactionPanel.activeSelf)
                {
                    interactionPanel.transform.SetAsLastSibling();
                    string header = locationHint != null ? locationHint.text ?? string.Empty : string.Empty;
                    int separator = header.IndexOf(" — ", StringComparison.Ordinal);
                    if (separator >= 0) header = header.Substring(0, separator);
                    interactionHeader.text = string.IsNullOrWhiteSpace(header) || header.StartsWith("장면 속", StringComparison.Ordinal)
                        ? "무엇을 할까"
                        : header;
                }
            }

            if (modalPanel.activeInHierarchy)
            {
                modalPanel.transform.SetAsLastSibling();
                RefreshSpeakerPortrait();
            }
            else
            {
                SetSpeakerPortrait(string.Empty);
            }
        }

        private void DecorateSceneTargets()
        {
            List<SceneTargetView04> characters = new List<SceneTargetView04>();
            List<SceneTargetView04> objects = new List<SceneTargetView04>();

            for (int i = 0; i < targetRoot.childCount; i++)
            {
                Transform child = targetRoot.GetChild(i);
                if (targetTemplate != null && child == targetTemplate.transform) continue;
                if (!child.gameObject.activeSelf) continue;

                Button button = child.GetComponent<Button>();
                Text label = child.GetComponentInChildren<Text>(true);
                if (button == null || label == null) continue;

                SceneTargetView04 view = child.GetComponent<SceneTargetView04>();
                if (view == null)
                {
                    view = child.gameObject.AddComponent<SceneTargetView04>();
                    IdentifyTarget(label.text, view);
                }

                if (string.IsNullOrEmpty(view.TargetId))
                    IdentifyTarget(label.text, view);

                RemoveLegacyPrefix(label);
                label.raycastTarget = false;

                if (view.TargetType == "CHARACTER") characters.Add(view);
                else objects.Add(view);
            }

            characters = characters.OrderBy(v => TargetName(v)).ToList();
            objects = objects.OrderBy(v => TargetName(v)).ToList();

            for (int i = 0; i < characters.Count; i++)
                StyleCharacterTarget(characters[i], CharacterSlots[i % CharacterSlots.Length]);
            for (int i = 0; i < objects.Count; i++)
                StyleObjectTarget(objects[i], ObjectSlots[i % ObjectSlots.Length]);
        }

        private void IdentifyTarget(string rawLabel, SceneTargetView04 view)
        {
            string raw = rawLabel ?? string.Empty;
            foreach (SandPlanetCharacter04 c in content.Characters.Values)
            {
                if (!c.Active || raw.IndexOf(c.Name, StringComparison.Ordinal) < 0) continue;
                view.TargetType = "CHARACTER";
                view.TargetId = c.Id;
                return;
            }

            foreach (SandPlanetWorldTarget04 w in content.WorldTargets.Values)
            {
                if (!w.Active || raw.IndexOf(w.Name, StringComparison.Ordinal) < 0) continue;
                view.TargetType = "WORLD_TARGET";
                view.TargetId = w.Id;
                return;
            }
        }

        private static void RemoveLegacyPrefix(Text label)
        {
            if (label == null || string.IsNullOrEmpty(label.text)) return;
            if (label.text.StartsWith("인물  ", StringComparison.Ordinal))
                label.text = label.text.Substring("인물  ".Length);
            else if (label.text.StartsWith("사물  ", StringComparison.Ordinal))
                label.text = label.text.Substring("사물  ".Length);
        }

        private void StyleCharacterTarget(SceneTargetView04 view, Vector2 slot)
        {
            Button button = view.GetComponent<Button>();
            Text label = view.GetComponentInChildren<Text>(true);
            if (button == null || label == null) return;

            RectTransform rect = button.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = slot;
            rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(132f, 174f);

            Image bg = button.GetComponent<Image>();
            if (bg != null) bg.color = new Color(.035f, .045f, .055f, .82f);

            label.fontSize = 15;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            SetRect(label.rectTransform, new Vector2(.02f, .00f), new Vector2(.98f, .24f), new Vector2(3f, 2f), new Vector2(-3f, -1f));

            Transform portraitTransform = button.transform.Find("UX18_ScenePortrait");
            Image portrait;
            if (portraitTransform == null)
            {
                GameObject portraitGo = new GameObject("UX18_ScenePortrait", typeof(RectTransform), typeof(Image));
                portraitGo.transform.SetParent(button.transform, false);
                portrait = portraitGo.GetComponent<Image>();
                portrait.preserveAspect = true;
                portrait.raycastTarget = false;
                SetRect(portrait.rectTransform, new Vector2(.05f, .25f), new Vector2(.95f, .96f), Vector2.zero, Vector2.zero);
                portraitGo.transform.SetAsFirstSibling();
            }
            else
            {
                portrait = portraitTransform.GetComponent<Image>();
            }

            if (content.Characters.TryGetValue(view.TargetId, out SandPlanetCharacter04 character))
            {
                Sprite sprite = GetPortraitSprite(character);
                if (sprite != null)
                {
                    portrait.sprite = sprite;
                    portrait.color = Color.white;
                }
                else
                {
                    portrait.color = new Color(.22f, .25f, .28f, 1f);
                }
            }
        }

        private static void StyleObjectTarget(SceneTargetView04 view, Vector2 slot)
        {
            Button button = view.GetComponent<Button>();
            Text label = view.GetComponentInChildren<Text>(true);
            if (button == null || label == null) return;

            RectTransform rect = button.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = slot;
            rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(190f, 56f);

            Image bg = button.GetComponent<Image>();
            if (bg != null) bg.color = new Color(.10f, .13f, .15f, .88f);

            label.fontSize = 14;
            label.fontStyle = FontStyle.Normal;
            label.alignment = TextAnchor.MiddleCenter;
            SetRect(label.rectTransform, Vector2.zero, Vector2.one, new Vector2(8f, 4f), new Vector2(-8f, -4f));

            Transform portrait = button.transform.Find("UX18_ScenePortrait");
            if (portrait != null) portrait.gameObject.SetActive(false);
        }

        private void RefreshSpeakerPortrait()
        {
            string speakerId = CurrentSpeakerCharacterId();
            SetSpeakerPortrait(speakerId);
        }

        private string CurrentSpeakerCharacterId()
        {
            string nodeId = GetString(activeNodeIdField);
            if (string.IsNullOrEmpty(nodeId)) return string.Empty;

            IEnumerable<SandPlanetFlowNode04> rows = Enumerable.Empty<SandPlanetFlowNode04>();
            SandPlanetInteractionFlow04 interactionFlow = activeInteractionFlowField?.GetValue(controller) as SandPlanetInteractionFlow04;
            SandPlanetEventFlow04 eventFlow = activeEventFlowField?.GetValue(controller) as SandPlanetEventFlow04;
            if (interactionFlow != null) rows = interactionFlow.GetNodeRows(nodeId);
            else if (eventFlow != null) rows = eventFlow.GetNodeRows(nodeId);

            SandPlanetFlowNode04 row = rows.FirstOrDefault(r => r.Active);
            if (row == null || !string.Equals(row.PresentationType, "DIALOGUE", StringComparison.OrdinalIgnoreCase))
                return string.Empty;

            string speaker = row.Speaker ?? string.Empty;
            return content.Characters.ContainsKey(speaker) ? speaker : string.Empty;
        }

        private void SetSpeakerPortrait(string characterId)
        {
            if (speakerPortraitFrame == null) return;
            if (string.Equals(characterId, visibleSpeakerId, StringComparison.Ordinal)) return;
            visibleSpeakerId = characterId ?? string.Empty;

            if (string.IsNullOrEmpty(visibleSpeakerId) || !content.Characters.TryGetValue(visibleSpeakerId, out SandPlanetCharacter04 character))
            {
                speakerPortraitFrame.SetActive(false);
                return;
            }

            Sprite sprite = GetPortraitSprite(character);
            if (sprite == null)
            {
                speakerPortraitFrame.SetActive(false);
                return;
            }

            speakerPortrait.sprite = sprite;
            speakerPortrait.color = Color.white;
            speakerPortraitName.text = character.Name;
            speakerPortraitFrame.SetActive(true);
            speakerPortraitFrame.transform.SetAsLastSibling();
        }

        private Sprite GetPortraitSprite(SandPlanetCharacter04 character)
        {
            if (character == null) return null;
            if (portraitCache.TryGetValue(character.Id, out Sprite cached)) return cached;

#if UNITY_EDITOR
            string path = "Assets/SandPlanet/Art/Portraits/" + character.Name + ".png";
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture != null)
            {
                Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(.5f, .5f), 100f);
                portraitCache[character.Id] = sprite;
                return sprite;
            }
#endif
            portraitCache[character.Id] = null;
            return null;
        }

        private string TargetName(SceneTargetView04 view)
        {
            if (view == null) return string.Empty;
            if (view.TargetType == "CHARACTER" && content.Characters.TryGetValue(view.TargetId, out SandPlanetCharacter04 c)) return c.Name;
            if (view.TargetType == "WORLD_TARGET" && content.WorldTargets.TryGetValue(view.TargetId, out SandPlanetWorldTarget04 w)) return w.Name;
            return view.TargetId ?? string.Empty;
        }

        private static int DynamicButtonCount(Transform root, Button template)
        {
            if (root == null) return 0;
            int count = 0;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (template != null && child == template.transform) continue;
                if (child.gameObject.activeSelf && child.GetComponent<Button>() != null) count++;
            }
            return count;
        }

        private bool GetBool(FieldInfo field)
        {
            if (field == null) return false;
            try { return (bool)field.GetValue(controller); }
            catch { return false; }
        }

        private string GetString(FieldInfo field)
        {
            if (field == null) return string.Empty;
            try { return field.GetValue(controller) as string ?? string.Empty; }
            catch { return string.Empty; }
        }

        private static void SetRect(RectTransform rect, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
        {
            if (rect == null) return;
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }
    }

    public sealed class SceneTargetView04 : MonoBehaviour
    {
        public string TargetType;
        public string TargetId;
    }
}
