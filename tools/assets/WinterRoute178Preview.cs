using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
public static class WinterRoute178Preview
{
 static void BakeLabel(UnityEngine.UI.Text text,Transform parent){var o=new GameObject("Preview_Sign_Text");o.transform.SetParent(parent,false);o.transform.localPosition=new Vector3(0,0,-.12f);var tm=o.AddComponent<TextMesh>();tm.font=text.font;tm.text=text.text;tm.fontSize=80;tm.characterSize=1;tm.anchor=TextAnchor.MiddleCenter;tm.alignment=TextAlignment.Center;tm.color=Color.white;var rr=o.GetComponent<MeshRenderer>();rr.sharedMaterial=text.font.material;rr.sortingOrder=1000;var b=rr.localBounds.size;float scale=b.x>.01f&&b.y>.01f?Mathf.Min(text.rectTransform.rect.width*.92f/b.x,text.rectTransform.rect.height*.8f/b.y):text.rectTransform.rect.height*.6f;o.transform.localScale=Vector3.one*scale;} static Transform Clone(Transform src,Transform parent){var canvas=src.GetComponent<Canvas>();if(canvas&&!src.GetComponentsInChildren<LocalizedText>(true).Any(t=>t.SelectedKey!=null&&t.SelectedKey.StartsWith("Castle/")))return null;bool rect=src is RectTransform;var go=rect?new GameObject(src.name,typeof(RectTransform)):new GameObject(src.name);var t=go.transform;t.SetParent(parent,false);if(rect){var a=(RectTransform)src;var b=(RectTransform)t;b.anchorMin=a.anchorMin;b.anchorMax=a.anchorMax;b.pivot=a.pivot;b.sizeDelta=a.sizeDelta;}t.localPosition=src.localPosition;t.localRotation=src.localRotation;t.localScale=src.localScale;var mf=src.GetComponent<MeshFilter>();var r=src.GetComponent<MeshRenderer>();if(mf&&r&&r.enabled&&mf.sharedMesh!=null){t.gameObject.AddComponent<MeshFilter>().sharedMesh=mf.sharedMesh;t.gameObject.AddComponent<MeshRenderer>().sharedMaterials=r.sharedMaterials;}if(canvas){var c=go.AddComponent<Canvas>();EditorUtility.CopySerialized(canvas,c);c.renderMode=RenderMode.WorldSpace;c.worldCamera=null;}foreach(var graphic in src.GetComponents<UnityEngine.UI.Graphic>()){var dst=(UnityEngine.UI.Graphic)go.AddComponent(graphic.GetType());EditorUtility.CopySerialized(graphic,dst);dst.raycastTarget=false;if(graphic is UnityEngine.UI.Text label){dst.enabled=false;BakeLabel(label,t);}}foreach(Transform c in src)if(c.gameObject.activeSelf)Clone(c,t);return t;}
    const string Folder = "Assets/Events/NewYear/Route178/";
    const string Preview = "Docs/locations/previews/event-winter178-";

    static void Place(Transform parent, string prefab, Vector3 position, float yaw)
    {
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + prefab + ".prefab");
        var copy = Clone(source.transform, parent);
        copy.localPosition = position;
        copy.localRotation = Quaternion.Euler(0, yaw, 0);
    }

    static void Plane(Transform root, string name, Vector3 center, Vector3 scale, Material material)
    {
        var plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        plane.name = name;
        SceneManager.MoveGameObjectToScene(plane, root.gameObject.scene);
        plane.transform.SetParent(root, false);
        plane.transform.localPosition = center;
        plane.transform.localScale = scale;
        plane.GetComponent<MeshRenderer>().sharedMaterial = material;
    }

    static void Render(Scene scene, Camera camera, string view, Vector3 position, Vector3 target)
    {
        camera.transform.position = position;
        camera.transform.LookAt(target);
        foreach (var canvas in scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<Canvas>()))
            canvas.worldCamera = camera;
        Canvas.ForceUpdateCanvases();
        var output = new RenderTexture(1500, 950, 24);
        var pixels = new Texture2D(1500, 950, TextureFormat.RGB24, false);
        var previous = RenderTexture.active;
        try
        {
            camera.targetTexture = output;
            camera.Render();
            RenderTexture.active = output;
            pixels.ReadPixels(new Rect(0, 0, 1500, 950), 0, 0);
            pixels.Apply();
            File.WriteAllBytes(Preview + view + ".png", pixels.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = null;
            RenderTexture.active = previous;
            Object.DestroyImmediate(output);
            Object.DestroyImmediate(pixels);
        }
    }

    public static void Main()
    {
        var scene = EditorSceneManager.NewPreviewScene();
        var water = new Material(Shader.Find("Standard")) { color = new Color(.10f,.38f,.54f), name = "Water_Preview" };
        try
        {
            var root = new GameObject("Winter route");
            SceneManager.MoveGameObjectToScene(root, scene);
            var go = new GameObject("Camera");
            SceneManager.MoveGameObjectToScene(go, scene);
            var camera = go.AddComponent<Camera>();
            camera.scene = scene;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.40f,.56f,.68f);
            camera.fieldOfView = 45;
            var sun = new GameObject("Sun");
            SceneManager.MoveGameObjectToScene(sun, scene);
            var light = sun.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.0f;
            sun.transform.rotation = Quaternion.Euler(46,-32,0);
            var fillObject = new GameObject("Fill");
            SceneManager.MoveGameObjectToScene(fillObject, scene);
            var fill = fillObject.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.intensity = .27f;
            fillObject.transform.rotation = Quaternion.Euler(25,140,0);

            var snow = AssetDatabase.LoadAssetAtPath<Material>(Folder + "Snowy_Riverbanks.mat");
            Plane(root.transform,"West bank",new Vector3(-51.75f,0,70),new Vector3(9.65f,1,21),snow);
            Plane(root.transform,"East bank",new Vector3(51.75f,0,70),new Vector3(9.65f,1,21),snow);
            Plane(root.transform,"River",new Vector3(0,-.65f,70),new Vector3(.70f,1,21),water);
            Place(root.transform,"Bank_Firs_And_Gifts",new Vector3(12,0,28),-22);
            Place(root.transform,"Bank_Single_Fir",new Vector3(-13,0,40),56);
            Place(root.transform,"Bank_Snow_And_Gifts",new Vector3(-12,0,18),29);
            Place(root.transform,"Bank_Snowdrifts",new Vector3(11,0,15),-12);
            Place(root.transform,"Bank_Snowdrifts",new Vector3(-21,0,28),66);
            Place(root.transform,"Bank_Single_Fir",new Vector3(18,0,58),4);
            Place(root.transform,"Bank_Snowdrifts",new Vector3(-20,0,57),-21);
            Render(scene,camera,"banks",new Vector3(-.7f,5.4f,2.5f),new Vector3(.2f,1.1f,30));
            root.SetActive(false);

            var castle = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Events/NewYear/Castle174/EventCastle_Winter.prefab");
            var court = new GameObject("Winter castle");
            SceneManager.MoveGameObjectToScene(court, scene);
            Clone(castle.transform, court.transform);
            Render(scene,camera,"shrubs",new Vector3(-5.0f,1.85f,-10.4f),new Vector3(-8.0f,.64f,-7.0f));
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(scene);
            Object.DestroyImmediate(water);
        }
    }
}
