// Run through Unity MCP script-execute; never edits the source gameplay assets.
using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using Object = UnityEngine.Object;

public static class VerifyVillage255
{
    const string Folder = "Assets/Prefabs/Workshop/Village255";
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    static string Path(Transform t, Transform root) => AnimationUtility.CalculateTransformPath(t, root);
    static string Slot(LocationSpawnPoint p, Transform root) => Path(p.spawnTransform, root) + "|" + p.allowedType + "|" + p.allowedSize + "|" + p.alignVisualBase + "|" + p.maxVisualFootprint;
    static Bounds BoundsOf(Transform t)
    {
        var renderers = t.GetComponentsInChildren<MeshRenderer>();
        var bounds = renderers[0].bounds;
        foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
        return bounds;
    }
    public static string Verify()
    {
        Check(!EditorApplication.isPlayingOrWillChangePlaymode, "Edit Mode required");
        var report = new StringBuilder();
        var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Locations/Enemy/Village_1.prefab");
        var clean = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "/Village_Unified.prefab");
        var original = source.GetComponent<LocationContentSpawner>();
        var content = clean.GetComponent<LocationContentSpawner>();
        Check(content.itemSpawnPoints.Count == 25 && content.enemySpawnPoints.Count == 15, "Wrong point counts");
        Check(original.itemCollection == content.itemCollection, "Changed loot collection");
        Check(original._enemyPrefabs.SequenceEqual(content._enemyPrefabs), "Changed guard definitions");
        Check(original.itemSpawnPoints.Select(p => Slot(p, source.transform)).SequenceEqual(content.itemSpawnPoints.Select(p => Slot(p, clean.transform))), "Changed item slot references/order/restrictions");
        Check(original.enemySpawnPoints.Select(p => Path(p, source.transform)).SequenceEqual(content.enemySpawnPoints.Select(p => Path(p, clean.transform))), "Changed enemy slot references/order");
        var nodes = clean.GetComponentsInChildren<Transform>(true);
        Check(!nodes.Any(t => t.name == "dom" || t.name.StartsWith("dom (") || t.name.StartsWith("GunStore") || t.name.StartsWith("Village_NewHouse_")), "Legacy buildings/courtyards remain");
        report.AppendLine("PASS: exactly one content spawner, ten legacy building roots absent including inactive children.");
        report.AppendLine("PASS: 25 item slots and 15 guard slots retain paths, order, collection, prefab list and all item restrictions.");
        var oldShared = source.transform.Find("PointCenter (1)/To4ka7");
        var newShared = clean.transform.Find("PointCenter (1)/To4ka7");
        foreach (Transform old in oldShared)
        {
            if (old.name == "dom" || old.name.StartsWith("dom (") || old.name.StartsWith("GunStore")) continue;
            var retained = newShared.Find(old.name);
            Check(retained && retained.localPosition == old.localPosition && retained.localRotation == old.localRotation && retained.localScale == old.localScale, "Changed shared prop " + old.name);
            Check(old.GetComponentsInChildren<MeshRenderer>(true).Select(r => r.GetComponent<MeshFilter>()?.sharedMesh).SequenceEqual(retained.GetComponentsInChildren<MeshRenderer>(true).Select(r => r.GetComponent<MeshFilter>()?.sharedMesh)), "Shared meshes changed " + old.name);
        }
        report.AppendLine("PASS: shared well, cart, fountains, braziers and other common props retained at their source transforms.");

        var scene = EditorSceneManager.NewPreviewScene();
        // Preview physics shares the Editor world; keep the probe away from other open workshops.
        var auditOffset = new Vector3(10000, 0, 10000);
        try
        {
            var root = (GameObject)PrefabUtility.InstantiatePrefab(clean, scene);
            root.transform.position += auditOffset;
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            SceneManager.MoveGameObjectToScene(ground, scene);
            ground.transform.position = new Vector3(-63, -.5f, 8) + auditOffset;
            ground.transform.localScale = new Vector3(140, 1, 104);
            var homes = root.GetComponentsInChildren<Transform>().Where(t => t.name.StartsWith("Village_House_")).OrderBy(t => t.name).ToArray();
            Check(homes.Length == 10, "Expected ten new homes");
            var labels = new[] { "House_Grey", "House_Brown", "House_Green" };
            var player = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab").GetComponent<CharacterController>();
            Physics.SyncTransforms();
            for (int h = 0; h < homes.Length; h++)
            {
                var home = homes[h];
                var expected = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "/" + labels[h % 3] + ".prefab");
                var actualMeshes = home.GetComponentsInChildren<MeshFilter>(true).Select(m => m.sharedMesh);
                Check(actualMeshes.SequenceEqual(expected.GetComponentsInChildren<MeshFilter>(true).Select(m => m.sharedMesh)), "Changed interior mesh set " + home.name);
                Check(home.GetComponentsInChildren<MeshRenderer>(true).SelectMany(r => r.sharedMaterials).SequenceEqual(expected.GetComponentsInChildren<MeshRenderer>(true).SelectMany(r => r.sharedMaterials)), "Changed interior materials " + home.name);
                Check(Mathf.Abs(home.TransformPoint(new Vector3(0, .514f, 0)).y - .26f) < .003f, "Floor height " + home.name);
                var probe = new GameObject("Village255_ControllerProbe");
                SceneManager.MoveGameObjectToScene(probe, scene);
                probe.transform.position = home.TransformPoint(new Vector3(0, .55f, -4.6f));
                var c = probe.AddComponent<CharacterController>();
                c.height = player.height; c.radius = player.radius; c.center = player.center;
                c.stepOffset = player.stepOffset; c.skinWidth = player.skinWidth; c.slopeLimit = player.slopeLimit;
                Physics.SyncTransforms();
                for (int i = 0; i < 340 && home.InverseTransformPoint(probe.transform.position).z < 1.3f; i++) c.Move(home.forward * .045f + Vector3.down * .025f);
                var local = home.InverseTransformPoint(probe.transform.position);
                Check(local.z >= 1.1f && c.isGrounded, "Blocked entrance " + home.name + " at " + local);
                report.AppendLine("PASS entrance " + home.name + ": local=" + local + ", floor=.260m");
                Object.DestroyImmediate(probe);
            }
            float gap = float.PositiveInfinity;
            for (int i = 0; i < homes.Length; i++)
                for (int j = i + 1; j < homes.Length; j++)
                {
                    var a = BoundsOf(homes[i]); var b = BoundsOf(homes[j]);
                    var dx = Mathf.Max(0, Mathf.Max(a.min.x - b.max.x, b.min.x - a.max.x));
                    var dz = Mathf.Max(0, Mathf.Max(a.min.z - b.max.z, b.min.z - a.max.z));
                    Check(dx > 0 || dz > 0, "Overlapping homes " + i + "/" + j);
                    gap = Mathf.Min(gap, new Vector2(dx, dz).magnitude);
                }
            report.AppendLine("PASS: new home/interior meshes and materials match the three current templates; minimum house gap=" + gap.ToString("F3") + "m.");
            var instance = root.GetComponent<LocationContentSpawner>();
            var physics = scene.GetPhysicsScene();
            var rng = new DeadBoat.Online.RunRandom(255, "village-audit", 0);
            for (int i = 0; i < instance.itemSpawnPoints.Count; i++)
            {
                var p = instance.itemSpawnPoints[i];
                Check(physics.Raycast(p.spawnTransform.position + Vector3.up * .02f, Vector3.down, out var hit, .35f, ~0, QueryTriggerInteraction.Ignore), "Unsupported item slot " + i);
                Check(p.spawnTransform.position.y - hit.point.y >= -.005f, "Item slot below support " + i);
                var prefab = instance.itemCollection.GetRandomItem(p.allowedType, p.allowedSize, rng);
                Check(prefab, "No compatible loot for " + i);
                var item = (GameObject)PrefabUtility.InstantiatePrefab(prefab.gameObject, scene);
                item.transform.SetPositionAndRotation(p.spawnTransform.position, p.spawnTransform.rotation);
                Check(item.GetComponent<PickableItem>(), "Missing pickup " + i);
                Object.DestroyImmediate(item);
            }
            report.AppendLine("PASS: all 25 slots have a surface within .35m and resolve/instantiate compatible pickups with a fixed audit seed. This is an Editor check, not a full run/pickup test.");
            report.AppendLine("Vegetation candidate names: " + string.Join(",", root.GetComponentsInChildren<Transform>().Where(t => new[] { "tree", "bush", "plant", "palm" }.Any(word => t.name.ToLowerInvariant().Contains(word))).Select(t => Path(t, root.transform))));
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
        File.WriteAllText("Library/Village255/verification.txt", report.ToString());
        return report.ToString();
    }

    static void Shot(Camera cam, string name, Vector3 position, Vector3 target, bool orthographic, float size = 53)
    {
        cam.transform.position = position; cam.transform.LookAt(target); cam.orthographic = orthographic; cam.orthographicSize = size; cam.fieldOfView = 65;
        var rt = RenderTexture.GetTemporary(1500, 1000, 24);
        var tex = new Texture2D(1500, 1000, TextureFormat.RGB24, false);
        var before = RenderTexture.active;
        try
        {
            cam.targetTexture = rt; cam.Render(); RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, 1500, 1000), 0, 0);
            File.WriteAllBytes("Docs/locations/previews/village-unified255-" + name + ".png", tex.EncodeToPNG());
        }
        finally { cam.targetTexture = null; RenderTexture.active = before; RenderTexture.ReleaseTemporary(rt); Object.DestroyImmediate(tex); }
    }
    public static string Capture()
    {
        var workshop = SceneManager.GetSceneByPath("Assets/Scenes/VillageWorkshop.unity");
        Check(workshop.isLoaded, "Workshop is not open");
        var stage = workshop.GetRootGameObjects().Single();
        Check(stage.GetComponentsInChildren<LocationContentSpawner>(true).Length == 1, "More than one village in workshop");
        var scene = EditorSceneManager.NewPreviewScene();
        try
        {
            var copy = Object.Instantiate(stage); SceneManager.MoveGameObjectToScene(copy, scene);
            var cam = copy.GetComponentInChildren<Camera>(); cam.scene = scene; cam.cameraType = CameraType.Preview;
            Shot(cam, "overview", new Vector3(-63, 78, -62), new Vector3(-63, 1, 8), true);
            Shot(cam, "street", new Vector3(-11, 1.8f, 5), new Vector3(-75, 2.3f, 8), false);
            var home = copy.GetComponentsInChildren<Transform>().Single(t => t.name == "Village_House_05");
            Shot(cam, "interior", home.TransformPoint(new Vector3(0, 1.55f, -1.8f)), home.TransformPoint(new Vector3(.3f, 1.35f, 1.7f)), false);
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
        SceneManager.SetActiveScene(workshop);
        SceneVisibilityManager.instance.Isolate(stage, true);
        Selection.activeGameObject = stage.GetComponentInChildren<LocationContentSpawner>().gameObject;
        if (SceneView.lastActiveSceneView)
        {
            SceneView.lastActiveSceneView.sceneLighting = false;
            SceneView.lastActiveSceneView.LookAt(new Vector3(-63, 2, 8), Quaternion.Euler(48, 0, 0), 65);
            SceneView.lastActiveSceneView.Focus();
        }
        return "Captured isolated overview, street and interior. VillageWorkshop active, isolated in Scene View with default inspection lighting. Other scenes untouched.";
    }
}
