using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Fusion;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using Object = UnityEngine.Object;

namespace DeadBoat.Online.Editor
{
    // Editor-only evidence collector. Does not unload assets, run GC or change gameplay.
    public static class SharedMemoryAudit
    {
        private static double next;
        private static string output;
        private static int index;

        public static string Begin()
        {
            End();
            Directory.CreateDirectory("Logs");
            output = Path.GetFullPath("Logs/SharedMemoryAudit-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".log");
            File.WriteAllText(output, "Editor memory audit; loaded assets include Editor resources; not WebGL memory.\n");
            index = 0;
            EditorApplication.update += Update;
            Capture("begin");
            next = EditorApplication.timeSinceStartup + 10;
            return output;
        }

        public static void End() => EditorApplication.update -= Update;

        private static void Update()
        {
            if (EditorApplication.timeSinceStartup < next) return;
            next = EditorApplication.timeSinceStartup + 10;
            Capture("sample-" + ++index);
        }

        public static string Capture(string tag)
        {
            var totals = new Dictionary<string, (int count, long bytes)>();
            var biggest = new List<(string name, string type, long bytes)>();
            foreach (var item in Resources.FindObjectsOfTypeAll<Object>())
            {
                if (item == null) continue;
                string type = item.GetType().Name;
                long size = Profiler.GetRuntimeMemorySizeLong(item);
                totals.TryGetValue(type, out var sum);
                totals[type] = (sum.count + 1, sum.bytes + size);
                if (size > 8L * 1048576) biggest.Add((item.name, type, size));
            }
            var lines = new List<string>();
            lines.Add(string.Format(CultureInfo.InvariantCulture,
                "tag={0} scene={1} allocatedMiB={2:F1} reservedMiB={3:F1} monoUsedMiB={4:F1} gfxMiB={5:F1} gc={6}/{7}/{8}",
                tag, UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
                Profiler.GetTotalAllocatedMemoryLong() / 1048576d,
                Profiler.GetTotalReservedMemoryLong() / 1048576d,
                Profiler.GetMonoUsedSizeLong() / 1048576d,
                Profiler.GetAllocatedMemoryForGraphicsDriver() / 1048576d,
                GC.CollectionCount(0), GC.CollectionCount(1), GC.CollectionCount(2)));
            lines.Add($"  profilerRecording={UnityEditorInternal.ProfilerDriver.enabled} frameRange={UnityEditorInternal.ProfilerDriver.firstFrameIndex}/{UnityEditorInternal.ProfilerDriver.lastFrameIndex}");
            foreach (var pair in totals.OrderByDescending(x => x.Value.bytes).Take(12))
                lines.Add($"  {pair.Key}: count={pair.Value.count} runtimeMiB={(pair.Value.bytes / 1048576d).ToString("F1", CultureInfo.InvariantCulture)}");
            foreach (var entry in biggest.OrderByDescending(x => x.bytes).Take(8))
                lines.Add($"  large {entry.type} {entry.name}: {(entry.bytes / 1048576d).ToString("F1", CultureInfo.InvariantCulture)} MiB");
            foreach (var runner in Object.FindObjectsByType<NetworkRunner>(FindObjectsSortMode.None))
            {
                if (!runner.IsRunning || !runner.TryGetFusionStatistics(out var stats)) continue;
                var memory = stats.MemorySnapshot;
                lines.Add($"  runner: objectsUsed={memory.ObjectAllocatorMemorySnapshot.TotalBytesUsed} objectsFree={memory.ObjectAllocatorMemorySnapshot.TotalBytesFree} generalUsed={memory.GeneralAllocatorMemorySnapshot.TotalBytesUsed} generalFree={memory.GeneralAllocatorMemorySnapshot.TotalBytesFree}");
            }
            string result = string.Join("\n", lines);
            if (output != null) File.AppendAllText(output, result + "\n");
            return result;
        }
    }
}
