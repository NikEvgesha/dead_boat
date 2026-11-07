using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class BuildLocationWorkshop126
{
    const string ScenePath="Assets/Scenes/LocationWorkshop.unity";
    const string Folder="Assets/Prefabs/Workshop/Locations";
    static Scene workshop;
    static Dictionary<UnityEngine.Object,UnityEngine.Object> copies=new Dictionary<UnityEngine.Object,UnityEngine.Object>();
    static UnityEngine.Object meshContainer;
    static Material cyan,orange,yellow,red,white;
    static GameObject New(string name,Transform parent=null)
    {
        var go=new GameObject(name); SceneManager.MoveGameObjectToScene(go,workshop);
        if(parent!=null)go.transform.SetParent(parent,false); return go;
    }
    static Material Material(string name,Color color)
    {
        var shader=Shader.Find(UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline!=null?"Universal Render Pipeline/Unlit":"Unlit/Color");
        if(shader==null)shader=Shader.Find("Sprites/Default");
        var m=new Material(shader){name=name,color=color};
        AssetDatabase.CreateAsset(m,Folder+"/"+name+".mat");return m;
    }
    static void StripContext(GameObject root)
    {
        foreach(var c in root.GetComponentsInChildren<MonoBehaviour>(true))if(c!=null)UnityEngine.Object.DestroyImmediate(c);
        foreach(var t in root.GetComponentsInChildren<Transform>(true))GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
        foreach(var c in root.GetComponentsInChildren<AudioSource>(true))UnityEngine.Object.DestroyImmediate(c);
        foreach(var c in root.GetComponentsInChildren<Light>(true))UnityEngine.Object.DestroyImmediate(c);
        foreach(var c in root.GetComponentsInChildren<Camera>(true))UnityEngine.Object.DestroyImmediate(c);
        foreach(var c in root.GetComponentsInChildren<Rigidbody>(true))UnityEngine.Object.DestroyImmediate(c);
    }
    static T Persist<T>(T obj) where T:UnityEngine.Object
    {
        if(obj==null)return null;
        var path=AssetDatabase.GetAssetPath(obj);
        if(!string.IsNullOrEmpty(path)&&!path.EndsWith(".unity"))return obj;
        if(copies.TryGetValue(obj,out var cached))return (T)cached;
        var clone=UnityEngine.Object.Instantiate(obj);clone.name="ForestContext_"+obj.name;clone.hideFlags=HideFlags.None;
        if(meshContainer==null)meshContainer=AssetDatabase.LoadMainAssetAtPath(Folder+"/ForestContext126.asset");
        if(meshContainer==null){AssetDatabase.CreateAsset(clone,Folder+"/ForestContext126.asset");meshContainer=clone;}
        else AssetDatabase.AddObjectToAsset(clone,meshContainer);
        copies[obj]=clone;return clone;
    }
    static void PersistGeometry(GameObject root)
    {
        foreach(var f in root.GetComponentsInChildren<MeshFilter>(true))f.sharedMesh=Persist(f.sharedMesh);
        foreach(var f in root.GetComponentsInChildren<MeshCollider>(true))f.sharedMesh=Persist(f.sharedMesh);
        foreach(var r in root.GetComponentsInChildren<Renderer>(true))r.sharedMaterials=r.sharedMaterials.Select(Persist).ToArray();
    }
    static GameObject CloneContext(GameObject source,string name,Transform parent)
    {
        var clone=UnityEngine.Object.Instantiate(source,parent);clone.name=name;
        if(PrefabUtility.IsPartOfPrefabInstance(clone))PrefabUtility.UnpackPrefabInstance(clone,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
        // ProBuilder clears generated MeshFilter meshes in OnDestroy. Preserve copies first.
        var filters=clone.GetComponentsInChildren<MeshFilter>(true).ToDictionary(f=>f,f=>Persist(f.sharedMesh));
        var colliders=clone.GetComponentsInChildren<MeshCollider>(true).ToDictionary(f=>f,f=>Persist(f.sharedMesh));
        var materials=clone.GetComponentsInChildren<Renderer>(true).ToDictionary(r=>r,r=>r.sharedMaterials.Select(Persist).ToArray());
        StripContext(clone);
        foreach(var pair in filters)if(pair.Key!=null)pair.Key.sharedMesh=pair.Value;
        foreach(var pair in colliders)if(pair.Key!=null)pair.Key.sharedMesh=pair.Value;
        foreach(var pair in materials)if(pair.Key!=null)pair.Key.sharedMaterials=pair.Value;
        clone.SetActive(true);return clone;
    }
    static void Line(string name,Vector3[] points,Material m,Transform parent,bool loop=false)
    {
        var go=New(name,parent);var lr=go.AddComponent<LineRenderer>();lr.useWorldSpace=true;lr.loop=loop;lr.positionCount=points.Length;lr.SetPositions(points);lr.startWidth=lr.endWidth=.12f;lr.sharedMaterial=m;
        lr.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;lr.receiveShadows=false;
    }
    static void Rect(string name,Vector3 min,Vector3 max,float y,Material m,Transform parent)
    {
        Line(name,new[]{new Vector3(min.x,y,min.z),new Vector3(max.x,y,min.z),new Vector3(max.x,y,max.z),new Vector3(min.x,y,max.z)},m,parent,true);
    }
    static void Label(string name,string text,Vector3 pos,float size,Transform parent)
    {
        var go=New(name,parent);go.transform.position=pos;
        go.transform.rotation=Quaternion.Euler(65,0,0);
        var tm=go.AddComponent<TextMesh>();tm.text=text;tm.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");tm.fontSize=48;tm.characterSize=size;tm.anchor=TextAnchor.MiddleCenter;tm.alignment=TextAlignment.Center;tm.color=Color.white;
        go.GetComponent<MeshRenderer>().sharedMaterial=tm.font.material;
    }
    static Bounds BoundsOf(GameObject root)
    {
        var rs=root.GetComponentsInChildren<Renderer>().Where(r=>r.enabled).ToArray();if(rs.Length==0)throw new Exception("No geometry "+root.name);
        var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);return b;
    }
    static Transform SourcePoint(GameObject asset,long id)
    {
        foreach(var t in asset.GetComponentsInChildren<Transform>(true))
        {
            if(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(t,out string guid,out long local)&&local==id)return t;
            var original=PrefabUtility.GetCorrespondingObjectFromOriginalSource(t);
            if(original!=null&&AssetDatabase.TryGetGUIDAndLocalFileIdentifier(original,out guid,out local)&&local==id)return t;
        }
        throw new Exception("Point reference not found: "+asset.name+" "+id);
    }
    static void Annotations(GameObject instance,GameObject asset,int side,Transform parent)
    {
        var b=BoundsOf(instance);
        Rect("Geometry_XZ",b.min,b.max,.95f,cyan,parent);
        var a=instance.transform.Find("Point1");var z=instance.transform.Find("Point2");
        if(a==null||z==null)throw new Exception("Cleaner corners missing");
        Rect("Cleaner_XZ",Vector3.Min(a.position,z.position),Vector3.Max(a.position,z.position),.8f,orange,parent);
        Label("Dimensions",string.Format(System.Globalization.CultureInfo.InvariantCulture,"HOUSE 1 / {0}\n{1:F1} x {2:F1} m",side==1?"LEFT":"RIGHT",b.size.x,b.size.z),new Vector3(b.center.x,3,b.max.z+9),.24f,parent);
        foreach(var line in File.ReadAllLines("tools/assets/house1-workshop-points.tsv"))
        {
            var parts=line.Split('\t');if(parts.Length!=4||parts[0]!=side.ToString())continue;
            var point=SourcePoint(asset,long.Parse(parts[3]));
            string path=AnimationUtility.CalculateTransformPath(point,asset.transform);
            var target=instance.transform.Find(path);if(target==null)throw new Exception("Missing instance point "+path);
            var go=GameObject.CreatePrimitive(PrimitiveType.Sphere);go.name=parts[1]+"_"+parts[2]+"__"+target.name;
            SceneManager.MoveGameObjectToScene(go,workshop);go.transform.SetParent(parent,false);go.transform.position=target.position+Vector3.up*.5f;go.transform.localScale=Vector3.one*.65f;
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());go.GetComponent<Renderer>().sharedMaterial=parts[1]=="Item"?yellow:red;
        }
    }
    public static string Main()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new Exception("Stable Edit Mode required");
        if(File.Exists(ScenePath)||AssetDatabase.IsValidFolder(Folder))throw new Exception("Workshop already exists; refusing to overwrite");
        AssetDatabase.CreateFolder("Assets/Prefabs/Workshop","Locations");
        for(int side=1;side<=2;side++)if(!AssetDatabase.CopyAsset("Assets/Prefabs/Locations/Enemy/house1_"+side+".prefab",Folder+"/House1_"+(side==1?"Left":"Right")+"_Workshop.prefab"))throw new Exception("Prefab copy failed");
        var oldScenes=Enumerable.Range(0,SceneManager.sceneCount).Select(SceneManager.GetSceneAt).Where(s=>s.isLoaded).ToArray();
        workshop=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(workshop);
        EditorSceneManager.SetSceneCullingMask(workshop,1UL<<60);
        var root=New("Location_Workshop__House1_Forest");
        var context=New("01_Forest_Route_Context__500m",root.transform);
        var houses=New("02_Working_Prefabs__Edit_These",root.transform);
        var helpers=New("03_Guides__Toggle_For_Clean_View",root.transform);
        var refs=New("04_Scale_References",root.transform);
        var cameras=New("05_Cameras_And_Light",root.transform);
        var source=EditorSceneManager.OpenPreviewScene("Assets/Scenes/LevelForest.unity");
        try
        {
            var ts=source.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).ToArray();
            var land=ts.Single(t=>t.name=="Land"&&t.parent!=null&&t.parent.name=="LendController");
            var ground=CloneContext(land.gameObject,"Forest_Land_From_LevelForest__Original_Overrides",context.transform);
            ground.transform.position=land.position;ground.transform.rotation=land.rotation;
            var board=ts.Single(t=>t.name=="BoardController");
            var boat=CloneContext(board.gameObject,"Boat_Visual_Reference__No_Gameplay",refs.transform);
            boat.transform.position=new Vector3(0,board.position.y,-28);
        }
        finally{EditorSceneManager.ClosePreviewScene(source);}
        cyan=Material("Guide_Geometry_Cyan126",new Color(.1f,.9f,1));orange=Material("Guide_Clearance_Orange126",new Color(1,.45f,.1f));yellow=Material("Guide_Item_Yellow126",Color.yellow);red=Material("Guide_Enemy_Red126",new Color(1,.1f,.2f));white=Material("Guide_Scale_White126",Color.white);
        for(int side=1;side<=2;side++)
        {
            var path=Folder+"/House1_"+(side==1?"Left":"Right")+"_Workshop.prefab";
            var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var house=(GameObject)PrefabUtility.InstantiatePrefab(asset,workshop);house.transform.SetParent(houses.transform,false);house.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
            Annotations(house,asset,side,New(side==1?"Left_Guides":"Right_Guides",helpers.transform).transform);
        }
        Line("River_Axis_X0__Forward_Z",new[]{new Vector3(0,.5f,-95),new Vector3(0,.5f,95)},cyan,helpers.transform);
        Rect("Working_Area_180x120",new Vector3(-80,0,-60),new Vector3(100,0,60),.6f,white,helpers.transform);
        foreach(float x in new[]{-40f,55f})
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Capsule);SceneManager.MoveGameObjectToScene(go,workshop);go.name="Player_Scale_1.8m";go.transform.SetParent(refs.transform,false);go.transform.position=new Vector3(x,1.1f,-29);go.transform.localScale=new Vector3(.6f,.9f,.6f);go.GetComponent<Renderer>().sharedMaterial=white;UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
        }
        Label("Legend","CYAN: GEOMETRY   ORANGE: CLEARANCE\nYELLOW: ITEMS   RED: ENEMIES\nVISUAL EDIT MODE / NOT A PLAYABLE LEVEL",new Vector3(10,2,-48),.21f,helpers.transform);
        var camGo=New("Overview_Camera",cameras.transform);var camera=camGo.AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=58;camera.nearClipPlane=.1f;camera.farClipPlane=1500;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.22f,.3f,.36f);camera.scene=workshop;camGo.transform.position=new Vector3(10,115,-105);camGo.transform.LookAt(new Vector3(10,0,0));
        var boatCam=New("Boat_View_Camera__Enable_To_Preview",cameras.transform).AddComponent<Camera>();boatCam.enabled=false;boatCam.fieldOfView=65;boatCam.farClipPlane=1000;boatCam.scene=workshop;boatCam.transform.position=new Vector3(0,5,-28);boatCam.transform.LookAt(new Vector3(-45,3,-10));
        var light=New("Workshop_Sun",cameras.transform).AddComponent<Light>();light.type=LightType.Directional;light.intensity=.85f;light.transform.rotation=Quaternion.Euler(50,-30,0);light.shadows=LightShadows.Soft;
        foreach(var c in root.GetComponentsInChildren<Camera>(true))c.overrideSceneCullingMask=1UL<<60;
        RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.35f,.38f,.42f);RenderSettings.fog=false;
        AssetDatabase.SaveAssets();
        if(!EditorSceneManager.SaveScene(workshop,ScenePath))throw new Exception("Scene save failed");
        foreach(var previous in oldScenes)foreach(var go in previous.GetRootGameObjects())SceneVisibilityManager.instance.Hide(go,true);
        Selection.activeGameObject=houses.transform.GetChild(0).gameObject;
        var view=SceneView.lastActiveSceneView;if(view!=null){view.LookAt(new Vector3(10,0,0),Quaternion.Euler(55,0,0),90);view.Repaint();}
        return Verify();
    }
    public static string Verify()
    {
        workshop=SceneManager.GetSceneByPath(ScenePath);if(!workshop.isLoaded)throw new Exception("Workshop not loaded");
        var root=workshop.GetRootGameObjects().Single(r=>r.name=="Location_Workshop__House1_Forest");
        var houses=root.transform.Find("02_Working_Prefabs__Edit_These");if(houses.childCount!=2)throw new Exception("Expected two houses");
        var log=new System.Text.StringBuilder();
        foreach(Transform house in houses)
        {
            var path=PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(house.gameObject);
            if(!path.StartsWith(Folder+"/House1_"))throw new Exception("Working prefab link missing");
            if(house.position!=Vector3.zero||house.rotation!=Quaternion.identity||house.lossyScale!=Vector3.one)throw new Exception("Root anchor changed");
            var bounds=BoundsOf(house.gameObject);log.AppendLine(path+" XZ="+bounds.size.x.ToString("F1")+" x "+bounds.size.z.ToString("F1"));
        }
        var guides=root.transform.Find("03_Guides__Toggle_For_Clean_View");
        int points=guides.GetComponentsInChildren<Transform>(true).Count(t=>t.name.StartsWith("Item_")||t.name.StartsWith("Enemy_"));if(points!=18)throw new Exception("Expected 18 configured point markers, got "+points);
        var context=root.transform.Find("01_Forest_Route_Context__500m");
        // Editable house ProBuilder components regenerate their own transient meshes on enable.
        // Only the stripped context snapshot must have independent persistent mesh assets.
        foreach(var f in context.GetComponentsInChildren<MeshFilter>(true))if(f.sharedMesh==null||!EditorUtility.IsPersistent(f.sharedMesh))throw new Exception("Missing/transient context mesh "+f.name);
        if(context.GetComponentsInChildren<MonoBehaviour>(true).Any(c=>c!=null))throw new Exception("Context behavior remains");
        log.AppendLine("PASS: two linked working prefabs; anchors unchanged; 8 item + 10 enemy markers; context meshes persistent; scene saved="+!workshop.isDirty);
        Directory.CreateDirectory("Docs/locations");File.WriteAllText("Docs/locations/workshop-verification-126.txt",log.ToString());
        return log.ToString();
    }
    public static string RepairContext()
    {
        workshop=SceneManager.GetSceneByPath(ScenePath);SceneManager.SetActiveScene(workshop);
        var root=workshop.GetRootGameObjects().Single(r=>r.name=="Location_Workshop__House1_Forest");
        var context=root.transform.Find("01_Forest_Route_Context__500m");
        var refs=root.transform.Find("04_Scale_References");
        var source=EditorSceneManager.OpenPreviewScene("Assets/Scenes/LevelForest.unity");
        try
        {
            var ts=source.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).ToArray();
            var land=ts.Single(t=>t.name=="Land"&&t.parent!=null&&t.parent.name=="LendController");
            var ground=CloneContext(land.gameObject,"Forest_Land_From_LevelForest__Original_Overrides",context);
            ground.transform.SetPositionAndRotation(land.position,land.rotation);
            for(int i=context.childCount-1;i>=0;i--)if(context.GetChild(i)!=ground.transform)UnityEngine.Object.DestroyImmediate(context.GetChild(i).gameObject);
            var board=ts.Single(t=>t.name=="BoardController");
            var boat=CloneContext(board.gameObject,"Boat_Visual_Reference__No_Gameplay",refs);
            boat.transform.position=new Vector3(0,board.position.y,-28);
            foreach(var t in refs.Cast<Transform>().ToArray())if(t!=boat.transform&&t.name==boat.name)UnityEngine.Object.DestroyImmediate(t.gameObject);
        }
        finally{EditorSceneManager.ClosePreviewScene(source);}
        AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(workshop);
        return Verify();
    }
    public static string RefreshGuides()
    {
        workshop=SceneManager.GetSceneByPath(ScenePath);
        var root=workshop.GetRootGameObjects().Single(r=>r.name=="Location_Workshop__House1_Forest");
        var helpers=root.transform.Find("03_Guides__Toggle_For_Clean_View");
        cyan=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Guide_Geometry_Cyan126.mat");orange=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Guide_Clearance_Orange126.mat");yellow=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Guide_Item_Yellow126.mat");red=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Guide_Enemy_Red126.mat");
        for(int side=1;side<=2;side++)
        {
            string name=side==1?"Left_Guides":"Right_Guides";var old=helpers.Find(name);if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
            string path=Folder+"/House1_"+(side==1?"Left":"Right")+"_Workshop.prefab";
            var house=root.transform.Find("02_Working_Prefabs__Edit_These").Cast<Transform>().Single(t=>PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject)==path);
            Annotations(house.gameObject,AssetDatabase.LoadAssetAtPath<GameObject>(path),side,New(name,helpers).transform);
        }
        EditorSceneManager.SaveScene(workshop);return Verify();
    }
    public static string Polish()
    {
        workshop=SceneManager.GetSceneByPath(ScenePath);SceneManager.SetActiveScene(workshop);
        var root=workshop.GetRootGameObjects().Single(r=>r.name=="Location_Workshop__House1_Forest");
        foreach(var path in new[]{"01_Forest_Route_Context__500m","04_Scale_References"})foreach(var l in root.transform.Find(path).GetComponentsInChildren<Light>(true))UnityEngine.Object.DestroyImmediate(l);
        root.transform.Find("05_Cameras_And_Light/Workshop_Sun").GetComponent<Light>().intensity=.85f;
        RenderSettings.ambientLight=new Color(.35f,.38f,.42f);
        EditorSceneManager.SaveScene(workshop);return Verify();
    }
    public static string Capture()
    {
        workshop=SceneManager.GetSceneByPath(ScenePath);var root=workshop.GetRootGameObjects().Single(r=>r.name=="Location_Workshop__House1_Forest");
        var camera=root.GetComponentsInChildren<Camera>(true).Single(c=>c.name=="Overview_Camera");
        var rt=RenderTexture.GetTemporary(1600,1000,24);var old=camera.targetTexture;var active=RenderTexture.active;
        Texture2D texture=null;
        try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;texture=new Texture2D(1600,1000,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,1600,1000),0,0);texture.Apply();File.WriteAllBytes("Docs/locations/previews/house1-workshop.png",texture.EncodeToPNG());}
        finally{camera.targetTexture=old;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);if(texture!=null)UnityEngine.Object.DestroyImmediate(texture);}
        return "Saved Docs/locations/previews/house1-workshop.png";
    }
    public static string CheckSaved()
    {
        var scene=EditorSceneManager.OpenPreviewScene(ScenePath);
        try
        {
            var root=scene.GetRootGameObjects().Single(r=>r.name=="Location_Workshop__House1_Forest");
            foreach(string path in new[]{"01_Forest_Route_Context__500m","04_Scale_References"})
                foreach(var f in root.transform.Find(path).GetComponentsInChildren<MeshFilter>(true))
                    if(f.sharedMesh==null)throw new Exception("Saved context lost mesh: "+f.name);
            var houses=root.transform.Find("02_Working_Prefabs__Edit_These");
            if(houses.childCount!=2||houses.Cast<Transform>().Any(t=>!PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject).StartsWith(Folder+"/House1_")))throw new Exception("Saved prefab links missing");
            const string result="PASS: scene reopened from disk in preview; land/water/boat meshes and both working prefab links preserved.";
            File.AppendAllText("Docs/locations/workshop-verification-126.txt",result+Environment.NewLine);
            return result;
        }
        finally{EditorSceneManager.ClosePreviewScene(scene);}
    }
}
