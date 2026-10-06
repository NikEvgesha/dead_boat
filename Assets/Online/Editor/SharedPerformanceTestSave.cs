using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeadBoat.Online.Editor
{
    // Editor fixture: opening Lobby must not auto-resume or overwrite the owner's solo save.
    [InitializeOnLoad]
    public static class SharedPerformanceTestSave
    {
        private const string Flag = "DeadBoat.Editor.PerformanceTestSave";
        private static SaveProvider original;
        private static SaveManager manager;
        private static SharedRunPreviewSave preview;
        static SharedPerformanceTestSave()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredEditMode) Restore();
            };
        }
        [MenuItem("Tools/Online/Prepare Isolated Performance Save (before Play)")]
        public static void Prepare()
        {
            if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Prepare before Play Mode");
            var type = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.LogEntries");
            var flags = (int)type.GetProperty("consoleFlags", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).GetValue(null);
            SessionState.SetBool(Flag + ".ErrorPause", (flags & 4) != 0);
            SessionState.SetBool(Flag + ".RestoreConsole", true);
            type.GetMethod("SetConsoleFlag", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                .Invoke(null, new object[] { 4, false });
            SessionState.SetBool(Flag, true);
            SessionState.SetBool(Flag + ".ResumeForest", false);
        }
        public static void PrepareResumeForest()
        {
            Prepare();
            SessionState.SetBool(Flag + ".ResumeForest", true);
        }
        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!SessionState.GetBool(Flag, false) || manager != null) return;
            manager = Object.FindAnyObjectByType<SaveManager>(FindObjectsInactive.Include);
            if (manager == null) return;
            original = manager.PersistentProvider;
            // In the normal boot scene the save manager is activated after SDK
            // readiness. Its Awake will initialize the temporary provider instead.
            if (SaveManager.Instance != manager) original.Initialize();
            preview = manager.gameObject.AddComponent<SharedRunPreviewSave>();
            preview.Persistent = original;
            if (SessionState.GetBool(Flag + ".ResumeForest", false))
                preview.SaveDistance(0);
            typeof(SaveManager).GetField("saveProvider", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(manager, preview);
            Debug.Log("[Stress fixture] Solo save isolated before scene Start.");
        }
        public static void Restore()
        {
            if (manager != null && original != null)
                typeof(SaveManager).GetField("saveProvider", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(manager, original);
            if (preview != null) Object.Destroy(preview);
            manager = null;
            original = null;
            preview = null;
            SessionState.SetBool(Flag, false);
            if (SessionState.GetBool(Flag + ".RestoreConsole", false))
            {
                typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.LogEntries")
                    .GetMethod("SetConsoleFlag", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                    .Invoke(null, new object[] { 4, SessionState.GetBool(Flag + ".ErrorPause", true) });
                SessionState.SetBool(Flag + ".RestoreConsole", false);
            }
        }
    }
}
