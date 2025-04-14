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
/*    private void OnEnable()
    {
        LocationSpawner.Change += Change;
    }
    private void OnDisable()
    {
        LocationSpawner.Change -= Change;
    }
    private void Change()
    {
        navMeshSurface.UpdateNavMesh(navMeshSurface.navMeshData);
    }*/
    // Если геометрия меняется, можно периодически обновлять навмеш:
    // void Update() { if(есть изменения) navMeshSurface.UpdateNavMesh(navMeshSurface.navMeshData); }
}
