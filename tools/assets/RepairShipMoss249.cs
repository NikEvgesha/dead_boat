// Run through Unity MCP script-execute. Only the three shared moss meshes are edited.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class RepairShipMoss249
{
    const string Art = "Assets/Prefabs/Workshop/Ship1/Ship1_ProductionVisual.prefab";
    const string Evidence = "Docs/locations/previews/event-moss249/";
    static readonly Vector3 Origin = new(-72.66f, 0, 5.9f);

    static Mesh[] Meshes() => AssetDatabase.LoadAssetAtPath<GameObject>(Art)
        .GetComponentsInChildren<MeshFilter>(true).Where(f => f.name == "Moss on planks")
        .Select(f => f.sharedMesh).Distinct().ToArray();

    static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    public static string Apply()
    {
        Require(!EditorApplication.isPlayingOrWillChangePlaymode, "Edit Mode required");
        Directory.CreateDirectory(Evidence);
        var meshes = Meshes();
        Require(meshes.Length == 3, "Expected exactly three shared moss meshes");
        var lines = new List<string>();
        foreach (var mesh in meshes)
        {
            string path = AssetDatabase.GetAssetPath(mesh);
            string backup = "Library/EventFix249/MossBackup/" + path;
            Directory.CreateDirectory(Path.GetDirectoryName(backup));
            if (!File.Exists(backup)) File.Copy(path, backup);
            var vertices = mesh.vertices;
            var normals = mesh.normals;
            if (normals.Length == vertices.Length && normals.All(n => n.sqrMagnitude > .99f))
            {
                lines.Add("UNCHANGED already repaired: " + mesh.name);
                continue;
            }
            var uv = mesh.uv;
            var colors = mesh.colors;
            var points = new List<Vector3>();
            var faceNormals = new List<Vector3>();
            var coords = new List<Vector2>();
            var paint = new List<Color>();
            var submeshes = new List<int[]>();
            var originalTriangles = mesh.triangles.Select(i => vertices[i]).ToArray();
            var bounds = mesh.bounds;
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(mesh, out string guid, out long id);
            for (int sub = 0; sub < mesh.subMeshCount; sub++)
            {
                var indices = mesh.GetTriangles(sub);
                var fresh = new List<int>();
                for (int i = 0; i < indices.Length; i += 3)
                {
                    var normal = Vector3.Cross(vertices[indices[i + 1]] - vertices[indices[i]],
                        vertices[indices[i + 2]] - vertices[indices[i]]).normalized;
                    Require(normal.sqrMagnitude > .99f, "Degenerate moss triangle");
                    // Opposite faces must not share vertices: RecalculateNormals cancels them out.
                    for (int k = 0; k < 3; k++)
                    {
                        int old = indices[i + k];
                        fresh.Add(points.Count);
                        points.Add(vertices[old]);
                        faceNormals.Add(normal);
                        if (uv.Length == vertices.Length) coords.Add(uv[old]);
                        if (colors.Length == vertices.Length) paint.Add(colors[old]);
                    }
                }
                submeshes.Add(fresh.ToArray());
            }
            Undo.RegisterCompleteObjectUndo(mesh, "Repair ship moss normals DB-ON-249");
            // Write real buffers, preserving the original subasset identity and every reference.
            mesh.Clear();
            mesh.SetVertices(points);
            mesh.SetNormals(faceNormals);
            if (coords.Count > 0) mesh.SetUVs(0, coords);
            if (paint.Count > 0) mesh.SetColors(paint);
            mesh.subMeshCount = submeshes.Count;
            for (int sub = 0; sub < submeshes.Count; sub++) mesh.SetTriangles(submeshes[sub], sub);
            mesh.bounds = bounds;
            if (coords.Count > 0) mesh.RecalculateTangents();
            mesh.UploadMeshData(false);
            EditorUtility.SetDirty(mesh);
            AssetDatabase.SaveAssetIfDirty(mesh);
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(mesh, out string newGuid, out long newId);
            Require(guid == newGuid && id == newId, "Mesh reference identity changed");
            Require(originalTriangles.SequenceEqual(mesh.triangles.Select(i => mesh.vertices[i])),
                "Moss geometry changed");
            lines.Add($"PASS {mesh.name}: vertices {vertices.Length}->{mesh.vertexCount}; " +
                $"zero normals {normals.Count(n => n.sqrMagnitude < .01f)}->0; " +
                $"triangle positions, UV, colors, bounds and GUID/local ID retained ({guid}/{id}).");
        }
        File.WriteAllLines(Evidence + "repair.txt", lines);
        return string.Join("\n", lines);
    }

    static Transform Copy(Transform source, Transform parent, Scene scene)
    {
        var g = new GameObject(source.name);
        SceneManager.MoveGameObjectToScene(g, scene);
        g.transform.SetParent(parent, false);
        g.transform.localPosition = source.localPosition;
        g.transform.localRotation = source.localRotation;
        g.transform.localScale = source.localScale;
        var f = source.GetComponent<MeshFilter>();
        var r = source.GetComponent<MeshRenderer>();
        if (f && r && r.enabled && f.sharedMesh)
        {
            g.AddComponent<MeshFilter>().sharedMesh = f.sharedMesh;
            var renderer = g.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = r.sharedMaterials;
            renderer.shadowCastingMode = r.shadowCastingMode;
            renderer.receiveShadows = r.receiveShadows;
        }
        foreach (Transform t in source) if (t.gameObject.activeSelf) Copy(t, g.transform, scene);
        return g.transform;
    }

    public static string RenderBefore() => Render("before");
    public static string RenderAfter() => Render("after");
    static string Render(string suffix)
    {
        Directory.CreateDirectory(Evidence);
        bool fog = RenderSettings.fog;
        var ambientMode = RenderSettings.ambientMode;
        var ambientLight = RenderSettings.ambientLight;
        var scene = EditorSceneManager.NewPreviewScene();
        try
        {
            // Restore the lighting overrides after rendering; do not save any open scene.
            RenderSettings.fog = false;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.35f, .38f, .42f);
            var sunObject = new GameObject("Verification sun");
            SceneManager.MoveGameObjectToScene(sunObject, scene);
            var sun = sunObject.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.15f;
            sun.shadows = LightShadows.Hard;
            sun.transform.rotation = Quaternion.Euler(40, -35, 0);
            var cameraObject = new GameObject("Verification eye camera");
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            var camera = cameraObject.AddComponent<Camera>();
            camera.enabled = false;
            camera.scene = scene;
            camera.fieldOfView = 60;
            camera.nearClipPlane = .05f;
            camera.farClipPlane = 500;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.27f, .43f, .53f);
            camera.allowHDR = false;
            for (int bank = 1; bank <= 2; bank++)
            {
                var root = Copy(AssetDatabase.LoadAssetAtPath<GameObject>(
                    $"Assets/Prefabs/Locations/Enemy/ship_{bank}.prefab").transform, null, scene);
                root.position = Vector3.zero;
                var art = root.Find("Ship1_ProductionVisual");
                Vector3 Map(Vector3 p) => art.TransformPoint(p - Origin);
                var poses = new[] {
                    (name: "approach", p: new Vector3(-72.75f,3.34f,-19.24f), t: new Vector3(-71.15f,5.39f,-8.1f)),
                    (name: "close", p: new Vector3(-71.15f,3.86f,-14.03f), t: new Vector3(-71.15f,6.3f,-8.1f)),
                    (name: "deck", p: new Vector3(-77.95f,12.2f,2.62f), t: new Vector3(-77.95f,17.2f,16.4f))
                };
                foreach (var pose in poses)
                {
                    camera.transform.SetPositionAndRotation(Map(pose.p), Quaternion.LookRotation(Map(pose.t)-Map(pose.p)));
                    Capture(camera, $"bank{bank}-{pose.name}-{suffix}.png");
                }
                root.gameObject.SetActive(false);
            }
        }
        finally
        {
            RenderSettings.fog = fog;
            RenderSettings.ambientMode = ambientMode;
            RenderSettings.ambientLight = ambientLight;
            EditorSceneManager.ClosePreviewScene(scene);
        }
        return "Saved six eye-level renders: " + suffix;
    }

    static void Capture(Camera camera, string name)
    {
        var rt = RenderTexture.GetTemporary(1280, 850, 24);
        var texture = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
        var prior = RenderTexture.active;
        try
        {
            camera.targetTexture = rt;
            camera.aspect = (float)rt.width / rt.height;
            camera.Render();
            RenderTexture.active = rt;
            texture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            texture.Apply();
            File.WriteAllBytes(Evidence + name, texture.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = null;
            RenderTexture.active = prior;
            RenderTexture.ReleaseTemporary(rt);
            Object.DestroyImmediate(texture);
        }
    }

    public static string Verify()
    {
        var lines = new List<string>();
        foreach (var mesh in Meshes())
        {
            var v = mesh.vertices;
            var n = mesh.normals;
            var t = mesh.triangles;
            Require(n.Length == v.Length && n.All(p => Mathf.Abs(p.magnitude - 1) < .001f), "Invalid moss normals");
            for (int i = 0; i < t.Length; i += 3)
            {
                var face = Vector3.Cross(v[t[i+1]]-v[t[i]], v[t[i+2]]-v[t[i]]).normalized;
                Require(Enumerable.Range(0, 3).All(k => Vector3.Dot(face, n[t[i+k]]) > .999f), "Moss face normal mismatch");
            }
            lines.Add($"PASS {mesh.name}: {v.Length} unit normals agree with all {t.Length/3} front/back triangles.");
        }
        foreach (var path in new[] { Art, "Assets/Prefabs/Workshop/Ship1/Ship1_Overgrown.prefab",
            "Assets/Prefabs/Locations/Enemy/ship_1.prefab", "Assets/Prefabs/Locations/Enemy/ship_2.prefab" })
        {
            var filters = AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponentsInChildren<MeshFilter>(true)
                .Where(f => f.name == "Moss on planks").ToArray();
            Require(filters.Length >= 3 && filters.All(f => f.sharedMesh.normals.All(n => n.sqrMagnitude > .99f)), "Moss propagation failed: " + path);
            lines.Add($"PASS {path}: all {filters.Length} moss overlays use repaired shared meshes.");
        }
        Require(!EditorUtility.scriptCompilationFailed, "Project compilation errors");
        lines.Add("PASS Unity reports no script compilation errors.");
        File.WriteAllLines(Evidence + "verification.txt", lines);
        return string.Join("\n", lines);
    }
}
