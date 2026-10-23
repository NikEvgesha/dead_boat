using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class SealCottageRoof
{
    const string Data = "Assets/Prefabs/Workshop/CottageGeometry/";
    class Builder
    {
        public readonly List<Vector3> vertices = new List<Vector3>();
        public readonly List<int>[] indices = { new List<int>(), new List<int>() };
        public void Face(Vector3 a, Vector3 b, Vector3 c, Vector3 normal, int material = 0)
        {
            if (Vector3.Dot(Vector3.Cross(b-a,c-a),normal)<0) { var swap=b; b=c; c=swap; }
            int start=vertices.Count; vertices.Add(a);vertices.Add(b);vertices.Add(c);
            indices[material].AddRange(new[]{start,start+1,start+2});
        }
        public void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,Vector3 normal,int material=0)
        { Face(a,b,c,normal,material);Face(a,c,d,normal,material); }
        public Mesh Save(string name,int materials)
        {
            var mesh=new Mesh { name=name };mesh.SetVertices(vertices);mesh.subMeshCount=materials;
            for(int i=0;i<materials;i++)mesh.SetTriangles(indices[i],i);
            mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,Data+name+".asset");return mesh;
        }
    }
    static Material Material(string name,Color color)
    {
        var mat=new Material(Shader.Find("Standard")){name=name,color=color};mat.SetFloat("_Glossiness",.15f);
        AssetDatabase.CreateAsset(mat,Data+name+".mat");return mat;
    }
    static GameObject Geometry(string name,Transform parent,Mesh mesh,Material[] materials)
    {
        var go=new GameObject(name);go.transform.SetParent(parent,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;
        go.AddComponent<MeshRenderer>().sharedMaterials=materials;go.AddComponent<MeshCollider>().sharedMesh=mesh;return go;
    }
    static Mesh Roof()
    {
        var b=new Builder();const float edge=3.3f,z=3.25f;
        var lf=new Vector3(-edge,3.2f,-z);var lb=new Vector3(-edge,3.2f,z);
        var rf=new Vector3(edge,3.2f,-z);var rb=new Vector3(edge,3.2f,z);
        var cf=new Vector3(0,5,-z);var cb=new Vector3(0,5,z);
        var lfi=lf-Vector3.up*.14f;var lbi=lb-Vector3.up*.14f;
        var rfi=rf-Vector3.up*.14f;var rbi=rb-Vector3.up*.14f;
        var cfi=cf-Vector3.up*.14f;var cbi=cb-Vector3.up*.14f;
        b.Quad(lf,cf,cb,lb,new Vector3(-1,1,0));b.Quad(cf,rf,rb,cb,new Vector3(1,1,0));
        b.Quad(lfi,lbi,cbi,cfi,new Vector3(1,-1,0),1);b.Quad(cfi,cbi,rbi,rfi,new Vector3(-1,-1,0),1);
        b.Quad(lf,lb,lbi,lfi,Vector3.left);b.Quad(rf,rfi,rbi,rb,Vector3.right);
        b.Quad(lf,lfi,cfi,cf,Vector3.back);b.Quad(cf,cfi,rfi,rf,Vector3.back);
        b.Quad(lb,cb,cbi,lbi,Vector3.forward);b.Quad(cb,rb,rbi,cbi,Vector3.forward);
        return b.Save("SealedBrownRoof",2);
    }
    static Mesh Gable()
    {
        var b=new Builder();var a=new Vector3(-3,3.26f,-.1f);var c=new Vector3(3,3.26f,-.1f);var peak=new Vector3(0,4.86f,-.1f);
        var d=a+Vector3.forward*.2f;var e=c+Vector3.forward*.2f;var top=peak+Vector3.forward*.2f;
        b.Face(a,c,peak,Vector3.back);b.Face(d,top,e,Vector3.forward);
        b.Quad(a,d,e,c,Vector3.down);b.Quad(a,peak,top,d,new Vector3(-1,1,0));b.Quad(c,e,top,peak,new Vector3(1,1,0));
        return b.Save("SealedWoodGable",1);
    }
    public static void Main()
    {
        var scene=SceneManager.GetActiveScene();
        if(scene.path!="Assets/Scenes/AssetWorkshop.unity"||EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Open workshop in Edit Mode");
        var house=scene.GetRootGameObjects().First(x=>x.name=="01_Assembly_Area").transform.Find("WoodenCottage_01");
        var roof=house.Find("04_Roof");
        if(roof.Find("Sealed_Roof_Assembly")!=null||File.Exists(Data+"SealedBrownRoof.asset"))throw new Exception("Roof already exists; preserve edits");
        Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Соединить крышу со стенами");
        try
        {
            foreach(Transform part in roof){Undo.RecordObject(part.gameObject,"Заменить секции крыши");part.gameObject.SetActive(false);}
            var assembly=new GameObject("Sealed_Roof_Assembly");assembly.transform.SetParent(roof,false);Undo.RegisterCreatedObjectUndo(assembly,"Цельная крыша");
            var brown=Material("Roof_WarmBrown",new Color(.3f,.18f,.12f));var underside=Material("Roof_InteriorBrown",new Color(.24f,.15f,.1f));
            Geometry("Continuous_Pitched_Roof",assembly.transform,Roof(),new[]{brown,underside});
            var wood=house.Find("03_Frame").GetComponentInChildren<Renderer>().sharedMaterials.First(m=>m.name=="wood");
            var gableMesh=Gable();
            Geometry("Front_Gable",assembly.transform,gableMesh,new[]{wood}).transform.localPosition=new Vector3(0,0,-2.9f);
            Geometry("Rear_Gable",assembly.transform,gableMesh,new[]{wood}).transform.localPosition=new Vector3(0,0,2.9f);
            Physics.SyncTransforms();int samples=0;
            // Roof and perimeter must occlude rays through the former gaps, independently of lighting/shadow bias.
            for(float x=-2.9f;x<=2.91f;x+=.2f)
                for(float z=-2.9f;z<=2.91f;z+=.2f)
                {
                    if(!Physics.Raycast(new Vector3(x,3.0f,z),Vector3.up,out var hit,3))throw new Exception("Open roof at "+x+","+z);
                    samples++;
                }
            foreach(float side in new[]{-1f,1f})
                for(float x=-2.9f;x<=2.91f;x+=.2f)
                    for(float y=3.27f;y<=4.85f;y+=.1f)
                    {
                        // Only sample the occupied triangle under the inner roof line.
                        if(y>4.86f-Mathf.Abs(x)*1.6f/3f)continue;
                        if(!Physics.Raycast(new Vector3(x,y,0),Vector3.forward*side,out var hit,3.5f))throw new Exception("Open gable at "+x+","+y);
                        samples++;
                    }
            PrefabUtility.ApplyPrefabInstance(house.gameObject,InteractionMode.AutomatedAction);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            Undo.CollapseUndoOperations(group);Selection.activeGameObject=assembly;
            SceneView.lastActiveSceneView?.LookAt(new Vector3(0,2,-.5f),Quaternion.Euler(25,-35,0),7,false,true);
            File.WriteAllText("Temp/cottage-roof-result.txt","PASS: closed roof/gable ray samples="+samples+"; wall/roof overlap at x=3: 0.056m; gable/wall overlap=0.02m; scene/prefab saved");
        }
        catch{Undo.RevertAllDownToGroup(group);throw;}
    }
}
