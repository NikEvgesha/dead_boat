using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class AddBackedChair195
{
    static void Board(Transform chair, string name, Vector3 position, Vector3 size, Material wood)
    {
        var piece=GameObject.CreatePrimitive(PrimitiveType.Cube);
        SceneManager.MoveGameObjectToScene(piece,chair.gameObject.scene);
        piece.name=name;
        piece.transform.SetParent(chair,false);
        piece.transform.localPosition=position;
        piece.transform.localScale=size;
        piece.GetComponent<Renderer>().sharedMaterial=wood;
        piece.GetComponent<BoxCollider>().enabled=false;
    }

    public static string Main()
    {
        const string path="Assets/Prefabs/Workshop/TreeHousePrototypes/TreeHouse_Original_Refined.prefab";
        var root=PrefabUtility.LoadPrefabContents(path);
        try
        {
            var group=root.transform.Find("Upper_Room_Furniture");
            var old=group.Find("Table_Chair");
            var collider=group.Cast<Transform>().FirstOrDefault(t=>t.name=="Furniture_Collider" &&
                Vector2.Distance(new Vector2(t.position.x,t.position.z),new Vector2(-64.64f,2.95f))<.1f);
            if(collider) UnityEngine.Object.DestroyImmediate(collider.gameObject);
            UnityEngine.Object.DestroyImmediate(old.gameObject);

            var chair=new GameObject("Table_Chair");
            SceneManager.MoveGameObjectToScene(chair,root.scene);
            chair.transform.SetParent(group,true);
            chair.transform.SetPositionAndRotation(new Vector3(-64.64f,9.62f,2.95f),Quaternion.Euler(0,5,0));
            var dark=AssetDatabase.LoadAssetAtPath<Material>("Assets/Prefabs/Workshop/CottageGeometry/Roof_InteriorBrown.mat");
            var warm=AssetDatabase.LoadAssetAtPath<Material>("Assets/Prefabs/Workshop/CottageGeometry/Roof_WarmBrown.mat");
            Board(chair.transform,"Seat",new Vector3(0,.46f,0),new Vector3(.59f,.10f,.56f),warm);
            foreach(float x in new[]{-.22f,.22f})
            {
                Board(chair.transform,"Front leg",new Vector3(x,.21f,-.21f),new Vector3(.105f,.42f,.105f),dark);
                Board(chair.transform,"Back leg and upright",new Vector3(x,.49f,.21f),new Vector3(.105f,.98f,.105f),dark);
            }
            Board(chair.transform,"Backrest",new Vector3(0,.87f,.214f),new Vector3(.52f,.18f,.10f),warm);
            var solid=chair.AddComponent<BoxCollider>();
            solid.center=new Vector3(0,.49f,0);
            solid.size=new Vector3(.59f,.98f,.56f);
            if(!PrefabUtility.SaveAsPrefabAsset(root,path)) throw new InvalidOperationException("Save failed");
            return "Wooden chair with a backrest saved";
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }
}
