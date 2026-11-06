using System.Collections.Generic;
using UnityEngine;
using DeadBoat.Online;

// Only weapon entry points create attacks. Burn, corpse and environmental damage never enter here.
public static class CombatCards
{
    public sealed class Attack
    {
        internal bool Sword, Hit, Third, ThirdNotified;
        internal float Edge, LastRound;
        internal int Blood, Rhythm;
        internal float MaximumHealth;
        internal readonly HashSet<EnemyCore> Targets = new();
    }
    private static CombatCardRules.Combo combo = new();
    private static CombatCardRules.HealingBudget healing = new();
    private static readonly Dictionary<int, (Attack attack, double sent)> pending = new();
    private static readonly List<int> expired = new();
    private static int sequence;
    // Local presentation snapshots. Never serialized or sent over the network.
    public static int PresentationEpoch { get; private set; }
    public static int ThirdHitRevision { get; private set; }
    public static double HealingTotal { get; private set; }
    public static int ComboHits => combo.Count(Time.timeAsDouble);
    public static void Reset()
    {
        combo = new CombatCardRules.Combo();
        healing = new CombatCardRules.HealingBudget();
        healing.Lock(Time.timeAsDouble); // Recovery/master handoff must not refill a spent second.
        pending.Clear(); sequence = 0;
        PresentationEpoch++; ThirdHitRevision = 0; HealingTotal = 0;
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Initialize() => Reset();
    public static Attack Begin(bool sword, int magazineBeforeShot = -1)
    {
        var stats = LevelStatManager.Instance != null ? LevelStatManager.Instance.Stats : default;
        var player = PlayerStatsManager.Instance;
        float maximum = player != null ? player.MaxHealth : 1;
        return new Attack
        {
            LastRound = !sword && magazineBeforeShot == 1 ? CombatCardRules.LastRoundBonus(CombatCardRules.Rank(stats, BoostType.LastRound)) : 0,
            Sword = sword, Blood = CombatCardRules.Rank(stats, BoostType.BloodPact),
            Rhythm = CombatCardRules.Rank(stats, BoostType.BattleRhythm), MaximumHealth = maximum,
            Edge = player != null && CombatCardRules.EdgeActive(player.Health, maximum)
                ? CombatCardRules.EdgeBonus(CombatCardRules.Rank(stats, BoostType.OnTheEdge)) : 0
        };
    }
    public static void End(Attack attack)
    {
        if (attack != null && attack.Sword && !attack.Hit) combo.Miss();
    }
    public static void Hit(Attack attack, EnemyCore enemy, int basicDamage)
    {
        if (attack == null || enemy == null || enemy.IsDead || enemy.SharedHP <= 0 || basicDamage <= 0 ||
            (SharedRunContext.Active && SharedLocalGameplay.Blocked) || !attack.Targets.Add(enemy)) return;
        if (!attack.Hit)
        {
            attack.Hit = true;
            if (attack.Sword && attack.Rhythm > 0) attack.Third = combo.Hit(Time.timeAsDouble);
        }
        int damage = CombatCardRules.Damage(basicDamage, attack.Edge + attack.LastRound + (attack.Third ? CombatCardRules.RhythmBonus(attack.Rhythm) : 0));
        if (SharedRunContext.Active)
        {
            var state = SharedRunContext.State;
            var identity = enemy.GetComponent<WorldSpawnIdentity>();
            if (!SharedRunContext.Playing || state == null || identity == null) return;
            ExpirePending();
            int slot = state.CrewSlot(state.Runner.LocalPlayer);
            if (slot < 0 || pending.Count >= 256) return;
            sequence = Mathf.Max(sequence, state.WeaponSequences[slot]) + 1;
            pending[sequence] = (attack, Time.timeAsDouble);
            state.RPC_WeaponDamage(identity.Id, damage, sequence);
        }
        else
        {
            int before = enemy.SharedHP;
            enemy.TakeDamage(damage);
            Heal(attack, Mathf.Clamp(before - enemy.SharedHP, 0, before));
        }
    }
    private static void ExpirePending()
    {
        expired.Clear();
        foreach (var pair in pending) if (Time.timeAsDouble - pair.Value.sent > 10) expired.Add(pair.Key);
        foreach (int key in expired) pending.Remove(key);
    }
    // Called only by the local avatar after validating the run authority's RPC.
    public static void Confirm(int id, int removedHP)
    {
        if (!pending.TryGetValue(id, out var hit)) return;
        pending.Remove(id); // A duplicate confirmation cannot create another heal.
        if (Time.timeAsDouble - hit.sent <= 10) Heal(hit.attack, removedHP);
    }
    private static void Heal(Attack attack, int removedHP)
    {
        var player = PlayerStatsManager.Instance;
        if (player == null || player.Health <= 0 || removedHP <= 0) return;
        if (attack.Third && !attack.ThirdNotified)
        {
            attack.ThirdNotified = true;
            ThirdHitRevision++;
        }
        float maximum = Mathf.Min(attack.MaximumHealth, player.MaxHealth);
        float amount = healing.Take(Time.timeAsDouble, attack.Blood, removedHP, maximum, player.MaxHealth - player.Health);
        if (amount > 0)
        {
            float before = player.Health;
            player.AddCardHealing(amount);
            HealingTotal += Mathf.Max(0, player.Health - before);
        }
    }
}
