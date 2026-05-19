using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(10000)]
public sealed class EventSystemGuard : MonoBehaviour
{
    private static EventSystemGuard _instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (_instance != null)
            return;

        GameObject guard = new GameObject(nameof(EventSystemGuard));
        DontDestroyOnLoad(guard);
        _instance = guard.AddComponent<EventSystemGuard>();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        EnsureSingleEventSystem();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void LateUpdate()
    {
        EnsureSingleEventSystem();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureSingleEventSystem();
    }

    private static void EnsureSingleEventSystem()
    {
        EventSystem[] eventSystems = FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (eventSystems.Length <= 1)
            return;

        EventSystem keep = eventSystems.FirstOrDefault(IsSceneEventSystem)
            ?? eventSystems.FirstOrDefault(eventSystem => eventSystem.isActiveAndEnabled)
            ?? eventSystems[0];

        EventSystem.current = keep;

        foreach (EventSystem eventSystem in eventSystems)
        {
            if (eventSystem == keep)
                continue;

            Destroy(eventSystem.gameObject);
        }
    }

    private static bool IsSceneEventSystem(EventSystem eventSystem)
    {
        return eventSystem != null
            && eventSystem.gameObject.scene.IsValid()
            && eventSystem.gameObject.scene.name != "DontDestroyOnLoad"
            && eventSystem.gameObject.activeInHierarchy
            && eventSystem.enabled;
    }
}
