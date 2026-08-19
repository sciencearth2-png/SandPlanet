using System.Collections.Generic;
using System.IO;
using SandPlanet.Prototype;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SandPlanet.EditorTools
{
    public static class SandPlanetPrototype03Builder
    {
        private const string RootFolder = "Assets/SandPlanet";
        private const string GeneratedFolder = RootFolder + "/Generated03";
        private const string MaterialFolder = GeneratedFolder + "/Materials";
        private const string ScenePath = "Assets/Scenes/SandPlanet_Prototype_03.unity";
        private const string PortraitFolder = RootFolder + "/Art/Portraits";

        [MenuItem("Tools/SandPlanet/Generate Prototype 0.3")]
        public static void GeneratePrototype03()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            EnsureFolder(RootFolder);
            EnsureFolder(GeneratedFolder);
            EnsureFolder(MaterialFolder);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "SandPlanet_Prototype_03";

            GameObject root = new GameObject("=== SAND PLANET PROTOTYPE 0.3 ===");

            CreateCamera(root.transform);
            CreateLight(root.transform);
            CreateGround(root.transform);

            CreateLocation(root.transform, "ship", "수송선", new Vector3(0f, 1.2f, 5.5f), new Vector3(5.0f, 2.4f, 3.2f), PrimitiveType.Cube, new Color(0.48f, 0.56f, 0.61f));
            CreateLocation(root.transform, "graveyard", "묘지", new Vector3(-6.2f, 0.7f, -1.6f), new Vector3(3.2f, 1.4f, 2.5f), PrimitiveType.Cube, new Color(0.38f, 0.38f, 0.42f));
            CreateLocation(root.transform, "settlement", "거주지", new Vector3(6.0f, 0.9f, 0f), new Vector3(4.2f, 1.8f, 3.5f), PrimitiveType.Cube, new Color(0.66f, 0.52f, 0.36f));
            CreateLocation(root.transform, "oasis", "오아시스", new Vector3(3.4f, 0.35f, -6.0f), new Vector3(3.8f, 0.7f, 3.8f), PrimitiveType.Cylinder, new Color(0.19f, 0.56f, 0.58f));

            GameObject controllerObject = new GameObject("Prototype03GameController");
            controllerObject.transform.SetParent(root.transform);
            SandPlanetPrototype03Controller controller = controllerObject.AddComponent<SandPlanetPrototype03Controller>();

            Sprite sam = LoadPortraitSprite("샘.png");
            Sprite jina = LoadPortraitSprite("지나.png");
            Sprite faye = LoadPortraitSprite("페이.png");
            Sprite benjamin = LoadPortraitSprite("벤자민.png");
            Sprite borichi = LoadPortraitSprite("보리치.png");
            Sprite diya = LoadPortraitSprite("디야.png");
            controller.ConfigurePortraits(sam, jina, faye, benjamin, borichi, diya);

            if (!Directory.Exists(Path.GetDirectoryName(ScenePath)))
                Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings(ScenePath);
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeGameObject = controllerObject;
            EditorGUIUtility.PingObject(controllerObject);

            int portraitCount = CountAssignedPortraits(sam, jina, faye, benjamin, borichi, diya);
            Debug.Log($"[SandPlanet] Prototype 0.3 generated. Scene: {ScenePath} / Portraits: {portraitCount}/6");
            EditorUtility.DisplayDialog(
                "SandPlanet Prototype 0.3",
                $"프로토타입 0.3 씬을 생성했습니다.\n\n{ScenePath}\n\n캐릭터 포트레이트 연결: {portraitCount}/6\n\nPlay를 눌러 Week 1 Encounter Foundation을 테스트하세요.",
                "확인");
        }

        private static Sprite LoadPortraitSprite(string fileName)
        {
            string path = PortraitFolder + "/" + fileName;
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogWarning("[SandPlanet] Portrait importer not found: " + path);
                return null;
            }

            bool changed = false;
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                changed = true;
            }

            if (importer.spriteImportMode != SpriteImportMode.Single)
            {
                importer.spriteImportMode = SpriteImportMode.Single;
                changed = true;
            }

            if (changed)
                importer.SaveAndReimport();

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
                Debug.LogWarning("[SandPlanet] Portrait sprite could not be loaded: " + path);

            return sprite;
        }

        private static int CountAssignedPortraits(params Sprite[] sprites)
        {
            int count = 0;
            foreach (Sprite sprite in sprites)
            {
                if (sprite != null)
                    count++;
            }
            return count;
        }

        private static Camera CreateCamera(Transform parent)
        {
            GameObject go = new GameObject("Main Camera");
            go.transform.SetParent(parent);
            go.tag = "MainCamera";
            go.transform.position = new Vector3(0f, 18f, -19f);
            go.transform.rotation = Quaternion.LookRotation(Vector3.zero - go.transform.position, Vector3.up);

            Camera camera = go.AddComponent<Camera>();
            camera.fieldOfView = 46f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 300f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.10f, 0.075f, 0.055f);
            return camera;
        }

        private static void CreateLight(Transform parent)
        {
            GameObject go = new GameObject("Directional Light");
            go.transform.SetParent(parent);
            go.transform.rotation = Quaternion.Euler(48f, -32f, 0f);

            Light light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.35f;
            light.shadows = LightShadows.Soft;
        }

        private static void CreateGround(Transform parent)
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "SandPlanet_GreyboxGround";
            ground.transform.SetParent(parent);
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(3.0f, 1f, 3.0f);

            Renderer renderer = ground.GetComponent<Renderer>();
            renderer.sharedMaterial = GetOrCreateMaterial("M03_Sand_Greybox", new Color(0.64f, 0.48f, 0.29f));
        }

        private static void CreateLocation(
            Transform parent,
            string id,
            string label,
            Vector3 position,
            Vector3 scale,
            PrimitiveType primitive,
            Color color)
        {
            GameObject location = GameObject.CreatePrimitive(primitive);
            location.transform.SetParent(parent);
            location.transform.position = position;
            location.transform.localScale = scale;

            PrototypeLocationNode node = location.AddComponent<PrototypeLocationNode>();
            node.Configure(id, label);

            Renderer renderer = location.GetComponent<Renderer>();
            renderer.sharedMaterial = GetOrCreateMaterial("M03_" + id, color);
        }

        private static Material GetOrCreateMaterial(string materialName, Color color)
        {
            string path = $"{MaterialFolder}/{materialName}.mat";
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
                return existing;

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");

            Material material = new Material(shader)
            {
                name = materialName
            };

            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            else if (material.HasProperty("_Color"))
                material.SetColor("_Color", color);

            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static void AddSceneToBuildSettings(string scenePath)
        {
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            foreach (EditorBuildSettingsScene existing in scenes)
            {
                if (existing.path == scenePath)
                    return;
            }

            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
                return;

            string parent = Path.GetDirectoryName(folderPath)?.Replace("\\", "/");
            string name = Path.GetFileName(folderPath);

            if (!string.IsNullOrEmpty(parent))
                EnsureFolder(parent);

            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
