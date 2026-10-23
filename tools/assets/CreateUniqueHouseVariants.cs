using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class CreateUniqueHouseVariants
{
    const string Folder="Assets/Prefabs/Workshop/";
    const string Data=Folder+"CottageGeometry/";
    const string Cat="Assets/Import/CatBorg Studio/3D Dungeon Lowpoly Pack/Models/";
    const string Kay="Assets/Import/KayKit_DungeonRemastered_1.1_FREE/Assets/fbx(unity)/";
    const string Medieval="Assets/Import/LowPolyMedievalPropsLite/Models/";
    static Material Mat(string name,Color color)
    {
        var mat=AssetDatabase.LoadAssetAtPath<Material>(Data+name+".mat");if(mat!=null)return mat;
        mat=new Material(Shader.Find("Standard")){name=name,color=color};mat.SetFloat("_Glossiness",.15f);AssetDatabase.CreateAsset(mat,Data+name+".mat");return mat;
    }
    static Material Existing(string name){return AssetDatabase.LoadAssetAtPath<Material>(Data+name+".mat");}
    static Bounds BoundsOf(GameObject go)
    {
        var rr=go.GetComponentsInChildren<Renderer>();var b=rr[0].bounds;foreach(var r in rr.Skip(1))b.Encapsulate(r.bounds);return b;
    }
    static GameObject Prop(string source,string name,Transform group,Vector3 target,float size,Material material=null,float yaw=0,bool solid=false,bool maximum=false)
    {
        var asset=AssetDatabase.LoadAssetAtPath<GameObject>(source);if(asset==null)throw new Exception("Missing model "+source);
        var go=(GameObject)PrefabUtility.InstantiatePrefab(asset,group.gameObject.scene);go.name=name;go.transform.SetParent(group,false);go.transform.localPosition=Vector3.zero;go.transform.localRotation=Quaternion.Euler(0,yaw,0);
        var b=BoundsOf(go);float measure=maximum?Mathf.Max(b.size.x,Mathf.Max(b.size.y,b.size.z)):b.size.y;go.transform.localScale*=size/measure;
        b=BoundsOf(go);var p=group.TransformPoint(target);go.transform.position+=new Vector3(p.x-b.center.x,p.y-b.min.y,p.z-b.center.z);
        if(material!=null)foreach(var renderer in go.GetComponentsInChildren<Renderer>())renderer.sharedMaterials=renderer.sharedMaterials.Select(x=>material).ToArray();
        if(solid){var collider=go.AddComponent<BoxCollider>();b=BoundsOf(go);collider.center=go.transform.InverseTransformPoint(b.center);var v=go.transform.InverseTransformVector(b.size);collider.size=new Vector3(Mathf.Abs(v.x),Mathf.Abs(v.y),Mathf.Abs(v.z));}
        return go;
    }
    static void Flag(Transform group,Vector3 bottom,float height,Material color)
    {
        var go=new GameObject("House_Banner");go.transform.SetParent(group,false);go.transform.localPosition=bottom;go.transform.localScale=new Vector3(1,height,1);
        go.AddComponent<MeshFilter>().sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(Data+"GuardBannerFabric.asset");go.AddComponent<MeshRenderer>().sharedMaterial=color;
        var crest=new GameObject("Silver_Crest");crest.transform.SetParent(go.transform,false);crest.transform.localPosition=new Vector3(0,.63f,-.012f);crest.AddComponent<MeshFilter>().sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(Data+"GuardBannerEmblem.asset");crest.AddComponent<MeshRenderer>().sharedMaterial=Existing("CastleStone_Trim");
    }
    static void Stretch(Transform house,float width,float rise)
    {
        foreach(string name in new[]{"01_Floor","02_Walls","03_Frame","04_Roof","05_Porch","06_Details","07_ContinuousFloorSupport","10_Castle_Masonry"})
        {
            var part=house.Find(name);if(part!=null)part.localScale=new Vector3(part.localScale.x*width,part.localScale.y,part.localScale.z);
        }
        if(rise==0)return;
        float ratio=(2.8f+rise)/2.8f;
        foreach(Transform wall in house.Find("02_Walls"))
        {
            if(!wall.gameObject.activeSelf)continue;
            if(wall.name=="Door_Lintel")wall.localScale=new Vector3(wall.localScale.x,.55f+rise,wall.localScale.z);
            else wall.localScale=new Vector3(wall.localScale.x,wall.localScale.y*ratio,wall.localScale.z);
        }
        foreach(Transform post in house.Find("03_Frame"))post.localScale=new Vector3(post.localScale.x,post.localScale.y+rise,post.localScale.z);
        house.Find("04_Roof").localPosition+=Vector3.up*rise;
        var chimney=house.Find("06_Details/chimney");if(chimney!=null)chimney.localPosition+=Vector3.up*rise;
        var masonry=house.Find("10_Castle_Masonry");if(masonry!=null){masonry.localScale=new Vector3(masonry.localScale.x,masonry.localScale.y*ratio,masonry.localScale.z);masonry.localPosition+=Vector3.up*(.48f*(1-ratio));}
    }
    static void RoofColors(Transform house,Material[] palette)
    {
        var tiles=house.Find("04_Roof/Sealed_Roof_Assembly/Clay_Tiles")??house.Find("04_Roof/Sealed_Roof_Assembly/Slate_Tiles");
        tiles.GetComponent<Renderer>().sharedMaterials=palette;
    }
    static void Fill(Transform house,int variant,float rise)
    {
        foreach(string old in new[]{"08_Interior_Props","09_Castle_Guardroom"}){var t=house.Find(old);if(t!=null)t.gameObject.SetActive(false);}
        var room=new GameObject("11_Unique_Interior");room.transform.SetParent(house,false);var g=room.transform;
        var iron=Existing("CastleIron_Charcoal");var steel=Existing("CastleSteel_Silver");
        var blue=Mat("VariantCloth_Indigo",new Color(.14f,.2f,.34f));var purple=Mat("VariantCloth_RoyalPurple",new Color(.25f,.18f,.38f));var green=Mat("VariantCloth_Emerald",new Color(.12f,.3f,.25f));
        if(variant==0)
        {
            Prop(Cat+"04_Bedroom_01_Beed_1.fbx","Scribe_Bed",g,new Vector3(-1.7f,.48f,-.25f),.65f,null,0,true);
            Prop(Cat+"04_Bedroom_01_Matrace_ForBeed_1.fbx","Scribe_Bedding",g,new Vector3(-1.7f,.84f,-.25f),.27f,blue);
            var desk=Prop(Cat+"01_DiningHall_01_WoodenTable#1.fbx","Writing_Desk",g,new Vector3(1.7f,.48f,.4f),.75f,null,90,true);
            Prop("Assets/Models/Item/stool.prefab","Desk_Stool",g,new Vector3(1.7f,.48f,-1.4f),.45f,null,0,true);
            Prop(Cat+"01_Library_01_BookShelve_1.fbx","Bookcase",g,new Vector3(-1.7f,.48f,2.35f),1.65f,null,0,true);
            float top=BoundsOf(desk).max.y+.005f;
            Prop(Cat+"01_Library_01_BigBook#1.fbx","Desk_Book",g,new Vector3(1.7f,top,.45f),.48f,null,0,false,true);
            Prop(Medieval+"Cup_01.fbx","Desk_Cup",g,new Vector3(2.05f,top,-.15f),.14f);
            Prop(Medieval+"Candle_03.fbx","Desk_Candles",g,new Vector3(1.5f,top,1),.25f);
        }
        else if(variant==1)
        {
            var bench=Prop(Cat+"02_Kitchen_01_WoodenTable#2.fbx","Craft_Workbench",g,new Vector3(-3,.48f,.4f),.85f,null,90,true);
            Prop("Assets/Models/Item/stool.prefab","Craft_Stool",g,new Vector3(-3,.48f,-1.6f),.45f,null,0,true);
            Prop(Cat+"03_Blacksmith_01_AnvilStand.fbx","Anvil_Stand",g,new Vector3(3,.48f,.1f),.7f,null,0,true);
            Prop(Cat+"03_Blacksmith_01_Anvil.fbx","Smith_Anvil",g,new Vector3(3,1.185f,.1f),.9f,steel,0,false,true);
            Prop(Kay+"crates_stacked.fbx","Material_Crates",g,new Vector3(3.2f,.48f,-1.75f),1.05f,null,0,true);
            Prop(Medieval+"Barrel_01.fbx","Workshop_Barrel",g,new Vector3(3.3f,.48f,2.3f),.8f,null,0,true);
            float top=BoundsOf(bench).max.y+.005f;
            var axe=Prop(Medieval+"Axe_01.fbx","Workbench_Axe",g,new Vector3(-3,top,.35f),.55f,null,0,false,true);
            axe.transform.localRotation=Quaternion.Euler(0,0,90);var axeBounds=BoundsOf(axe);var axeTarget=g.TransformPoint(new Vector3(-3,top,.35f));axe.transform.position+=new Vector3(axeTarget.x-axeBounds.center.x,axeTarget.y-axeBounds.min.y,axeTarget.z-axeBounds.center.z);
            Prop(Medieval+"Bucket_01.fbx","Workshop_Bucket",g,new Vector3(-3.3f,.48f,2.2f),.5f,null,0,true);
        }
        else if(variant==2)
        {
            var desk=Prop(Cat+"01_DiningHall_01_WoodenTable#1.fbx","Captain_Desk",g,new Vector3(-1.7f,.48f,.35f),.8f,iron,90,true);
            Prop(Cat+"01_Library_01_Chear_High#1.fbx","Captain_Chair",g,new Vector3(-1.7f,.48f,-1.5f),1.1f,iron,0,true);
            Prop(Cat+"01_Library_01_BookShelve_2.fbx","Archive_Shelf",g,new Vector3(1.7f,.48f,2.25f),1.75f,iron,180,true);
            Prop(Kay+"chest.fbx","Documents_Coffer",g,new Vector3(2,.48f,-1.7f),.7f,steel,0,true);
            float top=BoundsOf(desk).max.y+.005f;
            Prop(Cat+"01_Library_01_BigBook#2.fbx","Orders_Book",g,new Vector3(-1.7f,top,.3f),.55f,purple,0,false,true);
            Prop(Medieval+"Candle_03.fbx","Captain_Candles",g,new Vector3(-1.5f,top,1),.27f,steel);
            Flag(g,new Vector3(0,1.8f,2.65f),1.6f,purple);Flag(g,new Vector3(0,3.43f+rise,-3.18f),.85f,purple);
        }
        else
        {
            foreach(float side in new[]{-1f,1f})
            {
                Prop(Cat+"03_Blacksmith_01_WoodenRack.fbx","Armory_Rack",g,new Vector3(side*2.7f,.48f,1.7f),.9f,iron,0,true);
                Prop(Kay+"sword_shield.fbx","Armory_Weapons",g,new Vector3(side*2.7f,1.2f,1.7f),1.05f,steel);
            }
            Prop(Kay+"chest.fbx","Armory_Coffer",g,new Vector3(-2.9f,.48f,-1.8f),.7f,iron,0,true);
            Prop(Kay+"chest.fbx","Quartermaster_Coffer",g,new Vector3(2.9f,.48f,-.2f),.55f,steel,0,true);
            var table=Prop(Kay+"table_small.fbx","Quartermaster_Desk",g,new Vector3(2.8f,.48f,-1.8f),.75f,iron,0,true);
            Prop(Cat+"01_Library_01_BookSmall_Green.fbx","Inventory_Ledger",g,new Vector3(2.8f,BoundsOf(table).max.y+.005f,-1.8f),.4f,green,0,false,true);
            Prop(Kay+"barrel_small.fbx","Armory_Supplies",g,new Vector3(-2.9f,.48f,-.25f),.7f,iron,0,true);
            Flag(g,new Vector3(0,1.65f,2.65f),1.3f,green);Flag(g,new Vector3(0,3.43f,-3.18f),.85f,green);
        }
        var lightObject=new GameObject("Interior_FillLight");lightObject.transform.SetParent(g,false);lightObject.transform.localPosition=new Vector3(0,2.7f+rise*.4f,.8f);
        var light=lightObject.AddComponent<Light>();light.type=LightType.Point;light.range=variant%2==0?7:8;light.intensity=.7f;light.color=variant<2?new Color(1,.94f,.85f):new Color(.78f,.86f,1);light.shadows=LightShadows.Soft;
    }
    public static void Main()
    {
        var scene=SceneManager.GetActiveScene();if(scene.path!="Assets/Scenes/AssetWorkshop.unity"||EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Open workshop in Edit Mode");
        var area=scene.GetRootGameObjects().First(x=>x.name=="01_Assembly_Area").transform;
        string[] names={"WoodenHall_Tall_02","WoodenWorkshop_Wide_03","StoneCaptainHall_Tall_02","StoneArmory_Wide_03"};
        foreach(var name in names)if(area.Find(name)!=null||File.Exists(Folder+name+".prefab"))throw new Exception("Variant exists: "+name);
        var report=new System.Text.StringBuilder();
        for(int variant=0;variant<4;variant++)
        {
            bool stone=variant>=2,tall=variant%2==0;float width=tall?1:stone?1.4f:1.5f,rise=tall?1.2f:0;
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+(stone?"StoneCottage_01":"WoodenCottage_01")+".prefab");
            var go=(GameObject)PrefabUtility.InstantiatePrefab(source,scene);go.name=names[variant];go.transform.SetParent(area,false);go.transform.localPosition=Vector3.zero;
            PrefabUtility.UnpackPrefabInstance(go,PrefabUnpackMode.OutermostRoot,InteractionMode.AutomatedAction);Undo.RegisterCreatedObjectUndo(go,"Новый вариант дома");
            Stretch(go.transform,width,rise);Fill(go.transform,variant,rise);
            if(!tall)go.transform.Find("06_Details/chimney").gameObject.SetActive(false);
            Material[] colors;
            if(variant==0)colors=new[]{Existing("SlateTiles_Gray"),Existing("SlateTiles_Light"),Existing("SlateTiles_Dark")};
            else if(variant==1)colors=new[]{Mat("RustTiles_Base",new Color(.43f,.22f,.18f)),Mat("RustTiles_Light",new Color(.49f,.27f,.22f)),Mat("RustTiles_Dark",new Color(.34f,.17f,.14f))};
            else if(variant==2)colors=new[]{Mat("CaptainTiles_Navy",new Color(.17f,.24f,.34f)),Mat("CaptainTiles_Light",new Color(.22f,.3f,.4f)),Mat("CaptainTiles_Dark",new Color(.13f,.18f,.27f))};
            else colors=new[]{Mat("ArmoryTiles_Graphite",new Color(.23f,.26f,.28f)),Mat("ArmoryTiles_Light",new Color(.3f,.33f,.35f)),Mat("ArmoryTiles_Dark",new Color(.18f,.2f,.22f))};
            RoofColors(go.transform,colors);
            Physics.SyncTransforms();
            for(float z=-4.5f;z<=2.2f;z+=.1f)if(Physics.CheckCapsule(new Vector3(0,.8f,z),new Vector3(0,2,z),.3f,~0,QueryTriggerInteraction.Ignore))
            {
                // Other saved cottages occupy the origin; inspect only colliders belonging to this new variant.
                if(Physics.OverlapCapsule(new Vector3(0,.8f,z),new Vector3(0,2,z),.3f).Any(c=>c.transform.IsChildOf(go.transform)))throw new Exception("Blocked "+go.name+" z="+z);
            }
            var mats=go.GetComponentsInChildren<Renderer>().SelectMany(r=>r.sharedMaterials).ToArray();if(mats.Any(m=>m==null||m.shader==null||!m.shader.isSupported))throw new Exception("Material issue "+go.name);
            if(stone&&mats.Any(m=>m.color.r>m.color.g*1.15f&&m.color.g>m.color.b*1.15f))throw new Exception("Brown material in stone variant");
            var prefab=PrefabUtility.SaveAsPrefabAssetAndConnect(go,Folder+go.name+".prefab",InteractionMode.AutomatedAction);if(prefab==null)throw new Exception("Save failed");
            go.transform.localPosition=variant==0?new Vector3(0,0,15):variant==1?new Vector3(0,0,30):variant==2?new Vector3(12,0,15):new Vector3(14,0,30);
            report.AppendLine(go.name+": floorWidth="+(6*width)+" ceiling="+(2.8f+rise)+" props="+go.transform.Find("11_Unique_Interior").childCount+" saved");
        }
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        SceneView.lastActiveSceneView?.LookAt(new Vector3(7,2,15),Quaternion.Euler(35,-25,0),28,false,true);
        File.WriteAllText("Temp/unique-house-variants-result.txt",report.ToString());
    }
}
