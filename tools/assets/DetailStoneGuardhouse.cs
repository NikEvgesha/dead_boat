using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class DetailStoneGuardhouse
{
    const string Data="Assets/Prefabs/Workshop/CottageGeometry/";
    const string Cat="Assets/Import/CatBorg Studio/3D Dungeon Lowpoly Pack/Models/";
    const string Kay="Assets/Import/KayKit_DungeonRemastered_1.1_FREE/Assets/fbx(unity)/";
    static Material Mat(string name,Color color,float metallic=0)
    {
        var m=AssetDatabase.LoadAssetAtPath<Material>(Data+name+".mat");if(m!=null)return m;
        m=new Material(Shader.Find("Standard")){name=name,color=color};m.SetFloat("_Metallic",metallic);m.SetFloat("_Glossiness",.18f);
        AssetDatabase.CreateAsset(m,Data+name+".mat");return m;
    }
    class Masonry
    {
        readonly List<Vector3> vertices=new List<Vector3>();readonly List<int>[] indices={new List<int>(),new List<int>(),new List<int>()};
        public int blocks;
        void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,Vector3 outward,int mat)
        {
            if(Vector3.Dot(Vector3.Cross(b-a,c-a),outward)<0){var swap=b;b=d;d=swap;}
            int n=vertices.Count;vertices.AddRange(new[]{a,b,c,d});indices[mat].AddRange(new[]{n,n+1,n+2,n,n+2,n+3});
        }
        public void Box(Vector3 center,Vector3 size,int mat)
        {
            var lo=center-size*.5f;var hi=center+size*.5f;var a=new Vector3(lo.x,lo.y,lo.z);var b=new Vector3(hi.x,lo.y,lo.z);var c=new Vector3(hi.x,hi.y,lo.z);var d=new Vector3(lo.x,hi.y,lo.z);
            var e=new Vector3(lo.x,lo.y,hi.z);var f=new Vector3(hi.x,lo.y,hi.z);var g=new Vector3(hi.x,hi.y,hi.z);var h=new Vector3(lo.x,hi.y,hi.z);
            Quad(a,d,c,b,Vector3.back,mat);Quad(e,f,g,h,Vector3.forward,mat);Quad(a,e,h,d,Vector3.left,mat);Quad(b,c,g,f,Vector3.right,mat);Quad(d,h,g,c,Vector3.up,mat);Quad(a,b,f,e,Vector3.down,mat);blocks++;
        }
        public Mesh Save()
        {
            var mesh=new Mesh{name="GuardhouseMasonry"};mesh.SetVertices(vertices);mesh.subMeshCount=3;for(int i=0;i<3;i++)mesh.SetTriangles(indices[i],i);mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,Data+"GuardhouseMasonry.asset");return mesh;
        }
    }
    static bool Overlap(float a,float b,float c,float d){return a<d&&b>c;}
    static bool Opening(int side,float lo,float hi,float bottom,float top)
    {
        if(side==0&&Overlap(lo,hi,-.62f,.62f)&&bottom<2.76f)return true;
        if(side==1)return false;
        var centers=side==0?new[]{-2f,2f}:new[]{0f};
        return centers.Any(x=>Overlap(lo,hi,x-.46f,x+.46f)&&Overlap(bottom,top,1.0f,2.76f));
    }
    static Bounds BoundsOf(GameObject go)
    {
        var rr=go.GetComponentsInChildren<Renderer>();var b=rr[0].bounds;foreach(var r in rr.Skip(1))b.Encapsulate(r.bounds);return b;
    }
    static GameObject Prop(string path,string name,Transform parent,Vector3 position,float height,Material mat,float yaw=0,bool solid=false)
    {
        var a=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(a==null)throw new Exception("Missing "+path);
        var go=(GameObject)PrefabUtility.InstantiatePrefab(a,parent.gameObject.scene);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=Vector3.zero;go.transform.localRotation=Quaternion.Euler(0,yaw,0);
        var b=BoundsOf(go);go.transform.localScale*=height/b.size.y;b=BoundsOf(go);var target=parent.TransformPoint(position);go.transform.position+=new Vector3(target.x-b.center.x,target.y-b.min.y,target.z-b.center.z);
        foreach(var renderer in go.GetComponentsInChildren<Renderer>())renderer.sharedMaterials=renderer.sharedMaterials.Select(x=>mat).ToArray();
        if(solid){var collider=go.AddComponent<BoxCollider>();b=BoundsOf(go);collider.center=go.transform.InverseTransformPoint(b.center);var size=go.transform.InverseTransformVector(b.size);collider.size=new Vector3(Mathf.Abs(size.x),Mathf.Abs(size.y),Mathf.Abs(size.z));}
        return go;
    }
    static void FinishAccessories(Transform decor,Material blue,Material pale)
    {
        var shield=decor.Find("Wall_Shield");var bounds=BoundsOf(shield.gameObject);
        shield.localScale*=.7f/Mathf.Max(bounds.size.x,bounds.size.z);shield.localRotation=Quaternion.Euler(90,0,0);
        bounds=BoundsOf(shield.gameObject);var target=decor.TransformPoint(new Vector3(.85f,1.1f,2.61f));shield.position+=new Vector3(target.x-bounds.center.x,target.y-bounds.min.y,target.z-bounds.center.z);
        var fabric=AssetDatabase.LoadAssetAtPath<Mesh>(Data+"GuardBannerFabric.asset");
        if(fabric==null)
        {
            var v=new[]{new Vector3(-.32f,1,0),new Vector3(.32f,1,0),new Vector3(.32f,.2f,0),Vector3.zero,new Vector3(-.32f,.2f,0)};
            fabric=new Mesh{name="GuardBannerFabric",vertices=v.Concat(v).ToArray(),triangles=new[]{0,1,2,0,2,3,0,3,4,5,7,6,5,8,7,5,9,8}};fabric.RecalculateNormals();fabric.RecalculateBounds();AssetDatabase.CreateAsset(fabric,Data+"GuardBannerFabric.asset");
        }
        var emblem=AssetDatabase.LoadAssetAtPath<Mesh>(Data+"GuardBannerEmblem.asset");
        if(emblem==null)
        {
            var v=new[]{new Vector3(-.15f,0,0),new Vector3(0,.2f,0),new Vector3(.15f,0,0),new Vector3(0,-.2f,0)};
            emblem=new Mesh{name="GuardBannerEmblem",vertices=v.Concat(v).ToArray(),triangles=new[]{0,1,2,0,2,3,4,6,5,4,7,6}};emblem.RecalculateNormals();emblem.RecalculateBounds();AssetDatabase.CreateAsset(emblem,Data+"GuardBannerEmblem.asset");
        }
        for(int i=0;i<2;i++)
        {
            var flag=new GameObject(i==0?"Interior_FlagCloth":"Entrance_FlagCloth");flag.transform.SetParent(decor,false);flag.transform.localPosition=i==0?new Vector3(-.2f,1.65f,2.65f):new Vector3(0,3.43f,-3.18f);flag.transform.localScale=i==0?new Vector3(1,1.25f,1):new Vector3(1,.75f,1);
            flag.AddComponent<MeshFilter>().sharedMesh=fabric;flag.AddComponent<MeshRenderer>().sharedMaterial=blue;
            var crest=new GameObject("Silver_Crest");crest.transform.SetParent(flag.transform,false);crest.transform.localPosition=new Vector3(0,.63f,-.01f);crest.AddComponent<MeshFilter>().sharedMesh=emblem;crest.AddComponent<MeshRenderer>().sharedMaterial=pale;
        }
    }
    public static void FinishCurrentAccessories()
    {
        var s=SceneManager.GetActiveScene();var house=s.GetRootGameObjects().First(x=>x.name=="01_Assembly_Area").transform.Find("StoneCottage_01");
        FinishAccessories(house.Find("09_Castle_Guardroom"),AssetDatabase.LoadAssetAtPath<Material>(Data+"CastleCloth_Blue.mat"),AssetDatabase.LoadAssetAtPath<Material>(Data+"CastleStone_Trim.mat"));
        PrefabUtility.ApplyPrefabInstance(house.gameObject,InteractionMode.AutomatedAction);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(s);
    }
    public static void Main()
    {
        var scene=SceneManager.GetActiveScene();if(scene.path!="Assets/Scenes/AssetWorkshop.unity"||EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Open workshop in Edit Mode");
        var house=scene.GetRootGameObjects().First(x=>x.name=="01_Assembly_Area").transform.Find("StoneCottage_01");if(house.Find("09_Castle_Guardroom")!=null)throw new Exception("Already detailed, preserve edits");
        Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Крепостная караульная");
        var pale=Mat("CastleStone_Trim",new Color(.69f,.71f,.72f));var medium=Mat("CastleStone_Block",new Color(.53f,.56f,.58f));var darker=Mat("CastleStone_BlockDark",new Color(.45f,.48f,.5f));
        var iron=Mat("CastleIron_Charcoal",new Color(.18f,.22f,.25f),.35f);var steel=Mat("CastleSteel_Silver",new Color(.58f,.64f,.69f),.55f);var blue=Mat("CastleCloth_Blue",new Color(.12f,.23f,.39f));
        // Remove inherited warm timber appearances through local renderer overrides only.
        foreach(var renderer in house.GetComponentsInChildren<Renderer>())
        {
            Undo.RecordObject(renderer,"Убрать коричневую отделку");
            renderer.sharedMaterials=renderer.sharedMaterials.Select(m=>m.name.StartsWith("wood")?pale:m).ToArray();
        }
        var original=house.Find("08_Interior_Props");Undo.RecordObject(original.gameObject,"Заменить обстановку");original.gameObject.SetActive(false);
        var lantern=house.Find("06_Details/lantern");Undo.RecordObject(lantern.gameObject,"Убрать садовый фонарь");lantern.gameObject.SetActive(false);
        foreach(var r in house.Find("06_Details/chimney").GetComponentsInChildren<Renderer>()){Undo.RecordObject(r,"Каменный дымоход");r.sharedMaterials=r.sharedMaterials.Select(x=>pale).ToArray();}
        var decor=new GameObject("09_Castle_Guardroom");decor.transform.SetParent(house,false);Undo.RegisterCreatedObjectUndo(decor,"Обстановка караульной");
        var detail=new GameObject("10_Castle_Masonry");detail.transform.SetParent(house,false);Undo.RegisterCreatedObjectUndo(detail,"Каменная кладка");
        var masonry=new Masonry();
        for(int side=0;side<4;side++)for(int row=0;row<7;row++)for(int col=-1;col<13;col++)
        {
            float lo=Mathf.Max(-3,-3+col*.5f+(row%2)*.25f),hi=Mathf.Min(3,-3+(col+1)*.5f+(row%2)*.25f);float bottom=.48f+row*.4f,top=bottom+.4f;
            if(hi-lo<.05f||Opening(side,lo,hi,bottom,top))continue;
            float center=(lo+hi)*.5f;var p=side<2?new Vector3(center,(bottom+top)*.5f,side==0?-3.025f:3.025f):new Vector3(side==2?-3.025f:3.025f,(bottom+top)*.5f,center);
            var size=side<2?new Vector3(hi-lo-.018f,.382f,.075f):new Vector3(.075f,.382f,hi-lo-.018f);
            masonry.Box(p,size,(row+col+side+30)%3);
        }
        // Staggered corner quoins and a continuous stone cornice make the house read as fortification masonry.
        foreach(float x in new[]{-2.9f,2.9f})foreach(float z in new[]{-2.9f,2.9f})for(int row=0;row<7;row++)
            masonry.Box(new Vector3(x,.68f+row*.4f,z),row%2==0?new Vector3(.58f,.37f,.4f):new Vector3(.4f,.37f,.58f),0);
        foreach(float side in new[]{-1f,1f})
        {
            masonry.Box(new Vector3(0,3.2f,side*3.04f),new Vector3(6.15f,.16f,.2f),0);
            masonry.Box(new Vector3(side*3.04f,3.2f,0),new Vector3(.2f,.16f,6.15f),0);
        }
        foreach(float x in new[]{-2f,2f}){masonry.Box(new Vector3(x,1.0f,-3.08f),new Vector3(1.12f,.13f,.24f),0);masonry.Box(new Vector3(x,2.82f,-3.06f),new Vector3(1.14f,.17f,.18f),0);}
        foreach(float x in new[]{-3.08f,3.08f}){masonry.Box(new Vector3(x,1.0f,0),new Vector3(.24f,.13f,1.12f),0);masonry.Box(new Vector3(x,2.82f,0),new Vector3(.18f,.17f,1.14f),0);}
        masonry.Box(new Vector3(0,2.88f,-3.075f),new Vector3(1.52f,.27f,.2f),0);masonry.Box(new Vector3(0,3.08f,-3.10f),new Vector3(.32f,.32f,.22f),1);
        detail.AddComponent<MeshFilter>().sharedMesh=masonry.Save();detail.AddComponent<MeshRenderer>().sharedMaterials=new[]{pale,medium,darker};
        Prop(Cat+"04_Bedroom_01_Beed_1.fbx","Guard_Bed",decor.transform,new Vector3(-1.8f,.48f,.7f),.73f,iron,0,true);
        Prop(Cat+"04_Bedroom_01_Matrace_ForBeed_1.fbx","Blue_Bedding",decor.transform,new Vector3(-1.8f,.87f,.7f),.32f,blue);
        Prop(Cat+"03_Blacksmith_01_WoodenRack.fbx","Equipment_Rack",decor.transform,new Vector3(1.65f,.48f,1.95f),.9f,iron,0,true);
        Prop(Kay+"sword_shield.fbx","Rack_Weapons",decor.transform,new Vector3(1.65f,1.2f,1.95f),1.1f,steel);
        Prop(Kay+"chest.fbx","Guard_Chest",decor.transform,new Vector3(2,.48f,-1.55f),.65f,iron,0,true);
        Prop(Kay+"chest.fbx","Supply_Chest",decor.transform,new Vector3(2,.48f,-.35f),.5f,steel,0,true);
        Prop(Cat+"99_Decorations_WallBanner_0.fbx","Interior_Banner",decor.transform,new Vector3(-.2f,1.65f,2.67f),1.3f,blue);
        Prop("Assets/Import/3D Cartoon Village/Models/Shield.fbx","Wall_Shield",decor.transform,new Vector3(.85f,1.1f,2.66f),.7f,steel);
        Prop(Cat+"99_Decorations_WallBanner_0.fbx","Entrance_Banner",decor.transform,new Vector3(0,3.4f,-3.15f),.85f,blue);
        foreach(float x in new[]{-.98f,.98f})Prop(Cat+"97_Othert_Torch_1.fbx","Entrance_Sconce",decor.transform,new Vector3(x,1.4f,-3.16f),.72f,iron);
        var lightObject=new GameObject("Guardroom_SoftLight");lightObject.transform.SetParent(decor.transform,false);lightObject.transform.localPosition=new Vector3(.8f,2.8f,1);
        var light=lightObject.AddComponent<Light>();light.type=LightType.Point;light.color=new Color(.78f,.86f,1);light.intensity=.7f;light.range=5;light.shadows=LightShadows.Soft;
        FinishAccessories(decor.transform,blue,pale);
        Physics.SyncTransforms();for(float z=-4.5f;z<=2.2f;z+=.1f)
            if(Physics.CheckCapsule(house.position+new Vector3(0,.8f,z),house.position+new Vector3(0,2,z),.3f,~0,QueryTriggerInteraction.Ignore))throw new Exception("Blocked guardroom passage at "+z);
        var mats=house.GetComponentsInChildren<Renderer>().SelectMany(r=>r.sharedMaterials).Distinct().ToArray();
        if(mats.Any(m=>m.color.r>m.color.g*1.15f&&m.color.g>m.color.b*1.15f))throw new Exception("Warm brown material remains");
        if(mats.Any(m=>m==null||m.shader==null||!m.shader.isSupported))throw new Exception("Material issue");
        PrefabUtility.ApplyPrefabInstance(house.gameObject,InteractionMode.AutomatedAction);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Undo.CollapseUndoOperations(group);
        Selection.activeGameObject=house.gameObject;SceneView.lastActiveSceneView?.LookAt(house.position+new Vector3(0,2,-.5f),Quaternion.Euler(25,-35,0),7,false,true);
        File.WriteAllText("Temp/guardhouse-result.txt","PASS masonry blocks="+masonry.blocks+"; new props=11; old furnishings disabled; brown materials=0; passage clear; scene/prefab saved");
    }
}
