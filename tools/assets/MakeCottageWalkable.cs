using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Unity.Collections;

public static class MakeCottageWalkable
{
    const string Data = "Assets/Prefabs/Workshop/CottageGeometry";
    const string Prefab = "Assets/Prefabs/Workshop/WoodenCottage_01.prefab";
    static readonly Dictionary<Mesh, Mesh> Copies = new Dictionary<Mesh, Mesh>();
    static Mesh InteriorMesh(Mesh source)
    {
        if (Copies.TryGetValue(source, out var cached)) return cached;
        string path = Data + "/" + source.name + "_Interior.asset";
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing != null) { Copies[source] = existing; return existing; }
        using (var all = MeshUtility.AcquireReadOnlyMeshData(source))
        {
            var d = all[0]; int count = d.vertexCount;
            var vv = new NativeArray<Vector3>(count, Allocator.Temp);
            var nn = new NativeArray<Vector3>(count, Allocator.Temp);
            var uu = new NativeArray<Vector2>(count, Allocator.Temp);
            d.GetVertices(vv);
            if (d.HasVertexAttribute(VertexAttribute.Normal)) d.GetNormals(nn);
            if (d.HasVertexAttribute(VertexAttribute.TexCoord0)) d.GetUVs(0, uu);
            var vertices = new Vector3[count * 2]; var normals = new Vector3[count * 2]; var uv = new Vector2[count * 2];
            for (int i = 0; i < count; i++)
            {
                vertices[i] = vertices[i + count] = vv[i];
                normals[i] = nn[i]; normals[i + count] = -nn[i];
                uv[i] = uv[i + count] = uu[i];
            }
            var mesh = new Mesh { name = source.name + "_Interior", vertices = vertices, normals = normals, uv = uv, subMeshCount = d.subMeshCount };
            for (int s = 0; s < d.subMeshCount; s++)
            {
                var descriptor = d.GetSubMesh(s);
                var ids = new NativeArray<int>(descriptor.indexCount, Allocator.Temp); d.GetIndices(ids, s);
                var triangles = new int[ids.Length * 2];
                for (int i = 0; i < ids.Length; i += 3)
                {
                    triangles[i] = ids[i]; triangles[i + 1] = ids[i + 1]; triangles[i + 2] = ids[i + 2];
                    triangles[ids.Length + i] = ids[i] + count;
                    triangles[ids.Length + i + 1] = ids[i + 2] + count;
                    triangles[ids.Length + i + 2] = ids[i + 1] + count;
                }
                mesh.SetTriangles(triangles, s); ids.Dispose();
            }
            vv.Dispose(); nn.Dispose(); uu.Dispose();
            mesh.RecalculateBounds(); mesh.RecalculateTangents();
            AssetDatabase.CreateAsset(mesh, path); Copies[source] = mesh; return mesh;
        }
    }
    static void Solid(GameObject go)
    {
        foreach (var mf in go.GetComponentsInChildren<MeshFilter>())
        {
            Undo.RecordObject(mf, "Внутренние поверхности");
            mf.sharedMesh = InteriorMesh(mf.sharedMesh);
            var collider = mf.GetComponent<MeshCollider>();
            if (collider == null) collider = Undo.AddComponent<MeshCollider>(mf.gameObject);
            collider.sharedMesh = mf.sharedMesh;
        }
    }
    static GameObject WallSegment(Transform parent, string name, float x, float bottom, float height, float width)
    {
        var model = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Import/kenney_fantasy-town-kit/Models/FBX format/wallWood.fbx");
        var go = (GameObject)PrefabUtility.InstantiatePrefab(model, parent.gameObject.scene);
        Undo.RegisterCreatedObjectUndo(go, "Обрамление входа");
        go.name = name; go.transform.SetParent(parent, false);
        go.transform.rotation = Quaternion.Euler(0, -90, 0);
        go.transform.localScale = new Vector3(2, height, width);
        var b = go.GetComponentInChildren<Renderer>().bounds;
        go.transform.position += new Vector3(x - b.center.x, bottom - b.min.y, -2.9f - b.center.z);
        Solid(go); return go;
    }
    static Mesh GlassMesh()
    {
        string path = Data + "/WindowGlass.asset";
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path); if (existing != null) return existing;
        Vector3[] v = { new Vector3(-.45f, .2f, -.2f), new Vector3(-.45f, .8f, -.2f), new Vector3(-.45f, .8f, .2f), new Vector3(-.45f, .2f, .2f) };
        var mesh = new Mesh { name = "WindowGlass", vertices = v.Concat(v).ToArray(), triangles = new[] { 0, 1, 2, 0, 2, 3, 4, 6, 5, 4, 7, 6 } };
        mesh.RecalculateNormals(); mesh.RecalculateBounds(); AssetDatabase.CreateAsset(mesh, path); return mesh;
    }
    static Material GlassMaterial()
    {
        string path = Data + "/ClearWindowGlass.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path); if (mat != null) return mat;
        mat = new Material(Shader.Find("Standard")) { name = "ClearWindowGlass", color = new Color(.68f, .87f, .95f, .12f) };
        mat.SetFloat("_Mode", 3); mat.SetFloat("_SrcBlend", (float)BlendMode.One); mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        mat.SetFloat("_ZWrite", 0); mat.SetFloat("_Glossiness", .75f);
        mat.EnableKeyword("_ALPHAPREMULTIPLY_ON"); mat.SetOverrideTag("RenderType", "Transparent"); mat.renderQueue = 3000;
        AssetDatabase.CreateAsset(mat, path); return mat;
    }
    public static void Main()
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/AssetWorkshop.unity" || EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Open workshop in Edit Mode.");
        var house = scene.GetRootGameObjects().First(x => x.name == "01_Assembly_Area").transform.Find("WoodenCottage_01");
        var walls = house.Find("02_Walls");
        if (walls.Find("Door_LeftJamb") != null) throw new Exception("Already converted; preserve current changes.");
        if (!AssetDatabase.IsValidFolder(Data)) AssetDatabase.CreateFolder("Assets/Prefabs/Workshop", "CottageGeometry");
        Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("Сделать дом проходимым");
        try
        {
        foreach (Transform wall in walls)
        {
            Undo.RecordObject(wall, "Поднять потолок"); wall.localScale = new Vector3(wall.localScale.x, 2.8f, wall.localScale.z);
        }
        var door = walls.Find("wallWoodDoor");
        Undo.RecordObject(door.gameObject, "Открыть вход"); door.gameObject.SetActive(false);
        WallSegment(walls, "Door_LeftJamb", -.8f, .48f, 2.8f, .4f);
        WallSegment(walls, "Door_RightJamb", .8f, .48f, 2.8f, .4f);
        WallSegment(walls, "Door_Lintel", 0, 2.73f, .55f, 1.2f);
        foreach (Transform post in house.Find("03_Frame")) { Undo.RecordObject(post, "Поднять опоры"); post.localScale = new Vector3(post.localScale.x, 2.92f, post.localScale.z); }
        foreach (Transform roof in house.Find("04_Roof")) { Undo.RecordObject(roof, "Поднять крышу"); roof.position += Vector3.up * .8f; }
        var chimney = house.Find("06_Details/chimney"); Undo.RecordObject(chimney, "Поднять дымоход"); chimney.position += Vector3.up * .8f;
        foreach (string part in new[] { "01_Floor", "02_Walls", "03_Frame", "04_Roof", "05_Porch" }) Solid(house.Find(part).gameObject);
        var glassMesh = GlassMesh(); var glassMaterial = GlassMaterial();
        foreach (Transform window in walls)
        {
            if (!window.name.Contains("Window")) continue;
            var glass = new GameObject("Transparent_Glass"); Undo.RegisterCreatedObjectUndo(glass, "Добавить стекло");
            glass.transform.SetParent(window, false);
            glass.AddComponent<MeshFilter>().sharedMesh = glassMesh;
            var renderer = glass.AddComponent<MeshRenderer>(); renderer.sharedMaterial = glassMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            var collider = glass.AddComponent<BoxCollider>(); collider.center = new Vector3(-.45f, .5f, 0); collider.size = new Vector3(.012f, .6f, .4f);
        }
        // Plank models contain narrow visual gaps. Continuous support avoids snagging a character at seams.
        var support = new GameObject("07_ContinuousFloorSupport");
        Undo.RegisterCreatedObjectUndo(support, "Опора пола"); support.transform.SetParent(house, false);
        var floorCollider = support.AddComponent<BoxCollider>(); floorCollider.center = new Vector3(0, .42f, 0); floorCollider.size = new Vector3(6, .12f, 6);
        var porchCollider = support.AddComponent<BoxCollider>(); porchCollider.center = new Vector3(0, .42f, -4); porchCollider.size = new Vector3(2, .12f, 2);
        Physics.SyncTransforms();
        var report = new System.Text.StringBuilder(); int checks = 0;
        for (float z = -4.6f; z <= 2.2f; z += .1f)
        {
            var bottom = new Vector3(0, .48f + .3f + .02f, z);
            var top = new Vector3(0, .48f + 1.8f - .3f + .02f, z);
            if (Physics.CheckCapsule(bottom, top, .3f, ~0, QueryTriggerInteraction.Ignore)) throw new Exception("Capsule passage blocked at z=" + z);
            checks++;
        }
        foreach (var dir in new[] { Vector3.left, Vector3.right, Vector3.forward })
            if (!Physics.Raycast(new Vector3(0, 1.4f, 0), dir, out var hit, 4, ~0, QueryTriggerInteraction.Ignore)) throw new Exception("Missing wall collider: " + dir);
        if (!Physics.Raycast(new Vector3(0, 1.4f, 0), Vector3.down, 2)) throw new Exception("Missing floor");
        report.AppendLine("PASS capsule 1.8m / diameter .6m through door, checks=" + checks);
        report.AppendLine("PASS internal walls/floor raycasts");
        report.AppendLine("Windows=" + house.GetComponentsInChildren<Renderer>().Count(x => x.name == "Transparent_Glass"));
        report.AppendLine("Colliders=" + house.GetComponentsInChildren<Collider>().Length);
        PrefabUtility.ApplyPrefabInstance(house.gameObject, InteractionMode.AutomatedAction);
        AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        Undo.CollapseUndoOperations(group);
        Selection.activeGameObject = house.gameObject;
        SceneView.lastActiveSceneView?.LookAt(new Vector3(0, 1.5f, -.8f), Quaternion.Euler(25, -35, 0), 7, false, true);
        File.WriteAllText("Temp/cottage-walkable-result.txt", report.ToString());
        }
        catch { Undo.RevertAllDownToGroup(group); throw; }
    }
}
