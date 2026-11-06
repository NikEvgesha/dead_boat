using System;
using System.Collections.Generic;
using UnityEngine;

// Personal card state. Timers use game time: advertisements pause the budget as well as combat.
public static class CombatCardRules
{
    public const int MaximumRank = 3;
    public static bool IsSpecial(BoostType type) => type == BoostType.BloodPact || type == BoostType.BattleRhythm || type == BoostType.OnTheEdge || type == BoostType.LastRound || type == BoostType.HeavyCoat || type == BoostType.SteadyAim || type == BoostType.PatientReload;
    public static int Rank(Stats stats, BoostType type) => Mathf.Clamp(type == BoostType.BloodPact ? stats.BloodPactRank :
        type == BoostType.BattleRhythm ? stats.BattleRhythmRank : type == BoostType.OnTheEdge ? stats.OnTheEdgeRank : type == BoostType.LastRound ? stats.LastRoundRank : type == BoostType.HeavyCoat ? stats.HeavyCoatRank : type == BoostType.SteadyAim ? stats.SteadyAimRank : type == BoostType.PatientReload ? stats.PatientReloadRank : 0, 0, MaximumRank);
    public static float AimBonus(int rank) => rank > 0 ? .2f + .1f * Mathf.Clamp(rank, 1, 3) : 0;
    public static float PatientBonus(int rank) => rank > 0 ? .15f + .1f * Mathf.Clamp(rank, 1, 3) : 0;
    public static float CoatHealth(int rank) => rank > 0 ? .1f + .05f * Mathf.Clamp(rank, 1, 3) : 0;
    public static float CoatSpeed(int rank) => rank > 0 ? .92f : 1;
    public static float LastRoundBonus(int rank) => rank > 0 ? .3f + .15f * Mathf.Clamp(rank, 1, 3) : 0;
    public static float ReloadDurationMultiplier(int rank) => rank > 0 ? 1.15f : 1;
    public static float MaximumHealth(float original, int rank, int coatRank = 0) => Mathf.Max(1, original * (1 + CoatHealth(coatRank)) * (rank > 0 ? .8f : 1));
    public static float PreviewHealth(float maximum, int bloodRank, int coatRank, BoostType type) =>
        maximum / (1 + CoatHealth(coatRank)) / (bloodRank > 0 ? .8f : 1) *
        (1 + CoatHealth(type == BoostType.HeavyCoat ? Mathf.Min(3, coatRank + 1) : coatRank)) *
        (bloodRank > 0 || type == BoostType.BloodPact ? .8f : 1);
    public static float Lifesteal(int rank) => rank > 0 ? .02f + .02f * Mathf.Clamp(rank, 1, 3) : 0;
    public static float HealLimit(int rank) => rank > 0 ? .01f + .01f * Mathf.Clamp(rank, 1, 3) : 0;
    public static float RhythmBonus(int rank) => rank > 0 ? .25f + .15f * Mathf.Clamp(rank, 1, 3) : 0;
    public static float EdgeBonus(int rank) => rank > 0 ? .15f + .1f * Mathf.Clamp(rank, 1, 3) : 0;
    public static bool EdgeActive(float health, float maximum) => health > 0 && maximum > 0 &&
        (health <= maximum * .4f || Mathf.Approximately(health, maximum * .4f));
    public static int Damage(int basic, float bonus) => Mathf.Max(0, Mathf.RoundToInt(basic * (1 + Mathf.Clamp(bonus, 0, 1))));

    public sealed class Combo
    {
        private int hits;
        private double lastHit = double.NegativeInfinity;
        public int Count(double now) => now - lastHit > 2 ? 0 : hits;
        public bool Hit(double now)
        {
            if (now - lastHit > 2) hits = 0;
            lastHit = now;
            if (++hits < 3) return false;
            hits = 0;
            return true;
        }
        public void Miss() { hits = 0; }
    }

    public sealed class HealingBudget
    {
        private readonly Queue<(double time, float amount)> spent = new();
        private double lockedUntil;
        public void Lock(double now) { spent.Clear(); lockedUntil = now + 1; }
        public float Take(double now, int rank, int removedHP, float maxHP, float missingHP)
        {
            while (spent.Count > 0 && now - spent.Peek().time >= 1) spent.Dequeue();
            if (now < lockedUntil || rank <= 0 || removedHP <= 0 || missingHP <= 0 || spent.Count >= 256) return 0;
            float used = 0;
            foreach (var entry in spent) used += entry.amount;
            float amount = Mathf.Max(0, Mathf.Min(Mathf.Min(removedHP * Lifesteal(rank), maxHP * HealLimit(rank) - used), missingHP));
            if (amount > 0) spent.Enqueue((now, amount));
            return amount;
        }
    }
}
