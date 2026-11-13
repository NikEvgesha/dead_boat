using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class WinterShorePois204
{
    const string Event = "Assets/Events/NewYear/";
    const string Folder = Event + "ShorePOI";
    const string Source = Event + "Route181/";
    const string LocationPath = Event + "NY_Locations.asset";
    const string DecorationPath = Event + "NY_Decor.asset";
    const int LegacyPoolSize = 16;

    sealed class Site
    {
        public readonly string Model, Axis;
        public readonly float Bank, Scale;
        public readonly Vector2[] Loot, Enemies;
        public Site(string model, string axis, float bank, Vector2[] loot, Vector2[] enemies, float scale = 1f)
        {
            Model = model;
            Axis = axis;
            Bank = bank;
            Scale = scale;
            Loot = loot;
            Enemies = enemies;
        }
        public string Name => "POI_" + Model.Substring("Bank_".Length);
        public string AssetPath => Folder + "/" + Name + ".prefab";
    }

    static Vector2 At(float x, float z) => new(x,z);

    static readonly Site[] Sites =
    {
        new("Bank_01_Snow_Tunnel","Walkthrough_Shell",-16f,
            new[]{At(-.98f,-5.02f),At(.85f,5.10f)},new[]{At(3.45f,4.85f)},2f),
        new("Bank_02_Rooted_Log","Snowbound_Hollow_Log",18f,
            new[]{At(-1.08f,-5.03f),At(1.05f,5.02f)},new[]{At(-3.35f,4.90f)},2f),
        new("Bank_03_Frozen_Grotto","Walkthrough_Shell",-17f,
            new[]{At(-1.37f,-4.76f),At(.80f,5.08f)},new[]{At(-2.95f,4.78f)},2f),
        new("Bank_05_Gift_Sled","Painted_Gift_Sleigh",20f,
            new[]{At(-1.80f,2.24f),At(.98f,-1.91f)},new[]{At(1.90f,2.00f)},2f),
        new("Bank_06_Fir_Arch","Snowy_Fir_Arch",-16f,
            new[]{At(.81f,1.71f),At(-.83f,-1.72f)},new[]{At(2.32f,2.43f)}),
        new("Bank_07_Ice_Crystal_Field","Shattered_Ice_Clearing",16f,
            new[]{At(.29f,-1.70f),At(.91f,2.55f)},new[]{At(-2.82f,2.66f),At(2.35f,-2.68f)})
    };

    static Transform Marker(Transform parent, string name, Vector3 position)
    {
        var marker = new GameObject(name).transform;
        marker.SetParent(parent,false);
        marker.position = position;
        return marker;
    }

    static void BuildSite(Site site, Scene preview, LocationItemSpawnCollection loot, List<ZombieController> enemies)
    {
        var root = new GameObject(site.Name);
        SceneManager.MoveGameObjectToScene(root,preview);
        root.tag = "Location";
        root.layer = (int)Layer.SpawnLocations;
        var content = root.AddComponent<LocationContentSpawner>();
        content.itemCollection = loot;
        content._enemyPrefabs = new List<ZombieController>(enemies);
        content.itemSpawnPoints = new List<LocationSpawnPoint>();
        content.enemySpawnPoints = new List<Transform>();
        var serialized = new SerializedObject(content);
        var clearance = serialized.FindProperty("_cityClearance");
        if (clearance != null)
        {
            clearance.floatValue = 60f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        var source = AssetDatabase.LoadAssetAtPath<GameObject>(Source+site.Model+".prefab");
        if (!source) throw new InvalidOperationException("Missing art for "+site.Name);
        var art = (GameObject)PrefabUtility.InstantiatePrefab(source,root.transform);
        art.transform.localPosition = Vector3.right*site.Bank;
        art.transform.localScale = Vector3.one*site.Scale;
        art.tag = "Untagged";
        var oldTrigger = art.GetComponent<BoxCollider>();
        oldTrigger.enabled = false;
        PrefabUtility.RecordPrefabInstancePropertyModifications(art);
        PrefabUtility.RecordPrefabInstancePropertyModifications(art.transform);
        PrefabUtility.RecordPrefabInstancePropertyModifications(oldTrigger);

        var axis = art.transform.Find(site.Axis);
        var footprint = new Bounds(art.transform.TransformPoint(oldTrigger.center),oldTrigger.size*site.Scale);
        if (site.Scale > 1f)
            foreach (var renderer in art.GetComponentsInChildren<MeshRenderer>())
                footprint.Encapsulate(renderer.bounds);
        Vector3 OnGround(Vector2 point, float y)
        {
            var position=axis.TransformPoint(new Vector3(point.x,0f,point.y));
            position.y=y;
            footprint.Encapsulate(position);
            return position;
        }
        foreach (var point in site.Loot)
        {
            var marker=Marker(root.transform,"Loot_"+(content.itemSpawnPoints.Count+1),OnGround(point,.22f));
            content.itemSpawnPoints.Add(new LocationSpawnPoint {spawnTransform=marker,allowedSize=ItemSize.Small});
        }
        foreach (var point in site.Enemies)
            content.enemySpawnPoints.Add(Marker(root.transform,"Guard_"+(content.enemySpawnPoints.Count+1),OnGround(point,.03f)));

        var center=footprint.center;
        var size=footprint.size+new Vector3(1.6f,1.2f,2.4f);
        var region=root.AddComponent<BoxCollider>();
        region.isTrigger=true;
        region.center=center;
        region.size=size;
        var cleanup=Marker(root.transform,"Keep_Background_Decor_Clear",center).gameObject;
        var trigger=cleanup.AddComponent<BoxCollider>();
        trigger.isTrigger=true;
        trigger.size=size;
        var physics=cleanup.AddComponent<Rigidbody>();
        physics.isKinematic=true;
        physics.useGravity=false;
        cleanup.AddComponent<CityZoneCleaner>().forbiddenTags=new List<string>{"Decoration"};
        if (!PrefabUtility.SaveAsPrefabAsset(root,site.AssetPath))
            throw new InvalidOperationException("Could not save "+site.AssetPath);
        Object.DestroyImmediate(root);
    }

    public static void Author()
    {
        Debug.Log("[Shore POI] " + BuildPrefabs());
        Debug.Log("[Shore POI] " + LinkCollections());
    }

    public static string BuildPrefabs()
    {
        if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder(Event.TrimEnd('/'),"ShorePOI");
        var loot=AssetDatabase.LoadAssetAtPath<LocationItemSpawnCollection>(Event+"NY_Loot.asset");
        var enemies=new[]{"NY_just_zombie","NY_DrownedZombie"}
            .Select(name=>AssetDatabase.LoadAssetAtPath<GameObject>(Event+name+".prefab").GetComponent<ZombieController>())
            .ToList();
        var preview=EditorSceneManager.NewPreviewScene();
        try
        {
            foreach (var site in Sites)
            {
                var existing=AssetDatabase.LoadAssetAtPath<GameObject>(site.AssetPath);
                if (existing && (existing.transform.Find("Winter_Fair") || existing.transform.Find("Signal_Pass") ||
                    existing.transform.Find("Frozen_Wreck") || existing.transform.Find("Glacier_Hall"))) continue;
                BuildSite(site,preview,loot,enemies);
            }
        }
        finally { EditorSceneManager.ClosePreviewScene(preview); }
        return string.Join(", ",Sites.Select(site=>site.Name));
    }

    public static string LinkCollections()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Link the pools in Edit Mode to preserve the active run.");
        var locations=AssetDatabase.LoadAssetAtPath<LocationSpawnCollection>(LocationPath);
        var decorations=AssetDatabase.LoadAssetAtPath<DecorationSpawnCollection>(DecorationPath);
        var serialized=new SerializedObject(locations);
        var pool=serialized.FindProperty("spawnItems");
        var legacySize=serialized.FindProperty("_legacyPoolSize");
        int originalShoreSize=LegacyPoolSize+Sites.Length;
        var wreck=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/POI_08_Frozen_Wreck.prefab");
        bool hasWreck=pool.arraySize==originalShoreSize+1 && wreck &&
            pool.GetArrayElementAtIndex(originalShoreSize).FindPropertyRelative("prefab").objectReferenceValue==
            wreck.GetComponent<LocationContentSpawner>();
        if (pool.arraySize!=LegacyPoolSize && pool.arraySize!=originalShoreSize && !hasWreck)
            throw new InvalidOperationException("The location pool changed independently; review before linking.");
        foreach (var site in Sites)
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(site.AssetPath);
            var content=prefab.GetComponent<LocationContentSpawner>();
            if (new SerializedObject(content).FindProperty("_cityClearance")?.floatValue!=60f) throw new InvalidOperationException("Missing castle clearance on "+site.Name);
        }
        legacySize.intValue=LegacyPoolSize;
        if (!hasWreck) pool.arraySize=originalShoreSize;
        for (int i=0;i<Sites.Length;i++)
        {
            var entry=pool.GetArrayElementAtIndex(LegacyPoolSize+i);
            entry.FindPropertyRelative("prefab").objectReferenceValue=
                AssetDatabase.LoadAssetAtPath<GameObject>(Sites[i].AssetPath).GetComponent<LocationContentSpawner>();
            entry.FindPropertyRelative("chance").floatValue=30f;
            entry.FindPropertyRelative("HaveMinSpawnDistence").boolValue=false;
            entry.FindPropertyRelative("MinSpawnDistence").floatValue=0f;
            entry.FindPropertyRelative("HaveMaxSpawnDistence").boolValue=false;
            entry.FindPropertyRelative("MaxSpawnDistence").floatValue=0f;
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
        var models=new HashSet<string>(Sites.Select(site=>site.Model));
        decorations.spawnItems.RemoveAll(entry=>entry.prefab && models.Contains(entry.prefab.name));
        EditorUtility.SetDirty(decorations);
        AssetDatabase.SaveAssetIfDirty(locations);
        AssetDatabase.SaveAssetIfDirty(decorations);
        return $"Location pool: {pool.arraySize} entries, background pool: {decorations.spawnItems.Count} entries.";
    }
}
