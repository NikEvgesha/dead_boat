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
    [SerializeField] private RectTransform _closeButton;

    private bool _transitionStarted;
    private GameObject _errorPanel;
    private Coroutine _hideErrorRoutine;
    private readonly List<EventLevelCard> _eventRows = new();
    private readonly List<RectTransform> _cards = new();
    private AspectRatioFitter _pickerShape;
    private float _wideAspect;
    private GridLayoutGroup _compactGrid;
    private bool _fittingCards;
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

    public void SelectLevel(LevelData level) => SelectLevel(level, null);

    private void SelectLevel(LevelData level, RunEventContent? shown)
    {
        if (_transitionStarted || level == null || !level.Unlocked)
            return;
        int levelId = LevelManager.Instance.GetLevelId(level);
        if (!RunEventCalendar.Current.TryAdmit(levelId, out var content) ||
            (shown.HasValue && !shown.Value.Matches(content)))
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
        _errorPanel.GetComponentInChildren<Text>().text = eventClosed && LocalizationManager.Instance != null
            ? LocalizationManager.Instance.LocalizationData.GetTranslation("Level/EventClosed", language)
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
        _pickerShape = GetComponent<AspectRatioFitter>();
        if (_pickerShape != null) _wideAspect = _pickerShape.aspectRatio;
        for (int i = 0; i < levels.Count; i++)
        {
            LevelData lvlData = levels[i];
            if (RunEventCalendar.Current.TryGetEventLevel(lvlData.Scene, out int eventLevelId))
            {
                var card = EventLevelCard.Create(transform, eventLevelId, lvlData,
                    _levelPrefab.GetComponentInChildren<Text>(true).font, false, shown => SelectLevel(lvlData, shown));
                _eventRows.Add(card);
                _cards.Insert(0, (RectTransform)card.transform);
                continue;
            }
            LevelMapElement lvl = Instantiate(_levelPrefab, transform);
            lvl.Init(lvlData, this);
            _cards.Add((RectTransform)lvl.transform);
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
        foreach (var row in _eventRows) row.Refresh();
        UpdateCardLayout();
    }

    private void UpdateCardLayout()
    {
        if (_pickerShape == null || transform.parent is not RectTransform panel || panel.rect.height <= 0) return;
        var size = panel.rect.size;
        bool eventVisible = _eventRows.Exists(card => card.gameObject.activeSelf);
        if (eventVisible != _fittingCards)
        {
            foreach (var card in _cards)
                if (card.TryGetComponent<LevelMapElement>(out var map)) map.FitInsideCell(eventVisible);
            _fittingCards = eventVisible;
        }
        bool compact = size.x < size.y && eventVisible;
        bool wasCompact = _compactGrid != null && _compactGrid.gameObject.activeSelf;
        // The close button is a sibling anchored over the top right of the picker.
        var closeSize = _closeButton != null ? _closeButton.rect.size : Vector2.zero;
        if (!compact)
            _pickerShape.aspectRatio = eventVisible
                ? Mathf.Min(_wideAspect, Mathf.Max(size.x * .5f, size.x - 2 * closeSize.x) / size.y)
                : _wideAspect;
        if (compact)
        {
            if (_compactGrid == null)
            {
                var container = new GameObject("Map grid", typeof(RectTransform), typeof(GridLayoutGroup));
                container.transform.SetParent(transform, false);
                _compactGrid = container.GetComponent<GridLayoutGroup>();
                _compactGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                _compactGrid.constraintCount = 2;
                _compactGrid.spacing = new Vector2(8, 8);
                _compactGrid.childAlignment = TextAnchor.MiddleCenter;
            }
            _compactGrid.gameObject.SetActive(true);
            float height = Mathf.Max(size.y * .5f, size.y - 2 * closeSize.y);
            _pickerShape.aspectRatio = size.x / height;
            int visible = 0;
            foreach (var card in _cards)
                if (card.gameObject.activeSelf) visible++;
            int rows = Mathf.Max(1, (visible + 1) / 2);
            var cell = new Vector2((size.x - 8) / 2, (height - (rows - 1) * 8) / rows);
            if (_compactGrid.cellSize != cell) _compactGrid.cellSize = cell;
        }
        if (compact == wasCompact) return;
        foreach (var card in _cards)
            card.SetParent(compact ? _compactGrid.transform : transform, false);
        if (!compact) _compactGrid.gameObject.SetActive(false);
    }
}
