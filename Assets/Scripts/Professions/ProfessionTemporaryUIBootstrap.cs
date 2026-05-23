using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class ProfessionTemporaryUIBootstrap
{
    private const string LobbySceneName = "Lobby";
    private const string RootName = "TemporaryProfessionUI";
    private const string ShortcutListenerName = "ProfessionShortcutListener";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        EnsureShortcutListener();
        TryCreateForActiveScene();
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureShortcutListener();
        TryCreateForActiveScene();
    }

    public static void TogglePanelFromShortcut()
    {
        if (ProfessionService.GetTotalProfessionCount() <= 0)
            return;

        ProfessionSelectionPanel panel = EnsurePanel();
        if (panel == null)
            return;

        if (panel.IsOpen)
            panel.CloseFromButton();
        else
            panel.Open();
    }

    private static void TryCreateForActiveScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!string.Equals(scene.name, LobbySceneName, System.StringComparison.Ordinal))
            return;

        ProfessionSelectionPanel panel = EnsurePanel();
        if (panel == null)
            return;

        if (Object.FindObjectsByType<ProfessionHudButton>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length > 0)
            return;

        Canvas canvas = ResolveCanvas();
        if (canvas != null)
            ProfessionTemporaryUIFactory.EnsureFloatingOpenButton(canvas, panel.Open);
    }

    public static ProfessionSelectionPanel EnsurePanel()
    {
        if (ProfessionSelectionPanel.Instance != null)
            return ProfessionSelectionPanel.Instance;

        ProfessionSelectionPanel[] existingPanels = Object.FindObjectsByType<ProfessionSelectionPanel>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (existingPanels.Length > 0)
            return existingPanels[0];

        if (GameObject.Find(RootName) != null)
            return null;

        Canvas canvas = ResolveCanvas();
        if (canvas == null)
            canvas = CreateCanvas();

        GameObject panelRoot = new GameObject(RootName, typeof(RectTransform));
        panelRoot.layer = canvas.gameObject.layer;
        panelRoot.transform.SetParent(canvas.transform, false);

        RectTransform rect = panelRoot.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        ProfessionSelectionPanel panel = panelRoot.AddComponent<ProfessionSelectionPanel>();
        return panel;
    }

    private static Canvas ResolveCanvas()
    {
        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas canvas = canvases[i];
            if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                return canvas;
        }

        return canvases.Length > 0 ? canvases[0] : null;
    }

    private static Canvas CreateCanvas()
    {
        GameObject root = new GameObject("TemporaryUICanvas", typeof(RectTransform));
        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;

        CanvasScaler scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        root.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    private static void EnsureShortcutListener()
    {
        ProfessionShortcutListener[] listeners = Object.FindObjectsByType<ProfessionShortcutListener>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (listeners.Length > 0)
            return;

        GameObject listener = new GameObject(ShortcutListenerName);
        Object.DontDestroyOnLoad(listener);
        listener.AddComponent<ProfessionShortcutListener>();
    }
}
