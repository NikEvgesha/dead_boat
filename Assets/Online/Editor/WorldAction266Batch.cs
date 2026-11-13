using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DeadBoat.Online.Editor
{
    // Dedicated batch editor only. Never enters Play or replaces scenes in the
    // owner's interactive editor, and never prepares persistent save providers.
    [InitializeOnLoad]
    public static class WorldAction266Batch
    {
        private const string Key = "DeadBoat.World266.Batch.";

        static WorldAction266Batch()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            EditorApplication.update += Watchdog;
            UnityEditor.Compilation.CompilationPipeline.compilationStarted += _ =>
                SessionState.SetFloat(Key + "quietUntil", (float)EditorApplication.timeSinceStartup + 10);
        }

        public static void Prepare()
        {
            if (!Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Dedicated idle batch Editor required.");
            SharedAmbushCatalogBuilder.Prepare();
            Debug.Log(WorldAction266ContentProbe.Run());
            SharedItemScaleProbe.Run();
            SharedRunValidation.Validate();
        }

        public static void Run()
        {
            if (!Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Requires a separate idle batch Editor, without -quit.");
            Prepare();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SessionState.SetString(Key + "startScene", AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));
            EditorSceneManager.playModeStartScene = null;
            SessionState.SetFloat(Key + "previousScale", Time.timeScale);
            SessionState.SetFloat(Key + "deadline", (float)EditorApplication.timeSinceStartup + 480);
            SessionState.SetInt(Key + "exit", 1);
            SessionState.SetBool(Key + "active", true);
            SessionState.SetBool(Key + "started", false);
            SessionState.SetBool(Key + "preparing", true);
            SessionState.SetFloat(Key + "quietUntil", (float)EditorApplication.timeSinceStartup + 10);
        }

        private static async void OnPlayModeChanged(PlayModeStateChange mode)
        {
            if (!Application.isBatchMode || !SessionState.GetBool(Key + "active", false)) return;
            if (mode == PlayModeStateChange.EnteredEditMode)
            {
                Time.timeScale = SessionState.GetFloat(Key + "previousScale", 1);
                EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(SessionState.GetString(Key + "startScene", ""));
                SessionState.SetBool(Key + "active", false);
                EditorApplication.Exit(SessionState.GetInt(Key + "exit", 1));
                return;
            }
            if (mode != PlayModeStateChange.EnteredPlayMode || SessionState.GetBool(Key + "started", false)) return;
            SessionState.SetBool(Key + "started", true);
            try
            {
                await WorldActions266SoloProbe.RunAsync();
                if (!WorldActions266SoloProbe.Result.StartsWith("PASS", StringComparison.Ordinal))
                    throw new InvalidOperationException(WorldActions266SoloProbe.Result);
                await WorldActions266NetworkProbe.RunAsync();
                await System.Threading.Tasks.Task.Delay(100);
                if (!WorldActions266NetworkProbe.Result.StartsWith("PASS", StringComparison.Ordinal))
                    throw new InvalidOperationException(WorldActions266NetworkProbe.Result);
                if (SharedRunContext.Active || SoloRunBranch.Active ||
                    UnityEngine.Object.FindObjectsByType<Fusion.NetworkRunner>(FindObjectsSortMode.None).Length != 0)
                    throw new InvalidOperationException("World action fixture did not clean up.");
                SessionState.SetInt(Key + "exit", 0);
                Debug.Log("[World266 batch] PASS: native checks, seeded generation, four-peer protocol and cleanup.");
            }
            catch (Exception error)
            {
                Debug.LogError("[World266 batch] FAIL: " + error);
                SessionState.SetInt(Key + "exit", 1);
            }
            finally { EditorApplication.ExitPlaymode(); }
        }

        private static void Watchdog()
        {
            if (!Application.isBatchMode || !SessionState.GetBool(Key + "active", false)) return;
            if (SessionState.GetBool(Key + "preparing", false))
            {
                // Startup package resolvers may schedule a second compile after
                // executeMethod. Let the editor settle before opening transports.
                if (EditorApplication.isCompiling || EditorApplication.isUpdating)
                    SessionState.SetFloat(Key + "quietUntil", (float)EditorApplication.timeSinceStartup + 10);
                else if (EditorApplication.timeSinceStartup >= SessionState.GetFloat(Key + "quietUntil", float.MaxValue))
                {
                    SessionState.SetBool(Key + "preparing", false);
                    EditorApplication.EnterPlaymode();
                }
            }
            if (EditorApplication.timeSinceStartup < SessionState.GetFloat(Key + "deadline", float.MaxValue)) return;
            SessionState.SetInt(Key + "exit", 1);
            Debug.LogError("[World266 batch] FAIL: batch watchdog expired.");
            if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
            else { SessionState.SetBool(Key + "active", false); EditorApplication.Exit(1); }
        }
    }
}
