using System.Linq;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace DeadBoat.Online.SmokeTest.Editor
{
    public static class MirraCloudSmokeBuild
    {
        private const string Scene = "Assets/Online/SmokeTest/MirraCloudConnectionSmokeTest.unity";
        private const string Output = "Builds/MirraCloudSmokeTest";
        private const string GameOutput = "Builds/MirraCloudPlayableDraft";
        private const string PerformanceOutput = "Builds/CoopPerformanceDraft";

        public static void QueueGameChatPilot()
        {
            EditorApplication.update -= BuildQueuedGameChatPilot;
            EditorApplication.update += BuildQueuedGameChatPilot;
        }

        private static void BuildQueuedGameChatPilot()
        {
            if (BuildPipeline.isBuildingPlayer || EditorApplication.isCompiling) return;
            EditorApplication.update -= BuildQueuedGameChatPilot;
            if (EditorApplication.isPlaying)
                throw new BuildFailedException("Stop Play Mode before building game chat pilot");
            var scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
            if (scenes.Length == 0) throw new BuildFailedException("No enabled scenes in Editor Build Settings");
            BuildPlayer("Builds/MirraGameChatPilot", scenes,
                new[] { "DEADBOAT_MIRRA_GAME_CHAT_PILOT", "DEADBOAT_MIRRA_DIAGNOSTICS" });
            UnityEngine.Debug.Log("[Mirra game chat pilot] build PASS: Builds/MirraGameChatPilot");
        }

        public static void QueueChatProbe()
        {
            EditorApplication.update -= BuildQueuedChatProbe;
            EditorApplication.update += BuildQueuedChatProbe;
        }

        private static void BuildQueuedChatProbe()
        {
            if (BuildPipeline.isBuildingPlayer || EditorApplication.isCompiling) return;
            EditorApplication.update -= BuildQueuedChatProbe;
            if (EditorApplication.isPlaying) throw new BuildFailedException("Stop Play Mode before building chat probe");
            const string scenePath = "Assets/Online/SmokeTest/MirraChatWebGLProbe.unity";
            var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(
                UnityEditor.SceneManagement.NewSceneSetup.EmptyScene, UnityEditor.SceneManagement.NewSceneMode.Additive);
            try
            {
                var root = new UnityEngine.GameObject("Mirra WebGL chat probe");
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
                root.AddComponent<MirraChatWebGLProbe>();
                UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, scenePath);
            }
            finally { UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true); }
            BuildPlayer("Builds/MirraChatProbe", new[] { scenePath }, new[] { "DEADBOAT_MIRRA_CHAT_PROBE" });
            UnityEngine.Debug.Log("[Mirra browser probe] build PASS");
        }

        public static void QueuePerformanceDraft()
        {
            EditorApplication.update -= BuildQueuedPerformanceDraft;
            EditorApplication.update += BuildQueuedPerformanceDraft;
        }

        private static void BuildQueuedPerformanceDraft()
        {
            if (BuildPipeline.isBuildingPlayer || EditorApplication.isCompiling) return;
            EditorApplication.update -= BuildQueuedPerformanceDraft;
            var scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
            if (scenes.Length == 0) throw new BuildFailedException("No enabled scenes in Editor Build Settings");
            BuildPlayer(PerformanceOutput, scenes, new[] { "DEADBOAT_MIRRA_DIAGNOSTICS", "DEADBOAT_COOP_PERFORMANCE" });
            UnityEngine.Debug.Log("[Coop performance build] PASS: " + PerformanceOutput);
        }

        // Queue once from automation so a long BuildPlayer does not trigger tool retries.
        public static void QueuePlayableDraft()
        {
            EditorApplication.update -= BuildQueuedDraft;
            EditorApplication.update += BuildQueuedDraft;
        }

        private static void BuildQueuedDraft()
        {
            if (BuildPipeline.isBuildingPlayer) return;
            EditorApplication.update -= BuildQueuedDraft;
            BuildPlayableDraft();
        }

        [MenuItem("Tools/Dead Boat/Build Mirra Cloud Smoke Test")]
        public static void Build()
        {
            BuildPlayer(Output, new[] { Scene }, null);
        }

        [MenuItem("Tools/Dead Boat/Build Mirra Cloud Playable Draft")]
        public static void BuildPlayableDraft()
        {
            var scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled)
                .Select(scene => scene.path).ToArray();
            if (scenes.Length == 0)
                throw new BuildFailedException("No enabled scenes in Editor Build Settings");

            BuildPlayer(GameOutput, scenes, new[] { "DEADBOAT_MIRRA_DIAGNOSTICS" });
        }

        private static void BuildPlayer(string output, string[] scenes, string[] scriptingDefines)
        {
            // The SDK uses Token in editor tooling only. A nonempty value in Resources
            // would be embedded in the publicly downloadable WebGL data file.
            if (!string.IsNullOrEmpty(MirraCloud.Configuration.Load().Token))
                throw new BuildFailedException("Remove Mirra Cloud API token from Configuration.asset before WebGL build");

            Directory.CreateDirectory(output);

            DeadBoat.Online.Editor.OnlinePrefabBuildValidation.Prepare();

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = output,
                target = BuildTarget.WebGL,
                options = BuildOptions.None,
                extraScriptingDefines = scriptingDefines
            });

            if (report.summary.result != BuildResult.Succeeded)
                throw new BuildFailedException($"Mirra Cloud WebGL build: {report.summary.result}");

            AddWebViewScripts(output);
        }

        private static void AddWebViewScripts(string output)
        {
            // SDK v0.10.0 initializes UnityWebView even for guest authentication.
            // Its WebGL plugin expects these scripts to exist in the page before Unity starts.
            var package = UnityEditor.PackageManager.PackageInfo.FindForAssetPath(
                "Packages/com.mirrahub.cloud-sdk/package.json");
            if (package == null)
                throw new BuildFailedException("Mirra Cloud package not found");

            var webViewSource = Path.Combine(package.resolvedPath, "ThirdParty", "UnityWebView", "Assets",
                "WebGLTemplates", "unity-webview-2020", "unity-webview.js");
            var jquerySource = Path.Combine("tools", "online-webgl", "jquery-3.1.0.min.js");
            if (!File.Exists(webViewSource) || !File.Exists(jquerySource))
                throw new BuildFailedException("Mirra Cloud WebGL WebView dependencies not found");

            File.Copy(webViewSource, Path.Combine(output, "unity-webview.js"), true);
            File.Copy(jquerySource, Path.Combine(output, "jquery.min.js"), true);

            var index = Path.Combine(output, "index.html");
            var html = File.ReadAllText(index);
            const string loader = "<script src=\"unityApp.js\"></script>";
            if (!html.Contains(loader))
                throw new BuildFailedException("WebGL template loader was not found");

            html = html.Replace(loader,
                "<script src=\"jquery.min.js\"></script>\n    " +
                "<script src=\"unity-webview.js\"></script>\n    " + loader);
            File.WriteAllText(index, html);
        }
    }
}
