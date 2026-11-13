using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using DeadBoat.Online;
using DeadBoat.Online.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class WinterSled185Probe
{
    const string Flag = "WinterSled185Probe";
    const string Report = "Docs/locations/previews/event-winter200-play.txt";
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static WinterSled185Probe() => EditorApplication.playModeStateChanged += StateChanged;

    public static void Main()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/LoadingScene.unity");
        SharedPerformanceTestSave.Prepare();
        File.WriteAllText(Report, "");
        SessionState.SetBool(Flag, true);
        EditorApplication.EnterPlaymode();
    }

    static async void StateChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Flag, false)) return;
        EditorApplication.CallbackFunction clock = () => Time.timeScale = 1f;
        EditorApplication.update += clock;
        try
        {
            await Until(() => LoadingManager.Instance && LoadingManager.Instance.CurrentLocation == Location.Lobby, "Lobby");
            Check(SaveManager.Instance.PersistentProvider is SharedRunPreviewSave, "Isolated save");
            LoadingManager.Instance.LoadLocation(Location.Game, "LevelEvent", false);
            await Until(() => SceneManager.GetActiveScene().name == "LevelEvent" && LoadingManager.Instance.CurrentLocation == Location.Game &&
                BoardController.Instance && BoardController.Instance.StartGame, "LevelEvent started through the normal loader");
            await Task.Yield();
            await Verify();
            Note("PASS");
        }
        catch (Exception ex) { Note("FAIL " + ex); }
        finally
        {
            EditorApplication.update -= clock;
            SessionState.EraseBool(Flag);
            EditorApplication.ExitPlaymode();
        }
    }

    static async Task Until(Func<bool> condition, string label)
    {
        var deadline = DateTime.UtcNow.AddSeconds(175);
        while (!condition())
        {
            if (DateTime.UtcNow > deadline) throw new TimeoutException(label);
            await Task.Delay(35);
        }
        Note(label);
    }

    static async Task Verify()
    {
        const string design="Bank_05_Gift_Sled";
        var city=Object.FindAnyObjectByType<CitySpawner>();
        var spawner=Object.FindAnyObjectByType<DecorationSpawner>();
        var entry=spawner.decorationCollection.spawnItems.Single(i=>i.prefab.name==design);
        var shape=entry.prefab.transform.Find("Painted_Gift_Sleigh").GetComponent<MeshFilter>();
        Check(shape.sharedMesh && shape.sharedMesh.subMeshCount==8 && entry.chance==9f,
            "New sleigh is in the live pool at its original weight");
        Check(entry.prefab.GetComponent<BoxCollider>().isTrigger && entry.prefab.CompareTag("Decoration"),
            "Sleigh retains the decorative cleanup trigger");

        var expectedPaths=new[]{"Ruby","Gold","Route179/Log_Exposed_Wood","Route179/Log_Bark_Ridges",
            "Fir","WinterTown170/Gift_Blue","WinterTown170/Gift_Violet","Snow"}
            .Select(path=>"Assets/Events/NewYear/"+path+".mat").ToArray();
        var materials=shape.GetComponent<MeshRenderer>().sharedMaterials;
        Check(AssetDatabase.GetAssetPath(shape.sharedMesh)=="Assets/Events/NewYear/Route185/Painted_Gift_Sleigh.asset" &&
            materials.Select(AssetDatabase.GetAssetPath).SequenceEqual(expectedPaths) &&
            materials.All(m=>m.shader.isSupported) &&
            Enumerable.Range(0,materials.Length).All(i=>shape.sharedMesh.GetIndexCount(i)>0),
            "Original mesh, eight material slots in order, supported shaders");
        Check(shape.transform.localScale==Vector3.one*2f,"Uniform 2x scale on the complete composition");

        var spawn=typeof(DecorationSpawner).GetMethod("SpawnDecoration",Private);
        var placed=(List<GameObject>)typeof(DecorationSpawner).GetField("spawnedDecorations",Private).GetValue(spawner);
        var coordinate=FixCoordinate.Instance;
        var random=UnityEngine.Random.state;
        int before=placed.Count;
        try
        {
            UnityEngine.Random.InitState(185);
            for(int i=0;i<24;i++)
            {
                SpawnAt(0);
                SpawnAt(city.baseSpawnDistance);
            }
            Check(placed.Count==before,"Castle exclusion at start and next stop");
            for(int i=0;i<216;i++) SpawnAt(109f+i*16f);
        }
        finally { UnityEngine.Random.state=random; }
        Physics.SyncTransforms();
        var sleighs=placed.Skip(before).Where(o=>o && o.name==design+"(Clone)").ToArray();
        Check(sleighs.Any(o=>o.transform.position.x<0) && sleighs.Any(o=>o.transform.position.x>0),
            $"The normal generator spawned {sleighs.Length} new sleighs across both banks");
        foreach(var sleigh in sleighs)
        {
            var footprint=sleigh.GetComponent<BoxCollider>().bounds;
            var p=sleigh.transform.position;
            var filter=sleigh.transform.Find("Painted_Gift_Sleigh").GetComponent<MeshFilter>();
            var model=filter.transform;
            CheckGeometry(filter.sharedMesh==shape.sharedMesh && model.GetComponent<MeshRenderer>().sharedMaterials.SequenceEqual(materials),
                "Spawned model changed mesh or materials");
            var localVertices=filter.sharedMesh.vertices;
            var vertices=localVertices.Select(model.TransformPoint).ToArray();
            var bounds=new Bounds(vertices[0],Vector3.zero);
            for(int i=0;i<vertices.Length;i++)
            {
                var expected=model.position+model.rotation*(localVertices[i]*2f);
                CheckGeometry((vertices[i]-expected).sqrMagnitude<.000004f,"Distorted vertex in spawned model");
                bounds.Encapsulate(vertices[i]);
            }
            CheckGeometry((bounds.min-footprint.min).sqrMagnitude<.000004f &&
                (bounds.max-footprint.max).sqrMagnitude<.000004f,"Cleanup box does not enclose exact scaled bounds");
            CheckGeometry(footprint.min.x>=3.5f || footprint.max.x<=-3.5f,"Sleigh reaches into the channel");
            CheckGeometry(!city.IsWithinCityClearance(p.z+coordinate.BoardAddPos,60f),"Sleigh enters the castle buffer");
            CheckGeometry(placed.All(other=>!other || other==sleigh ||
                Vector2.Distance(new Vector2(p.x,p.z),new Vector2(other.transform.position.x,other.transform.position.z))>=11.99f),
                "Sleigh too close to another route decoration");
        }
        Note("Verified proportional vertex transforms, materials and exact cleanup bounds on every sample.");
        Note("Every sampled sleigh clears the channel, castle buffers and neighboring decorations");

        var chosen=sleighs.OrderBy(o=>Mathf.Abs(o.transform.position.x)+(o.transform.position.x<0f?2.4f:0f)).First();
        var target=chosen.GetComponent<BoxCollider>().bounds.center;
        target.y=2.38f;
        var board=BoardController.Instance;
        var player=PlayerMovement.Instance;
        var camera=Camera.main;
        Check(camera && camera.transform.IsChildOf(player.transform),"Captured with the live first-person player camera");
        await Shoot("boat-approach",10f,5.4f);
        await Shoot("boat",3.8f,5.4f);
        await Shoot("boat-rail",4.7f,-2.30f);

        void SpawnAt(float distance)
        {
            float baseZ=distance-coordinate.BoardAddPos-spawner.spawnAreaOffset.y-spawner.spawnAreaSize.y*.5f;
            spawn.Invoke(spawner,new object[]{baseZ});
        }

        async Task Shoot(string name,float lead,float alongDeck)
        {
            float side=Mathf.Sign(chosen.transform.position.x);
            float distance=board.TotalDistanceTraveled+target.z-lead-alongDeck-board.transform.position.z;
            board.ApplySharedBoat(distance,0,board.SharedBaseFuel,board.SharedBaseFuel,board.SharedBaseSpeed,false);
            await Task.Yield();
            Physics.SyncTransforms();
            var deck=board.transform.position+new Vector3(side*2.14f,0f,alongDeck);
            var supports=Physics.RaycastAll(deck+Vector3.up*4f,Vector3.down,5.5f,~0,QueryTriggerInteraction.Ignore)
                .Where(h=>h.collider.transform.IsChildOf(board.transform) && h.point.y<deck.y+.65f)
                .OrderByDescending(h=>h.point.y).ToArray();
            CheckGeometry(supports.Length>0,"Camera lacks solid deck support");
            deck.y=supports[0].point.y+.04f;
            player.Teleport(deck);
            Physics.SyncTransforms();
            Note(name+" deck point "+deck+"; support "+(supports.Length==0?"none":supports[0].collider.name));
            var direction=target-camera.transform.position;
            var yaw=Quaternion.LookRotation(new Vector3(direction.x,0f,direction.z));
            player.transform.rotation=yaw;
            var aim=target-camera.transform.position;
            camera.transform.rotation=Quaternion.LookRotation(aim);
            Capture(camera,name);
            Note($"{name}: camera {camera.transform.position}, sleigh {chosen.transform.position}, {aim.magnitude:F1} m, FOV {camera.fieldOfView:F1}.");
        }
    }

    static void Capture(Camera camera,string name)
    {
        const int width=1600,height=900;
        var output=new RenderTexture(width,height,24);
        var image=new Texture2D(width,height,TextureFormat.RGB24,false);
        var previous=RenderTexture.active;
        var target=camera.targetTexture;
        try
        {
            camera.targetTexture=output;
            camera.Render();
            RenderTexture.active=output;
            image.ReadPixels(new Rect(0,0,width,height),0,0);
            image.Apply();
            File.WriteAllBytes("Docs/locations/previews/event-winter200-"+name+".png",image.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture=target;
            RenderTexture.active=previous;
            Object.Destroy(output);
            Object.Destroy(image);
        }
    }

    static void CheckGeometry(bool condition, string label)
    {
        if (!condition) throw new InvalidOperationException(label);
    }
    static void Check(bool condition, string label)
    {
        CheckGeometry(condition, label);
        Note(label);
    }
    static void Note(string line)
    {
        File.AppendAllText(Report,line+Environment.NewLine);
        Debug.Log("[Winter200] "+line);
    }
}


