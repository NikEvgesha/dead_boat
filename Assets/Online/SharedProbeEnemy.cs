#if UNITY_EDITOR
using UnityEngine;
namespace DeadBoat.Online.Editor
{
    public sealed class SharedProbeEnemy : EnemyCore
    {
        public override void TakeDamage(int damage)
        {
            if (SharedEnemiesRuntime.RequestDamage(this, damage)) return;
            currentHP = Mathf.Max(0, currentHP - damage);
            isDead = currentHP == 0;
        }
    }
}
#endif
