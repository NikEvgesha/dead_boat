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
public static class WinterHollowLog199Probe
{
    const string Flag = "WinterHollowLog199Probe";
    const string Report = "Docs/locations/previews/event-winter199-play.txt";
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static WinterHollowLog199Probe() => EditorApplication.playModeStateChanged += StateChanged;

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
        const string design="Bank_02_Rooted_Log";
        var city=Object.FindAnyObjectByType<CitySpawner>();
        var spawner=Object.FindAnyObjectByType<DecorationSpawner>();
        var entry=spawner.decorationCollection.spawnItems.Single(i=>i.prefab.name==design);
        var shape=entry.prefab.transform.Find("Snowbound_Hollow_Log").GetComponent<MeshFilter>();
        Check(shape.sharedMesh && shape.sharedMesh.subMeshCount==8 && entry.chance==7f,
            "New log is in the live pool at its original weight");
        Check(entry.prefab.GetComponent<BoxCollider>().isTrigger && entry.prefab.CompareTag("Decoration"),
            "Log retains the decorative cleanup trigger");

        var spawn=typeof(DecorationSpawner).GetMethod("SpawnDecoration",Private);
        var placed=(List<GameObject>)typeof(DecorationSpawner).GetField("spawnedDecorations",Private).GetValue(spawner);
        var coordinate=FixCoordinate.Instance;
        var random=UnityEngine.Random.state;
        int before=placed.Count;
        try
        {
            UnityEngine.Random.InitState(199);
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
        var logs=placed.Skip(before).Where(o=>o && o.name==design+"(Clone)").ToArray();
        Check(logs.Any(o=>o.transform.position.x<0) && logs.Any(o=>o.transform.position.x>0),
            $"The normal generator spawned {logs.Length} new logs across both banks");
        foreach(var log in logs)
        {
            var footprint=log.GetComponent<BoxCollider>().bounds;
            var p=log.transform.position;
            CheckGeometry(footprint.min.x>=3.5f || footprint.max.x<=-3.5f,"Log reaches into the channel");
            CheckGeometry(!city.IsWithinCityClearance(p.z+coordinate.BoardAddPos,60f),"Log enters the castle buffer");
            CheckGeometry(placed.All(other=>!other || other==log ||
                Vector2.Distance(new Vector2(p.x,p.z),new Vector2(other.transform.position.x,other.transform.position.z))>=11.99f),
                "Log too close to another route decoration");
        }
        Note("Every sampled log clears the channel, castle buffers and neighboring decorations");

        var chosen=logs.OrderBy(o=>Mathf.Abs(o.transform.position.x)+(o.transform.position.x<0f?4.5f:0f)).First();
        var target=chosen.GetComponent<BoxCollider>().bounds.center;
        target.y=1.92f;
        var board=BoardController.Instance;
        var player=PlayerMovement.Instance;
        var camera=Camera.main;
        Check(camera && camera.transform.IsChildOf(player.transform),"Captured with the live first-person player camera");
        await Shoot("boat-approach",10f,5.4f);
        await Shoot("boat",3.8f,5.4f);
        await Shoot("boat-rail",4.7f,-2.30f);
        VerifyPassage(chosen);
        var opposite=logs.Where(o=>o.transform.position.x*chosen.transform.position.x<0f).OrderBy(o=>Mathf.Abs(o.transform.position.x)).First();
        var oppositeZ=opposite.transform.position.z;
        board.ApplySharedBoat(board.TotalDistanceTraveled+oppositeZ-board.transform.position.z,0,
            board.SharedBaseFuel,board.SharedBaseFuel,board.SharedBaseSpeed,false);
        player.Teleport(board.transform.position);
        await Task.Delay(200);
        VerifyPassage(opposite);

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
            Note($"{name}: camera {camera.transform.position}, log {chosen.transform.position}, {aim.magnitude:F1} m, FOV {camera.fieldOfView:F1}.");
        }
    }

    static void VerifyPassage(GameObject log)
    {
        var model=log.transform.Find("Snowbound_Hollow_Log");
        var shell=model.GetComponent<MeshCollider>();
        CheckGeometry(shell && !shell.convex && shell.sharedMesh==model.GetComponent<MeshFilter>().sharedMesh,
            "The new non-convex shell is the active passage collider");
        var source=PlayerMovement.Instance.GetComponent<CharacterController>();
        var terrain=Object.FindAnyObjectByType<LendController>().locations[0].groundSegments;
        var test=new GameObject("Hollow log player capsule") {layer=source.gameObject.layer};
        try
        {
            var capsule=test.AddComponent<CharacterController>();
            capsule.radius=source.radius;
            capsule.height=source.height;
            capsule.center=Vector3.up*(source.height*.5f);
            capsule.stepOffset=source.stepOffset;
            capsule.skinWidth=source.skinWidth;

            foreach(float x in new[]{-.42f,0f,.42f})
            {
                foreach(float z in new[]{-4.8f,-2f,0f,2f,4.8f})
                {
                    var floor=model.TransformPoint(new Vector3(x,0f,z));
                    bool bank=Physics.RaycastAll(floor+Vector3.up*.42f,Vector3.down,.66f,~0,QueryTriggerInteraction.Ignore)
                        .Any(h=>Mathf.Abs(h.point.y-floor.y)<.075f && terrain.Any(t=>h.collider.transform.IsChildOf(t)));
                    CheckGeometry(bank,"No bank surface below the path at " + floor);
                }
                foreach(float direction in new[]{-1f,1f})
                {
                    Place(new Vector3(x,.05f,-4.75f*direction));
                    for(int step=0;step<43;step++) capsule.Move(model.forward*(.23f*direction));
                    var finish=model.InverseTransformPoint(test.transform.position);
                    CheckGeometry(finish.z*direction>=4.9f && Mathf.Abs(finish.x-x)<.15f,
                        $"Player-sized capsule blocked at x {x:F2} in direction {direction}, ended at {finish}");
                }
            }
            foreach(float side in new[]{-1f,1f})
            {
                Place(new Vector3(side*3.7f,.05f,0f));
                for(int step=0;step<28;step++) capsule.Move(model.right*(-.18f*side));
                CheckGeometry(model.InverseTransformPoint(test.transform.position).x*side>1.75f,
                    "Side wall does not stop the character");
            }
            Note($"{(log.transform.position.x>0f?"Right":"Left")} bank: 6 player capsule passes, both entrances and offsets; both walls block lateral movement.");

            void Place(Vector3 local)
            {
                capsule.enabled=false;
                test.transform.position=model.TransformPoint(local);
                capsule.enabled=true;
                Physics.SyncTransforms();
            }
        }
        finally { Object.Destroy(test); }
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
            File.WriteAllBytes("Docs/locations/previews/event-winter199-"+name+".png",image.EncodeToPNG());
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
        Debug.Log("[Winter199] "+line);
    }
}


