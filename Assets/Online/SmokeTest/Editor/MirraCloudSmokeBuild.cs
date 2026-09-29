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

        [MenuItem("Tools/Dead Boat/Build Mirra Cloud Smoke Test")]
        public static void Build()
        {
            // The SDK uses Token in editor tooling only. A nonempty value in Resources
            // would be embedded in the publicly downloadable WebGL data file.
            if (!string.IsNullOrEmpty(MirraCloud.Configuration.Load().Token))
                throw new BuildFailedException("Remove Mirra Cloud API token from Configuration.asset before WebGL build");

            Directory.CreateDirectory(Output);

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { Scene },
                locationPathName = Output,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            });

            if (report.summary.result != BuildResult.Succeeded)
                throw new BuildFailedException($"Mirra Cloud smoke build: {report.summary.result}");

            AddWebViewScripts();
        }

        private static void AddWebViewScripts()
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

            File.Copy(webViewSource, Path.Combine(Output, "unity-webview.js"), true);
            File.Copy(jquerySource, Path.Combine(Output, "jquery.min.js"), true);

            var index = Path.Combine(Output, "index.html");
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
