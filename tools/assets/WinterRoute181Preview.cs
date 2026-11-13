using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class WinterRoute181Preview
{
    const string Folder = "Assets/Events/NewYear/Route181/";
    static readonly string[] Props =
    {
        "Bank_01_Snow_Tunnel", "Bank_02_Rooted_Log", "Bank_03_Frozen_Grotto",
        "Bank_05_Gift_Sled", "Bank_06_Fir_Arch", "Bank_07_Ice_Crystal_Field"
    };
    static readonly Vector3[] Views =
    {
        new Vector3(-2.05f,2.85f,-12.2f),new Vector3(-2.53f,2.15f,-10.8f),new Vector3(2.55f,2.85f,-12.8f),
        new Vector3(-4.2f,2.15f,3.65f),new Vector3(-.63f,2.15f,-10.25f),new Vector3(-1.15f,2.87f,-10.65f)
    };
    static readonly float[] TargetHeights = {1.85f,1.46f,1.92f,.58f,1.57f,1.25f};

    static Transform Clone(Transform source, Transform parent)
    {
        var clone = new GameObject(source.name).transform;
        clone.SetParent(parent,false);
        clone.localPosition=source.localPosition;
        clone.localRotation=source.localRotation;
        clone.localScale=source.localScale;
        var filter=source.GetComponent<MeshFilter>();
        var renderer=source.GetComponent<MeshRenderer>();
        if (filter && renderer && renderer.enabled)
        {
            clone.gameObject.AddComponent<MeshFilter>().sharedMesh=filter.sharedMesh;
            clone.gameObject.AddComponent<MeshRenderer>().sharedMaterials=renderer.sharedMaterials;
        }
        foreach(Transform child in source) if(child.gameObject.activeSelf) Clone(child,clone);
        return clone;
    }

    static GameObject InScene(string name, Scene scene)
    {
        var go = new GameObject(name);
        SceneManager.MoveGameObjectToScene(go,scene);
        return go;
    }

    public static string Render()
    {
        var scene=EditorSceneManager.NewPreviewScene();
        var distantIce = new Material(Shader.Find("Standard")) { color = new Color(.35f,.71f,.76f) };
        try
        {
            var stage=InScene("Winter props",scene).transform;
            var snow = GameObject.CreatePrimitive(PrimitiveType.Plane);
            SceneManager.MoveGameObjectToScene(snow,scene);
            snow.transform.localScale=new Vector3(16,1,16);
            snow.GetComponent<MeshRenderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Events/NewYear/Route178/Snowy_Riverbanks.mat");
            var rock=AssetDatabase.LoadAssetAtPath<Mesh>(Folder+"Faceted_Boulder.asset");
            foreach(var point in new[]{new Vector3(-29f,6f,28f),new Vector3(-5.8f,11f,51f),new Vector3(33f,7f,48f)})
            {
                var cliff=InScene("Distant cliff",scene);
                cliff.transform.position=point;
                cliff.transform.localScale=new Vector3(26f,25f,29f);
                cliff.AddComponent<MeshFilter>().sharedMesh=rock;
                cliff.AddComponent<MeshRenderer>().sharedMaterial=distantIce;
            }
            var sun=InScene("Sun",scene).AddComponent<Light>();
            sun.type=LightType.Directional;
            sun.intensity=1.05f;
            sun.shadows=LightShadows.Soft;
            sun.transform.rotation=Quaternion.Euler(48,-42,0);
            var fill=InScene("Fill",scene).AddComponent<Light>();
            fill.type=LightType.Directional;
            fill.intensity=.48f;
            fill.transform.rotation=Quaternion.Euler(23,19,0);
            var camera=InScene("Camera",scene).AddComponent<Camera>();
            camera.scene=scene;
            camera.fieldOfView=48;
            camera.clearFlags=CameraClearFlags.SolidColor;
            camera.backgroundColor=new Color(.30f,.76f,.84f);
            var output=new RenderTexture(1280,840,24);
            var pixels=new Texture2D(1280,840,TextureFormat.RGB24,false);
            var previous=RenderTexture.active;
            try
            {
                camera.targetTexture=output;
                for(int i=0;i<Props.Length;i++)
                {
                    var prop=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+Props[i]+".prefab");
                    var clone=Clone(prop.transform,stage);
                    camera.transform.position=Views[i];
                    camera.transform.LookAt(Vector3.up*TargetHeights[i]);
                    camera.Render();
                    RenderTexture.active=output;
                    pixels.ReadPixels(new Rect(0,0,1280,840),0,0);
                    pixels.Apply();
                    File.WriteAllBytes("Docs/locations/previews/event-winter181-"+Props[i].Substring(5,2)+".png",pixels.EncodeToPNG());
                    Object.DestroyImmediate(clone.gameObject);
                }
            }
            finally
            {
                camera.targetTexture=null;
                RenderTexture.active=previous;
                Object.DestroyImmediate(output);
                Object.DestroyImmediate(pixels);
            }
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(scene);
            Object.DestroyImmediate(distantIce);
        }
        return "Six Unity prefab renders.";
    }
}

