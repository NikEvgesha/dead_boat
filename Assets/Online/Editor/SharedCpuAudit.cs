using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Unity.Profiling;
using Unity.Profiling.LowLevel.Unsafe;
using UnityEditor;
using UnityEngine;

namespace DeadBoat.Online.Editor
{
    // Editor-only rolling counters, without recording or clearing Profiler frames.
    public static class SharedCpuAudit
    {
        private sealed class Counter
        {
            public string Name;
            public bool Time;
            public ProfilerRecorder Recorder;
            public double Sum, Max;
        }
        private static readonly HashSet<string> names = new()
        {
            "Main Thread", "Render Thread", "PlayerLoop", "BehaviourUpdate",
            "Update.ScriptRunBehaviourUpdate", "PreLateUpdate.ScriptRunBehaviourLateUpdate",
            "FixedUpdate.ScriptRunBehaviourFixedUpdate", "Physics.Simulate", "Physics.SyncTransforms",
            "Physics.FetchResults", "Physics.Raycast", "Physics.OverlapSphere",
            "Camera.Render", "RenderLoop", "Gfx.WaitForPresentOnGfxThread", "Gfx.WaitForRenderThread",
            "Culling", "Animation.Update", "NavMeshManager", "Components.NavMeshAgent.State",
            "Batches Count", "SetPass Calls Count", "Triangles Count", "Vertices Count",
            "DeadBoat.SharedItems", "DeadBoat.SharedEnemies", "DeadBoat.SharedAuthority", "DeadBoat.EnemyPath"
        };
        private static readonly List<Counter> counters = new();
        private static string path, stage;
        private static SharedPerformanceMonitor monitor;
        private static int frame, samples;
        private static double next;

        public static string Begin()
        {
            End();
            Directory.CreateDirectory("Logs");
            path = Path.GetFullPath("Logs/SharedCpuAudit-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".log");
            File.WriteAllText(path, "Editor CPU diagnostic. Timings overlap: do not sum nested markers. Counts are all Editor views, not WebGL.\n");
            var handles = new List<ProfilerRecorderHandle>();
            ProfilerRecorderHandle.GetAvailable(handles);
            var found = new HashSet<string>();
            foreach (var handle in handles)
            {
                var d = ProfilerRecorderHandle.GetDescription(handle);
                if (!names.Contains(d.Name) || !found.Add(d.Name)) continue;
                var recorder = ProfilerRecorder.StartNew(d.Category, d.Name, 1);
                if (!recorder.Valid) { recorder.Dispose(); continue; }
                counters.Add(new Counter { Name = d.Name, Time = d.UnitType.ToString() == "TimeNanoseconds", Recorder = recorder });
            }
            frame = -1; stage = "starting"; samples = 0; monitor = null;
            next = EditorApplication.timeSinceStartup + 10;
            EditorApplication.update += Update;
            return path;
        }

        private static void Update()
        {
            if (!EditorApplication.isPlaying || frame == UnityEngine.Time.frameCount) return;
            frame = UnityEngine.Time.frameCount;
            if (monitor == null) monitor = UnityEngine.Object.FindAnyObjectByType<SharedPerformanceMonitor>();
            string current = monitor != null ? monitor.Stage : "loading";
            if (stage != current) { Flush(); stage = current; next = EditorApplication.timeSinceStartup + 10; }
            samples++;
            foreach (var c in counters)
            {
                long value = c.Recorder.LastValue;
                c.Sum += value; c.Max = Math.Max(c.Max, value);
            }
            if (EditorApplication.timeSinceStartup >= next) { Flush(); next = EditorApplication.timeSinceStartup + 10; }
        }

        private static void Flush()
        {
            if (path == null || samples == 0) return;
            var lines = new List<string> { $"stage={stage} samples={samples} profilerRecording={UnityEditorInternal.ProfilerDriver.enabled}" };
            foreach (var c in counters)
            {
                double divisor = c.Time ? 1000000d : 1;
                lines.Add(string.Format(CultureInfo.InvariantCulture, "  {0}: mean={1:F3} max={2:F3} unit={3}",
                    c.Name, c.Sum / samples / divisor, c.Max / divisor, c.Time ? "ms" : "raw"));
                c.Sum = c.Max = 0;
            }
            File.AppendAllLines(path, lines);
            samples = 0;
        }

        public static void End()
        {
            EditorApplication.update -= Update;
            Flush();
            foreach (var c in counters) c.Recorder.Dispose();
            counters.Clear();
            path = null;
        }
    }
}
