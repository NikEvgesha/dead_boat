using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class AnimalMergeTemporaryUIBootstrap
{
    private const string LobbySceneName = "Lobby";
    private const string RootName = "TemporaryAnimalMergeUI";

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
        // Animal merge now uses the configured AnimalMergeSelectionPanel prefab in GameCanvas.
        // Keep this bootstrap inert so an unbound temporary panel cannot shadow the prefab.
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
