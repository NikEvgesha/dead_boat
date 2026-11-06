using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DeadBoat.Online.Editor
{
    public static class EggNestArtTools
    {
        public static string Capture()
        {
            var scene=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
            try
            {
                var root=new GameObject("Nest states");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,scene);
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/EggSystem/Nest.prefab");
                for(int i=0;i<3;i++)
                {
                    var nest=Object.Instantiate(prefab,root.transform);nest.transform.localPosition=Vector3.right*(i-1)*2;
                    nest.transform.localRotation=Quaternion.identity;
                    foreach(var canvas in nest.GetComponentsInChildren<Canvas>(true))canvas.gameObject.SetActive(false);
                    var presentation=nest.GetComponent<EggNestPresentation>();presentation.ApplyState(i);
                    if(presentation.DisplayedState!=i || presentation.statusRing.enabled!=(i>0) || presentation.readyMarker.activeSelf!=(i==2))throw new System.Exception("state presentation");
                    if(i>0) nest.GetComponent<EggNestPoint>().SetEggPreview(Resources.Load<EggHatchingCatalog>("Eggs/EggHatchingCatalog").Definitions[0].eggPreviewPrefab);
                    foreach(var t in nest.GetComponentsInChildren<Transform>(true))t.gameObject.layer=31;
                }
                var c=new GameObject("Nest camera",typeof(Camera));c.transform.SetParent(root.transform);var cam=c.GetComponent<Camera>();cam.scene=scene;cam.cullingMask=1<<31;
                cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.14f,.18f,.21f);cam.orthographic=true;cam.orthographicSize=2;
                cam.transform.position=new Vector3(0,4,-6);cam.transform.LookAt(Vector3.zero);
                var l=new GameObject("Nest light",typeof(Light));l.transform.SetParent(root.transform);l.transform.rotation=Quaternion.Euler(65,20,0);l.GetComponent<Light>().type=LightType.Directional;l.GetComponent<Light>().intensity=1.7f;
                var rt=new RenderTexture(1200,700,24);var previous=RenderTexture.active;var tex=new Texture2D(1200,700,TextureFormat.RGB24,false);
                try{cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1200,700),0,0);tex.Apply();System.IO.File.WriteAllBytes("Logs/EggNestStatesPreview.png",tex.EncodeToPNG());}
                finally{RenderTexture.active=previous;cam.targetTexture=null;Object.DestroyImmediate(tex);Object.DestroyImmediate(rt);}
                return "PASS: empty/incubating/ready visuals; preview Logs/EggNestStatesPreview.png";
            }
            finally{UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);}
        }
        public static string Install()
        {
            if (EditorApplication.isPlaying) throw new System.Exception("Edit Mode required");
            const string folder = "Assets/Prefabs/EggSystem/Art";
            if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/Prefabs/EggSystem", "Art");
            var vertices = new List<Vector3>(); var triangles = new List<int>();
            void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
            {
                int n = vertices.Count; vertices.AddRange(new[] {a,b,c,d});
                triangles.AddRange(new[] {n,n+1,n+2,n,n+2,n+3});
            }
            Vector3 At(int i, float radius, float y) { float a = i * Mathf.PI * 2 / 16; return new Vector3(Mathf.Cos(a)*radius,y,Mathf.Sin(a)*radius); }
            // Bowl rim, inner slope, outer side; low polygon reed bundles across the rim.
            for (int i=0;i<16;i++)
            {
                Quad(At(i,.74f,.04f),At(i,.68f,.16f),At(i+1,.68f,.16f),At(i+1,.74f,.04f));
                Quad(At(i,.68f,.16f),At(i,.41f,.12f),At(i+1,.41f,.12f),At(i+1,.68f,.16f));
                Quad(At(i,.41f,.12f),At(i,.30f,.02f),At(i+1,.30f,.02f),At(i+1,.41f,.12f));
                Quad(Vector3.up*.02f,At(i+1,.30f,.02f),At(i,.30f,.02f),Vector3.up*.02f);
                var a=At(i,.55f,.17f);var tangent=(At(i+1,.55f,.17f)-a).normalized;
                var side=Vector3.Cross(Vector3.up,tangent)*.025f;
                Quad(a-tangent*.22f-side,a+tangent*.22f-side,a+tangent*.22f+side,a-tangent*.22f+side);
            }
            var mesh = new Mesh {name="Low polygon straw nest"}; mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
            Save(mesh,folder+"/NestBowl.asset");
            vertices.Clear();triangles.Clear();
            for(int i=0;i<16;i++) Quad(At(i,.78f,.025f),At(i,.75f,.025f),At(i+1,.75f,.025f),At(i+1,.78f,.025f));
            var ring=new Mesh {name="Nest state ring"};ring.SetVertices(vertices);ring.SetTriangles(triangles,0);ring.RecalculateNormals();ring.RecalculateBounds();Save(ring,folder+"/StateRing.asset");
            var straw=new Material(Shader.Find("Standard")) {name="Nest straw"};straw.color=new Color(.58f,.34f,.12f);straw.SetFloat("_Glossiness",0);Save(straw,folder+"/Straw.mat");
            var marker=new Material(Shader.Find("Unlit/Color")) {name="Nest state"};marker.color=Color.white;Save(marker,folder+"/State.mat");
            string path="Assets/Prefabs/EggSystem/Nest.prefab";var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                if(root.transform.Find("StrawNest")) throw new System.Exception("Nest art already installed");
                // Keep the original physical platform/collider and all anchors/actions.
                root.transform.Find("Cube").GetComponent<Renderer>().enabled=false;
                GameObject Part(string name, Mesh m, Material mat)
                {
                    var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(root.transform,false);
                    go.GetComponent<MeshFilter>().sharedMesh=m;go.GetComponent<MeshRenderer>().sharedMaterial=mat;return go;
                }
                Part("StrawNest",AssetDatabase.LoadAssetAtPath<Mesh>(folder+"/NestBowl.asset"),AssetDatabase.LoadAssetAtPath<Material>(folder+"/Straw.mat"));
                var status=Part("StateRing",AssetDatabase.LoadAssetAtPath<Mesh>(folder+"/StateRing.asset"),AssetDatabase.LoadAssetAtPath<Material>(folder+"/State.mat"));
                var ready=GameObject.CreatePrimitive(PrimitiveType.Cube);ready.name="Ready marker";ready.transform.SetParent(root.transform,false);
                ready.transform.localPosition=new Vector3(.70f,.35f,-.25f);ready.transform.localRotation=Quaternion.Euler(0,0,45);ready.transform.localScale=Vector3.one*.15f;
                Object.DestroyImmediate(ready.GetComponent<Collider>());ready.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(folder+"/State.mat");
                var visual=root.AddComponent<EggNestPresentation>();visual.statusRing=status.GetComponent<Renderer>();visual.readyMarker=ready;visual.ApplyState(0);
                PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            finally {PrefabUtility.UnloadPrefabContents(root);}
            AssetDatabase.SaveAssets();return "nest presentation installed; anchors/storage/colliders retained";
        }
        private static void Save(Object value,string path)
        {
            var existing=AssetDatabase.LoadMainAssetAtPath(path);
            if(existing) {EditorUtility.CopySerialized(value,existing);Object.DestroyImmediate(value);} else AssetDatabase.CreateAsset(value,path);
        }
    }
}
