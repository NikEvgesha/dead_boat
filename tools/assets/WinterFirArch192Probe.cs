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
public static class WinterFirArch192Probe
{
    const string Flag = "WinterFirArch192Probe";
    const string Report = "Docs/locations/previews/event-winter192-play.txt";
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static WinterFirArch192Probe() => EditorApplication.playModeStateChanged += StateChanged;

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
        const string design="Bank_06_Fir_Arch";
        var city=Object.FindAnyObjectByType<CitySpawner>();
        var spawner=Object.FindAnyObjectByType<DecorationSpawner>();
        var entry=spawner.decorationCollection.spawnItems.Single(i=>i.prefab.name==design);
        var shape=entry.prefab.transform.Find("Snowy_Fir_Arch").GetComponent<MeshFilter>();
        Check(shape.sharedMesh && shape.sharedMesh.subMeshCount==8 && entry.chance==8f,
            "New arch is in the live pool at its original weight");
        Check(entry.prefab.GetComponent<BoxCollider>().isTrigger && entry.prefab.CompareTag("Decoration"),
            "Arch retains the decorative cleanup trigger");

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
        var arches=placed.Skip(before).Where(o=>o && o.name==design+"(Clone)").ToArray();
        Check(arches.Any(o=>o.transform.position.x<0) && arches.Any(o=>o.transform.position.x>0),
            $"The normal generator spawned {arches.Length} new arches across both banks");
        foreach(var arch in arches)
        {
            var footprint=arch.GetComponent<BoxCollider>().bounds;
            var p=arch.transform.position;
            CheckGeometry(footprint.min.x>=3.5f || footprint.max.x<=-3.5f,"Arch reaches into the channel");
            CheckGeometry(!city.IsWithinCityClearance(p.z+coordinate.BoardAddPos,60f),"Arch enters the castle buffer");
            CheckGeometry(placed.All(other=>!other || other==arch ||
                Vector2.Distance(new Vector2(p.x,p.z),new Vector2(other.transform.position.x,other.transform.position.z))>=11.99f),
                "Arch too close to another route decoration");
        }
        Note("Every sampled arch clears the channel, castle buffers and neighboring decorations");

        var chosen=arches.Where(o=>o.transform.position.x>0f).OrderBy(o=>o.transform.position.x).First();
        var target=chosen.GetComponent<BoxCollider>().bounds.center;
        target.y=2.34f;
        var board=BoardController.Instance;
        var player=PlayerMovement.Instance;
        var camera=Camera.main;
        Check(camera && camera.transform.IsChildOf(player.transform),"Captured with the live first-person player camera");
        await Shoot("boat-approach",10f,5.4f);
        await Shoot("boat",3.8f,5.4f);
        await Shoot("boat-rail",4.7f,-2.30f);
        VerifyPassage(chosen);
        CheckGround(chosen);
        var opposite=arches.Where(o=>o.transform.position.x<0f).OrderBy(o=>Mathf.Abs(o.transform.position.x)).First();
        var oppositeZ=opposite.transform.position.z;
        board.ApplySharedBoat(board.TotalDistanceTraveled+oppositeZ-board.transform.position.z,0,
            board.SharedBaseFuel,board.SharedBaseFuel,board.SharedBaseSpeed,false);
        player.Teleport(board.transform.position);
        await Task.Delay(200);
        CheckGround(opposite);

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
            Note($"{name}: camera {camera.transform.position}, arch {chosen.transform.position}, {aim.magnitude:F1} m, FOV {camera.fieldOfView:F1}.");
        }
    }

    static void CheckGround(GameObject arch)
    {
        var land=Object.FindAnyObjectByType<LendController>();
        var surfaces=land.locations[0].groundSegments.Where(t=>t.gameObject.activeInHierarchy)
            .Select(t=>t.GetComponent<MeshFilter>()).Where(f=>f && f.sharedMesh).ToArray();
        var model=arch.transform.Find("Snowy_Fir_Arch").GetComponent<MeshFilter>();
        var mesh=model.sharedMesh;
        var vertices=mesh.vertices;
        var bark=mesh.GetTriangles(0).Distinct().Select(i=>vertices[i]).ToArray();
        foreach(float side in new[]{-1f,1f})
        {
            var trunk=bark.Where(v=>v.x*side>1.45f && v.y<.02f).ToArray();
            CheckGeometry(trunk.Length>=7,"Cannot identify visible trunk base");
            var foot=model.transform.TransformPoint(trunk.Aggregate(Vector3.zero,(a,b)=>a+b)/trunk.Length);
            var samples=trunk.Select(v=>model.transform.TransformPoint(v)).Append(foot).ToArray();
            var gaps=samples.Select(p=>p.y-SurfaceY(p,surfaces)).ToArray();
            Check(gaps.All(g=>g>=-.16f && g<=.02f),
                $"{(arch.transform.position.x>0f?"Right":"Left")} bank trunk at {foot}: rendered ground {SurfaceY(foot,surfaces):F3} m, max gap {gaps.Max():F3} m.");
        }
    }

    // Sample the top-facing triangles of the streamed bank directly below the visible bark vertices.
    static float SurfaceY(Vector3 p, MeshFilter[] surfaces)
    {
        float highest=float.NegativeInfinity;
        foreach(var surface in surfaces)
        {
            var bounds=surface.GetComponent<MeshRenderer>().bounds;
            if(p.x<bounds.min.x || p.x>bounds.max.x || p.z<bounds.min.z || p.z>bounds.max.z) continue;
            var mesh=surface.sharedMesh;
            var vertices=mesh.vertices.Select(surface.transform.TransformPoint).ToArray();
            var triangles=mesh.triangles;
            for(int i=0;i<triangles.Length;i+=3)
            {
                var a=vertices[triangles[i]];
                var b=vertices[triangles[i+1]];
                var c=vertices[triangles[i+2]];
                if(Vector3.Cross(b-a,c-a).y<=1e-6f) continue;
                float det=(b.z-c.z)*(a.x-c.x)+(c.x-b.x)*(a.z-c.z);
                float u=((b.z-c.z)*(p.x-c.x)+(c.x-b.x)*(p.z-c.z))/det;
                float v=((c.z-a.z)*(p.x-c.x)+(a.x-c.x)*(p.z-c.z))/det;
                if(u<-.00001f || v<-.00001f || u+v>1.00001f) continue;
                float y=u*a.y+v*b.y+(1f-u-v)*c.y;
                if(y<=p.y+.20f) highest=Mathf.Max(highest,y);
            }
        }
        return highest;
    }

    static void VerifyPassage(GameObject arch)
    {
        var model=arch.transform.Find("Snowy_Fir_Arch").GetComponent<MeshFilter>();
        var source=PlayerMovement.Instance.GetComponent<CharacterController>();
        var mesh=model.sharedMesh;
        var vertices=mesh.vertices;
        var triangles=mesh.triangles;
        float limit=source.height+.16f;
        float halfGap=float.PositiveInfinity;
        for(int i=0;i<triangles.Length;i+=3)
        {
            var a=vertices[triangles[i]];
            var b=vertices[triangles[i+1]];
            var c=vertices[triangles[i+2]];
            if(Mathf.Min(a.y,Mathf.Min(b.y,c.y))>limit) continue;
            var left=Mathf.Min(a.x,Mathf.Min(b.x,c.x));
            var right=Mathf.Max(a.x,Mathf.Max(b.x,c.x));
            halfGap=Mathf.Min(halfGap,left>0f?left:right<0f?-right:0f);
        }
        Check(halfGap>source.radius+.15f,$"Clear central corridor at player height, width at least {halfGap*2f:F2} m.");
        var test=new GameObject("Fir arch player capsule");
        test.layer=source.gameObject.layer;
        try
        {
            var capsule=test.AddComponent<CharacterController>();
            capsule.radius=source.radius;
            capsule.height=source.height;
            capsule.center=Vector3.up*(source.height*.5f);
            capsule.stepOffset=source.stepOffset;
            capsule.skinWidth=source.skinWidth;
            foreach(float direction in new[]{-1f,1f})
            {
                capsule.enabled=false;
                test.transform.position=model.transform.TransformPoint(new Vector3(0f,.055f,-2f*direction));
                capsule.enabled=true;
                Physics.SyncTransforms();
                for(int step=0;step<18;step++) capsule.Move(model.transform.forward*(.24f*direction));
                CheckGeometry(model.transform.InverseTransformPoint(test.transform.position).z*direction>=2.1f,
                    "Player-sized capsule blocked in fir arch");
            }
            Note("Player-sized capsule traversed the opening in both directions.");
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
            File.WriteAllBytes("Docs/locations/previews/event-winter192-"+name+".png",image.EncodeToPNG());
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
        Debug.Log("[Winter192] "+line);
    }
}


