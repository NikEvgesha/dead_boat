using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Fusion;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DeadBoat.Online.Editor
{
    public static class AvatarNamesProbe
    {
        public static string Result { get; private set; } = "not run";
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        public static string Validate()
        {
            Require(AvatarDisplayName.Sanitize("  Капитан\n  Джек  ") == "Капитан Джек", "single-line whitespace");
            Require(AvatarDisplayName.Sanitize("<size=999><b>Jack</b></size>\u202e\0") == "Jack", "tags/control/bidi removed");
            Require(AvatarDisplayName.Sanitize(new string('я', 300)).Length == 24, "bounded length");
            Require(AvatarDisplayName.Sanitize(new string('a', 23) + "\U0001F600").Length == 23, "surrogate pair not cut");
            Require(AvatarDisplayName.Resolve("\u202e<invalid>", 2, false) == "Игрок 2", "guest fallback");
            Require(AvatarDisplayName.Resolve("", 2, true) == "Player 2", "English fallback");
            bool Show(bool remote, Vector3 offset, float depth, bool menu) => (bool)typeof(AvatarNameplate)
                .GetMethod("CanShow", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { remote, offset, depth, menu });
            Require(Show(true, Vector3.forward * 5, 5, false), "near visible");
            Require(!Show(true, Vector3.forward * 31, 31, false) && !Show(true, Vector3.back, -1, false), "distance/behind-camera hidden");
            Require(!Show(false, Vector3.forward, 1, false) && !Show(true, Vector3.forward, 1, true), "local/menu hidden");
            var original = Fusion.Photon.Realtime.PhotonAppSettings.Global.AppSettings.AppVersion;
            Require(SharedRunRecovery.Settings().AppVersion == (original ?? "") + "-dbseed4" &&
                Fusion.Photon.Realtime.PhotonAppSettings.Global.AppSettings.AppVersion == original, "protocol copy leaves global SDK config untouched");
            return "PASS: safe bounded single-line names, Unicode boundaries, guest/localization, distance/behind/local/menu gates, protocol settings copy";
        }

        public static async void Run()
        {
            if (!EditorApplication.isPlaying || Result == "running") return;
            Result = "running";
            NetworkRunner first = null, second = null;
            GameObject cameraObject = null;
            float scale = Time.timeScale;
            EditorApplication.CallbackFunction clock = () => Time.timeScale = 1;
            EditorApplication.update += clock;
            try
            {
                Require(LoadingManager.Instance == null && !SharedRunContext.Active, "isolated empty scene");
                Validate();
                cameraObject = new GameObject("Nameplate probe camera", typeof(Camera));
                cameraObject.tag = "MainCamera";
                cameraObject.transform.position = new Vector3(0, 1.7f, -6);
                string room = "nameplate-" + Guid.NewGuid().ToString("N");
                first = await Connect(room, 3701, true);
                var prefab = AssetDatabase.LoadAssetAtPath<NetworkObject>("Assets/Online/Avatars/LobbyNetworkAvatar.prefab");
                var local = first.Spawn(prefab, new Vector3(-1, 0, 3)).GetComponent<LobbyNetworkAvatar>();
                first.SetPlayerObject(first.LocalPlayer, local.Object);
                SetName(local, "<b>Капитан</b>\nДжек");
                second = await Connect(room, 3702, false);
                await Until(() => Replica(second, first.LocalPlayer) != null, "late join avatar");
                var remote = Replica(second, first.LocalPlayer);
                await Until(() => remote.DisplayName.ToString() == "Капитан Джек", "late join current name");
                Require(local.GetComponentInChildren<AvatarNameplate>(true) == null, "no local FPS label");
                var plate = remote.GetComponentInChildren<AvatarNameplate>(true);
                Require(plate != null && plate.GetComponent<GraphicRaycaster>() == null && !plate.GetComponent<CanvasGroup>().blocksRaycasts,
                    "noninteractive world label");
                await Until(() => plate.GetComponent<Canvas>().enabled && Text(plate).text == "Капитан Джек", "visible near label");
                Require(!Text(plate).supportRichText && Text(plate).material.shader.name == "UI/Default", "plain text depth-tested UI material");
                SetName(local, "Боцман");
                await Until(() => Text(plate).text == "Боцман", "name change replicated");
                SetName(local, "");
                await Until(() => Text(plate).text.StartsWith("Игрок ") || Text(plate).text.StartsWith("Player "), "guest label");
                cameraObject.transform.position = new Vector3(0, 1.7f, -50);
                await Until(() => !plate.GetComponent<Canvas>().enabled, "far label hidden");
                cameraObject.transform.position = new Vector3(0, 1.7f, -6);
                cameraObject.transform.rotation = Quaternion.Euler(0, 180, 0);
                await Until(() => !plate.GetComponent<Canvas>().enabled, "behind camera hidden");
                cameraObject.transform.rotation = Quaternion.identity;
                await Until(() => plate.GetComponent<Canvas>().enabled, "camera returned");
                SetName(local, "Капитан Джек");
                var other = second.Spawn(prefab, new Vector3(1, 0, 3)).GetComponent<LobbyNetworkAvatar>();
                second.SetPlayerObject(second.LocalPlayer, other.Object); SetName(other, "Боцман");
                await Until(() => Replica(first, second.LocalPlayer)?.GetComponentInChildren<AvatarNameplate>(true) != null,
                    "multiple nearby labels");
                await Task.Delay(300);
                int visiblePixels = Capture(cameraObject.GetComponent<Camera>(), "Builds/AvatarNamesPreview.png");
                var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                var wallMaterial = new Material(Shader.Find("Unlit/Color")); wallMaterial.color = Color.black;
                try
                {
                    wall.transform.position = new Vector3(0, 2, -1); wall.transform.localScale = new Vector3(20, 20, .2f);
                    wall.GetComponent<Renderer>().sharedMaterial = wallMaterial;
                    Require(visiblePixels > 20 && Capture(cameraObject.GetComponent<Camera>(), null) == 0,
                        "names are occluded by solid scenery");
                }
                finally { UnityEngine.Object.DestroyImmediate(wall); UnityEngine.Object.DestroyImmediate(wallMaterial); }
                // Leave no scene/global test state behind; owner checks independent WebGL/mobile later.
                Result = "PASS: two Cloud peers, late-join name, live update, guest fallback, no own label/input capture, near/far/behind camera, two nearby labels, opaque-wall occlusion";
                Debug.Log("[Avatar names probe] " + Result);
            }
            catch (Exception e) { Result = "FAIL: " + e; Debug.LogWarning("[Avatar names probe] " + Result); }
            finally
            {
                if (second != null) await second.Shutdown();
                if (first != null) await first.Shutdown();
                if (cameraObject != null) UnityEngine.Object.Destroy(cameraObject);
                EditorApplication.update -= clock; Time.timeScale = scale;
            }
        }
        private static int Capture(Camera camera, string path)
        {
            var rt = RenderTexture.GetTemporary(1280, 720, 24);
            var active = RenderTexture.active; var target = camera.targetTexture;
            var image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            try
            {
                camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.07f, .12f, .16f);
                camera.targetTexture = rt; Canvas.ForceUpdateCanvases(); camera.Render(); RenderTexture.active = rt;
                image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); image.Apply();
                if (path != null) { System.IO.Directory.CreateDirectory("Builds"); System.IO.File.WriteAllBytes(path, image.EncodeToPNG()); }
                return image.GetPixels32().Count(c => c.r > 220 && c.g > 220 && c.b > 220);
            }
            finally { camera.targetTexture = target; RenderTexture.active = active; RenderTexture.ReleaseTemporary(rt); UnityEngine.Object.DestroyImmediate(image); }
        }
        private static Text Text(AvatarNameplate plate) => plate.GetComponentInChildren<Text>(true);
        private static void SetName(LobbyNetworkAvatar avatar, string value)
        {
            typeof(LobbyNetworkAvatar).GetField("nextNameRefresh", Private).SetValue(avatar, avatar.Runner.SimulationTime + 1000d);
            NetworkString<_32> name = AvatarDisplayName.Sanitize(value);
            typeof(LobbyNetworkAvatar).GetProperty("DisplayName").SetValue(avatar, name);
        }
        private static LobbyNetworkAvatar Replica(NetworkRunner runner, PlayerRef player) =>
            UnityEngine.Object.FindObjectsByType<LobbyNetworkAvatar>(FindObjectsSortMode.None)
                .FirstOrDefault(a => a.Runner == runner && a.Object != null && a.Object.StateAuthority == player);
        private static async Task<NetworkRunner> Connect(string room, long id, bool create) =>
            await (Task<NetworkRunner>)typeof(SharedAdProtocolProbe).GetMethod("Connect", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { room, id, create });
        private static async Task Until(Func<bool> test, string label)
        {
            double end = Time.realtimeSinceStartupAsDouble + 25;
            while (!test()) { Require(Time.realtimeSinceStartupAsDouble < end, "timeout: " + label); await Task.Delay(50); }
        }
        private static void Require(bool test, string label) { if (!test) throw new InvalidOperationException(label); }
    }
}
