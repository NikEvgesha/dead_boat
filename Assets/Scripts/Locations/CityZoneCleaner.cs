using UnityEngine;
using System.Collections.Generic;

public class CityZoneCleaner : MonoBehaviour
{
    [Tooltip("Список тегов объектов, которые запрещены в зоне города (например, Location, Decoration)")]
    public List<string> forbiddenTags = new List<string>() { "Location", "Decoration" };

    private void OnTriggerEnter(Collider other)
    {
        if (forbiddenTags.Contains(other.tag))
        {
            Debug.Log($"Объект с тегом {other.tag} вошёл в зону города и будет удалён.");
            Destroy(other.gameObject);
        }
    }
}
