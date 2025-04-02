using Unity.AI.Navigation;
using UnityEngine;

[RequireComponent(typeof(NavMeshSurface))]
public class DynamicNavMeshUpdater : MonoBehaviour
{
    private NavMeshSurface navMeshSurface;

    void Awake()
    {
        navMeshSurface = GetComponent<NavMeshSurface>();
    }

    void Start()
    {
        // Строим навмеш в начале
        navMeshSurface.BuildNavMesh();
    }

    // Если геометрия меняется, можно периодически обновлять навмеш:
    // void Update() { if(есть изменения) navMeshSurface.UpdateNavMesh(navMeshSurface.navMeshData); }
}
