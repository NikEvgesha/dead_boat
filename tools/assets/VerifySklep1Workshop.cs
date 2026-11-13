using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using Object=UnityEngine.Object;

public static class VerifySklep1Workshop
{
    const BindingFlags Flags=BindingFlags.NonPublic|BindingFlags.Instance;
    const string Prefab="Assets/Prefabs/Workshop/Sklep1/Sklep1_OvergrownRuins.prefab";
    static void Check(bool ok,string message){if(!ok)throw new InvalidOperationException(message);}
    public static string Run()
    {
        var report=new List<string>();
        var type=TypeCache.GetTypesDerivedFrom<EditorWindow>().Single(t=>t.Name=="Sklep1WalkWindow");
        var win=Resources.FindObjectsOfTypeAll(type).OfType<EditorWindow>().Single();var basis=type.BaseType;
        var cc=(CharacterController)basis.GetField("player",Flags).GetValue(win);
        var cam=(Camera)basis.GetField("walkCamera",Flags).GetValue(win);
        var items=(List<(GameObject model,string label)>)basis.GetField("items",Flags).GetValue(win);
        var reset=basis.GetMethod("ResetWalk",Flags);var collect=basis.GetMethod("CollectNearby",Flags);
        var root=cam.scene.GetRootGameObjects().Single(g=>g.name=="Sklep1_OvergrownRuins");
        reset.Invoke(win,null);int checkpoints=0;float travelled=0;
        void Go(Vector3 p)
        {
            var before=cc.transform.position;int n=Mathf.CeilToInt(Vector3.Distance(p,before)/.05f)+80;
            for(int i=0;i<n;i++)
            {
                var d=p-cc.transform.position;d.y=0;cc.Move(Vector3.ClampMagnitude(d,.05f)+Vector3.down*.035f);
                if(i%4==0)collect.Invoke(win,null);
            }
            var diff=cc.transform.position-p;
            Check(new Vector2(diff.x,diff.z).magnitude<.32f&&Mathf.Abs(diff.y)<.4f&&cc.isGrounded,"Blocked walking to "+p+"; player="+cc.transform.position);
            checkpoints++;travelled+=Vector3.Distance(before,cc.transform.position);
        }
        try
        {
            // Follow the source maze explicitly: other open workshop scenes have overlapping NavMeshes.
            var destinations=new[]{new Vector3(40,.18f,24.9f),new Vector3(43,.18f,27),new Vector3(50,.18f,26),new Vector3(58,.18f,26),
                new Vector3(68,.18f,24),new Vector3(65.6f,.18f,24),new Vector3(65.6f,.18f,17),new Vector3(65.6f,.18f,10.6f),
                new Vector3(65.6f,.18f,24),new Vector3(80,.18f,24),new Vector3(79,.18f,12),new Vector3(79,.18f,1),new Vector3(80,.18f,-13),
                new Vector3(66,.18f,-11.4f),new Vector3(56,.18f,-11.4f),new Vector3(56,.18f,.5f),new Vector3(56,.18f,-11.4f),
                new Vector3(42,.18f,-11.4f),new Vector3(42,.18f,13),new Vector3(42,.18f,-11.4f),new Vector3(27,0,-11.4f),new Vector3(26,0,24.9f)};
            foreach(var p in destinations)Go(p);
            Check(items.All(i=>!i.model.activeSelf),"Five preview items not collected");
            report.Add($"PASS walking: {checkpoints} controller checkpoints, {travelled:F1} m; two external entrances, internal passages, lever approach, return; collected 5/5.");
        }
        finally{reset.Invoke(win,null);}
        var solids=root.GetComponentsInChildren<Collider>().Where(c=>c.enabled&&!c.isTrigger).ToArray();
        float Support(Vector3 p)=>solids.Select(c=>c.Raycast(new Ray(p+Vector3.up*.08f,Vector3.down),out var h,.3f)&&h.normal.y>.6f?h.point.y:float.NegativeInfinity).Max();
        foreach(var plant in root.transform.Find("Overgrown_Concept_02/Grounded_Plants_And_Rocks").Cast<Transform>())
        {
            var vertices=plant.GetComponentsInChildren<MeshFilter>().Where(f=>f.sharedMesh).SelectMany(f=>f.sharedMesh.vertices.Select(f.transform.TransformPoint)).ToArray();
            float low=vertices.Min(v=>v.y);var gaps=vertices.Where(v=>v.y<low+.02f).Select(v=>v.y-Support(v)).ToArray();
            Check(gaps.All(g=>g>-.04f&&g<.015f),"Unsupported visible plant/rock base: "+plant.name);
            report.Add(plant.name+" visible-base ground gap "+gaps.Min().ToString("F3")+".."+gaps.Max().ToString("F3")+" m");
        }
        foreach(var item in items)
        {
            var rs=item.model.GetComponentsInChildren<MeshRenderer>();var b=rs[0].bounds;foreach(var r in rs.Skip(1))b.Encapsulate(r.bounds);
            float gap=b.min.y-Support(new Vector3(b.center.x,b.min.y,b.center.z));Check(Mathf.Abs(gap)<.025f,"Unsupported loot "+item.label);
            report.Add(item.label+" visible-base gap "+gap.ToString("F3")+" m");
        }
        var saved=AssetDatabase.LoadAssetAtPath<GameObject>(Prefab);
        var original=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Locations/Enemy/Sklep_1.prefab");
        string Path(Transform t,Transform r)=>AnimationUtility.CalculateTransformPath(t,r);
        var oldDoors=original.GetComponentsInChildren<MeshFilter>(true).Where(f=>f.name.StartsWith("Wall_Stone_Doorway")).ToArray();
        foreach(var door in oldDoors)
        {
            var copy=saved.transform.Find(Path(door.transform,original.transform));
            Check(copy&&Vector3.Distance(copy.position,door.transform.position)<.001f&&copy.GetComponent<MeshFilter>().sharedMesh==door.sharedMesh,"Changed source doorway "+door.name);
        }
        var traps=saved.GetComponentsInChildren<DungeonTrap>(true);Check(traps.Length==5,"Expected five DungeonTrap components");
        foreach(var trap in traps)
        {
            var so=new SerializedObject(trap);var spawn=(Transform)so.FindProperty("_spawnPos").objectReferenceValue;
            Check(spawn&&spawn.IsChildOf(saved.transform),"External spawn reference "+trap.name);
            Check(so.FindProperty("_enemyTypes").objectReferenceValue&&so.FindProperty("_spawnCount").intValue==3,"Missing enemy definition "+trap.name);
        }
        var zone=saved.GetComponentInChildren<ActivateZone>(true);Check(zone.PlayerOnTriggerEnter.GetPersistentEventCount()==5,"Expected gate and four spawn events");
        for(int i=0;i<5;i++)Check(zone.PlayerOnTriggerEnter.GetPersistentTarget(i)!=null,"Broken trigger target "+i);
        var leverSaved=saved.GetComponentInChildren<Lever>();Check(leverSaved._activeSubject==saved.GetComponentInChildren<ActivateObject>(),"Broken lever target");
        report.Add("PASS source preservation: six doorway transforms/meshes unchanged, five DungeonTrap with local spawn refs, gate + four _InTrap event targets, Lever -> ActivateObject.");
        var test=EditorSceneManager.NewPreviewScene();
        try
        {
            var instance=(GameObject)PrefabUtility.InstantiatePrefab(saved,test);var trapRoot=instance.transform.Find("Trap");
            foreach(var behavior in instance.GetComponentsInChildren<MonoBehaviour>(true))if(behavior)behavior.enabled=false;
            int total=0;
            foreach(var trap in instance.transform.Find("Trap/spawnPoints").GetComponentsInChildren<DungeonTrap>())
            {
                var routine=(IEnumerator)typeof(DungeonTrap).GetMethod("TrupActivate",Flags).Invoke(trap,null);int steps=0;
                while(routine.MoveNext()){if(++steps>10)throw new Exception("Spawn routine did not terminate");}
                var spawned=trap.GetComponentsInChildren<ZombieController>();Check(spawned.Length==3,"Spawn count "+trap.name);
                var so=new SerializedObject(trap);var spawn=(Transform)so.FindProperty("_spawnPos").objectReferenceValue;
                Check(spawned.All(e=>Vector3.Distance(e.transform.position,spawn.position)<.01f),"Wrong spawn position "+trap.name);total+=spawned.Length;
            }
            var zoneObject=trapRoot.Find("zoneTrap").gameObject;zoneObject.SetActive(true);zoneObject.transform.Find("WallActive").gameObject.SetActive(true);
            var animator=trapRoot.GetComponent<Animator>();animator.Rebind();animator.Update(0);
            var lever=instance.GetComponentInChildren<Lever>();lever.GetComponent<Animator>().Rebind();lever.GetComponent<Animator>().Update(0);
            lever.Use();animator.Update(1);animator.Update(1);
            Check(!zoneObject.activeSelf,"Lever did not deactivate gate zone through source Animator");
            report.Add("PASS isolated editor component check: actual four spawn enumerators created "+total+" enemies at local points; Lever.Use -> source Animator disabled zoneTrap and its gates.");
            report.Add("LIMIT: Play Mode trigger entry, enemy combat/AI and multiplayer were not exercised; walking window previews gates without enemy simulation.");
        }
        finally{EditorSceneManager.ClosePreviewScene(test);}
        var rt=RenderTexture.GetTemporary(1400,1000,24);var image=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);var active=RenderTexture.active;
        var position=cam.transform.position;var rotation=cam.transform.rotation;float fov=cam.fieldOfView;
        try
        {
            cam.targetTexture=rt;cam.farClipPlane=260;
            void Shot(string name,Vector3 p,Vector3 target,float field)
            {
                cam.transform.position=p;cam.transform.LookAt(target);cam.fieldOfView=field;cam.Render();RenderTexture.active=rt;
                image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);File.WriteAllBytes("Docs/locations/previews/sklep1-workshop-"+name+".png",image.EncodeToPNG());
            }
            Shot("overview",new Vector3(-3,62,-59),new Vector3(60,3.5f,7),43);
            Shot("player",new Vector3(27,1.7f,24.9f),new Vector3(51,2.2f,24.9f),70);
            Shot("interior",new Vector3(58,1.78f,24.9f),new Vector3(66,2.6f,18),70);
            Shot("plants",new Vector3(20,1.7f,-11),new Vector3(29,1.4f,-4),65);
        }
        finally{cam.transform.SetPositionAndRotation(position,rotation);cam.fieldOfView=fov;cam.targetTexture=null;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);Object.DestroyImmediate(image);}
        win.Focus();win.Repaint();File.WriteAllLines("Docs/locations/previews/sklep1-workshop-check.txt",report);
        return string.Join("\n",report);
    }
}
