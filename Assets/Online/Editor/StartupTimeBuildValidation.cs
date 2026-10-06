using System.IO;
using System.Text.RegularExpressions;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace DeadBoat.Online.Editor
{
    // Runtime pause/focus belongs to the SDK. The serialized startup clock must
    // remain running, otherwise the SDK remembers zero as its resume speed.
    public sealed class StartupTimeBuildValidation : IPreprocessBuildWithReport
    {
        public int callbackOrder => -1000;

        public void OnPreprocessBuild(BuildReport report)
        {
            Validate(File.ReadAllText("ProjectSettings/TimeManager.asset"));
        }

        public static void Validate(string settings)
        {
            if (!Regex.IsMatch(settings, @"(?m)^\s*m_TimeScale:\s*1(?:\.0+)?\s*$"))
                throw new BuildFailedException("Dead Boat requires startup Time Scale = 1 in Project Settings > Time. Exit Play Mode and restore this value before building.");
        }
    }
}
