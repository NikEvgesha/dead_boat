using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

public static class WinterSplitGrottos215
{
    const string Event="Assets/Events/NewYear/";
    const string Hall=Event+"ShorePOI/POI_03_Frozen_Grotto.prefab";
    const string Wreck=Event+"ShorePOI/POI_08_Frozen_Wreck.prefab";
    const int PriorSize=22;

    public static string Split()
    {
        var asset=AssetDatabase.LoadAssetAtPath<LocationSpawnCollection>(Event+"NY_Locations.asset");
        var serialized=new SerializedObject(asset);
        var pool=serialized.FindProperty("spawnItems");
        if(pool.arraySize!=PriorSize && pool.arraySize!=PriorSize+1)
            throw new InvalidOperationException("The winter location pool changed; review before splitting");
        var hallPrefab=AssetDatabase.LoadAssetAtPath<GameObject>(Hall);
        if(pool.GetArrayElementAtIndex(18).FindPropertyRelative("prefab").objectReferenceValue!=hallPrefab.GetComponent<LocationContentSpawner>())
            throw new InvalidOperationException("The original point 03 reference changed");
        if(pool.arraySize==PriorSize+1 && AssetDatabase.GetAssetPath(
            pool.GetArrayElementAtIndex(PriorSize).FindPropertyRelative("prefab").objectReferenceValue)!=Wreck)
            throw new InvalidOperationException("The last pool entry belongs to another location");

        if(!AssetDatabase.LoadAssetAtPath<GameObject>(Wreck) && !AssetDatabase.CopyAsset(Hall,Wreck))
            throw new InvalidOperationException("Could not copy the grotto for point 08");
        ConfigureWreck();
        var wreck=AssetDatabase.LoadAssetAtPath<GameObject>(Wreck).GetComponent<LocationContentSpawner>();
        if(pool.arraySize==PriorSize) pool.arraySize=PriorSize+1;
        var entry=pool.GetArrayElementAtIndex(PriorSize);
        var reference=entry.FindPropertyRelative("prefab");
        reference.objectReferenceValue=wreck;
        entry.FindPropertyRelative("chance").floatValue=30f;
        entry.FindPropertyRelative("HaveMinSpawnDistence").boolValue=false;
        entry.FindPropertyRelative("HaveMaxSpawnDistence").boolValue=false;
        entry.FindPropertyRelative("MinSpawnDistence").floatValue=0f;
        entry.FindPropertyRelative("MaxSpawnDistence").floatValue=0f;
        var previous=serialized.FindProperty("_revisionOnePoolSize");
        if(previous!=null) previous.intValue=PriorSize;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssetIfDirty(asset);

        var hall=PrefabUtility.LoadPrefabContents(Hall);
        try
        {
            var duplicate=hall.transform.Find("Frozen_Wreck");
            if(duplicate)
            {
                if(duplicate.gameObject.activeSelf || !hall.transform.Find("Glacier_Hall"))
                    throw new InvalidOperationException("Point 03 must contain the active glacier hall");
                Object.DestroyImmediate(duplicate.gameObject);
                Save(hall,Hall);
            }
        }
        finally { PrefabUtility.UnloadPrefabContents(hall); }
        return "NY_Locations: 23 entries. Point 03 is the hall; point 08 is the separate wreck.";
    }

    static void ConfigureWreck()
    {
        var root=PrefabUtility.LoadPrefabContents(Wreck);
        try
        {
            var wreck=root.transform.Find("Frozen_Wreck");
            if(!wreck) throw new InvalidOperationException("The copied grotto has no shipwreck");
            var hall=root.transform.Find("Glacier_Hall");
            if(hall) Object.DestroyImmediate(hall.gameObject);
            root.name="POI_08_Frozen_Wreck";
            wreck.gameObject.SetActive(true);
            var content=root.GetComponent<LocationContentSpawner>();
            var marks=content.itemSpawnPoints.Select(p=>p.spawnTransform).Concat(content.enemySpawnPoints).ToArray();
            var art=root.transform.GetChild(0);
            var local=marks.Select(mark=>art.InverseTransformPoint(mark.position)).ToArray();
            art.localPosition=wreck.localPosition;
            art.localRotation=wreck.localRotation;
            PrefabUtility.RecordPrefabInstancePropertyModifications(art);
            for(int i=0;i<marks.Length;i++) marks[i].position=art.TransformPoint(local[i]);
            var renderers=root.GetComponentsInChildren<Renderer>();
            var bounds=renderers[0].bounds;
            foreach(var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            foreach(var mark in marks) bounds.Encapsulate(mark.position);
            if(bounds.max.x>-6f) throw new InvalidOperationException("The wreck must stay on the bank");
            var region=root.GetComponent<BoxCollider>();
            region.center=bounds.center;
            region.size=bounds.size+new Vector3(1.6f,1.2f,2.4f);
            var cleaner=root.transform.Find("Keep_Background_Decor_Clear");
            cleaner.localPosition=region.center;
            cleaner.GetComponent<BoxCollider>().size=region.size;
            Save(root,Wreck);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    static void Save(GameObject root,string path)
    {
        PrefabUtility.SaveAsPrefabAsset(root,path,out var saved);
        if(!saved) throw new InvalidOperationException("Could not save "+path);
    }
}
