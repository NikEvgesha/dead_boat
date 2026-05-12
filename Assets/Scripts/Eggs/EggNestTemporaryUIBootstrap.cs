using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class EggNestTemporaryUIBootstrap
{
    private const string LobbySceneName = "Lobby";
    private const string RootName = "TemporaryEggNestSelectionUI";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        TryCreateForActiveScene();
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryCreateForActiveScene();
    }

    private static void TryCreateForActiveScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!string.Equals(scene.name, LobbySceneName, System.StringComparison.Ordinal))
            return;

        EnsureNestUseButtonShowSetup();
        if (EggNestSelectionPanel.Instance != null)
            return;

        if (Object.FindObjectsByType<EggNestSelectionPanel>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length > 0)
            return;

        if (GameObject.Find(RootName) != null)
            return;

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

        panelRoot.AddComponent<EggNestSelectionPanel>();
    }

    private static void EnsureNestUseButtonShowSetup()
    {
        EggNestPoint[] nests = Object.FindObjectsByType<EggNestPoint>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < nests.Length; i++)
        {
            EggNestPoint nest = nests[i];
            if (nest == null)
                continue;

            if (nest.GetComponent<EggNestUseButtonShowSetup>() == null)
                nest.gameObject.AddComponent<EggNestUseButtonShowSetup>();

            if (nest.GetComponent<EggNestProgressDisplay>() == null)
                nest.gameObject.AddComponent<EggNestProgressDisplay>();
        }
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
}
