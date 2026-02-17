using UnityEngine;

public class EggSpawnBalancer : MonoBehaviour
{
    [Header("Decrease chance after each egg pickup in current run")]
    [Range(0f, 1f)]
    [SerializeField] private float _decreasePerCollected = 0.12f;

    [Range(0.01f, 1f)]
    [SerializeField] private float _minChanceMultiplier = 0.15f;

    [SerializeField] private bool _resetRunOnStart = true;

    private void Awake()
    {
        EggSpawnRuntimeState.Configure(_decreasePerCollected, _minChanceMultiplier);
    }

    private void Start()
    {
        if (_resetRunOnStart)
            EggSpawnRuntimeState.ResetRun();
    }
}
