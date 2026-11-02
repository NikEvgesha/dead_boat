using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Unity.Profiling;
using Unity.Profiling.LowLevel.Unsafe;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace DeadBoat.Online.Editor
{
    public static class SharedBillboardProbe
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        public static async Task<string> Run(int mode)
        {
            if (!EditorApplication.isPlaying || LoadingManager.Instance != null)
                throw new InvalidOperationException("empty isolated Play scene required");
            var root = new GameObject("Temporary billboard probe");
            var targetTexture = new RenderTexture(128, 128, 16);
            var panels = new List<SeeInCamera>();
            var updates = new List<Action>();
            ProfilerRecorder canvas = default;
            try
            {
                var cameraObject = new GameObject("Temporary billboard camera"); cameraObject.transform.SetParent(root.transform);
                cameraObject.tag = "MainCamera";
                var camera = cameraObject.AddComponent<Camera>();
                camera.targetTexture = targetTexture; camera.orthographic = true; camera.orthographicSize = 25;
                camera.transform.position = new Vector3(0, 10, -40); camera.transform.LookAt(new Vector3(0, 5, 0));
                for (int i = 0; i < 200; i++)
                {
                    var go = new GameObject("World UI fixture", typeof(RectTransform), typeof(Canvas));
                    go.transform.SetParent(root.transform);
                    go.transform.position = new Vector3(i % 20 - 10, i / 20, 0);
                    var ui = go.GetComponent<Canvas>(); ui.renderMode = RenderMode.WorldSpace; ui.worldCamera = camera;
                    ((RectTransform)go.transform).sizeDelta = Vector2.one;
                    var image = new GameObject("Image", typeof(RectTransform), typeof(Image)); image.transform.SetParent(go.transform, false);
                    ((RectTransform)image.transform).sizeDelta = Vector2.one;
                    var panel = go.AddComponent<SeeInCamera>(); panel.enabled = false;
                    typeof(SeeInCamera).GetField("_mainCamera", Private).SetValue(panel, camera);
                    typeof(SeeInCamera).GetField("testVector", Private).SetValue(panel, mode);
                    panels.Add(panel);
                    updates.Add((Action)Delegate.CreateDelegate(typeof(Action), panel, typeof(SeeInCamera).GetMethod("Update", Private)));
                }
                CheckMotion(panels[0], updates[0], camera);
                foreach (var panel in panels) typeof(SeeInCamera).GetField("testVector", Private).SetValue(panel, mode);
                foreach (var update in updates) update();
                foreach (var panel in panels) panel.transform.hasChanged = false;
                foreach (var update in updates) update();
                int changed = 0;
                foreach (var panel in panels) if (panel.transform.hasChanged) changed++;
                var timer = Stopwatch.StartNew();
                for (int i = 0; i < 200; i++) foreach (var update in updates) update();
                timer.Stop();
                Canvas.ForceUpdateCanvases();
                var handles = new List<ProfilerRecorderHandle>(); ProfilerRecorderHandle.GetAvailable(handles);
                foreach (var handle in handles)
                {
                    var d = ProfilerRecorderHandle.GetDescription(handle);
                    if (d.Name != "Canvas.BuildBatch") continue;
                    canvas = ProfilerRecorder.StartNew(d.Category, d.Name, 1); break;
                }
                int frames = 0, previous = Time.frameCount;
                double ns = 0, until = Time.realtimeSinceStartupAsDouble + 20;
                while (frames < 120)
                {
                    if (!EditorApplication.isPlaying || Time.realtimeSinceStartupAsDouble > until)
                        throw new TimeoutException("billboard frame sample");
                    await Task.Delay(1);
                    if (Time.frameCount == previous) continue;
                    previous = Time.frameCount;
                    if (canvas.Valid) ns += canvas.LastValue;
                    foreach (var update in updates) update();
                    Canvas.ForceUpdateCanvases();
                    frames++;
                }
                string result = string.Format(CultureInfo.InvariantCulture,
                    "[Billboard probe] PASS mode={0} panels=200 unchangedWrites={1} calls=40000 elapsedMs={2:F3} frames={3} canvasBatchMeanMs={4:F4}; motion/mode/external rotation PASS; synthetic UI diagnostic, not whole-game FPS",
                    mode, changed, timer.Elapsed.TotalMilliseconds, frames, canvas.Valid ? ns / frames / 1000000d : -1);
                UnityEngine.Debug.Log(result);
                File.WriteAllText("Logs/Billboard-mode" + mode + "-20261102.log", result);
                return result;
            }
            finally
            {
                canvas.Dispose(); root.SetActive(false);
                Object.Destroy(root); targetTexture.Release(); Object.Destroy(targetTexture);
            }
        }

        private static void CheckMotion(SeeInCamera panel, Action update, Camera camera)
        {
            var mode = typeof(SeeInCamera).GetField("testVector", Private);
            var startCamera = camera.transform.position; var startPosition = panel.transform.position;
            foreach (int value in new[] { 0, 12 })
            {
                mode.SetValue(panel, value);
                update(); Check();
                camera.transform.position += Vector3.right; update(); Check();
                panel.transform.position += Vector3.up; update(); Check();
                panel.transform.rotation = Quaternion.Euler(20, 30, 40); update(); Check();
                panel.enabled = true; panel.enabled = false; update(); Check();
                camera.transform.position = startCamera; panel.transform.position = startPosition;
                void Check()
                {
                    Vector3 direction = value == 0 ? camera.transform.position - panel.transform.position : panel.transform.position - camera.transform.position;
                    Require(Quaternion.Angle(panel.transform.rotation, Quaternion.LookRotation(direction, Vector3.up)) < .05f, "billboard orientation");
                }
            }
            mode.SetValue(panel, 12); update();
        }
        private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    }
}
