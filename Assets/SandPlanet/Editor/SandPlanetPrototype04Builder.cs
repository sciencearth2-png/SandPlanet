#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SandPlanet.Prototype;
using SandPlanet.Prototype.DataDriven;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SandPlanet.EditorTools
{
    /// <summary>
    /// Creates a separate 0.4 scene. 0.1~0.3 remain untouched.
    /// The generated scene contains two editable uGUI/3D roots:
    /// Day 1~14 planet hub and Day 15~21 ship interior hub.
    /// </summary>
    public static class SandPlanetPrototype04Builder
    {
        private const string Root = "Assets/SandPlanet";
        private const string Generated = Root + "/Generated04";
        private const string Materials = Generated + "/Materials";
        private const string ScenePath = "Assets/Scenes/SandPlanet_Prototype_04.unity";

        private static readonly string[] CsvFiles =
        {
            "Locations.csv", "Characters.csv", "WorldTargets.csv", "Quests.csv", "QuestSteps.csv",
            "Interactions.csv", "Choices.csv", "ChoiceBeats.csv", "States.csv", "Events.csv", "EventTriggers.csv", "NpcSchedules.csv"
        };

        [MenuItem("Tools/SandPlanet/Generate Prototype 0.4")]
        public static void Generate()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            bool hasMaster = File.Exists(SandPlanetSpreadsheetExporter04.MasterWorkbookPath);
            if (hasMaster && !SandPlanetSpreadsheetExporter04.Export(false))
            {
                EditorUtility.DisplayDialog("SandPlanet Prototype 0.4", "Master Excel → CSV 변환/검증에 실패했습니다. Console을 확인하세요.", "확인");
                return;
            }
            if (!hasMaster && !SandPlanetSpreadsheetExporter04.GeneratedCsvExists())
            {
                EditorUtility.DisplayDialog("SandPlanet Prototype 0.4",
                    "Master Excel과 Generated CSV가 모두 없습니다.\n\n먼저 SandPlanet_Master.xlsx를 다음 위치에 넣으세요.\n" + SandPlanetSpreadsheetExporter04.MasterWorkbookPath,
                    "확인");
                return;
            }
            if (!hasMaster)
                Debug.LogWarning("[SandPlanet 0.4] Master Excel 미탑재 상태입니다. GitHub에 포함된 Generated CSV로 씬을 생성합니다.");

            EnsureFolder(Root); EnsureFolder(Generated); EnsureFolder(Materials);
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "SandPlanet_Prototype_04";
            GameObject root = new GameObject("=== SAND PLANET PROTOTYPE 0.4 DATA DRIVEN ===");

            Camera camera = CreateCamera(root.transform);
            CreateLight(root.transform);
            GameObject planet = CreatePlanetHub(root.transform);
            GameObject ship = CreateShipInteriorHub(root.transform);
            ship.SetActive(false);
            CreateEventSystem(root.transform);
            Ui ui = CreateUi(root.transform);

            GameObject controllerObject = new GameObject("Prototype04GameController");
            controllerObject.transform.SetParent(root.transform);
            SandPlanetPrototype04Controller controller = controllerObject.AddComponent<SandPlanetPrototype04Controller>();
            controller.Configure(LoadCsvAssets(), camera, planet, ship,
                ui.Hud, ui.QuestTracker, ui.Log, ui.EndDay,
                ui.LocationPanel, ui.LocationTitle, ui.LocationHint,
                ui.TargetRoot, ui.InteractionRoot, ui.TargetTemplate, ui.InteractionTemplate, ui.Back,
                ui.ModalPanel, ui.ModalTitle, ui.ModalBody, ui.ModalRoot, ui.ModalTemplate);

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath) ?? "Assets/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings(ScenePath);
            AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
            Selection.activeGameObject = controllerObject;
            EditorGUIUtility.PingObject(controllerObject);
            EditorUtility.DisplayDialog("SandPlanet Prototype 0.4",
                $"0.4 씬 생성 완료\n\n{ScenePath}\n\nDay 1~14: 행성 4장소\nDay 15~21: 수송선 내부 4구역\n\n정적 콘텐츠는 Generated CSV에서 읽습니다.", "확인");
        }

        private static TextAsset[] LoadCsvAssets()
        {
            List<TextAsset> result = new List<TextAsset>();
            foreach (string file in CsvFiles)
            {
                string path = SandPlanetSpreadsheetExporter04.CsvFolderPath + "/" + file;
                TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
                if (asset == null) Debug.LogError("[SandPlanet 0.4] CSV TextAsset 없음: " + path);
                else result.Add(asset);
            }
            return result.ToArray();
        }

        private static Camera CreateCamera(Transform parent)
        {
            GameObject go = new GameObject("Main Camera"); go.tag = "MainCamera"; go.transform.SetParent(parent);
            go.transform.position = new Vector3(0f, 20f, -23f);
            go.transform.rotation = Quaternion.LookRotation(new Vector3(0f, 0f, 0f) - go.transform.position, Vector3.up);
            Camera cam = go.AddComponent<Camera>(); cam.fieldOfView = 44f; cam.nearClipPlane = 0.1f; cam.farClipPlane = 300f;
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.08f, 0.065f, 0.055f);
            return cam;
        }

        private static void CreateLight(Transform parent)
        {
            GameObject go = new GameObject("Directional Light"); go.transform.SetParent(parent); go.transform.rotation = Quaternion.Euler(52f, -35f, 0f);
            Light light = go.AddComponent<Light>(); light.type = LightType.Directional; light.intensity = 1.35f; light.shadows = LightShadows.Soft;
        }

        private static GameObject CreatePlanetHub(Transform parent)
        {
            GameObject root = new GameObject("PlanetHubRoot_Day01_14"); root.transform.SetParent(parent);
            Primitive(root.transform, "PlanetGround", PrimitiveType.Plane, Vector3.zero, new Vector3(3.3f, 1f, 3.3f), "M04_Sand", new Color(0.62f, 0.46f, 0.28f));
            Location(root.transform, "LOC_01_SETTLEMENT", "거주지", new Vector3(6f, .9f, 0f), new Vector3(4.2f, 1.8f, 3.3f), new Color(.68f, .52f, .34f));
            Location(root.transform, "LOC_02_SHIP", "수송선", new Vector3(0f, 1.2f, 5.5f), new Vector3(5f, 2.4f, 3f), new Color(.48f, .56f, .62f));
            Location(root.transform, "LOC_03_GRAVEYARD", "묘지", new Vector3(-6f, .7f, -1.4f), new Vector3(3.4f, 1.4f, 2.4f), new Color(.37f, .37f, .42f));
            Location(root.transform, "LOC_04_OASIS", "오아시스", new Vector3(3.2f, .35f, -5.8f), new Vector3(3.7f, .7f, 3.7f), new Color(.18f, .52f, .55f), PrimitiveType.Cylinder);
            return root;
        }

        private static GameObject CreateShipInteriorHub(Transform parent)
        {
            GameObject root = new GameObject("ShipInteriorHubRoot_Day15_21"); root.transform.SetParent(parent);
            Primitive(root.transform, "ShipInteriorFloor", PrimitiveType.Cube, new Vector3(0f, -.45f, 0f), new Vector3(18f, .6f, 13f), "M04_ShipFloor", new Color(.13f, .16f, .19f));
            Location(root.transform, "LOC_05_COMMAND", "지휘·항해", new Vector3(-4.7f, .75f, 3.4f), new Vector3(7.2f, 1.5f, 4.6f), new Color(.18f, .32f, .46f));
            Location(root.transform, "LOC_06_SUPPLY", "보급·생명유지", new Vector3(4.7f, .75f, 3.4f), new Vector3(7.2f, 1.5f, 4.6f), new Color(.35f, .37f, .22f));
            Location(root.transform, "LOC_07_TECH", "기관·연구", new Vector3(-4.7f, .75f, -3.4f), new Vector3(7.2f, 1.5f, 4.6f), new Color(.30f, .24f, .43f));
            Location(root.transform, "LOC_08_HABIT", "거주", new Vector3(4.7f, .75f, -3.4f), new Vector3(7.2f, 1.5f, 4.6f), new Color(.44f, .29f, .25f));
            return root;
        }

        private static void Location(Transform parent, string id, string label, Vector3 pos, Vector3 scale, Color color, PrimitiveType type = PrimitiveType.Cube)
        {
            GameObject go = Primitive(parent, id, type, pos, scale, "M04_" + id, color);
            PrototypeLocationNode node = go.AddComponent<PrototypeLocationNode>(); node.Configure(id, label);
            GameObject textGo = new GameObject("Label_" + id); textGo.transform.SetParent(parent); textGo.transform.position = pos + new Vector3(0f, Mathf.Max(1.4f, scale.y * .75f), -.25f); textGo.transform.rotation = Quaternion.Euler(55f, 0f, 0f);
            TextMesh mesh = textGo.AddComponent<TextMesh>(); mesh.text = label; mesh.fontSize = 42; mesh.characterSize = .18f; mesh.anchor = TextAnchor.MiddleCenter; mesh.alignment = TextAlignment.Center; mesh.color = Color.white;
        }

        private static GameObject Primitive(Transform parent, string name, PrimitiveType type, Vector3 pos, Vector3 scale, string materialName, Color color)
        {
            GameObject go = GameObject.CreatePrimitive(type); go.name = name; go.transform.SetParent(parent); go.transform.position = pos; go.transform.localScale = scale;
            Renderer renderer = go.GetComponent<Renderer>(); if (renderer != null) renderer.sharedMaterial = Material(materialName, color);
            return go;
        }

        private static Material Material(string name, Color color)
        {
            string path = Materials + "/" + name + ".mat";
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path); if (existing != null) return existing;
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material material = new Material(shader) { name = name };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color); else if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            AssetDatabase.CreateAsset(material, path); return material;
        }

        private sealed class Ui
        {
            public Text Hud, QuestTracker, Log, LocationTitle, LocationHint, ModalTitle, ModalBody;
            public Button EndDay, TargetTemplate, InteractionTemplate, Back, ModalTemplate;
            public GameObject LocationPanel, ModalPanel;
            public Transform TargetRoot, InteractionRoot, ModalRoot;
        }

        private static Ui CreateUi(Transform parent)
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            GameObject canvasGo = new GameObject("Canvas_Prototype04", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)); canvasGo.transform.SetParent(parent);
            Canvas canvas = canvasGo.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
            Ui ui = new Ui();

            GameObject hudPanel = Panel(canvasGo.transform, "HUDPanel", new Color(.03f, .035f, .04f, .94f)); Anchor(hudPanel.GetComponent<RectTransform>(), new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -62), Vector2.zero);
            ui.Hud = Text(hudPanel.transform, "HUD", font, 22, TextAnchor.MiddleLeft); Stretch(ui.Hud.rectTransform, 18, 18, 8, 8);
            ui.EndDay = Button(canvasGo.transform, "EndDay", font, "하루 종료", new Color(.42f, .22f, .14f)); SetTopRight(ui.EndDay.GetComponent<RectTransform>(), new Vector2(-18, -72), new Vector2(210, 52));

            GameObject questPanel = Panel(canvasGo.transform, "QuestTrackerPanel", new Color(.04f, .05f, .06f, .90f)); SetTopLeft(questPanel.GetComponent<RectTransform>(), new Vector2(16, -76), new Vector2(500, 320));
            ui.QuestTracker = Text(questPanel.transform, "QuestTracker", font, 18, TextAnchor.UpperLeft); Stretch(ui.QuestTracker.rectTransform, 15, 15, 15, 15);
            GameObject logPanel = Panel(canvasGo.transform, "LogPanel", new Color(.04f, .05f, .06f, .82f)); SetBottomLeft(logPanel.GetComponent<RectTransform>(), new Vector2(16, 16), new Vector2(610, 230));
            ui.Log = Text(logPanel.transform, "Log", font, 16, TextAnchor.LowerLeft); Stretch(ui.Log.rectTransform, 14, 14, 14, 14);

            ui.LocationPanel = Panel(canvasGo.transform, "LocationPanel", new Color(.04f, .05f, .06f, .97f)); Anchor(ui.LocationPanel.GetComponent<RectTransform>(), new Vector2(.53f, .06f), new Vector2(.99f, .92f), Vector2.zero, Vector2.zero);
            ui.LocationTitle = Text(ui.LocationPanel.transform, "LocationTitle", font, 30, TextAnchor.MiddleLeft); Anchor(ui.LocationTitle.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(20, -76), new Vector2(-175, -16));
            ui.Back = Button(ui.LocationPanel.transform, "Back", font, "허브로", new Color(.22f, .28f, .34f)); SetTopRight(ui.Back.GetComponent<RectTransform>(), new Vector2(-18, -18), new Vector2(140, 48));
            ui.LocationHint = Text(ui.LocationPanel.transform, "LocationHint", font, 17, TextAnchor.UpperLeft); Anchor(ui.LocationHint.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(20, -136), new Vector2(-20, -82));
            GameObject targetPanel = Panel(ui.LocationPanel.transform, "Targets", new Color(.08f, .095f, .11f, .95f)); Anchor(targetPanel.GetComponent<RectTransform>(), new Vector2(.02f, .05f), new Vector2(.36f, .81f), Vector2.zero, Vector2.zero);
            ui.TargetRoot = Vertical(targetPanel.transform, "TargetButtons"); Stretch(ui.TargetRoot.GetComponent<RectTransform>(), 10, 10, 10, 10); ui.TargetTemplate = Button(ui.TargetRoot, "TargetTemplate", font, "대상", new Color(.18f, .25f, .31f)); Height(ui.TargetTemplate.gameObject, 52);
            GameObject interactionPanel = Panel(ui.LocationPanel.transform, "Interactions", new Color(.08f, .095f, .11f, .95f)); Anchor(interactionPanel.GetComponent<RectTransform>(), new Vector2(.38f, .05f), new Vector2(.98f, .81f), Vector2.zero, Vector2.zero);
            ui.InteractionRoot = Vertical(interactionPanel.transform, "InteractionButtons"); Stretch(ui.InteractionRoot.GetComponent<RectTransform>(), 10, 10, 10, 10); ui.InteractionTemplate = Button(ui.InteractionRoot, "InteractionTemplate", font, "행동", new Color(.31f, .25f, .18f)); Height(ui.InteractionTemplate.gameObject, 58);

            ui.ModalPanel = Panel(canvasGo.transform, "ModalPanel", new Color(.02f, .025f, .03f, .99f)); Anchor(ui.ModalPanel.GetComponent<RectTransform>(), new Vector2(.24f, .17f), new Vector2(.76f, .85f), Vector2.zero, Vector2.zero);
            ui.ModalTitle = Text(ui.ModalPanel.transform, "ModalTitle", font, 31, TextAnchor.MiddleLeft); Anchor(ui.ModalTitle.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(28, -78), new Vector2(-28, -20));
            ui.ModalBody = Text(ui.ModalPanel.transform, "ModalBody", font, 20, TextAnchor.UpperLeft); Anchor(ui.ModalBody.rectTransform, new Vector2(0, .36f), new Vector2(1, .86f), new Vector2(28, 10), new Vector2(-28, -8));
            ui.ModalRoot = Vertical(ui.ModalPanel.transform, "ModalButtons"); Anchor(ui.ModalRoot.GetComponent<RectTransform>(), new Vector2(0, 0), new Vector2(1, .35f), new Vector2(28, 22), new Vector2(-28, -8)); ui.ModalTemplate = Button(ui.ModalRoot, "ModalTemplate", font, "선택지", new Color(.27f, .32f, .37f)); Height(ui.ModalTemplate.gameObject, 58);
            ui.LocationPanel.SetActive(false); ui.ModalPanel.SetActive(false);
            return ui;
        }

        private static void CreateEventSystem(Transform parent)
        {
            GameObject go = new GameObject("EventSystem", typeof(EventSystem)); go.transform.SetParent(parent);
            Type inputSystemType = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (inputSystemType != null) go.AddComponent(inputSystemType); else go.AddComponent<StandaloneInputModule>();
        }

        private static GameObject Panel(Transform parent, string name, Color color) { GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image)); go.transform.SetParent(parent); go.GetComponent<Image>().color = color; return go; }
        private static Text Text(Transform parent, string name, Font font, int size, TextAnchor anchor) { GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text)); go.transform.SetParent(parent); Text t = go.GetComponent<Text>(); t.font = font; t.fontSize = size; t.alignment = anchor; t.color = Color.white; t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow; return t; }
        private static Button Button(Transform parent, string name, Font font, string label, Color color) { GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button)); go.transform.SetParent(parent); go.GetComponent<Image>().color = color; Button b = go.GetComponent<Button>(); Text t = Text(go.transform, "Text", font, 17, TextAnchor.MiddleCenter); t.text = label; Stretch(t.rectTransform, 10, 10, 6, 6); return b; }
        private static Transform Vertical(Transform parent, string name) { GameObject go = new GameObject(name, typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter)); go.transform.SetParent(parent); VerticalLayoutGroup v = go.GetComponent<VerticalLayoutGroup>(); v.spacing = 8; v.childControlHeight = true; v.childControlWidth = true; v.childForceExpandHeight = false; v.childForceExpandWidth = true; ContentSizeFitter f = go.GetComponent<ContentSizeFitter>(); f.verticalFit = ContentSizeFitter.FitMode.PreferredSize; return go.transform; }
        private static void Height(GameObject go, float h) { LayoutElement e = go.AddComponent<LayoutElement>(); e.preferredHeight = h; e.minHeight = h; }
        private static void Stretch(RectTransform r, float l, float rr, float b, float t) { r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = new Vector2(l, b); r.offsetMax = new Vector2(-rr, -t); }
        private static void Anchor(RectTransform r, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax) { r.anchorMin = min; r.anchorMax = max; r.offsetMin = offsetMin; r.offsetMax = offsetMax; }
        private static void SetTopLeft(RectTransform r, Vector2 pos, Vector2 size) { r.anchorMin = r.anchorMax = r.pivot = new Vector2(0, 1); r.anchoredPosition = pos; r.sizeDelta = size; }
        private static void SetBottomLeft(RectTransform r, Vector2 pos, Vector2 size) { r.anchorMin = r.anchorMax = r.pivot = Vector2.zero; r.anchoredPosition = pos; r.sizeDelta = size; }
        private static void SetTopRight(RectTransform r, Vector2 pos, Vector2 size) { r.anchorMin = r.anchorMax = r.pivot = Vector2.one; r.anchoredPosition = pos; r.sizeDelta = size; }

        private static void AddSceneToBuildSettings(string path)
        {
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (scenes.All(s => s.path != path)) { scenes.Add(new EditorBuildSettingsScene(path, true)); EditorBuildSettings.scenes = scenes.ToArray(); }
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            string parent = Path.GetDirectoryName(folder)?.Replace('\\', '/'); string name = Path.GetFileName(folder);
            if (!string.IsNullOrEmpty(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
#endif