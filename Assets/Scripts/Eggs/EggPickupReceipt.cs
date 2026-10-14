using System;

// A local pickup result, never added to the normal run inventory or Photon state.
public readonly struct EggPickupReceipt
{
    public readonly string EggId;
    public readonly int Amount, Total;
    public readonly bool FirstDiscovery;
    private EggPickupReceipt(string id, int amount, int total, bool first)
    { EggId = id; Amount = amount; Total = total; FirstDiscovery = first; }

    public static EggPickupReceipt Apply(EggFeatureState state, string eggId, int amount, string oneShotId = null)
    {
        if (state == null || string.IsNullOrWhiteSpace(eggId) || amount <= 0) return default;
        state.Normalize();
        if (oneShotId != null && state.collectedOneShotEggIds.Contains(oneShotId)) return default;
        bool first = !state.hasDiscoveredEggs;
        state.AddEgg(eggId, amount);
        if (oneShotId != null) state.collectedOneShotEggIds.Add(oneShotId);
        long total = 0;
        foreach (var entry in state.ownedEggs) total += Math.Max(0, entry.amount);
        return new EggPickupReceipt(eggId, amount, (int)Math.Min(int.MaxValue, total), first);
    }
}
