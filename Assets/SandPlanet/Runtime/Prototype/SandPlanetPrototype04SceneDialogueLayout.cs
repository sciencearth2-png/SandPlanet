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
    /// Final presentation authority for the Prototype 0.4 location/dialogue layout.
    /// Existing quest/interaction/event rules are unchanged.
    ///
    /// Important: an older UX layer still writes legacy LocationPanel anchors during Start.
    /// This component therefore owns the final panel geometry and reasserts it before render,
    /// so execution-order differences cannot move the location scene back to the right side.
    /// </summary>
    [DefaultExecutionOrder(20000)]
    public sealed class SandPlanetPrototype04SceneDialogueLayout : MonoBehaviour
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

        // Measured from the user's reference screenshot:
        // x 26..1011 on 1391px => 1.9%..72.7%
        // y 126..754 on 775px  => bottom 2.6%..top 83.7%
        private static readonly Vector2 LocationMin = new Vector2(.019f, .026f);
        private static readonly Vector2 LocationMax = new Vector2(.728f, .837f);
        private static readonly Vector2 RightPanelMin = new Vector2(.739f, .018f);
        private static readonly Vector2 RightPanelMax = new Vector2(.995f, .837f);

        // Dark, slightly transparent temporary location backdrop.
        private static readonly Color ScenePanelColor = new Color(.025f, .030f, .035f, .90f);

        // Final browse composition. TargetRoot is scaled to 1.5x elsewhere, so these positions
        // deliberately stay inside a tighter safe area. ID-order below maps settlement characters
        // as Diya -> Jina -> Sam, keeping Jina away from the clipping edge.
        private static readonly Vector2[] CharacterSlots =
        {
            new Vector2(.28f, .69f),
            new Vector2(.48f, .55f),
            new Vector2(.64f, .68f),
            new Vector2(.33f, .43f),
            new Vector2(.57f, .43f),
            new Vector2(.45f, .63f)
        };

        // World targets are map elements, not a bottom toolbar. These slots intentionally mix
        // lower/middle heights while remaining safe under the default 1.5x browse scale.
        private static readonly Vector2[] ObjectSlots =
        {
            new Vector2(.31f, .40f),
            new Vector2(.58f, .31f),
            new Vector2(.53f, .52f),
            new Vector2(.38f, .58f),
            new Vector2(.63f, .44f),
            new Vector2(.43f, .29f)
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
        private FieldInfo modalButtonTemplateField;
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
        private Button modalButtonTemplate;

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
            modalButtonTemplateField = t.GetField("modalButtonTemplate", PrivateInstance);
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
            modalButtonTemplate = modalButtonTemplateField?.GetValue(controller) as Button;

            if (content == null || canvas == null || locationPanel == null || targetRoot == null ||
                interactionRoot == null || modalPanel == null || modalButtonRoot == null)
            {
                enabled = false;
                return;
            }

            ApplyLocationLayout();
            ApplyInteractionLayout();
            ApplyDialogueLayout();
            CreateSpeakerPortrait();

            Canvas.willRenderCanvases += RefreshBeforeRender;
            RefreshBeforeRender();
        }

        private void OnDestroy() => Canvas.willRenderCanvases -= RefreshBeforeRender;
        private void LateUpdate() => RefreshBeforeRender();

        private void ApplyLocationLayout()
        {
            RectTransform rect = locationPanel.GetComponent<RectTransform>();
            SetRect(rect, LocationMin, LocationMax);

            Image bg = locationPanel.GetComponent<Image>();
            if (bg != null)
            {
                bg.color = ScenePanelColor;
                bg.raycastTarget = true;
            }

            // Scene is behind the existing Quest / Log overlays, matching the reference composition.
            locationPanel.transform.SetSiblingIndex(Mathf.Min(1, locationPanel.transform.parent.childCount - 1));

            if (locationTitle != null)
            {
                locationTitle.fontSize = 34;
                locationTitle.fontStyle = FontStyle.Normal;
                locationTitle.alignment = TextAnchor.MiddleCenter;
                locationTitle.color = Color.white;
                locationTitle.raycastTarget = false;
                SetRect(locationTitle.rectTransform, new Vector2(.28f, .43f), new Vector2(.72f, .57f));
            }

            // The controller still updates this text; it is used to derive the right-panel header.
            if (locationHint != null)
            {
                locationHint.enabled = false;
                locationHint.raycastTarget = false;
            }

            Transform targetPanel = targetRoot.parent;
            if (targetPanel != null)
            {
                RectTransform targetRect = targetPanel as RectTransform;
                if (targetRect != null)
                    SetRect(targetRect, new Vector2(.22f, .12f), new Vector2(.975f, .91f));

                Image targetBg = targetPanel.GetComponent<Image>();
                if (targetBg != null)
                {
                    targetBg.color = Color.clear;
                    targetBg.raycastTarget = false;
                }
            }

            RectTransform rootRect = targetRoot as RectTransform;
            if (rootRect != null) SetRect(rootRect, Vector2.zero, Vector2.one);
            DisableAutomaticSizing(targetRoot);
        }

        private void ApplyInteractionLayout()
        {
            Transform panel = interactionRoot.parent;
            if (panel == null) return;

            interactionPanel = panel.gameObject;
            panel.SetParent(canvas.transform, false);
            RectTransform panelRect = panel as RectTransform;
            if (panelRect != null) SetRect(panelRect, RightPanelMin, RightPanelMax);

            Image image = interactionPanel.GetComponent<Image>();
            if (image != null)
            {
                image.color = new Color(.035f, .043f, .055f, .98f);
                image.raycastTarget = true;
            }

            Font font = locationTitle != null && locationTitle.font != null
                ? locationTitle.font
                : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            Transform existing = panel.Find("UX18_InteractionHeader");
            if (existing != null)
                interactionHeader = existing.GetComponent<Text>();
            else
            {
                GameObject go = new GameObject("UX18_InteractionHeader", typeof(RectTransform), typeof(Text));
                go.transform.SetParent(panel, false);
                interactionHeader = go.GetComponent<Text>();
                interactionHeader.font = font;
            }

            if (interactionHeader != null)
            {
                interactionHeader.fontSize = 22;
                interactionHeader.fontStyle = FontStyle.Bold;
                interactionHeader.color = Color.white;
                interactionHeader.alignment = TextAnchor.MiddleLeft;
                interactionHeader.raycastTarget = false;
                interactionHeader.supportRichText = true;
                SetRect(interactionHeader.rectTransform, new Vector2(.07f, .89f), new Vector2(.93f, .97f));
            }

            RectTransform root = interactionRoot as RectTransform;
            if (root != null) SetRect(root, new Vector2(.06f, .055f), new Vector2(.94f, .865f));

            DisableContentSizeFitter(interactionRoot);
            VerticalLayoutGroup layout = interactionRoot.GetComponent<VerticalLayoutGroup>();
            if (layout != null)
            {
                layout.enabled = true;
                layout.childAlignment = TextAnchor.UpperCenter;
                layout.spacing = 10f;
                layout.padding = new RectOffset(0, 0, 0, 0);
                layout.childControlWidth = true;
                layout.childForceExpandWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandHeight = false;
            }

            StyleTemplate(interactionTemplate, 78f, 15, TextAnchor.MiddleLeft);
            interactionPanel.SetActive(false);
        }

        private void ApplyDialogueLayout()
        {
            RectTransform panel = modalPanel.GetComponent<RectTransform>();
            SetRect(panel, RightPanelMin, RightPanelMax);

            Image image = modalPanel.GetComponent<Image>();
            if (image != null)
            {
                image.color = new Color(.02f, .027f, .035f, .985f);
                image.raycastTarget = true;
            }

            if (modalTitle != null)
            {
                modalTitle.fontSize = 23;
                modalTitle.fontStyle = FontStyle.Bold;
                modalTitle.alignment = TextAnchor.MiddleLeft;
                modalTitle.horizontalOverflow = HorizontalWrapMode.Wrap;
                modalTitle.verticalOverflow = VerticalWrapMode.Truncate;
                SetRect(modalTitle.rectTransform, new Vector2(.07f, .88f), new Vector2(.93f, .965f));
            }

            if (modalBody != null)
            {
                modalBody.fontSize = 17;
                modalBody.fontStyle = FontStyle.Normal;
                modalBody.lineSpacing = 1.08f;
                modalBody.alignment = TextAnchor.UpperLeft;
                modalBody.horizontalOverflow = HorizontalWrapMode.Wrap;
                modalBody.verticalOverflow = VerticalWrapMode.Overflow;
                SetRect(modalBody.rectTransform, new Vector2(.07f, .39f), new Vector2(.93f, .855f));
            }

            RectTransform root = modalButtonRoot as RectTransform;
            if (root != null) SetRect(root, new Vector2(.06f, .025f), new Vector2(.94f, .365f));

            DisableContentSizeFitter(modalButtonRoot);
            VerticalLayoutGroup layout = modalButtonRoot.GetComponent<VerticalLayoutGroup>();
            if (layout != null)
            {
                layout.enabled = true;
                layout.childAlignment = TextAnchor.LowerCenter;
                layout.spacing = 9f;
                layout.padding = new RectOffset(0, 0, 0, 0);
                layout.childControlWidth = true;
                layout.childForceExpandWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandHeight = false;
            }

            StyleTemplate(modalButtonTemplate, 58f, 15, TextAnchor.MiddleCenter);
        }

        private void EnforceFinalGeometry()
        {
            // This is intentionally small: only final geometry/color values are reasserted.
            // It prevents legacy Start-order code from moving the panels after our setup.
            if (locationPanel != null)
            {
                SetRect(locationPanel.GetComponent<RectTransform>(), LocationMin, LocationMax);
                Image bg = locationPanel.GetComponent<Image>();
                if (bg != null) bg.color = ScenePanelColor;
            }

            if (interactionPanel != null)
                SetRect(interactionPanel.GetComponent<RectTransform>(), RightPanelMin, RightPanelMax);

            if (modalPanel != null)
                SetRect(modalPanel.GetComponent<RectTransform>(), RightPanelMin, RightPanelMax);
        }

        private void RefreshBeforeRender()
        {
            if (!enabled || content == null) return;

            EnforceFinalGeometry();

            bool modalBusy = GetBool(modalBusyField);
            string locationId = GetString(currentLocationField);
            bool locationOpen = !string.IsNullOrEmpty(locationId) && locationPanel.activeInHierarchy;

            if (locationOpen) DecorateSceneTargets();

            bool hasChoices = DynamicButtonCount(interactionRoot, interactionTemplate) > 0;
            if (interactionPanel != null)
            {
                interactionPanel.SetActive(locationOpen && !modalBusy && hasChoices);
                if (interactionPanel.activeSelf)
                {
                    interactionPanel.transform.SetAsLastSibling();
                    interactionHeader.text = SelectedTargetHeader();
                    StyleInteractionButtons();
                }
            }

            if (modalPanel.activeInHierarchy)
            {
                modalPanel.transform.SetAsLastSibling();
                StyleModalButtons();
                RefreshSpeakerPortrait();
            }
            else
            {
                SetSpeakerPortrait(string.Empty);
            }
        }

        private string SelectedTargetHeader()
        {
            string raw = locationHint != null ? locationHint.text ?? string.Empty : string.Empty;
            int separator = raw.IndexOf(" — ", StringComparison.Ordinal);
            if (separator >= 0) raw = raw.Substring(0, separator);
            if (string.IsNullOrWhiteSpace(raw) || raw.StartsWith("사람/사물", StringComparison.Ordinal))
                return "무엇을 할까";
            return raw;
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
                if (string.IsNullOrEmpty(view.TargetId)) IdentifyTarget(label.text, view);

                RemoveLegacyPrefix(label);
                label.raycastTarget = false;

                if (view.TargetType == "CHARACTER") characters.Add(view);
                else if (view.TargetType == "WORLD_TARGET") objects.Add(view);
            }

            // Use stable IDs rather than display-name ordering. For the settlement this gives
            // Diya -> Jina -> Sam, which is the approved redistributed composition.
            characters = characters.OrderBy(v => v.TargetId, StringComparer.Ordinal).ToList();
            objects = objects.OrderBy(v => v.TargetId, StringComparer.Ordinal).ToList();

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
            rect.sizeDelta = new Vector2(120f, 158f);

            Image buttonBg = button.GetComponent<Image>();
            if (buttonBg != null) buttonBg.color = new Color(.03f, .035f, .04f, .62f);

            label.fontSize = 14;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            SetRect(label.rectTransform, new Vector2(.02f, .00f), new Vector2(.98f, .22f));

            Transform portraitTransform = button.transform.Find("UX18_ScenePortrait");
            Image portrait;
            if (portraitTransform == null)
            {
                GameObject go = new GameObject("UX18_ScenePortrait", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(button.transform, false);
                portrait = go.GetComponent<Image>();
                portrait.preserveAspect = true;
                portrait.raycastTarget = false;
                go.transform.SetAsFirstSibling();
            }
            else
            {
                portrait = portraitTransform.GetComponent<Image>();
                portraitTransform.gameObject.SetActive(true);
            }

            SetRect(portrait.rectTransform, new Vector2(.06f, .23f), new Vector2(.94f, .96f));

            if (content.Characters.TryGetValue(view.TargetId, out SandPlanetCharacter04 character))
            {
                Sprite sprite = GetPortraitSprite(character);
                portrait.sprite = sprite;
                portrait.color = sprite != null ? Color.white : new Color(.22f, .25f, .28f, 1f);
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
            rect.sizeDelta = new Vector2(172f, 46f);

            Image bg = button.GetComponent<Image>();
            if (bg != null) bg.color = new Color(.045f, .055f, .065f, .72f);

            label.fontSize = 13;
            label.fontStyle = FontStyle.Normal;
            label.alignment = TextAnchor.MiddleCenter;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            SetRect(label.rectTransform, new Vector2(.03f, .05f), new Vector2(.97f, .95f));

            Transform portrait = button.transform.Find("UX18_ScenePortrait");
            if (portrait != null) portrait.gameObject.SetActive(false);
        }

        private void StyleInteractionButtons()
        {
            for (int i = 0; i < interactionRoot.childCount; i++)
            {
                Transform child = interactionRoot.GetChild(i);
                if (interactionTemplate != null && child == interactionTemplate.transform) continue;
                if (!child.gameObject.activeSelf) continue;

                Button button = child.GetComponent<Button>();
                if (button == null) continue;

                LayoutElement le = child.GetComponent<LayoutElement>();
                if (le == null) le = child.gameObject.AddComponent<LayoutElement>();
                le.minHeight = 78f;
                le.preferredHeight = 78f;

                Text text = button.GetComponentInChildren<Text>(true);
                if (text != null)
                {
                    text.fontSize = 15;
                    text.fontStyle = FontStyle.Normal;
                    text.alignment = TextAnchor.MiddleLeft;
                    text.horizontalOverflow = HorizontalWrapMode.Wrap;
                    text.verticalOverflow = VerticalWrapMode.Truncate;
                }
            }
        }

        private void StyleModalButtons()
        {
            for (int i = 0; i < modalButtonRoot.childCount; i++)
            {
                Transform child = modalButtonRoot.GetChild(i);
                if (modalButtonTemplate != null && child == modalButtonTemplate.transform) continue;
                if (!child.gameObject.activeSelf) continue;

                Button button = child.GetComponent<Button>();
                if (button == null) continue;

                Text[] texts = button.GetComponentsInChildren<Text>(true);
                foreach (Text text in texts)
                {
                    Transform chip = button.transform.Find("QuestActionChip");
                    if (chip != null && text.transform.IsChildOf(chip)) continue;
                    text.fontSize = 15;
                    text.alignment = TextAnchor.MiddleCenter;
                    text.horizontalOverflow = HorizontalWrapMode.Wrap;
                    text.verticalOverflow = VerticalWrapMode.Truncate;
                    break;
                }
            }
        }

        private void CreateSpeakerPortrait()
        {
            Transform existing = modalPanel.transform.Find("UX18_SpeakerPortraitFrame");
            if (existing != null)
            {
                speakerPortraitFrame = existing.gameObject;
                speakerPortrait = existing.Find("Portrait")?.GetComponent<Image>();
                speakerPortraitName = existing.Find("Name")?.GetComponent<Text>();
                ApplySpeakerPortraitLayout();
                speakerPortraitFrame.SetActive(false);
                return;
            }

            speakerPortraitFrame = new GameObject("UX18_SpeakerPortraitFrame", typeof(RectTransform), typeof(Image));
            speakerPortraitFrame.transform.SetParent(modalPanel.transform, false);
            Image frame = speakerPortraitFrame.GetComponent<Image>();
            frame.color = new Color(.04f, .045f, .05f, .96f);
            frame.raycastTarget = false;

            GameObject portraitGo = new GameObject("Portrait", typeof(RectTransform), typeof(Image));
            portraitGo.transform.SetParent(speakerPortraitFrame.transform, false);
            speakerPortrait = portraitGo.GetComponent<Image>();
            speakerPortrait.preserveAspect = true;
            speakerPortrait.raycastTarget = false;

            GameObject nameGo = new GameObject("Name", typeof(RectTransform), typeof(Text));
            nameGo.transform.SetParent(speakerPortraitFrame.transform, false);
            speakerPortraitName = nameGo.GetComponent<Text>();
            speakerPortraitName.font = modalTitle != null && modalTitle.font != null
                ? modalTitle.font
                : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            speakerPortraitName.fontSize = 14;
            speakerPortraitName.fontStyle = FontStyle.Bold;
            speakerPortraitName.color = Color.white;
            speakerPortraitName.alignment = TextAnchor.MiddleCenter;
            speakerPortraitName.raycastTarget = false;

            ApplySpeakerPortraitLayout();
            speakerPortraitFrame.SetActive(false);
        }

        private void ApplySpeakerPortraitLayout()
        {
            if (speakerPortraitFrame == null) return;

            RectTransform frame = speakerPortraitFrame.GetComponent<RectTransform>();
            frame.anchorMin = frame.anchorMax = new Vector2(0f, .67f);
            frame.pivot = new Vector2(1f, .5f);
            frame.anchoredPosition = new Vector2(-14f, 0f);
            frame.sizeDelta = new Vector2(150f, 210f);

            if (speakerPortrait != null)
                SetRect(speakerPortrait.rectTransform, new Vector2(.05f, .16f), new Vector2(.95f, .96f));
            if (speakerPortraitName != null)
                SetRect(speakerPortraitName.rectTransform, new Vector2(.04f, .02f), new Vector2(.96f, .16f));
        }

        private void RefreshSpeakerPortrait() => SetSpeakerPortrait(CurrentSpeakerCharacterId());

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
                Sprite sprite = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(.5f, .5f),
                    100f);
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

        private static void StyleTemplate(Button template, float height, int fontSize, TextAnchor alignment)
        {
            if (template == null) return;

            LayoutElement le = template.GetComponent<LayoutElement>();
            if (le == null) le = template.gameObject.AddComponent<LayoutElement>();
            le.minHeight = height;
            le.preferredHeight = height;

            Text text = template.GetComponentInChildren<Text>(true);
            if (text != null)
            {
                text.fontSize = fontSize;
                text.alignment = alignment;
                text.horizontalOverflow = HorizontalWrapMode.Wrap;
                text.verticalOverflow = VerticalWrapMode.Truncate;
            }
        }

        private static void DisableAutomaticSizing(Transform root)
        {
            if (root == null) return;
            VerticalLayoutGroup layout = root.GetComponent<VerticalLayoutGroup>();
            if (layout != null) layout.enabled = false;
            DisableContentSizeFitter(root);
        }

        private static void DisableContentSizeFitter(Transform root)
        {
            if (root == null) return;
            ContentSizeFitter fitter = root.GetComponent<ContentSizeFitter>();
            if (fitter != null) fitter.enabled = false;
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

        private static void SetRect(RectTransform rect, Vector2 min, Vector2 max)
        {
            if (rect == null) return;
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }

    public sealed class SceneTargetView04 : MonoBehaviour
    {
        public string TargetType;
        public string TargetId;
    }
}
