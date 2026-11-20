using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DeadBoat.Online;

public class LevelMapUI : MonoBehaviour
{
    [SerializeField] private GameObject _lockedLevelWindow;
    [SerializeField] private LevelMapElement _levelPrefab;

    private bool _transitionStarted;
    private GameObject _errorPanel;
    private Coroutine _hideErrorRoutine;
    private readonly List<(int levelId, GameObject row)> _eventRows = new();
    private float _nextCalendarCheck;

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
        int levelId = LevelManager.Instance.GetLevelId(level);
        if (!RunEventCalendar.Current.TryAdmit(levelId, out var content))
        {
            ShowLoadError(true);
            return;
        }

        if (LoadingManager.Instance == null || GameLoader.Instance == null ||
            !RunEventCalendar.Current.TryResolve(levelId, content, out var eventScene) ||
            !Application.CanStreamedLevelBeLoaded(eventScene ?? level.Scene))
        {
            ShowLoadError();
            return;
        }

        _transitionStarted = true;
        if (_errorPanel != null)
            _errorPanel.SetActive(false);
        GameLoader.Instance.ShowLoadingScreen();
        StartCoroutine(LoadAfterVisibleFrame(levelId, level, content));
    }

    private IEnumerator LoadAfterVisibleFrame(int levelId, LevelData level, RunEventContent content)
    {
        // Let the loading canvas render once before the ad SDK or scene loading can block WebGL.
        yield return null;

        if (!RunEventCalendar.Current.IsOpenFor(levelId, content))
        {
            OnLoadFailed(true);
            yield break;
        }
        // A solo map selection never keeps a Photon CCU slot during the run.
        LobbyOnlineBootstrap online = FindFirstObjectByType<LobbyOnlineBootstrap>();
        if (online != null)
        {
            var disconnect = online.DisconnectForSoloRunAsync();
            while (!disconnect.IsCompleted)
                yield return null;
            if (disconnect.IsFaulted || disconnect.IsCanceled)
            {
                if (disconnect.IsFaulted) Debug.LogException(disconnect.Exception);
                OnLoadFailed();
                yield break;
            }
        }

        if (!RunEventCalendar.Current.IsOpenFor(levelId, content))
        {
            OnLoadFailed(true);
            yield break;
        }
        try
        {
            ControlManager.Instance.CursorActive = false;
            SharedRunRecovery.Forget();
            SoloRunBranch.Clear();
            SaveManager.Instance.ResetGameProgress();
            LoadingManager.Instance.LoadAcceptedRun(levelId, level, content, true);
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

    private void OnLoadFailed(bool eventClosed = false)
    {
        GameLoader.Instance?.HideLoadingScreen();
        FindFirstObjectByType<LobbyOnlineBootstrap>()?.CancelSoloRunStart();
        _transitionStarted = false;
        if (ControlManager.Instance != null)
            ControlManager.Instance.CursorActive = true;
        ShowLoadError(eventClosed);
    }

    private void ShowLoadError(bool eventClosed = false)
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
        bool russian = language != null && language.StartsWith("Ru", StringComparison.OrdinalIgnoreCase);
        _errorPanel.GetComponentInChildren<Text>().text = eventClosed
            ? (russian ? "Событие сейчас закрыто." : "This event is currently closed.")
            : (russian ? "Не удалось открыть уровень. Попробуйте ещё раз." : "Could not open this level. Please try again.");
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
        for (int i = 0; i < levels.Count; i++)
        {
            LevelData lvlData = levels[i];
            LevelMapElement lvl = Instantiate(_levelPrefab, transform);
            lvl.Init(lvlData, this);
            if (RunEventCalendar.Current.TryGetEventLevel(lvlData.Scene, out _))
                _eventRows.Add((i, lvl.gameObject));
        }
        RefreshEventRows();
    }

    private void Update()
    {
        if (_eventRows.Count != 0 && Time.unscaledTime >= _nextCalendarCheck) RefreshEventRows();
    }

    private void RefreshEventRows()
    {
        _nextCalendarCheck = Time.unscaledTime + 1;
        foreach (var (levelId, row) in _eventRows)
        {
            bool open = RunEventCalendar.Current.TryAdmit(levelId, out _);
            if (row.activeSelf != open) row.SetActive(open);
        }
    }
}
