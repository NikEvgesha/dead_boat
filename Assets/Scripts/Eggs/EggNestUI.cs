using UnityEngine;
using UnityEngine.UI;

public class EggNestUI : MonoBehaviour
{
    [SerializeField] private EggNestPoint _nest;
    [SerializeField] private Text _timerText;
    [SerializeField] private Text _eggNameText;
    [SerializeField] private GameObject _emptyState;
    [SerializeField] private GameObject _incubationState;
    [SerializeField] private GameObject _readyState;

    private EggHatchingManager _manager;

    private void Start()
    {
        TryBindManager();
        Refresh();
    }

    private void OnEnable()
    {
        Refresh();
    }

    private void OnDestroy()
    {
        if (_manager != null)
            _manager.StateChanged -= Refresh;
    }

    public void Refresh()
    {
        TryBindManager();

        if (_nest == null)
            return;

        if (_manager == null)
            return;

        EggNestState nestState = _manager.GetNestState(_nest.NestId);
        bool hasEgg = nestState != null;

        if (!hasEgg)
        {
            SetState(true, false, false);
            if (_timerText != null)
                _timerText.text = string.Empty;
            if (_eggNameText != null)
                _eggNameText.text = string.Empty;
            return;
        }

        int remaining = _manager.GetRemainingSeconds(_nest.NestId);
        bool ready = remaining <= 0;

        SetState(false, !ready, ready);

        if (_eggNameText != null)
            _eggNameText.text = ready && !string.IsNullOrWhiteSpace(nestState.hatchedAnimalId)
                ? nestState.hatchedAnimalId
                : nestState.eggId;

        if (_timerText != null)
            _timerText.text = ready
                ? EggFeatureLocalization.Text("Eggs/ReadyToCollect", "\u0413\u043E\u0442\u043E\u0432\u043E", "Ready")
                : FormatSeconds(remaining);
    }

    private void SetState(bool empty, bool incubating, bool ready)
    {
        if (_emptyState != null)
            _emptyState.SetActive(empty);

        if (_incubationState != null)
            _incubationState.SetActive(incubating);

        if (_readyState != null)
            _readyState.SetActive(ready);
    }

    private static string FormatSeconds(int totalSeconds)
    {
        int seconds = Mathf.Max(0, totalSeconds);
        int hours = seconds / 3600;
        int minutes = (seconds % 3600) / 60;
        int secs = seconds % 60;

        return $"{hours:00}:{minutes:00}:{secs:00}";
    }

    private void TryBindManager()
    {
        EggHatchingManager manager = EggHatchingManager.Instance;
        if (manager == null || manager == _manager)
            return;

        if (_manager != null)
            _manager.StateChanged -= Refresh;

        _manager = manager;
        _manager.StateChanged += Refresh;
    }
}
