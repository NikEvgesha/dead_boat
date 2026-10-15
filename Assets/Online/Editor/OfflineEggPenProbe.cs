using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeadBoat.Online.Editor
{
    public static class OfflineEggPenProbe
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        public static string Result { get; private set; } = "not run";
        public static void Verify() => Run();

        private static async void Run()
        {
            if (!EditorApplication.isPlaying || Result == "running") return;
            Result = "running";
            Scene preview = default;
            GameObject hud = null, lobby = null;
            using (EggFeatureStorage.UseTemporaryStorageForTests())
            try
            {
                Require(LoadingManager.Instance == null && EggHatchingManager.Instance == null, "isolated empty scene");
                var catalog = Resources.Load<EggHatchingCatalog>("Eggs/EggHatchingCatalog");
                var state = new EggFeatureState();
                state.AddEgg(catalog.Definitions.First(e => e != null).eggId, 2);
                EggFeatureStorage.Save(state);
                string saved = JsonUtility.ToJson(EggFeatureStorage.Load());
                hud = new GameObject("Egg pen probe persistent HUD", typeof(RectTransform));
                UnityEngine.Object.DontDestroyOnLoad(hud);
                var button = hud.AddComponent<EggInventoryHudButton>();
                var runtime = (EggHatchingManager)typeof(EggInventoryHudButton).GetMethod("CreateRuntimeManager", Private).Invoke(button, null);
                Require(runtime != null && runtime.transform.IsChildOf(hud.transform), "real HUD fallback factory");
                runtime.LoadState();
                preview = EditorSceneManager.OpenPreviewScene("Assets/Scenes/Lobby.unity");
                var source = preview.GetRootGameObjects().Single(r => r.name == "EggSystem");
                lobby = UnityEngine.Object.Instantiate(source);
                lobby.name = "Egg pen probe Lobby EggSystem";
                await Task.Delay(300);
                Require(lobby != null && EggHatchingManager.Instance == lobby.GetComponent<EggHatchingManager>(), "scene manager replaces HUD fallback");
                Require(runtime == null && hud != null && button != null, "only fallback root removed, HUD survives");
                Require(lobby.GetComponentsInChildren<EggNestPoint>(true).Length == 4, "four real nests survive");
                var visibility = lobby.GetComponent<EggFeatureUnlockVisibility>();
                visibility.Refresh();
                var pen = ((GameObject[])typeof(EggFeatureUnlockVisibility).GetField("_eggNestObjects", Private).GetValue(visibility)).Single();
                Require(pen.activeInHierarchy, "offline pen visible after run return");
                Require(JsonUtility.ToJson(EggFeatureStorage.Load()) == saved, "handoff preserves storage");
                visibility.SetWorldHidden(true);
                Require(!pen.activeSelf, "online presentation still hides pen");
                visibility.SetWorldHidden(false);
                Require(pen.activeInHierarchy, "offline presentation restores pen");
                UnityEngine.Object.Destroy(lobby);
                await Task.Delay(200);
                Require(EggHatchingManager.Instance == null, "scene manager released when Lobby unloads");
                lobby = UnityEngine.Object.Instantiate(source);
                await Task.Delay(200);
                Require(lobby != null && EggHatchingManager.Instance == lobby.GetComponent<EggHatchingManager>(), "repeat Lobby entry without fallback");
                Require(lobby.GetComponentsInChildren<EggNestPoint>(true).Length == 4, "repeat entry retains nests");
                Result = "PASS: persistent HUD fallback replaced, pen/four nests survive return and repeat entry, storage unchanged, online hide/offline restore";

                Debug.Log("[Offline egg pen probe] " + Result);
            }
            catch (Exception e) { Result = "FAIL: " + e; Debug.LogWarning("[Offline egg pen probe] " + Result); }
            finally
            {
                if (lobby != null) UnityEngine.Object.DestroyImmediate(lobby);
                if (hud != null) UnityEngine.Object.DestroyImmediate(hud);
                if (preview.IsValid()) EditorSceneManager.ClosePreviewScene(preview);
            }
        }
        private static void Require(bool condition, string label) { if (!condition) throw new InvalidOperationException(label); }
    }
}
