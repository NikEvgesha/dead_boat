using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeadBoat.Online.Editor
{
    public static class ProfessionSkinCollectionTools
    {
        private static readonly string[] Order = {"yunga","mechanic","hunter","trader","scout","berserk","vampire","gambler","pyro","tank","glass_cannon","ninja","cursed"};
        private static readonly string[] Labels = {"Юнга","Механик","Охотник","Торговец","Разведчик","Берсерк","Вампир","Азартный игрок","Поджигатель","Танк","Стеклянная пушка","Ниндзя","Проклятый"};
        public static string Validate()
        {
            Require(!EditorApplication.isPlaying && !EditorUtility.scriptCompilationFailed && !BuildPipeline.isBuildingPlayer,"idle compiled Editor");
            var catalog=Resources.Load<ProfessionCatalog>("Professions/ProfessionCatalog");
            var ids=catalog.GetValidDefinitions().Select(d=>d.professionId).ToArray();
            Require(ids.Length==13 && ids.OrderBy(s=>s).SequenceEqual(Order.OrderBy(s=>s)),"complete actual profession catalog");
            Require(ids.Select(AvatarProfessionVisual.SkinFor).Distinct().Count()==ids.Length,"unique wire IDs");
            Require(AvatarProfessionVisual.SkinFor("mechanic")==1 && AvatarProfessionVisual.SkinFor("hunter")==2 && AvatarProfessionVisual.SkinFor("unknown")==0,"stable old IDs and fallback");
            var memory=new List<string>();
            foreach(var id in ids)
            {
                Require(AvatarProfessionVisual.ProfessionFor(AvatarProfessionVisual.SkinFor(id))==id,"ID roundtrip "+id);
                var material=Resources.Load<Material>("OnlineUI/AvatarSkins/"+id);
                Require(material && material.mainTexture,"material/texture "+id);
                var texture=material.mainTexture;
                var importer=(TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture));
                Require(texture.width==1024 && texture.height==1024 && importer.mipmapEnabled && !importer.isReadable && texture.wrapMode==TextureWrapMode.Repeat,"1024/mips/nonreadable/repeat "+id);
                memory.Add(id+": "+texture.width+"x"+texture.height+", Editor bytes="+UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(texture));
            }
            var avatar=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Online/Avatars/LobbyNetworkAvatar.prefab");
            var animator=avatar.GetComponentInChildren<Animator>(true);
            Require(animator && animator.runtimeAnimatorController,"existing animator retained");
            Directory.CreateDirectory("Logs");
            File.WriteAllLines("Logs/ProfessionCollectionAssets.txt",memory);
            return "PASS: all13 actual professions, unique stable IDs/materials, 1024/mips/Repeat/nonreadable, animator retained; Editor memory only";
        }
        public static void ValidateAndCapture()
        {
            Debug.Log("[Profession collection] "+Validate());
            Directory.CreateDirectory("Docs/previews");
            CaptureRange(0,13,"Docs/previews/AllProfessionSkins.png",5);
            CaptureRange(0,13,"Docs/previews/AllProfessionSkinsBack.png",5,true);
            for(int row=0;row<3;row++) CaptureRange(row*5,Math.Min(5,13-row*5),"Docs/previews/ProfessionSkinsRow"+(row+1)+".png",5);
            Debug.Log("[Profession collection] Gallery captured from real mesh");
        }
        private static void CaptureRange(int offset,int count,string path,int columns,bool reverse=false)
        {
            var scene=EditorSceneManager.NewPreviewScene();
            try
            {
                var root=new GameObject("Profession collection");SceneManager.MoveGameObjectToScene(root,scene);
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Online/Avatars/Kenney/character-a.fbx");
                int rows=(count+columns-1)/columns;
                for(int n=0;n<count;n++)
                {
                    int index=offset+n;float x=((columns-1)*.5f-n%columns)*2.1f;float y=(rows-1-n/columns)*3.5f;
                    var model=UnityEngine.Object.Instantiate(prefab,root.transform);model.transform.localPosition=new Vector3(x,y,0);
                    if(reverse)model.transform.localRotation=Quaternion.Euler(0,180,0);
                    var visual=model.AddComponent<AvatarProfessionVisual>();visual.Initialize(model.transform);visual.Apply(AvatarProfessionVisual.SkinFor(Order[index]));
                    var label=new GameObject("Label "+Order[index],typeof(TextMesh));label.transform.SetParent(root.transform,false);
                    label.transform.localPosition=new Vector3(x,y+3.02f,0);label.transform.localRotation=Quaternion.Euler(0,180,0);
                    var text=label.GetComponent<TextMesh>();text.text=Labels[index]+"\n"+Order[index];text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.fontSize=48;text.characterSize=.033f;text.anchor=TextAnchor.MiddleCenter;text.alignment=TextAlignment.Center;text.color=Color.white;
                    label.GetComponent<Renderer>().sharedMaterial=text.font.material;
                }
                foreach(var t in root.GetComponentsInChildren<Transform>(true))t.gameObject.layer=31;
                var c=new GameObject("Gallery camera",typeof(Camera));c.transform.SetParent(root.transform);var cam=c.GetComponent<Camera>();cam.scene=scene;cam.cullingMask=1<<31;
                cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.10f,.14f,.18f);cam.orthographic=true;
                int width=2000,height=rows==1?700:1800;
                cam.orthographicSize=rows==1?1.95f:5.45f;
                var center=new Vector3(0,(rows-1)*1.75f+1.45f,0);cam.transform.position=center+new Vector3(0,1,15);cam.transform.LookAt(center);
                var l=new GameObject("Gallery light",typeof(Light));l.transform.SetParent(root.transform);l.transform.rotation=Quaternion.Euler(35,155,0);var light=l.GetComponent<Light>();light.type=LightType.Directional;light.intensity=.75f;light.cullingMask=1<<31;
                var rt=new RenderTexture(width,height,24);var previous=RenderTexture.active;var png=new Texture2D(width,height,TextureFormat.RGB24,false);
                try{cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;png.ReadPixels(new Rect(0,0,width,height),0,0);png.Apply();File.WriteAllBytes(path,png.EncodeToPNG());}
                finally{RenderTexture.active=previous;cam.targetTexture=null;UnityEngine.Object.DestroyImmediate(png);UnityEngine.Object.DestroyImmediate(rt);}
            }
            finally{EditorSceneManager.ClosePreviewScene(scene);}
        }
        private static void Require(bool ok,string message){if(!ok)throw new InvalidOperationException(message);}
    }
}
