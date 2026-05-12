using System;
using System.Collections.Generic;
using UnityEngine;

public class EggNestPoint : MonoBehaviour
{
    [SerializeField] private string _nestId;
    [SerializeField] private Transform _eggVisualAnchor;
    [Header("Optional preview/support anchors")]
    [SerializeField] private List<Transform> _animalSpawnPoints = new();
    [SerializeField] private EggNestSelectionPanel _selectionPanel;
    [Header("Incubation boost")]
    [SerializeField] private bool _boostIncubationWithRewardedAd = true;
    [SerializeField] private string _incubationBoostRewardId = "EggIncubationBoost";
    [SerializeField] private int _incubationBoostSeconds = 1800;
    [SerializeField] private AudioSource _incubationBoostAudio;

    private GameObject _eggPreviewInstance;
    private bool _incubationBoostAdInProgress;

    public string NestId => _nestId;
    public Transform EggVisualAnchor => _eggVisualAnchor != null ? _eggVisualAnchor : transform;
    public IReadOnlyList<Transform> AnimalSpawnPoints => _animalSpawnPoints;

    public event Action<EggNestPoint> NestActionRequested;

    public void _Use()
    {
        Use();
    }

    public void _RequestAction()
    {
        Use();
    }

    public void _PlaceEggAction()
    {
        PlaceEggAction();
    }

    public void _SkipAction()
    {
        SkipAction();
    }

    public void _PlaceAnimalAction()
    {
        PlaceAnimalAction();
    }

    public void RequestAction()
    {
        Use();
    }

    public void Use()
    {
        NestActionRequested?.Invoke(this);
        ExecuteUseAction();
    }

    public void PlaceEggAction()
    {
        EggHatchingManager manager = EggHatchingManager.Instance;
        if (manager == null)
            return;

        if (manager.GetNestState(_nestId) != null)
            return;

        EggNestSelectionPanel panel = ResolveSelectionPanel();
        if (panel != null)
        {
            panel.OpenForNest(_nestId);
            return;
        }

        manager.TryStartIncubationWithAnyEgg(_nestId);
    }

    public void SkipAction()
    {
        EggHatchingManager.Instance?.TrySkipIncubation(_nestId);
    }

    public void PlaceAnimalAction()
    {
        EggHatchingManager.Instance?.TryCollectReadyAnimal(_nestId);
    }

    public void SetEggPreview(GameObject previewPrefab)
    {
        ClearEggPreview();

        if (previewPrefab == null)
            return;

        _eggPreviewInstance = Instantiate(previewPrefab, EggVisualAnchor);
        _eggPreviewInstance.transform.localPosition = Vector3.zero;
        _eggPreviewInstance.transform.localRotation = Quaternion.identity;

        // Preview should stay visual-only and never be collected as a real item.
        DisablePreviewInteraction(_eggPreviewInstance);
    }

    public void ClearEggPreview()
    {
        if (_eggPreviewInstance != null)
            Destroy(_eggPreviewInstance);

        _eggPreviewInstance = null;
    }

    public bool TryGetSpawnPoint(int index, out Transform point)
    {
        point = null;

        if (index < 0 || index >= _animalSpawnPoints.Count)
            return false;

        point = _animalSpawnPoints[index];
        return point != null;
    }

    public int GetSpawnPointCount()
    {
        return _animalSpawnPoints != null ? _animalSpawnPoints.Count : 0;
    }

    private void ExecuteUseAction()
    {
        if (string.IsNullOrWhiteSpace(_nestId))
            return;

        EggHatchingManager manager = EggHatchingManager.Instance;
        if (manager == null)
            return;

        EggNestState state = manager.GetNestState(_nestId);
        if (state == null)
        {
            PlaceEggAction();
            return;
        }

        int remainingSeconds = manager.GetRemainingSeconds(_nestId);
        if (remainingSeconds <= 0)
        {
            PlaceAnimalAction();
            return;
        }

        BoostIncubationWithAd();
    }

    private void BoostIncubationWithAd()
    {
        if (!_boostIncubationWithRewardedAd)
            return;

        if (_incubationBoostAdInProgress)
            return;

        if (AdsManager.Instance == null)
            return;

        _incubationBoostAdInProgress = true;
        AdsManager.Instance.ShowRewardedAd(
            _incubationBoostRewardId,
            success =>
            {
                _incubationBoostAdInProgress = false;

                if (success && EggHatchingManager.Instance != null)
                {
                    EggHatchingManager.Instance.TryReduceIncubation(_nestId, _incubationBoostSeconds);

                    if (_incubationBoostAudio != null)
                        _incubationBoostAudio.Play();
                }
            });
    }

    private EggNestSelectionPanel ResolveSelectionPanel()
    {
        return _selectionPanel != null ? _selectionPanel : EggNestSelectionPanel.Instance;
    }

    private static void DisablePreviewInteraction(GameObject root)
    {
        if (root == null)
            return;

        PickableItem pickable = root.GetComponent<PickableItem>();
        if (pickable != null)
            pickable.enabled = false;

        Rigidbody rb = root.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.detectCollisions = false;
        }

        Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
            colliders[i].enabled = false;
    }
}
