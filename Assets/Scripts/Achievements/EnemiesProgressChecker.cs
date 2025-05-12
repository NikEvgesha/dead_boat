using UnityEngine;

public class EnemiesProgressChecker : AchievementProgressChecker
{
    [SerializeField] private ZombieController _enemy;

    protected void Start()
    {
        _enemy = GetComponent<ZombieController>();
        _enemy.Death += OnValueChange;
    }

    private void OnDisable()
    {
        _enemy.Death -= OnValueChange;
    }

    private void OnValueChange()
    {
        AchievementManager.Instance.UpdateData(_achievementType);
    }
}