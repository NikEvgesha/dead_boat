using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class WinterRoute182Preview
{
    const string ScenePath = "Assets/Scenes/LevelEvent.unity";
    const string Prefix = "Docs/locations/previews/event-winter182-";

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
            var copied=clone.gameObject.AddComponent<MeshRenderer>();
            copied.sharedMaterials=renderer.sharedMaterials;
            copied.receiveShadows=renderer.receiveShadows;
            copied.shadowCastingMode=renderer.shadowCastingMode;
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

    static void Capture(Camera camera, string file, Vector3 position, Vector3 target, float fov)
    {
        camera.transform.position=position;
        camera.transform.LookAt(target);
        camera.fieldOfView=fov;
        var output=new RenderTexture(1440,900,24);
        var pixels=new Texture2D(1440,900,TextureFormat.RGB24,false);
        var previous=RenderTexture.active;
        try
        {
            camera.targetTexture=output;
            camera.Render();
            RenderTexture.active=output;
            pixels.ReadPixels(new Rect(0,0,1440,900),0,0);
            pixels.Apply();
            File.WriteAllBytes(Prefix+file+".png",pixels.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture=null;
            RenderTexture.active=previous;
            Object.DestroyImmediate(output);
            Object.DestroyImmediate(pixels);
        }
    }

    public static string Render()
    {
        var saved=SceneManager.GetSceneByPath(ScenePath);
        bool openedHere=!saved.isLoaded;
        if(openedHere) saved=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);
        var preview=EditorSceneManager.NewPreviewScene();
        var river=new Material(Shader.Find("Standard")) {color=new Color(.12f,.44f,.62f)};
        try
        {
            var route=InScene("Snowy bank segments",preview).transform;
            var land=saved.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<LendController>(true)).Single();
            for(int i=0;i<3;i++)
            {
                var source=land.locations[0].groundSegments[i];
                var clone=Clone(source,route);
                clone.localPosition=new Vector3(source.position.x,source.position.y,i*land.segmentLength);
                clone.localScale=source.lossyScale;
                clone.localRotation=source.rotation;
                foreach(var water in clone.GetComponentsInChildren<MeshRenderer>().Where(r=>r.name=="Water"))
                    water.sharedMaterial=river;
            }
            var sun=InScene("Sun",preview).AddComponent<Light>();
            sun.type=LightType.Directional;
            sun.intensity=1.12f;
            sun.shadows=LightShadows.Soft;
            sun.transform.rotation=Quaternion.Euler(48,-37,0);
            var fill=InScene("Fill",preview).AddComponent<Light>();
            fill.type=LightType.Directional;
            fill.intensity=.53f;
            fill.transform.rotation=Quaternion.Euler(22,33,0);
            var camera=InScene("Camera",preview).AddComponent<Camera>();
            camera.scene=preview;
            camera.farClipPlane=1800;
            camera.clearFlags=CameraClearFlags.SolidColor;
            camera.backgroundColor=new Color(.24f,.76f,.86f);

            Capture(camera,"mountains",new Vector3(0,13,-182),new Vector3(0,38,280),54);

            var tunnel=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Events/NewYear/Route181/Bank_01_Snow_Tunnel.prefab");
            var hill=Clone(tunnel.transform,route);
            hill.localPosition=new Vector3(13.2f,0,94f);
            var fir=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Events/NewYear/Route178/Bank_Decorated_Fir.prefab");
            var marker=Clone(fir.transform,route);
            marker.localPosition=new Vector3(8.62f,0,93.9f);
            marker.localScale=Vector3.one*.87f;
            Capture(camera,"snow-hill",new Vector3(9.4f,2.55f,82.8f),new Vector3(13.65f,2.17f,94.3f),52);
            Capture(camera,"snow-hill-side",new Vector3(5.6f,2.82f,86.3f),new Vector3(13.65f,2.23f,94.1f),57);
            return "Rendered the rebuilt snow hill and the snowy mountains from the LevelEvent scene.";
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(preview);
            Object.DestroyImmediate(river);
            if(openedHere) EditorSceneManager.CloseScene(saved,true);
        }
    }
}


