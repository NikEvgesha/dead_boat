using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-10000)]
public sealed class EventSystemGuard : MonoBehaviour
{
    private const string EventSystemName = "EventSystem";
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

    private void Update()
    {
        EnsureSingleEventSystem();
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

        if (eventSystems.Length == 0)
        {
            CreateEventSystem();
            return;
        }

        EventSystem keep = eventSystems.FirstOrDefault(IsSceneEventSystem)
            ?? eventSystems.FirstOrDefault(eventSystem => eventSystem.isActiveAndEnabled)
            ?? eventSystems[0];

        EnsureInputModule(keep);
        EventSystem.current = keep;

        if (eventSystems.Length == 1)
            return;

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

    private static EventSystem CreateEventSystem()
    {
        GameObject eventSystemObject = new GameObject(EventSystemName);
        EventSystem eventSystem = eventSystemObject.AddComponent<EventSystem>();
        StandaloneInputModule inputModule = eventSystemObject.AddComponent<StandaloneInputModule>();
        EnsureSafeInput(eventSystem, inputModule);
        EventSystem.current = eventSystem;
        return eventSystem;
    }

    private static void EnsureInputModule(EventSystem eventSystem)
    {
        if (eventSystem == null)
            return;

        StandaloneInputModule standaloneInputModule = eventSystem.GetComponent<StandaloneInputModule>();
        if (eventSystem.GetComponent<BaseInputModule>() == null)
            standaloneInputModule = eventSystem.gameObject.AddComponent<StandaloneInputModule>();

        if (standaloneInputModule != null)
            EnsureSafeInput(eventSystem, standaloneInputModule);
    }

    private static void EnsureSafeInput(EventSystem eventSystem, StandaloneInputModule standaloneInputModule)
    {
        SafeEventSystemInput safeInput = eventSystem.GetComponent<SafeEventSystemInput>();
        if (safeInput == null)
            safeInput = eventSystem.gameObject.AddComponent<SafeEventSystemInput>();

        if (standaloneInputModule.inputOverride != safeInput)
            standaloneInputModule.inputOverride = safeInput;
    }
}
