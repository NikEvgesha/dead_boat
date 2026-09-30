using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LevelMapUI : MonoBehaviour
{
    [SerializeField] private GameObject _lockedLevelWindow;
    [SerializeField] private LevelMapElement _levelPrefab;

    private bool _transitionStarted;
    private GameObject _errorPanel;
    private Coroutine _hideErrorRoutine;

    private void Start()
    {
        LevelManager.Instance.SetMapUI(this);
        if (GameLoader.Instance != null)
            GameLoader.Instance.OnLoadFailed += OnLoadFailed;
    }

    private void OnDestroy()
    {
        if (GameLoader.Instance != null)
            GameLoader.Instance.OnLoadFailed -= OnLoadFailed;
    }

    public void SelectLevel(LevelData level)
    {
        if (_transitionStarted || level == null || !level.Unlocked)
            return;

        if (LoadingManager.Instance == null || GameLoader.Instance == null ||
            string.IsNullOrEmpty(level.Scene) || !Application.CanStreamedLevelBeLoaded(level.Scene))
        {
            ShowLoadError();
            return;
        }

        _transitionStarted = true;
        if (_errorPanel != null)
            _errorPanel.SetActive(false);
        GameLoader.Instance.ShowLoadingScreen();
        StartCoroutine(LoadAfterVisibleFrame(level.Scene));
    }

    private IEnumerator LoadAfterVisibleFrame(string sceneName)
    {
        // Let the loading canvas render once before the ad SDK or scene loading can block WebGL.
        yield return null;

        try
        {
            ControlManager.Instance.CursorActive = false;
            LoadingManager.Instance.LoadLocation(Location.Game, sceneName);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            OnLoadFailed();
        }
    }

    private void OnLoadFailed(string message)
    {
        OnLoadFailed();
    }

    private void OnLoadFailed()
    {
        GameLoader.Instance?.HideLoadingScreen();
        _transitionStarted = false;
        if (ControlManager.Instance != null)
            ControlManager.Instance.CursorActive = true;
        ShowLoadError();
    }

    private void ShowLoadError()
    {
        Debug.LogWarning("Level selection: could not open the selected level.");
        if (_lockedLevelWindow == null || _levelPrefab == null)
            return;

        if (_errorPanel == null)
        {
            _errorPanel = new GameObject("Level load error", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            _errorPanel.transform.SetParent(_lockedLevelWindow.transform.parent, false);
            RectTransform panelRect = (RectTransform)_errorPanel.transform;
            panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(480f, 100f);
            panelRect.anchoredPosition = Vector2.zero;
            _errorPanel.GetComponent<Image>().color = new Color(0.08f, 0.09f, 0.12f, 0.95f);

            GameObject label = new GameObject("Message", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            label.transform.SetParent(_errorPanel.transform, false);
            RectTransform labelRect = (RectTransform)label.transform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(16f, 8f);
            labelRect.offsetMax = new Vector2(-16f, -8f);
            Text text = label.GetComponent<Text>();
            text.font = _levelPrefab.GetComponentInChildren<Text>(true).font;
            text.fontSize = 25;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
        }

        string language = LocalizationManager.Instance != null ? LocalizationManager.Instance.CurrentLanguage : null;
        _errorPanel.GetComponentInChildren<Text>().text =
            language != null && language.StartsWith("Ru", StringComparison.OrdinalIgnoreCase)
                ? "Не удалось открыть уровень. Попробуйте ещё раз."
                : "Could not open this level. Please try again.";
        _errorPanel.SetActive(true);
        _errorPanel.transform.SetAsLastSibling();
        if (_hideErrorRoutine != null)
            StopCoroutine(_hideErrorRoutine);
        _hideErrorRoutine = StartCoroutine(HideErrorAfterDelay());
    }

    private IEnumerator HideErrorAfterDelay()
    {
        yield return new WaitForSecondsRealtime(4f);
        if (_errorPanel != null)
            _errorPanel.SetActive(false);
        _hideErrorRoutine = null;
    }

    public void Init(List<LevelData> levels)
    {
       foreach (LevelData lvlData in levels)
        {
            LevelMapElement lvl = Instantiate(_levelPrefab, transform);
            lvl.Init(lvlData, this);
        }
    }
}
