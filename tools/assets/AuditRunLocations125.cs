using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

// Read-only audit. Preview scenes are always closed; no assets or scene settings are saved.
public static class AuditRunLocations125
{
    public static string Capture()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit mode required");
        string[] families={"house1","house2","house3","TreeHouse","ruins","ship","castle","Village","pond","boshka","clocks","tower","To4ka6","Sklep"};
        Directory.CreateDirectory("Docs/locations/previews");
        foreach(var family in families)
        {
            var preview=new PreviewRenderUtility();
            GameObject clone=null;
            try
            {
                var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Locations/Enemy/"+family+"_1.prefab");
                clone=(GameObject)PrefabUtility.InstantiatePrefab(source,preview.camera.scene);
                preview.AddSingleGO(clone);
                var renderers=clone.GetComponentsInChildren<Renderer>().Where(r=>r.enabled).ToArray();
                var bounds=renderers[0].bounds;
                foreach(var r in renderers)bounds.Encapsulate(r.bounds);
                var camera=preview.camera;
                camera.orthographic=true;
                camera.transform.position=bounds.center+new Vector3(1,1.4f,-1).normalized*(bounds.size.magnitude+100);
                camera.transform.LookAt(bounds.center);
                float half=0;
                for(int i=0;i<8;i++)
                {
                    var pt=bounds.center+Vector3.Scale(bounds.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));
                    var v=camera.transform.InverseTransformPoint(pt);
                    half=Mathf.Max(half,Mathf.Abs(v.y),Mathf.Abs(v.x)/1.5f);
                }
                camera.orthographicSize=Mathf.Max(2,half*1.12f);
                camera.nearClipPlane=.1f;camera.farClipPlane=2000;
                camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.12f,.16f,.19f);
                preview.lights[0].intensity=1.3f;preview.lights[0].transform.rotation=Quaternion.Euler(45,30,0);
                preview.lights[1].intensity=.7f;
                preview.ambientColor=new Color(.65f,.65f,.65f);
                preview.BeginStaticPreview(new Rect(0,0,768,512));
                camera.Render();
                var texture=preview.EndStaticPreview();
                File.WriteAllBytes("Docs/locations/previews/"+family+".png",texture.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(texture);
            }
            finally { preview.Cleanup(); }
        }
        return "Saved 14 prefab previews; no gameplay scenes saved";
    }
    [Serializable] public class Report { public string unity; public bool playing, compiling; public List<Entry> entries = new List<Entry>(); }
    [Serializable] public class Entry
    {
        public string asset, hierarchy, type, source, data;
        public Vector3 position, rotation, scale, boundsCenter, boundsSize;
        public Vector3 activeBoundsCenter, activeBoundsSize;
        public int renderers, activeRenderers, colliders, missing;
        public List<string> fields = new List<string>();
        public List<string> children = new List<string>();
    }
    static string PathOf(Transform t) { return t.parent == null ? t.name : PathOf(t.parent) + "/" + t.name; }
    // Unity JsonUtility omits DTO lists from dynamically compiled Roslyn assemblies.
    static string Json(object v)
    {
        if(v==null)return "null";
        if(v is string) return "\""+((string)v).Replace("\\","\\\\").Replace("\"","\\\"").Replace("\r","\\r").Replace("\n","\\n").Replace("\t","\\t")+"\"";
        if(v is bool)return (bool)v?"true":"false";
        if(v.GetType().IsPrimitive)return Convert.ToString(v,System.Globalization.CultureInfo.InvariantCulture);
        if(v is System.Collections.IEnumerable)return "["+string.Join(",",((System.Collections.IEnumerable)v).Cast<object>().Select(Json))+"]";
        return "{"+string.Join(",",v.GetType().GetFields(System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.Instance).Select(f=>Json(f.Name)+":"+Json(f.GetValue(v))))+"}";
    }
    static bool Wanted(Component c)
    {
        return c != null && new[] { "LocationContentSpawner", "LocationSpawner", "CitySpawner", "LendController", "ZoneCleaner", "CityZoneCleaner", "DecorationSpawner", "FixCoordinate", "NavMeshSurface", "DungeonTrap", "Portal", "Lever", "ActivateZone", "CheckItemOnLocation", "DamageArea", "DarkZone" }.Contains(c.GetType().Name);
    }
    static Entry Inspect(UnityEngine.Object obj, string asset)
    {
        var c = obj as Component;
        var e = new Entry { asset = asset, type = obj.GetType().Name, data = EditorJsonUtility.ToJson(obj) };
        var so = new SerializedObject(obj);
        var p = so.GetIterator();
        while (p.Next(true))
        {
            if (p.propertyType != SerializedPropertyType.ObjectReference) continue;
            var v = p.objectReferenceValue;
            e.fields.Add(p.propertyPath + " = " + (v == null ? "null" : AssetDatabase.GetAssetPath(v) + " :: " + (v is Component ? PathOf(((Component)v).transform) : v is GameObject ? PathOf(((GameObject)v).transform) : v.name)));
        }
        if (c != null)
        {
            var t = c.transform;
            e.hierarchy = PathOf(t); e.position = t.position; e.rotation = t.eulerAngles; e.scale = t.lossyScale;
            var source = PrefabUtility.GetCorrespondingObjectFromSource(c);
            e.source = source == null ? "" : AssetDatabase.GetAssetPath(source);
            var rs = c.GetComponentsInChildren<Renderer>(true); e.renderers = rs.Length;
            e.colliders = c.GetComponentsInChildren<Collider>(true).Length;
            bool first = true, firstActive = true; Bounds bounds = new Bounds(), activeBounds = new Bounds();
            foreach (var r in rs)
            {
                bool active=r.enabled;
                for(var ancestor=r.transform; ancestor!=null; ancestor=ancestor.parent) if(!ancestor.gameObject.activeSelf)active=false;
                if(active)e.activeRenderers++;
                var b = r.localBounds;
                for (int i=0;i<8;i++)
                {
                    var corner = b.center + Vector3.Scale(b.extents, new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));
                    var pt = t.InverseTransformPoint(r.transform.TransformPoint(corner));
                    if(first){ bounds = new Bounds(pt,Vector3.zero); first=false;} else bounds.Encapsulate(pt);
                    if(active) { if(firstActive){activeBounds=new Bounds(pt,Vector3.zero);firstActive=false;}else activeBounds.Encapsulate(pt); }
                }
            }
            e.boundsCenter = bounds.center; e.boundsSize = bounds.size;
            e.activeBoundsCenter = activeBounds.center; e.activeBoundsSize = activeBounds.size;
            foreach(Transform child in t) e.children.Add(child.name+" pos="+child.localPosition+" rot="+child.localEulerAngles+" scale="+child.localScale+" source="+PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(child.gameObject));
            foreach(var child in c.GetComponentsInChildren<Transform>(true)) e.missing += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject);
        }
        return e;
    }
    public static string Main()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Edit mode required");
        var report = new Report { unity=Application.unityVersion, playing=EditorApplication.isPlaying, compiling=EditorApplication.isCompiling };
        foreach(var guid in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Prefabs/Locations"}))
        {
            var path=AssetDatabase.GUIDToAssetPath(guid);
            var root=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            report.entries.Add(Inspect(root.transform,path));
            foreach(var c in root.GetComponentsInChildren<Component>(true).Where(Wanted)) report.entries.Add(Inspect(c,path));
        }
        foreach(var path in new[]{"Assets/Scenes/LevelForest.unity","Assets/Scenes/LevelDesert.unity","Assets/Scenes/LevelWinter.unity"})
        {
            var scene=EditorSceneManager.OpenPreviewScene(path);
            try { foreach(var root in scene.GetRootGameObjects()) foreach(var c in root.GetComponentsInChildren<Component>(true).Where(Wanted)) report.entries.Add(Inspect(c,path)); }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }
        foreach(var query in new[]{"t:LocationSpawnCollection","t:LocationItemSpawnCollection","t:LocationBalanceProfile"})
            foreach(var guid in AssetDatabase.FindAssets(query))
            { var path=AssetDatabase.GUIDToAssetPath(guid); report.entries.Add(Inspect(AssetDatabase.LoadMainAssetAtPath(path),path)); }
        Directory.CreateDirectory("Docs/locations");
        File.WriteAllText("Docs/locations/audit-2026-10-23.json",Json(report));
        return "Saved "+report.entries.Count+" records; Unity="+report.unity+" playing="+report.playing+" compiling="+report.compiling;
    }
}
