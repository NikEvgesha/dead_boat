using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class BuildShopChest086 {
    const string Dir="Assets/Prefabs/Workshop/Supplies/";
    static Material oak,dark,brass,green,cream;
    static Transform Group(string n,Transform p){var t=new GameObject(n).transform;t.SetParent(p,false);return t;}
    static GameObject Box(string n,Transform p,Vector3 v,Vector3 s,Material m,bool solid=false){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=n;g.transform.SetParent(p,false);g.transform.localPosition=v;g.transform.localScale=s;g.GetComponent<Renderer>().sharedMaterial=m;if(!solid)UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());return g;}
    static Material Mat(string n,Color c){var m=AssetDatabase.LoadAssetAtPath<Material>(Dir+n+".mat");if(m!=null)return m;m=new Material(Shader.Find("Standard")){name=n,color=c};m.SetFloat("_Glossiness",.12f);AssetDatabase.CreateAsset(m,Dir+n+".mat");return m;}
    static Bounds B(Transform t){var rs=t.GetComponentsInChildren<Renderer>().Where(r=>r is MeshRenderer||r is SkinnedMeshRenderer).ToArray();var b=rs[0].bounds;foreach(var r in rs.Skip(1))b.Encapsulate(r.bounds);return b;}
    static void Fit(Transform t,Transform h,Vector3 target,float height){var b=B(t);t.localScale*=height/b.size.y;b=B(t);t.position+=h.TransformPoint(target)-new Vector3(b.center.x,b.min.y,b.center.z);}
    static void Record(Transform t){PrefabUtility.RecordPrefabInstancePropertyModifications(t);}
    static Transform Prop(string path,Transform h,Transform parent,string name,Vector3 p,float height){var g=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path),parent);g.name=name;foreach(var c in g.GetComponentsInChildren<Component>(true)){if(c==null)throw new Exception("Decor source contains missing script");if(c is Transform||c is Renderer||c is MeshFilter)continue;UnityEngine.Object.DestroyImmediate(c);}Fit(g.transform,h,p,height);return g.transform;}
    static void Rug(string n,Transform p,Vector3 pos,float width,float depth,bool pattern){var g=Group(n,p);g.localPosition=pos;Box("Woven_Green_Cloth",g,Vector3.zero,new Vector3(width,.012f,depth),green);foreach(float s in new[]{-1f,1f}){Box("Cream_Border",g,new Vector3(s*(width*.5f-.07f),.009f,0),new Vector3(.045f,.006f,depth-.09f),cream);Box("Cream_Border",g,new Vector3(0,.009f,s*(depth*.5f-.07f)),new Vector3(width-.09f,.006f,.045f),cream);}if(pattern)foreach(float z in new[]{-.65f,0,.65f}){var d=Box("Woven_Diamond",g,new Vector3(0,.010f,z),new Vector3(.32f,.006f,.32f),cream);d.transform.localRotation=Quaternion.Euler(0,45,0);var c=Box("Green_Diamond",g,new Vector3(0,.014f,z),new Vector3(.20f,.004f,.20f),dark);c.transform.localRotation=Quaternion.Euler(0,45,0);}}
    public static string Main(){
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.path!="Assets/Scenes/CastleServicesWorkshop.unity"||scene.isDirty||EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Saved castle Edit Mode required");
        if(!File.Exists("Library/WorkshopBackups/DB-ON-086/OwnerSaved.unity"))throw new Exception("Owner checkpoint missing");
        var hs=GameObject.Find("Castle_Services_Prototype").transform.Find("03_Service_Houses");var shop=hs.Find("01_Supplies");var trader=hs.Find("03_LootTrader");if(shop.Find("25_Supplies_Counters")!=null)throw new Exception("Already applied");
        var room=shop.Find("16_Run_Interior");var tradeRoom=trader.Find("16_Run_Interior");var points=shop.Find("15_Gameplay_Source/StorePoints").Cast<Transform>().Where(t=>t.name.StartsWith("storePoint")).ToArray();if(points.Length!=3)throw new Exception("Expected three native points");
        Directory.CreateDirectory(Dir);AssetDatabase.Refresh();oak=tradeRoom.Find("Payment_Counter").GetComponent<Renderer>().sharedMaterial;dark=AssetDatabase.LoadAssetAtPath<Material>("Assets/Prefabs/Workshop/Armory/Armory_DarkOak.mat");brass=AssetDatabase.LoadAssetAtPath<Material>("Assets/Prefabs/Workshop/Sheriff/Sheriff_LockBrass.mat");green=Mat("Supplies_ForestCloth",new Color(.15f,.31f,.24f));cream=Mat("Supplies_WovenCream",new Color(.80f,.70f,.46f));
        // Reuse the unwanted treasury chest as the finds intake. Native sale/reward objects stay intact.
        var chest=tradeRoom.Find("Treasury_Chest");Undo.RecordObject(chest,"Move chest to finds intake");chest.name="Finds_Delivery_Chest";chest.localRotation=Quaternion.identity;Fit(chest,trader,new Vector3(1.65f,.48f,.35f),1.0f);
        foreach(var r in chest.GetComponentsInChildren<Renderer>())r.sharedMaterials=r.sharedMaterials.Select(m=>oak).ToArray();
        var cd=Group("24_Finds_Chest_Detail",trader);var cb=B(chest);var lc=trader.InverseTransformPoint(cb.center); // Add warm ironwork to match the reward counter.
        Box("Chest_Label_Panel",cd,new Vector3(1.65f,1.02f,-.35f),new Vector3(1.18f,.30f,.045f),dark);
        var label=tradeRoom.Find("Loot_Hint");label.localPosition=new Vector3(1.65f,1.02f,-.378f);Record(label);
        tradeRoom.Find("Loot_Counter").gameObject.SetActive(false);
        foreach(Transform t in trader.Find("22_Trader_Detail"))if((t.name=="Counter_Top"||t.name=="Counter_Name_Panel"||t.name=="Brass_Corner")&&t.localPosition.x>0)t.gameObject.SetActive(false);
        // All three native slots remain; ItemStore still supplies names/prices and its original assortment.
        foreach(var stand in room.Cast<Transform>().Where(t=>t.name=="Merchandise_Stand"))stand.gameObject.SetActive(false);
        var previews=room.Find("EditorOnly_StockPreview");previews.gameObject.SetActive(false);
        var counters=Group("25_Supplies_Counters",shop);var decor=Group("26_Supplies_Decor",shop);var rugs=Group("27_Supplies_Pickup_Mats",shop);
        Vector3[] centers={new Vector3(-2.8f,0,1.15f),new Vector3(0,0,2.70f),new Vector3(2.8f,0,1.15f)};
        for(int i=0;i<3;i++){
            var p=centers[i];var bay=Group("Supply_Counter_"+(i+1),counters);bay.localPosition=p;
            Box("Oak_Counter",bay,new Vector3(0,.87f,0),new Vector3(1.55f,.78f,1.18f),oak,true);
            Box("Dark_Counter_Top",bay,new Vector3(0,1.30f,0),new Vector3(1.67f,.09f,1.30f),dark);
            Box("Counter_Name_Panel",bay,new Vector3(0,1.01f,-.613f),new Vector3(1.32f,.32f,.035f),dark);
            foreach(float s in new[]{-1f,1f})Box("Brass_Corner",bay,new Vector3(s*.71f,.88f,-.633f),new Vector3(.04f,.70f,.04f),brass);
            var drop=new Vector3(p.x,.49f,p.z-1.45f);Rug("Supply_Pickup_"+(i+1),rugs,drop,2.00f,1.20f,false);
            var point=points[i];point.localPosition=new Vector3(p.x,.48f,p.z);point.localRotation=Quaternion.identity;Record(point);
            var trigger=point.GetComponent<BoxCollider>();trigger.center=new Vector3(0,.85f,-1.45f);trigger.size=new Vector3(2.00f,1.70f,1.20f);PrefabUtility.RecordPrefabInstancePropertyModifications(trigger);
            var buy=point.GetComponentsInChildren<Transform>(true).First(t=>t.name=="buyPoint");buy.position=shop.TransformPoint(new Vector3(drop.x,.85f,drop.z));buy.rotation=shop.rotation;buy.localScale=Vector3.one;Record(buy);
            var display=point.GetComponentsInChildren<Transform>(true).First(t=>t.name=="SellPoint");display.position=shop.TransformPoint(new Vector3(p.x,1.36f,p.z));display.rotation=shop.rotation;display.localScale=Vector3.one*.6f;Record(display);
            foreach(var canvas in point.GetComponentsInChildren<Canvas>(true)){
                if(canvas.name=="Tutor1")continue;canvas.transform.localRotation=Quaternion.identity;canvas.transform.localScale=Vector3.one*(canvas.name=="priceCanvas"?.008f:.006f);canvas.transform.localPosition=new Vector3(0,canvas.name=="priceCanvas"?.53f:.53f,-.65f);Record(canvas.transform);canvas.GetComponent<CanvasScaler>().dynamicPixelsPerUnit=1;
                foreach(var text in canvas.GetComponentsInChildren<Text>(true)){text.resizeTextForBestFit=true;text.resizeTextMinSize=10;text.resizeTextMaxSize=text.name=="itemName"?24:text.fontSize;text.raycastTarget=false;if(text.name=="itemName")text.text="";PrefabUtility.RecordPrefabInstancePropertyModifications(text);}
            }
        }
        Rug("Central_Woven_Runner",decor,new Vector3(0,.49f,-1.05f),1.95f,2.55f,true);
        const string Props="Assets/Import/LowPolyMedievalPropsLite/Prefabs/";
        foreach(float x in new[]{-2.8f,2.8f}){
            Box("Rear_Display_Shelf",decor,new Vector3(x,1.95f,3.38f),new Vector3(1.6f,.08f,.42f),dark);
            foreach(float s in new[]{-1f,1f})Box("Shelf_Bracket",decor,new Vector3(x+s*.58f,1.77f,3.50f),new Vector3(.05f,.38f,.08f),brass);
            Prop(Props+"Jug_01.prefab",shop,decor,"Glazed_Jug",new Vector3(x-.40f,2.00f,3.36f),.38f);
            Prop(Props+"Jug_02.prefab",shop,decor,"Small_Jug",new Vector3(x+.05f,2.00f,3.36f),.28f);
            Prop(Props+"Plate_01.prefab",shop,decor,"Ceramic_Bowl",new Vector3(x+.47f,2.00f,3.36f),.10f);
        }
        foreach(float side in new[]{-1f,1f}){
            var herbs=Group("Hanging_Herbs",decor);herbs.localPosition=new Vector3(side*4.12f,2.55f,2.45f);
            Box("Wooden_Herb_Rack",herbs,Vector3.zero,new Vector3(.07f,.09f,.86f),dark);
            for(int j=0;j<3;j++){float z=(j-1)*.27f;Box("Twine",herbs,new Vector3(-side*.05f,-.12f,z),new Vector3(.02f,.25f,.02f),cream);for(int k=0;k<3;k++){var leaf=GameObject.CreatePrimitive(PrimitiveType.Sphere);leaf.name="Dried_Leaf_Bunch";leaf.transform.SetParent(herbs,false);leaf.transform.localPosition=new Vector3(-side*(.08f+k*.035f),-.30f-k*.06f,z);leaf.transform.localScale=new Vector3(.14f,.24f,.13f);leaf.GetComponent<Renderer>().sharedMaterial=green;UnityEngine.Object.DestroyImmediate(leaf.GetComponent<Collider>());}}
        }
        var light=room.Find("Run_Interior_Light").GetComponent<Light>();light.color=new Color(1,.84f,.66f);light.intensity=1.05f;light.range=6.5f;
        AssetDatabase.SaveAssets();Physics.SyncTransforms();EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new Exception("Save failed");Selection.activeGameObject=shop.gameObject;
        return "Saved: treasury chest reused as finds intake; old intake counter hidden; three shop counters preserve native StorePoints; woven runner, shelf ceramics and hanging herbs added. Gameplay purchase/reward references unchanged.";
    }
}
