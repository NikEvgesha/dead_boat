using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class CheckTreeHouse187
{
    static Vector3 Compact(Vector3 point)
    {
        float y = .2f + (Mathf.Min(point.y, 17.45f) - .2f) * .66f;
        y += Mathf.Max(0, Mathf.Min(point.y, 28f) - 17.45f) * .98f;
        return new Vector3(-57 + (point.x + 57) * .8f, y, 9 + (point.z - 9) * .8f);
    }

    public static string Main()
    {
        var scene = EditorSceneManager.NewPreviewScene();
        try
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Workshop/TreeHousePrototypes/TreeHouse_Original_Refined.prefab");
            var house = (GameObject)PrefabUtility.InstantiatePrefab(asset, scene);
            foreach (var component in house.GetComponentsInChildren<MonoBehaviour>()) if (component) component.enabled = false;
            var offset = new Vector3(0,0,350);
            house.transform.position += offset;
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            SceneManager.MoveGameObjectToScene(ground,scene);
            ground.transform.position = new Vector3(-57,-.3f,15) + offset;
            ground.transform.localScale = new Vector3(70,1,70);

            var probe = new GameObject("Player controller probe");
            SceneManager.MoveGameObjectToScene(probe,scene);
            probe.transform.position = house.transform.Find("Workshop_Walk_Anchors/Start").position;
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
                new Vector3(-58.67f,21.50f,11.75f), new Vector3(-58.67f,21.74f,8f), new Vector3(-58.67f,21.74f,4f), new Vector3(-61.9f,21.74f,1f), new Vector3(-62.34f,21.74f,-2f)
            };
            var report = new System.Text.StringBuilder();
            foreach (var point in route.Select(Compact))
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
            var closure = house.GetComponentsInChildren<BoxCollider>().Single(c => c.name == "Upper_Room_Closed_Far_Door");
            var inside = Compact(new Vector3(-62.34f,22.8f,-2.2f)) + offset;
            report.AppendLine("Rear doorway closed=" + closure.Raycast(new Ray(inside,Vector3.back),out _,3f));
            return report.ToString();
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }
}

