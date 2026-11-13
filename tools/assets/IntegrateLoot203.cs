using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using DeadBoat.Online;

public static class IntegrateLoot203
{
    const string VisualRoot="Assets/Prefabs/Workshop/LootConcept201", Runtime="Assets/Prefabs/Items/Loot203";
    const string Event="Assets/Events/NewYear/NY_Loot.asset", Catalog="Assets/Resources/Online/SharedItemCatalog.asset", Loc="Assets/Igrodelnya/Localization/LocalizationData.asset";
    const string Balance="Assets/Resources/LocationBalanceProfile.asset";
    public const string Report="Docs/locations/previews/loot203";
    static readonly string[] EN={"Gilded tree topper","Music box","Silver bell","Aurora crystal","Porcelain fox","Pocket compass","Ship in a bottle","Ruby signet","Lighthouse snow globe","Treasure chest","Broken sled","Crate scraps","Dry pine cones","Dried wreath","Candle stubs","Torn gift wrap","Charcoal scraps","Dry straw bundle","Broken nutcracker","Old hemp rope"};
    static readonly string[] RU={"Позолоченная звезда","Музыкальная шкатулка","Серебряный колокольчик","Кристалл северного сияния","Фарфоровая лиса","Карманный компас","Кораблик в бутылке","Рубиновый перстень","Снежный шар с маяком","Сундук с сокровищами","Сломанные санки","Обломки ящика","Сухие шишки","Высохший венок","Огарки свечей","Рваная подарочная бумага","Угольная крошка","Пучок сухой соломы","Сломанный щелкунчик","Старая пеньковая верёвка"};
    static readonly int[] value={22,32,26,34,28,24,32,33,31,36,12,8,5,6,5,4,14,6,9,7};
    static readonly float[] weight={7,6,7,5,7,7,6,5,6,5,6,9,9,8,8,9,6,9,7,8};
    public static bool Shared(string id)=>id=="T06"||id=="T07"||id=="T10";
    public static string Id(int n)=>n<10?"T"+(n+1).ToString("00"):"F"+(n-9).ToString("00");
    public static bool NewItem(PickableItem item)=>item&&item.name.StartsWith("Loot203_",StringComparison.Ordinal);
    public static string IdOf(PickableItem item)=>item.name.Substring(8,3);
    static T Asset<T>(string path) where T:UnityEngine.Object {var a=AssetDatabase.LoadAssetAtPath<T>(path);if(!a)throw new Exception("Missing "+path);return a;}
    static void Dir(string p){var parts=p.Split('/');var parent=parts[0];foreach(var n in parts.Skip(1)){if(!AssetDatabase.IsValidFolder(parent+"/"+n))AssetDatabase.CreateFolder(parent,n);parent+="/"+n;}}
    static void Save(UnityEngine.Object o){EditorUtility.SetDirty(o);AssetDatabase.SaveAssetIfDirty(o);}
    static void Backup(string p){var copy="Temp/DB-ON-203/"+p;Directory.CreateDirectory(Path.GetDirectoryName(copy));if(!File.Exists(copy))File.Copy(p,copy);}
    static string PrefabPath(string name)=>Runtime+"/"+name+".prefab";
    static int TemplateId(PickableItem item){uint hash=2166136261;foreach(char c in AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(item)))hash=unchecked((hash^c)*16777619);return(int)(hash&0x7fffffff);}
    static GameObject Node(Scene s,string name){var g=new GameObject(name);SceneManager.MoveGameObjectToScene(g,s);return g;}
    static Sprite Icon(Scene sc, Camera cam, GameObject visual, string path){
        var go=(GameObject)PrefabUtility.InstantiatePrefab(visual,sc);var b=go.GetComponent<MeshFilter>().sharedMesh.bounds;
        cam.orthographicSize=Mathf.Max(.145f,b.size.magnitude*.54f);cam.transform.position=b.center+new Vector3(1.15f,.96f,-1.6f);cam.transform.LookAt(b.center);
        var rt=RenderTexture.GetTemporary(128,128,24,RenderTextureFormat.ARGB32);var tex=new Texture2D(128,128,TextureFormat.RGBA32,false);var old=RenderTexture.active;
        try{cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,128,128),0,0);tex.Apply();File.WriteAllBytes(path,tex.EncodeToPNG());}
        finally{cam.targetTexture=null;RenderTexture.active=old;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(tex);UnityEngine.Object.DestroyImmediate(go);}
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
        return Asset<Sprite>(path);
    }
    public static Dictionary<string,LocationItemSpawnCollection> Tables(){
        var profile=Asset<LocationBalanceProfile>(Balance);var result=new Dictionary<string,LocationItemSpawnCollection>();
        foreach(string n in new[]{"LevelForest","LevelDesert","LevelWinter","LevelEvent"}){
            var b=profile.GetBalanceForScene(n);if(b==null||b.sceneName!=n||!b.itemCollectionOverride)throw new Exception("Missing dedicated table for "+n);result.Add(n,b.itemCollectionOverride);
        }
        if(AssetDatabase.GetAssetPath(result["LevelEvent"])!=Event||result.Values.Distinct().Count()!=4)throw new Exception("Unexpected shared loot table");
        var intro=EditorBuildSettings.scenes.Single(s=>s.enabled&&s.path.EndsWith("/FirstRunAuthored.unity"));
        foreach(var p in AssetDatabase.GetDependencies(intro.path,true)){
            if(AssetDatabase.GetMainAssetTypeAtPath(p)!=typeof(LocationItemSpawnCollection))continue;
            var a=Asset<LocationItemSpawnCollection>(p);if(!result.Values.Contains(a))result.Add("fallback/"+a.name,a);
        }
        var fallback=Asset<LocationItemSpawnCollection>("Assets/Scripts/Locations/Items/LocationItemSpawnCollection.asset");
        if(!result.Values.Contains(fallback))result.Add("fallback/"+fallback.name,fallback);
        return result;
    }
    public static string Install(){
        if(AssetDatabase.IsValidFolder(Runtime))throw new Exception("Loot203 already exists; use verification before retry");
        var source=Asset<GameObject>("Assets/Prefabs/Items/goldCup.prefab");
        var bank=Asset<GameObject>("Assets/Prefabs/Managers.prefab").GetComponentInChildren<ItemsManager>(true);
        var lookup=(StoreItemsCollection)new SerializedObject(bank).FindProperty("collection").objectReferenceValue;if(!lookup)throw new Exception("Missing solo lookup collection");
        var cat=Asset<SharedItemCatalog>(Catalog);var loc=Asset<LocalizationData>(Loc);var tables=Tables();
        if(loc.Languages.Count!=2||!loc.Languages.Contains("Ru")||!loc.Languages.Contains("En")||!loc.Entries.Any(e=>e.Key=="Item"&&e.Translations.All(x=>x=="Item/")))throw new Exception("Unexpected item localization schema");
        if(lookup.Items.Any(NewItem)||cat.entries.Any(e=>NewItem(e.prefab))||tables.Values.Any(t=>t.spawnEntries.Any(e=>NewItem(e.itemPrefab))))throw new Exception("Partial Loot203 registration exists");
        foreach(var p in tables.Values.Select(AssetDatabase.GetAssetPath).Concat(new[]{Catalog,Loc,AssetDatabase.GetAssetPath(lookup)}))Backup(p);
        var visuals=AssetDatabase.FindAssets("t:Prefab",new[]{VisualRoot}).Select(AssetDatabase.GUIDToAssetPath).Select(Asset<GameObject>).ToDictionary(a=>a.name.Substring(0,3));
        if(visuals.Count!=20)throw new Exception("Expected all 20 visual models");
        Dir(Runtime+"/Data");Dir(Runtime+"/Icons");
        var sc=EditorSceneManager.NewPreviewScene();var result=new List<PickableItem>();var sizes=new List<ItemSize>();
        try{
            var cam=Node(sc,"Loot203 icon camera").AddComponent<Camera>();cam.scene=sc;cam.enabled=false;cam.orthographic=true;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=Color.clear;cam.allowHDR=false;cam.nearClipPlane=.03f;cam.farClipPlane=15;
            var sun=Node(sc,"Icon light").AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=.85f;sun.transform.rotation=Quaternion.Euler(44,30,0);
            var holder=Node(sc,"Inactive prefab editing holder");holder.SetActive(false);
            for(int i=0;i<20;i++){
                string id=Id(i);var model=visuals[id];var name="Loot203_"+model.name;var key="Loot"+model.name.Replace("_","");
                var b=model.GetComponent<MeshFilter>().sharedMesh.bounds;if(Mathf.Abs(b.min.y)>.001f||b.size.magnitude<.1f)throw new Exception("Bad mesh "+id);
                var sprite=Icon(sc,cam,model,Runtime+"/Icons/"+model.name+".png");
                var data=ScriptableObject.CreateInstance<ItemData>();data.name=key;var dataSO=new SerializedObject(data);dataSO.FindProperty("_name").stringValue=key;dataSO.FindProperty("_img").objectReferenceValue=sprite;dataSO.ApplyModifiedPropertiesWithoutUndo();AssetDatabase.CreateAsset(data,Runtime+"/Data/"+key+".asset");
                var g=(GameObject)PrefabUtility.InstantiatePrefab(source,holder.transform);
                try{
                    if(g.GetComponentsInChildren<Component>(true).Any(c=>!c))throw new Exception("Missing script on template");
                    PrefabUtility.UnpackPrefabInstance(g,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);g.name=name;g.transform.localPosition=Vector3.zero;g.transform.localRotation=Quaternion.identity;g.transform.localScale=Vector3.one;
                    var pick=g.GetComponent<PickableItem>();var so=new SerializedObject(pick);var vis=(GameObject)so.FindProperty("_visualObj").objectReferenceValue;
                    if(!vis||vis==g||!vis.transform.IsChildOf(g.transform))throw new Exception("Unexpected visual slot");
                    foreach(var ch in vis.transform.Cast<Transform>().ToArray())UnityEngine.Object.DestroyImmediate(ch.gameObject);
                    vis.transform.localPosition=Vector3.zero;vis.transform.localRotation=Quaternion.identity;vis.transform.localScale=Vector3.one;
                    var mesh=(GameObject)PrefabUtility.InstantiatePrefab(model,vis.transform);mesh.transform.localPosition=-b.center;mesh.transform.localRotation=Quaternion.identity;mesh.transform.localScale=Vector3.one;
                    so.FindProperty("_itemData").objectReferenceValue=data;so.ApplyModifiedPropertiesWithoutUndo();
                    var box=g.GetComponent<BoxCollider>();box.center=Vector3.zero;box.size=Vector3.Max(b.size,Vector3.one*.12f);box.isTrigger=false;
                    var sphere=g.GetComponent<SphereCollider>();sphere.center=Vector3.zero;sphere.radius=Mathf.Max(.29f,b.extents.magnitude+.08f);
                    g.GetComponent<Rigidbody>().collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
                    var sale=g.GetComponent<SellableItem>();
                    if(i<10){var st=new SerializedObject(sale);st.FindProperty("_cost").intValue=value[i];st.ApplyModifiedPropertiesWithoutUndo();}
                    else{UnityEngine.Object.DestroyImmediate(sale);g.AddComponent<FuelItem>().fuelValue=value[i];}
                    if(!PrefabUtility.SaveAsPrefabAsset(g,PrefabPath(name)))throw new Exception("Failed to save "+name);
                }finally{UnityEngine.Object.DestroyImmediate(g);}
                var prefab=Asset<GameObject>(PrefabPath(name)).GetComponent<PickableItem>();
                if(!prefab||prefab.Data!=data||!prefab.Data.IMG||!prefab.GetComponent<BoxCollider>()||(prefab.GetComponent<SellableItem>()!=null)!=(i<10)||(prefab.GetComponent<FuelItem>()!=null)!=(i>=10))throw new Exception("Invalid gameplay prefab "+id);
                result.Add(prefab);sizes.Add(Mathf.Max(b.size.x,Mathf.Max(b.size.y,b.size.z))>.55f?ItemSize.Medium:ItemSize.Small);
            }
        }finally{EditorSceneManager.ClosePreviewScene(sc);}
        var ids=new HashSet<int>(cat.entries.Select(e=>e.id));var names=new HashSet<string>(cat.entries.Where(e=>e.prefab).Select(e=>e.prefab.name));
        var keys=new HashSet<string>(lookup.Items.Where(p=>p&&p.Data).Select(p=>p.Data.Name));var translated=new HashSet<string>(loc.Entries.Select(e=>e.Key));
        var newCatalog=result.Select(p=>new SharedItemCatalog.Entry{id=TemplateId(p),prefab=p}).ToArray();
        if(newCatalog.Any(e=>e.id==0||!ids.Add(e.id)||!names.Add(e.prefab.name)||!keys.Add(e.prefab.Data.Name)||!translated.Add("Item/"+e.prefab.Data.Name)))throw new Exception("New item ID/name collision");
        foreach(var t in tables){for(int i=0;i<result.Count;i++)if(t.Key=="LevelEvent"||Shared(Id(i)))t.Value.spawnEntries.Add(new LocationItemSpawnEntry{itemPrefab=result[i],chance=weight[i],itemType=i<10?ItemType.Treasure:ItemType.Fuel,itemSize=sizes[i]});Save(t.Value);}
        lookup.Items.AddRange(result);Save(lookup);
        cat.entries.AddRange(newCatalog);Save(cat);
        for(int i=0;i<result.Count;i++){var n=i;loc.Entries.Add(new LocalizationEntry{Key="Item/"+result[i].Data.Name,Translations=loc.Languages.Select(l=>l=="Ru"?RU[n]:EN[n]).ToList()});}Save(loc);
        Directory.CreateDirectory(Report);
        var lines=Verify().Split('\n').Concat(Enumerable.Range(0,20).Select(i=>$"{Id(i)} {EN[i]}: {(i<10?"coins":"fuel")}={value[i]}, weight={weight[i]}, size={sizes[i]}, network ID={newCatalog[i].id}"));
        File.WriteAllLines(Report+"/asset-validation.txt",lines);return string.Join("\n",lines);
    }
    public static string Verify(){
        var tables=Tables();var lines=new List<string>();
        foreach(var t in tables){var values=t.Value.spawnEntries.Where(e=>NewItem(e.itemPrefab)).Select(e=>IdOf(e.itemPrefab)).OrderBy(s=>s).ToArray();
            var expected=Enumerable.Range(0,20).Select(Id).Where(id=>t.Key=="LevelEvent"||Shared(id)).OrderBy(s=>s).ToArray();if(!values.SequenceEqual(expected))throw new Exception("Wrong map distribution "+t.Key);lines.Add(t.Key+" = "+string.Join(",",values));}
        var loc=Asset<LocalizationData>(Loc);var cat=Asset<SharedItemCatalog>(Catalog);var bank=Asset<GameObject>("Assets/Prefabs/Managers.prefab").GetComponentInChildren<ItemsManager>(true);
        var lookup=(StoreItemsCollection)new SerializedObject(bank).FindProperty("collection").objectReferenceValue;
        var loot=lookup.Items.Where(NewItem).ToArray();if(loot.Length!=20||cat.entries.Count(e=>NewItem(e.prefab))!=20)throw new Exception("Wrong catalog totals");
        foreach(var p in loot){var id=IdOf(p);int i=id[0]=='T'?int.Parse(id.Substring(1))-1:int.Parse(id.Substring(1))+9;
            var sale=p.GetComponent<SellableItem>();var fuel=p.GetComponent<FuelItem>();var box=p.GetComponent<BoxCollider>();var body=p.GetComponent<Rigidbody>();
            if((sale!=null)!=(i<10)||(fuel!=null)!=(i>=10)||!box||box.isTrigger||!body||!p.Data.IMG||cat.Find(TemplateId(p))!=p||!loc.Entries.Any(e=>e.Key=="Item/"+p.Data.Name&&e.Translations.Count==2))throw new Exception("Broken item wiring "+id);}
        lines.Add("20 pickable prefabs with icons, Russian/English labels, offline save lookup and unique shared IDs verified");return string.Join("\n",lines);
    }
    public static void RefreshRevisedIcons(){
        var scene=EditorSceneManager.NewPreviewScene();
        try{
            var cam=Node(scene,"Loot203 revision icon camera").AddComponent<Camera>();cam.scene=scene;cam.enabled=false;cam.orthographic=true;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=Color.clear;cam.allowHDR=false;
            var sun=Node(scene,"Icon light").AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=.85f;sun.transform.rotation=Quaternion.Euler(44,30,0);
            foreach(string id in new[]{"T05","T06","T10","F06"}){
                var path=AssetDatabase.FindAssets("t:Prefab",new[]{VisualRoot}).Select(AssetDatabase.GUIDToAssetPath).Single(p=>Path.GetFileName(p).StartsWith(id+"_",StringComparison.Ordinal));
                var visual=Asset<GameObject>(path);var item=Asset<GameObject>(PrefabPath("Loot203_"+visual.name)).GetComponent<PickableItem>();
                var iconPath=Runtime+"/Icons/"+visual.name+".png";var guid=AssetDatabase.AssetPathToGUID(iconPath);
                var icon=Icon(scene,cam,visual,iconPath);if(guid!=AssetDatabase.AssetPathToGUID(iconPath))throw new Exception("Icon GUID changed");
                var data=new SerializedObject(item.Data);data.FindProperty("_img").objectReferenceValue=icon;data.ApplyModifiedPropertiesWithoutUndo();Save(item.Data);
                var rbMesh=item.GetComponentInChildren<MeshFilter>().sharedMesh;var bounds=item.GetComponent<BoxCollider>().size;
                if(rbMesh!=visual.GetComponent<MeshFilter>().sharedMesh||(bounds-Vector3.Max(rbMesh.bounds.size,Vector3.one*.12f)).sqrMagnitude>.00001f)throw new Exception("Collider and revised model disagree "+id);
            }
        }finally{EditorSceneManager.ClosePreviewScene(scene);}
    }
}
