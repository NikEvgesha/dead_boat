using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class RestoreCastleProportions
{
    static Bounds B(Transform t) { return B(t.GetComponentsInChildren<MeshRenderer>().Where(r => r.enabled && r.gameObject.activeInHierarchy).ToArray()); }
    static Bounds B(Renderer[] rs) { var b = rs[0].bounds; foreach (var r in rs.Skip(1)) b.Encapsulate(r.bounds); return b; }
    static Bounds Local(Transform h, Renderer[] rs) {
        bool first = true; var b = new Bounds();
        foreach (var r in rs) foreach (float x in new[] {r.bounds.min.x, r.bounds.max.x})
            foreach (float y in new[] {r.bounds.min.y, r.bounds.max.y}) foreach (float z in new[] {r.bounds.min.z, r.bounds.max.z}) {
                var p = h.InverseTransformPoint(new Vector3(x,y,z)); if(first){b=new Bounds(p,Vector3.zero);first=false;}else b.Encapsulate(p);
            }
        return b;
    }
    static void Capture(Vector3 position, Vector3 target, string path, float fov) {
        var go = new GameObject("Temporary_Proportion_Camera"); var rt = new RenderTexture(1400,1000,24); var tex = new Texture2D(1400,1000,TextureFormat.RGB24,false);
        var old = RenderTexture.active;
        var labels=GameObject.Find("Castle_Services_Prototype").GetComponentsInChildren<TextMesh>(true).Select(t=>t.gameObject).Where(g=>g.activeSelf).ToArray();
        var canvases=UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c=>c.enabled).ToArray();
        try { foreach(var label in labels)label.SetActive(false);foreach(var canvas in canvases)canvas.enabled=false;var c=go.AddComponent<Camera>();c.transform.position=position;c.transform.LookAt(target);c.fieldOfView=fov;c.farClipPlane=300;c.clearFlags=CameraClearFlags.SolidColor;c.backgroundColor=new Color(.18f,.23f,.28f);c.targetTexture=rt;c.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1400,1000),0,0);tex.Apply();File.WriteAllBytes(path,tex.EncodeToPNG()); }
        finally {foreach(var canvas in canvases)canvas.enabled=true;foreach(var label in labels)label.SetActive(true);RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex);}
    }
    public static string Main() {
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(scene.path!="Assets/Scenes/CastleServicesWorkshop.unity" || scene.isDirty || EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Saved castle Edit Mode required");
        var root=GameObject.Find("Castle_Services_Prototype").transform;
        var all=root.GetComponentsInChildren<Transform>(true);
        var before=all.ToDictionary(t=>t,t=>new[]{t.localPosition,t.localScale,t.localEulerAngles});
        var changed=new System.Collections.Generic.HashSet<Transform>();var report=new StringBuilder();
        var trees=all.Where(t=>t.name=="Shade_Tree"&&t.GetComponentsInChildren<MeshRenderer>().Any(r=>r.enabled&&r.gameObject.activeInHierarchy)).ToArray();
        foreach(var t in trees){
            var b=B(t);var source=PrefabUtility.GetCorrespondingObjectFromSource(t);
            if(source==null)throw new Exception("Tree source missing: "+t.parent.name);
            Undo.RecordObject(t,"Restore proportional tree");t.localScale=source.localScale;var natural=B(t);t.localScale*=b.size.y/natural.size.y;
            var after=B(t);t.position+=new Vector3(b.center.x-after.center.x,b.min.y-after.min.y,b.center.z-after.center.z);after=B(t);
            var ratio=t.localScale.x/source.localScale.x;
            if(Mathf.Abs(t.localScale.y/source.localScale.y-ratio)>.001f||Mathf.Abs(t.localScale.z/source.localScale.z-ratio)>.001f||Mathf.Abs(after.min.y-b.min.y)>.001f||Mathf.Abs(after.max.y-b.max.y)>.001f)throw new Exception("Tree proportion/base failed");
            PrefabUtility.RecordPrefabInstancePropertyModifications(t);changed.Add(t);
            report.AppendLine(t.parent.name+": proportional source scale="+t.localScale.ToString("F3")+", bounds "+after.ToString("F3")+", original center/base/height retained.");
        }
        var house=root.Find("03_Service_Houses/05_Nursery");var interior=house.Find("18_Original_Sheriff_Interior");var cage=interior.Find("Cage_For_Prisoner");
        var walls=cage.GetComponentsInChildren<MeshRenderer>().Where(r=>r.name.StartsWith("wall_")).Cast<Renderer>().ToArray();var oldCell=Local(house,walls);
        var furniture=interior.Cast<Transform>().Where(t=>t!=cage).ToArray();var positions=furniture.ToDictionary(t=>t,t=>t.position);
        var furnitureBounds=furniture.Where(t=>t.GetComponentsInChildren<MeshRenderer>().Any()).ToDictionary(t=>t,t=>B(t));
        Undo.RecordObjects(interior.GetComponentsInChildren<Transform>().Cast<UnityEngine.Object>().ToArray(),"Restore proportional sheriff interior");
        interior.localScale=Vector3.one*.5f;changed.Add(interior);
        foreach(var t in furniture){t.position=positions[t];changed.Add(t);if(t.name=="chair")t.localScale=Vector3.one*1.6f;
            if(t.name.StartsWith("pillar_decorated"))t.localScale=Vector3.one;
            if(furnitureBounds.ContainsKey(t)){var a=B(t);var b=furnitureBounds[t];t.position+=new Vector3(b.center.x-a.center.x,b.min.y-a.min.y,b.center.z-a.center.z);}}
        var bed=cage.Cast<Transform>().First(t=>t.name.StartsWith("bed_"));bed.localScale=Vector3.one*1.2f;changed.Add(bed);
        var cell=Local(house,walls);cage.position+=house.TransformVector(new Vector3(oldCell.min.x-cell.min.x,oldCell.min.y-cell.min.y,oldCell.max.z-cell.max.z));changed.Add(cage);cell=Local(house,walls);
        var rightPillar=interior.Find("pillar_decorated (1)");var rp=house.InverseTransformPoint(rightPillar.position);rp.x=2.2f;rp.z=2.15f;rightPillar.position=house.TransformPoint(rp);
        if(cell.min.x<-2.61f||cell.max.x>2.55f||cell.min.z<-2.61f||cell.max.z>2.61f)throw new Exception("Cell outside room: "+cell);
        if(cage.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("wall_")).Any(t=>Mathf.Abs(t.lossyScale.x-t.lossyScale.y)>.002f||Mathf.Abs(t.lossyScale.z-t.lossyScale.y)>.002f))throw new Exception("Bars distorted");
        var sell=house.Find("15_Gameplay_Source/SellPoint");var money=sell.GetComponentsInChildren<Transform>(true).First(t=>t.name=="MoneyBagPoint");var payout=money.position;
        var trigger=sell.GetComponent<BoxCollider>();Undo.RecordObject(trigger,"Fit trigger to proportional cell");Undo.RecordObject(sell,"Fit cell interaction point");
        sell.position=house.TransformPoint(new Vector3(cell.center.x,cell.min.y,cell.center.z));trigger.center=new Vector3(0,cell.size.y*.5f,0);trigger.size=cell.size;money.position=payout;changed.Add(sell);changed.Add(money);
        report.AppendLine("Cell: uniform scale .5, source proportions restored; house-local bounds="+cell.ToString("F3")+"; saved left/rear/floor alignment retained. Bed uniform .6, chair .8. Enemy trigger fitted; payout position retained.");
        var flags=root.Find("06_Territory_Decor").Cast<Transform>().Where(t=>t.name=="Banner_Base"||t.name=="Banner_Pole"||t.name=="Blue_Castle_Banner").ToArray();
        if(flags.Length!=12)throw new Exception("Expected four complete flag assemblies");
        foreach(var t in flags){Undo.RecordObject(t,"Move riverside flag");var p=t.localPosition;p.x=Mathf.Sign(p.x)*4.8f;p.z=Mathf.Sign(p.z)*22.8f;t.localPosition=p;changed.Add(t);}
        report.AppendLine("Four blue flag assemblies (12 objects): x +/-7 -> +/-4.8, z +/-20 -> +/-22.8, height/scale retained. River banks +/-3.5; arch inner faces +/-23.15.");
        foreach(var t in all.Where(t=>!changed.Contains(t))){var v=before[t];if(t.localPosition!=v[0]||t.localScale!=v[1]||t.localEulerAngles!=v[2])throw new Exception("Unrelated transform changed: "+t.name);}
        report.AppendLine("All unrelated local transforms unchanged versus owner's saved scene.");
        Physics.SyncTransforms();
        var probe=new GameObject("Temporary_Proportion_Aisle_Probe");
        try {var cc=probe.AddComponent<CharacterController>();cc.height=1.8f;cc.radius=.4f;cc.center=new Vector3(0,.9f,0);cc.stepOffset=.3f;cc.skinWidth=.08f;probe.transform.position=house.TransformPoint(new Vector3(0,.55f,-4.5f));Physics.SyncTransforms();
            for(int i=0;i<55;i++)cc.Move(house.TransformVector(new Vector3(0,0,.04f))+Vector3.down*.025f);
            for(int i=0;i<55;i++)cc.Move(house.TransformVector(new Vector3(.04f,0,0))+Vector3.down*.025f);
            for(int i=0;i<100;i++)cc.Move(house.TransformVector(new Vector3(0,0,.04f))+Vector3.down*.025f);
            var p=house.InverseTransformPoint(probe.transform.position);if(p.z<.5f||!cc.isGrounded)throw new Exception("Aisle blocked "+p);report.AppendLine("Player-size entry/right aisle PASS: "+p.ToString("F3"));
        } finally {UnityEngine.Object.DestroyImmediate(probe);}
        Capture(new Vector3(-49,62,-58),new Vector3(0,4,0),"Docs/assets/examples/Castle_Proportions_Overview.png",48);
        Capture(house.TransformPoint(new Vector3(.3f,1.8f,-2.55f)),house.TransformPoint(new Vector3(-.3f,1.2f,.6f)),"Docs/assets/examples/Castle_Proportions_Sheriff.png",105);
        EditorSceneManager.MarkSceneDirty(scene);AssetDatabase.SaveAssets();if(!EditorSceneManager.SaveScene(scene))throw new Exception("Save failed");
        File.WriteAllText("Docs/assets/examples/CASTLE_PROPORTIONS_FLAGS_VALIDATION.txt",report.ToString());return report.ToString();
    }
}
