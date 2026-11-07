using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DeadBoat.Online.Editor
{
    public static class ProfessionPortraitTools
    {
        private const string Folder = "Assets/Art/ProfessionPortraits";

        public static void GenerateAndCheck()
        {
            if (EditorApplication.isPlaying || BuildPipeline.isBuildingPlayer)
                throw new InvalidOperationException("Idle Editor required");
            Directory.CreateDirectory(Folder);
            var catalog = Resources.Load<ProfessionCatalog>("Professions/ProfessionCatalog");
            foreach (var definition in catalog.GetValidDefinitions())
            {
                var path = Folder + "/" + definition.professionId + ".png";
                CaptureAvatar(definition.professionId, path);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaSource = TextureImporterAlphaSource.FromInput;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.isReadable = false;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.maxTextureSize = 512;
                importer.textureCompression = TextureImporterCompression.Compressed;
                importer.SaveAndReimport();
                definition.icon = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (!definition.icon) throw new Exception("Missing portrait " + definition.professionId);
            }
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            CheckAndCaptureUI();
        }

        private static GameObject SceneRoot(Scene scene)
        {
            var root = new GameObject("Portrait preview only");
            SceneManager.MoveGameObjectToScene(root, scene);
            return root;
        }

        private static Camera CameraFor(GameObject root, Scene scene)
        {
            var go = new GameObject("Preview camera", typeof(Camera));
            go.transform.SetParent(root.transform, false);
            var cam = go.GetComponent<Camera>(); cam.scene = scene;
            cam.cullingMask = 1 << 31; cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.clear; cam.orthographic = true;
            return cam;
        }

        private static void Isolate(GameObject root)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 31;
        }

        private static void CaptureAvatar(string id, string path)
        {
            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var root = SceneRoot(scene);
                var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Online/Avatars/Kenney/character-a.fbx");
                var model = UnityEngine.Object.Instantiate(source, root.transform);
                var visual = model.AddComponent<AvatarProfessionVisual>();
                visual.Initialize(model.transform); visual.Apply(AvatarProfessionVisual.SkinFor(id));
                var camera = CameraFor(root, scene);
                camera.orthographicSize = 1.62f;
                var center = new Vector3(0, 1.45f, 0);
                camera.transform.position = center + new Vector3(.6f, .25f, 10);
                camera.transform.LookAt(center);
                var lightGo = new GameObject("Portrait light", typeof(Light));
                lightGo.transform.SetParent(root.transform, false);
                lightGo.transform.rotation = Quaternion.Euler(35, 155, 0);
                var light = lightGo.GetComponent<Light>(); light.type = LightType.Directional;
                light.intensity = .75f; light.cullingMask = 1 << 31;
                Isolate(root);
                Capture(camera, 384, 512, path);
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }

        public static void CheckAndCaptureUI()
        {
            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var root = SceneRoot(scene);
                var cam = CameraFor(root, scene);
                cam.transform.position = new Vector3(0, 0, -10);
                cam.backgroundColor = new Color(.13f, .21f, .26f, 1);
                var canvasGo = new GameObject("UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvasGo.transform.SetParent(root.transform, false);
                var canvas = canvasGo.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = cam;
                canvas.planeDistance = 5;
                var scaler = canvasGo.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1000, 700);
                var card = ProfessionUnlockPresentation.Create(canvas.transform);
                int count = 0;
                foreach (var definition in Resources.Load<ProfessionCatalog>("Professions/ProfessionCatalog").GetValidDefinitions())
                {
                    card.Show(definition);
                    var image = card.transform.Find("Card/Character").GetComponent<Image>();
                    if (!card.gameObject.activeSelf || image.sprite != definition.icon || !image.sprite)
                        throw new Exception("Reward mismatch " + definition.professionId);
                    card.transform.Find("Card/Continue").GetComponent<Button>().onClick.Invoke();
                    if (card.gameObject.activeSelf) throw new Exception("Continue failed");
                    count++;
                }
                var catalog = Resources.Load<ProfessionCatalog>("Professions/ProfessionCatalog");
                catalog.TryGet("vampire", out var sample); card.Show(sample);
                Isolate(root); Canvas.ForceUpdateCanvases();
                Directory.CreateDirectory("Docs/previews");
                var localization = AssetDatabase.LoadAssetAtPath<LocalizationData>("Assets/Igrodelnya/Localization/LocalizationData.asset");
                // Batch has no gameplay LocalizationManager. Use the same actual table for its preview.
                foreach (var language in new[] { "Ru", "En" })
                {
                    if (!localization.TryGetTranslation("UI/Profession/Definition/vampire/Title", language, out var name) ||
                        !localization.TryGetTranslation("UI/Profession/UnlockedFormat", language, out var format) ||
                        !localization.TryGetTranslation("UI/Profession/Button/Continue", language, out var button))
                        throw new Exception("Missing reward translation " + language);
                    card.transform.Find("Card/Title").GetComponent<Text>().text = string.Format(format, name);
                    card.transform.Find("Card/Continue/Label").GetComponent<Text>().text = button;
                    foreach (var graphic in root.GetComponentsInChildren<Graphic>(true)) graphic.SetAllDirty();
                    Canvas.ForceUpdateCanvases();
                    // A single render target capture per preview canvas: repeated Camera.Render
                    // after detaching its target can cull Image batches in batchmode.
                    if (language == "Ru") Capture(cam, 1000, 700, "Docs/previews/ProfessionUnlockReward.png");
                }
                card.Hide();
                Debug.Log("[Profession portraits] PASS: " + count + " catalog sprites; reusable reward matching, Continue dismisses; RU/EN table, RU preview; no purchase/save/Play/Build");
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }

        private static void Capture(Camera camera, int width, int height, string path)
        {
            var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            var previous = RenderTexture.active;
            var png = new Texture2D(width, height, TextureFormat.RGBA32, false);
            try
            {
                camera.targetTexture = rt; Canvas.ForceUpdateCanvases(); camera.Render(); RenderTexture.active = rt;
                png.ReadPixels(new Rect(0, 0, width, height), 0, 0); png.Apply();
                File.WriteAllBytes(path, png.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous; camera.targetTexture = null;
                UnityEngine.Object.DestroyImmediate(png); UnityEngine.Object.DestroyImmediate(rt);
            }
        }
    }
}
