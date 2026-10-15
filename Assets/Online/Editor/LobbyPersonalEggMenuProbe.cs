using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DeadBoat.Online.Editor
{
    public static class LobbyPersonalEggMenuProbe
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        public static string Run()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Run in Edit Mode");
            var singleton = typeof(EggHatchingManager).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic);
            var previous = singleton.GetValue(null);
            Scene preview = default;
            GameObject uiRoot = null, ownerObject = null;
            string before = PlayerPrefs.GetString("EggFeatureState_v1", "");
            using (EggFeatureStorage.UseTemporaryStorageForTests())
            try
            {
                preview = EditorSceneManager.OpenPreviewScene("Assets/Scenes/Lobby.unity");
                var components = preview.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<MonoBehaviour>(true)).ToArray();
                var manager = components.OfType<EggHatchingManager>().Single();
                var gameCanvas = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/GameCanvas.prefab");
                foreach (var type in new[] { typeof(EggNestSelectionPanel), typeof(AnimalInventoryPanel), typeof(AnimalLoadoutPanel), typeof(AnimalMergeSelectionPanel) })
                {
                    var panels = gameCanvas.GetComponentsInChildren(type, true);
                    Require(panels.Length == 1, "GameCanvas contains " + type.Name);
                    var serialized = new SerializedObject(panels[0]);
                    Require(serialized.FindProperty("_panel").objectReferenceValue != null && panels[0].gameObject.activeSelf,
                        "active bound panel " + type.Name);
                    foreach (string field in new[] { "_slotsRoot", "_slotPrefab", "_inventorySlotsRoot", "_inventorySlotPrefab" })
                    {
                        var property = serialized.FindProperty(field);
                        if (property != null) Require(property.objectReferenceValue != null, "bound " + type.Name + field);
                    }
                }

                singleton.SetValue(null, manager);
                var catalog = Resources.Load<EggHatchingCatalog>("Eggs/EggHatchingCatalog");
                var egg = catalog.Definitions.First(x => x != null && !string.IsNullOrEmpty(x.eggId));
                var state = new EggFeatureState { hasDiscoveredEggs = true, hasHatchedAnimal = true, hasUnlockedAnimalMerge = true };
                state.AddEgg(egg.eggId, 3);
                EggFeatureStorage.Save(state);
                manager.LoadState();
                var points = manager.GetNestPoints().Where(p => p != null).ToArray();
                Require(points.Length == 4, "four real scene nests");
                manager.SetWorldHidden(true);
                string id = points[0].NestId;
                Require(manager.TryStartIncubation(id, egg.eggId), "start incubation");
                Require(!manager.TryStartIncubation(id, egg.eggId) && manager.GetTotalEggCount() == 2, "occupied nest cannot consume another egg");
                long finish = manager.GetNestState(id).finishAtUnix;
                var visibility = components.OfType<EggFeatureUnlockVisibility>().Single();
                var personalRoots = new[] { "_eggNestObjects", "_animalPlacementObjects", "_animalMergeObjects", "_eggNestTutorialObjects", "_animalPlacementTutorialObjects" }
                    .SelectMany(f => (GameObject[])typeof(EggFeatureUnlockVisibility).GetField(f, Private).GetValue(visibility)).Where(x => x != null).ToArray();
                Require(personalRoots.Length == 5, "all personal scene groups configured");
                Require(components.Where(x => x is EggNestPoint || x is AnimalLoadoutPoint || x is AnimalPlacementPoint || x is AnimalMergePoint)
                    .All(x => personalRoots.Any(r => x.transform.IsChildOf(r.transform))), "personal interaction points are under hidden roots");
                manager.SetWorldHidden(true);
                visibility.SetWorldHidden(true);
                foreach (var p in components.OfType<AnimalPlacementPoints>()) { p.SetWorldHidden(true); p.RefreshNow(); }
                foreach (var p in components.OfType<AnimalLoadoutBoardSpawner>()) { p.SetWorldHidden(true); p.RefreshNow(); }
                visibility.Refresh();
                Require(personalRoots.All(x => !x.activeSelf), "world groups including colliders and arrows remain hidden after refresh");
                Call(manager, "TryRestoreMissingSpawnedAnimals");
                Require(points.All(p => (GameObject)typeof(EggNestPoint).GetField("_eggPreviewInstance", Private).GetValue(p) == null), "no nest previews while hidden");
                manager.LoadState();
                Require(manager.GetNestState(id).finishAtUnix == finish && manager.GetTotalEggCount() == 2, "incubation and inventory survive hidden reload");
                state = EggFeatureStorage.Load();
                state.nests.First(n => n.nestId == id).finishAtUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - 1;
                EggFeatureStorage.Save(state);
                manager.LoadState();
                Require(manager.TryCollectReadyAnimal(id) && !manager.TryCollectReadyAnimal(id), "collect exactly once while hidden");
                var animal = manager.GetOwnedAnimals().First();
                Require(manager.TryAssignAnimalToLoadoutSlot(0, animal.animalId, animal.stage), "private bonus selection");
                manager.LoadState();
                Require(manager.GetAnimalLoadoutSlot(0)?.animalId == animal.animalId, "loadout survives reload");
                manager.SetWorldHidden(false);
                visibility.SetWorldHidden(false);
                foreach (var p in components.OfType<AnimalPlacementPoints>()) p.SetWorldHidden(false);
                foreach (var p in components.OfType<AnimalLoadoutBoardSpawner>()) p.SetWorldHidden(false);
                Require(personalRoots.Take(3).All(x => x.activeSelf), "offline nests, animal points and merge restored");

                manager.SetWorldHidden(true);
                Call(visibility, "OnDisable");
                Call(visibility, "OnEnable");
                personalRoots[0].SetActive(false);
                Require(manager.TryStartIncubation(points[1].NestId, egg.eggId) && personalRoots[0].activeSelf,
                    "visibility resubscribes after disable/enable");
                manager.SetWorldHidden(true);
                visibility.SetWorldHidden(true);

                // UI construction uses the same four nest IDs, no additional capacity or cloned progress.
                ownerObject = new GameObject("Personal menu probe owner");
                SceneManager.MoveGameObjectToScene(ownerObject, preview);
                var owner = ownerObject.AddComponent<LobbyOnlineBootstrap>();
                typeof(LobbyOnlineBootstrap).GetField("mode", Private).SetValue(owner, LobbyOnlineMode.Online);
                typeof(LobbyOnlineBootstrap).GetField("connecting", Private).SetValue(owner, true);
                var menu = ownerObject.AddComponent<LobbyPersonalEggMenu>();
                menu.Initialize(owner);
                uiRoot = (GameObject)typeof(LobbyPersonalEggMenu).GetField("root", Private).GetValue(menu);
                SceneManager.MoveGameObjectToScene(uiRoot, preview);
                var chip = (GameObject)typeof(LobbyPersonalEggMenu).GetField("chip", Private).GetValue(menu);
                Require(chip.activeSelf, "private entry available in online lobby");
                Call(menu, "SetOpen", true);
                var panel = (GameObject)typeof(LobbyPersonalEggMenu).GetField("panel", Private).GetValue(menu);
                Require(panel.activeSelf && panel.GetComponentsInChildren<Button>(true).Count(x => x.name == "Nest action") == 4, "four incubation actions rendered");
                Capture(uiRoot, preview);
                typeof(LobbyOnlineBootstrap).GetField("connecting", Private).SetValue(owner, false);
                typeof(LobbyOnlineBootstrap).GetField("mode", Private).SetValue(owner, LobbyOnlineMode.Offline);
                Call(menu, "Refresh");
                Require(!panel.activeSelf && !chip.activeSelf && manager.GetAnimalLoadoutSlot(0) != null, "offline transition closes UI without losing loadout");
                Require(PlayerPrefs.GetString("EggFeatureState_v1", "") == before, "owner PlayerPrefs untouched");
                return "PASS: real Lobby four nests/groups, occupied-click guard, hidden incubation/reload/collect exactly once, loadout restore, offline world restore and visibility rebind, bound GameCanvas panels, UI online/offline; temporary storage only";
            }
            finally
            {
                // Clear detached runtime UI before destroying its component in Edit Mode.
                if (ownerObject != null)
                {
                    var menu = ownerObject.GetComponent<LobbyPersonalEggMenu>();
                    if (menu != null) typeof(LobbyPersonalEggMenu).GetField("root", Private).SetValue(menu, null);
                }
                if (uiRoot != null) UnityEngine.Object.DestroyImmediate(uiRoot);
                if (ownerObject != null) UnityEngine.Object.DestroyImmediate(ownerObject);
                if (preview.IsValid()) EditorSceneManager.ClosePreviewScene(preview);
                singleton.SetValue(null, previous);
            }
        }
        private static void Capture(GameObject root, Scene scene)
        {
            var cameraObject = new GameObject("Personal menu preview camera");
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            var camera = cameraObject.AddComponent<Camera>();
            camera.overrideSceneCullingMask = EditorSceneManager.GetSceneCullingMask(scene);
            camera.enabled = false; camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.07f, .12f, .16f); camera.cullingMask = 1 << 5;
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1;
            foreach (var t in root.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 5;
            var rt = RenderTexture.GetTemporary(1280, 720, 24);
            var previous = RenderTexture.active;
            Texture2D image = null;
            try
            {
                camera.targetTexture = rt; Canvas.ForceUpdateCanvases(); camera.Render(); RenderTexture.active = rt;
                image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); image.Apply();
                System.IO.Directory.CreateDirectory("Builds");
                System.IO.File.WriteAllBytes("Builds/PersonalEggMenuPreview.png", image.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous; camera.targetTexture = null; RenderTexture.ReleaseTemporary(rt);
                if (image != null) UnityEngine.Object.DestroyImmediate(image);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }
        private static object Call(object target, string method, params object[] args) => target.GetType().GetMethod(method, Private).Invoke(target, args);
        private static void Require(bool condition, string name) { if (!condition) throw new InvalidOperationException(name); }
    }
}
