using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class CheckTreeHouse185
{
    public static string Main()
    {
        var scene = EditorSceneManager.NewPreviewScene();
        try
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Workshop/TreeHousePrototypes/TreeHouse_Original_Refined.prefab");
            var house = (GameObject)PrefabUtility.InstantiatePrefab(asset, scene);
            var offset = new Vector3(0,0,350);
            house.transform.position += offset;
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            SceneManager.MoveGameObjectToScene(ground,scene);
            ground.transform.position = new Vector3(-57,-.3f,15) + offset;
            ground.transform.localScale = new Vector3(70,1,70);

            var probe = new GameObject("Player controller probe");
            SceneManager.MoveGameObjectToScene(probe,scene);
            probe.transform.position = new Vector3(-56.645f,.25f,20.6f) + offset;
            var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab").GetComponentInChildren<CharacterController>();
            var controller = probe.AddComponent<CharacterController>();
            controller.center = source.center;
            controller.radius = source.radius;
            controller.height = source.height;
            controller.slopeLimit = source.slopeLimit;
            controller.stepOffset = source.stepOffset;
            controller.skinWidth = source.skinWidth;
            Physics.SyncTransforms();
            var route = new[] {
                new Vector3(-56.645f,5.5f,11.25f), new Vector3(-52.60f,5.5f,11.25f),
                new Vector3(-52.60f,9.55f,18.10f), new Vector3(-56.645f,9.55f,18.10f),
                new Vector3(-56.645f,13.60f,11.25f), new Vector3(-52.60f,13.60f,11.25f),
                new Vector3(-52.60f,17.70f,18.10f), new Vector3(-58.67f,17.70f,18.10f),
                new Vector3(-58.67f,21.50f,11.75f)
            };
            var report = new System.Text.StringBuilder();
            foreach (var point in route)
            {
                for (int step=0; step<600; step++)
                {
                    var delta = point+offset-probe.transform.position;
                    delta.y=0;
                    if (delta.magnitude<.09f) break;
                    controller.Move(Vector3.ClampMagnitude(delta,.055f) + Vector3.down*.015f);
                }
                var reached = probe.transform.position-offset;
                report.AppendLine("Target " + point + " reached " + reached + " grounded=" + controller.isGrounded);
                if (Vector2.Distance(new Vector2(reached.x,reached.z),new Vector2(point.x,point.z))>.18f ||
                    Mathf.Abs(reached.y-point.y)>.5f || !controller.isGrounded)
                    break;
            }
            return report.ToString();
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }
}
