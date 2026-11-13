using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;
public static class VerifyTo4ka6Walk
{
    static Vector3 Polar(float a,float r,float y){a*=Mathf.Deg2Rad;return new Vector3(Mathf.Sin(a)*r,y,-Mathf.Cos(a)*r);}
    public static string Run()
    {
        const BindingFlags f=BindingFlags.Instance|BindingFlags.NonPublic;
        var type=TypeCache.GetTypesDerivedFrom<EditorWindow>().Single(t=>t.Name=="To4ka6WalkWindow");
        var win=Resources.FindObjectsOfTypeAll(type).OfType<EditorWindow>().Single();
        var basis=type.BaseType;
        var cc=(CharacterController)basis.GetField("player",f).GetValue(win);
        var cam=(Camera)basis.GetField("walkCamera",f).GetValue(win);
        var items=(List<(GameObject model,string label)>)basis.GetField("items",f).GetValue(win);
        var reset=basis.GetMethod("ResetWalk",f);var collect=basis.GetMethod("CollectNearby",f);
        var root=cam.scene.GetRootGameObjects().Single(g=>g.name=="To4ka6_OvergrownCourt");
        var report=new List<string>();
        void Check(bool ok,string m){if(!ok)throw new InvalidOperationException(m);}
        int count=0;
        void Go(Vector3 p)
        {
            int n=Mathf.CeilToInt(Vector3.Distance(p,cc.transform.position)/.05f)+65;
            float fall=.015f;
            for(int i=0;i<n;i++)
            {
                var d=p-cc.transform.position;d.y=0;
                fall=cc.isGrounded?.015f:Mathf.Min(.14f,fall+.005f);
                cc.Move(Vector3.ClampMagnitude(d,.05f)+Vector3.down*fall);
                if(i%6==0)collect.Invoke(win,null);
            }
            var diff=cc.transform.position-p;
            Check(new Vector2(diff.x,diff.z).magnitude<.32f&&Mathf.Abs(diff.y)<.35f&&cc.isGrounded,
                $"Blocked {p:F2}, player {cc.transform.position:F2}");
            count++;
        }
        Check(!root.GetComponentsInChildren<MeshFilter>(true).Any(m=>m.sharedMesh&&AssetDatabase.GetAssetPath(m.sharedMesh).EndsWith("/Flower.fbx")),"Flowers must be absent from new courtyard");
        reset.Invoke(win,null);
        try
        {
            var route=new[]{new Vector3(0,0,-8),new Vector3(0,0,-5),new Vector3(0,0,-3),
                new Vector3(1.15f,0,-2.7f),new Vector3(1.8f,0,-1.6f),new Vector3(2.5f,0,-.8f),
                new Vector3(2.5f,0,1.0f),new Vector3(2.5f,0,1.3f),new Vector3(0,0,1.7f),
                new Vector3(-.6f,0,2.0f),new Vector3(-1.0f,0,1.0f),new Vector3(-1.3f,0,.3f),
                new Vector3(-1.1f,0,-1),new Vector3(-2.8f,0,-1),new Vector3(-2.8f,0,-2.5f),
                new Vector3(0,0,-2.5f),new Vector3(0,0,-5),new Vector3(0,0,-8)};
            foreach(var p in route)Go(p);
            Check(items.All(i=>!i.model.activeSelf),"Not all five courtyard items collected");
            report.Add($"PASS {count} walking checkpoints: clear courtyard entrance, furniture approaches and return; pickups 5/5.");
        }
        finally{reset.Invoke(win,null);}
        var solids=root.GetComponentsInChildren<Collider>().Where(c=>c.enabled&&!c.isTrigger).ToArray();
        float Ground(Vector3 p)
        {
            return solids.Select(c=>c.Raycast(new Ray(p+Vector3.up*.08f,Vector3.down),out var h,.25f)&&h.normal.y>.6f
                ?h.point.y:float.NegativeInfinity).Max();
        }
        bool Intersects(Renderer other,Bounds bounds)
        {
            if(!other.bounds.Intersects(bounds))return false;
            if(!other.TryGetComponent<MeshCollider>(out var mesh))return true;
            // Concave stair and parapet meshes surround the roof: their AABB includes empty space.
            var probe=new GameObject("Pickup clearance probe");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(probe,cam.scene);
            try
            {
                var box=probe.AddComponent<BoxCollider>();box.size=bounds.size;
                return Physics.ComputePenetration(box,bounds.center,Quaternion.identity,mesh,mesh.transform.position,
                    mesh.transform.rotation,out _,out float depth)&&depth>.001f;
            }
            finally{Object.DestroyImmediate(probe);}
        }
        foreach(var item in items)
        {
            var rs=item.model.GetComponentsInChildren<MeshRenderer>().Where(r=>r.enabled).ToArray();
            var b=rs[0].bounds;foreach(var r in rs.Skip(1))b.Encapsulate(r.bounds);
            var gap=b.min.y-Ground(new Vector3(b.center.x,b.min.y,b.center.z));
            Check(Mathf.Abs(gap)<.025f,"Item unsupported "+item.label);
            var obstacles=root.GetComponentsInChildren<Renderer>();
            Check(!obstacles.Any(r=>Intersects(r,b)),"Preview pickup intersects environment: "+item.label+" with "+string.Join(",",obstacles.Where(r=>Intersects(r,b)).Select(r=>r.name)));
            Check(!items.Where(i=>i.model!=item.model).Any(i=>i.model.GetComponentsInChildren<Renderer>().Any(r=>r.bounds.Intersects(b))),
                "Preview pickups intersect each other");
            report.Add(item.label+" bottom gap "+gap.ToString("F3")+" m");
        }
        var props=root.transform.Find("Grounded_Plants_And_Rocks").Cast<Transform>().ToArray();
        foreach(var plant in props.Where(t=>t.name.Contains("Oak")||t.name.Contains("Bush")||t.name.StartsWith("Purple")||t.name.EndsWith("Rock")))
        {
            var rs=plant.GetComponentsInChildren<MeshRenderer>();
            var vertices=plant.GetComponentsInChildren<MeshFilter>().Where(m=>m.sharedMesh)
                .SelectMany(m=>m.sharedMesh.vertices.Select(m.transform.TransformPoint)).ToArray();
            float low=vertices.Min(v=>v.y);
            var gaps=vertices.Where(v=>v.y<low+.02f).Select(v=>v.y-Ground(v)).ToArray();
            Check(gaps.All(v=>v<.012f&&v>-.045f),"Unsupported plant "+plant.name);
            var other=props.Where(t=>t!=plant).SelectMany(t=>t.GetComponentsInChildren<Renderer>())
                .Concat(root.transform.Find("Court").GetComponentsInChildren<Renderer>())
                .Concat(root.transform.Find("Furniture").GetComponentsInChildren<Renderer>()).ToArray();
            var collisions=rs.SelectMany(r=>other.Where(o=>r.bounds.Intersects(o.bounds))).Select(r=>r.transform.parent?.name+"/"+r.name).Distinct().ToArray();
            Check(collisions.Length==0,"Plant intersection "+plant.name+" with "+string.Join(",",collisions));
            report.Add(plant.name+" ground gap "+gaps.Min().ToString("F3")+".."+gaps.Max().ToString("F3"));
        }
        var rt=RenderTexture.GetTemporary(1280,820,24);var previous=RenderTexture.active;var target=cam.targetTexture;
        var image=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);
        var pos=cam.transform.position;var rot=cam.transform.rotation;
        try
        {
            cam.targetTexture=rt;
            void Picture(string name){cam.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);File.WriteAllBytes("Library/To4ka6ConceptReview/"+name+".png",image.EncodeToPNG());}
            Picture("player-start");
            cam.fieldOfView=50;
            cam.transform.position=new Vector3(-12,9,-14);cam.transform.LookAt(new Vector3(0,1.4f,0));Picture("overview");
            cam.transform.position=new Vector3(0,1.7f,-2.7f);cam.transform.LookAt(new Vector3(0,1.1f,2.2f));Picture("interior");
        }
        finally{cam.fieldOfView=70;cam.transform.SetPositionAndRotation(pos,rot);cam.targetTexture=target;RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);Object.DestroyImmediate(image);}
        win.Focus();win.Repaint();
        File.WriteAllLines("Library/To4ka6ConceptReview/walk-check.txt",report);
        return string.Join(Environment.NewLine,report);
    }
}
