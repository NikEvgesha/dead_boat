using System.Collections;
using UnityEngine;

/// <summary>
/// Менеджер боя с боссом Дракон: привязка к системе TriggerBossFight,
/// управление здоровьем и смертью дракона.
/// </summary>
public class DragonBoss : TriggerBossFight
{
    [Header("Ссылки")]
    [SerializeField] private DragonBossController dragonController;

    private float _currentHP;
    public float HP
    {
        get { return _currentHP; }
        set
        {
            _currentHP = value;
            hpBar.size = value / _startHP;
        }
    }
    private float _startHP = 0.1f;

    public override void StartBossFight()
    {
        base.StartBossFight();
        //dragonController.StartScene();

        if (dragonController == null)
        {
            Debug.LogError("[DragonBoss] Не задан DragonBossController!");
            return;
        }
        // Включаем дракона и инициализируем HP
        dragonController.gameObject.SetActive(true);
        dragonController.AddDamage += AddDamage;
        dragonController.Dead += Dead;
        _startHP = dragonController.GetStartHP();
        HP = _startHP;
    }

    private IEnumerator HandleBossDeath()
    {
        // Скрываем HP
        hpBar.gameObject.SetActive(false);
        // Задержка перед окончанием боя
        yield return new WaitForSeconds(1f);
        // Открываем экран победы
        EndGameUIManager.EndGame(EndGameState.Win);
    }
    private void AddDamage(float damage)
    {
        HP -= damage;
    }
    private void Dead()
    {
        StartCoroutine(HandleBossDeath());
    }
}
