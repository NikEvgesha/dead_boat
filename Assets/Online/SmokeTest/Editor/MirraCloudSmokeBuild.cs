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
        }
    }
}
