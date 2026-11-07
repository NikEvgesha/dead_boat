using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Fusion;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DeadBoat.Online.Editor
{
    public static class ProfessionSkinProbe
    {
        public static string Result = "not run";
        public static void Capture()
        {
            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var root = new GameObject("Skin comparison");
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Online/Avatars/Kenney/character-a.fbx");
                var bounds = new Bounds(Vector3.up * 1.35f, new Vector3(5, 2.7f, 1));
                for (int i = 0; i < 3; i++)
                {
                    var model = UnityEngine.Object.Instantiate(prefab, root.transform);
                    model.transform.position = new Vector3((i - 1) * 1.7f, 0, 0);
                    foreach (var t in model.GetComponentsInChildren<Transform>()) t.gameObject.layer = 31;
                    var visual = model.AddComponent<AvatarProfessionVisual>(); visual.Initialize(model.transform); visual.Apply((byte)i);
                }
                var c = new GameObject("Preview camera", typeof(Camera)); c.transform.SetParent(root.transform);
                var cam = c.GetComponent<Camera>(); cam.scene = scene; cam.cullingMask = 1 << 31;
                cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(.13f, .17f, .21f);
                cam.orthographic = true; cam.orthographicSize = 1.9f;
                cam.transform.position = bounds.center + new Vector3(0, .1f, 10); cam.transform.LookAt(bounds.center);
                var l = new GameObject("Preview light", typeof(Light)); l.transform.SetParent(root.transform);
                l.transform.rotation = Quaternion.Euler(35, -25, 0); var light = l.GetComponent<Light>();
                light.type = LightType.Directional; light.intensity = 1.3f; light.cullingMask = 1 << 31;
                var rt = new RenderTexture(1200, 700, 24); var previous = RenderTexture.active;
                var tex = new Texture2D(1200, 700, TextureFormat.RGB24, false);
                try
                {
                    cam.targetTexture = rt; cam.Render(); RenderTexture.active = rt;
                    tex.ReadPixels(new Rect(0, 0, 1200, 700), 0, 0); tex.Apply();
                    System.IO.File.WriteAllBytes("Logs/AvatarProfessionPreview.png", tex.EncodeToPNG());
                }
                finally { RenderTexture.active = previous; cam.targetTexture = null; UnityEngine.Object.DestroyImmediate(tex); UnityEngine.Object.DestroyImmediate(rt); }
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }
        public static async void Run()
        {
            if (!EditorApplication.isPlaying || Result == "running") return;
            Result = "running"; NetworkRunner first = null, second = null;
            float scale = Time.timeScale; EditorApplication.CallbackFunction clock = () => Time.timeScale = 1;
            EditorApplication.update += clock;
            try
            {
                Require(LoadingManager.Instance == null, "empty isolated scene");
                string room = "skin-probe-" + Guid.NewGuid().ToString("N");
                first = await Connect(room, 5101, true);
                var prefab = AssetDatabase.LoadAssetAtPath<NetworkObject>("Assets/Online/Avatars/LobbyNetworkAvatar.prefab");
                var local = first.Spawn(prefab).GetComponent<LobbyNetworkAvatar>();
                first.SetPlayerObject(first.LocalPlayer, local.Object);
                typeof(LobbyNetworkAvatar).GetProperty("ProfessionSkin").SetValue(local, AvatarProfessionVisual.LastSkinId);
                second = await Connect(room, 5102, false);
                await Until(() => Remote(second)?.ProfessionSkin == AvatarProfessionVisual.LastSkinId, "late join highest skin ID");
                var remote = Remote(second);
                await Until(() => remote.GetComponent<AvatarProfessionVisual>().Displayed == AvatarProfessionVisual.LastSkinId, "late join material applied");
                var model = remote.GetComponentInChildren<Animator>(true);
                Require(model != null && model.runtimeAnimatorController != null, "animator retained");
                typeof(LobbyNetworkAvatar).GetProperty("HeldItemId", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(local, (byte)1);
                await Until(() => remote.GetComponentsInChildren<Transform>(true).Any(t => t.name.Contains("Visual(Clone)")), "held item visual retained");
                for (byte id = 1; id <= AvatarProfessionVisual.LastSkinId; id++)
                {
                    typeof(LobbyNetworkAvatar).GetProperty("ProfessionSkin").SetValue(local, id);
                    await Until(() => remote.GetComponent<AvatarProfessionVisual>().Displayed == id, "skin change " + id);
                    Require(model.GetComponentInChildren<Renderer>().sharedMaterial.name == AvatarProfessionVisual.ProfessionFor(id), "material for ID " + id);
                }
                typeof(LobbyNetworkAvatar).GetProperty("ProfessionSkin").SetValue(local, (byte)255);
                await Until(() => remote.GetComponent<AvatarProfessionVisual>().Displayed == 255, "unknown ID fallback");
                Require(model.GetComponentInChildren<Renderer>().sharedMaterial.name == "character-a", "original material restored");
                Result = "PASS: two Photon peers, highest-ID late join, all13 skin/material changes, unknown fallback, animation and held item retained";
            }
            catch (Exception e) { Result = "FAIL: " + e; Debug.LogError(Result); }
            finally
            {
                if (second != null) await second.Shutdown(); if (first != null) await first.Shutdown();
                EditorApplication.update -= clock; Time.timeScale = scale;
            }
        }
        private static LobbyNetworkAvatar Remote(NetworkRunner runner) => UnityEngine.Object.FindObjectsByType<LobbyNetworkAvatar>(FindObjectsSortMode.None).FirstOrDefault(a => a.Runner == runner && !a.Object.HasStateAuthority);
        private static Task<NetworkRunner> Connect(string room, long id, bool create) => (Task<NetworkRunner>)typeof(SharedAdProtocolProbe).GetMethod("Connect", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { room, id, create });
        private static async Task Until(Func<bool> predicate, string label)
        {
            double deadline = Time.realtimeSinceStartupAsDouble + 25;
            while (!predicate()) { Require(Time.realtimeSinceStartupAsDouble < deadline, label); await Task.Delay(50); }
        }
        private static void Require(bool ok, string label) { if (!ok) throw new Exception(label); }
    }
}
