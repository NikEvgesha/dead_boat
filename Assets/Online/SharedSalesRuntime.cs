using System.Collections.Generic;
using UnityEngine;

namespace DeadBoat.Online
{
    // Bindings are local visuals. Balances, consumption and collection are
    // canonical room state. No per-frame scene searches or separate bag physics.
    public static class SharedSalesRuntime
    {
        private static readonly Dictionary<ulong, SellPoint> points = new();
        private static readonly Dictionary<SellPoint, ulong> keys = new();
        private static readonly Dictionary<ulong, double> requests = new();
        private static SharedDepartureState observed;
        public static int AppliedCredits { get; private set; }

        public static void Begin(SharedDepartureState state)
        {
            observed = state; AppliedCredits = 0; requests.Clear();
            points.Clear(); keys.Clear();
        }
        public static void End() { Begin(null); }
        public static void RestoreCredits(int total) => AppliedCredits = Mathf.Max(0, total);
        public static bool TryPoint(ulong id, out SellPoint point) => points.TryGetValue(id, out point) && point != null;
        public static void Remove(SellPoint point)
        {
            if (!keys.TryGetValue(point, out var id)) return;
            keys.Remove(point);
            if (points.TryGetValue(id, out var current) && current == point) points.Remove(id);
        }
        public static ulong Key(SellPoint point)
        {
            if (keys.TryGetValue(point, out var id)) return id;
            id = WorldSpawnIdentity.IdForKey("sale:" + WorldSpawnIdentity.StablePath(point.transform));
            // A restored root can replace a destroyed binding with the same key.
            if (points.TryGetValue(id, out var previous) && previous != null && previous != point)
                throw new System.InvalidOperationException("Duplicate live sale point key");
            keys[point] = id; points[id] = point;
            return id;
        }
        public static void Tick(SellPoint point)
        {
            if (!SharedRunContext.Active && !SoloRunBranch.Active) return;
            ulong id = Key(point);
            if (SharedRunContext.Playing)
            {
                var state = SharedRunContext.State;
                if (state != observed) Begin(state);
                id = Key(point);
                bool found = state.Sales.TryGet(id, out var record);
                point.ApplySharedBalance(found ? record.Coins : 0, found ? record.Revision : 0);
            }
            else if (SoloRunBranch.Active) SoloRunBranch.ObserveSale(point, id);
        }
        internal static void ApplyCredits(SharedDepartureState state)
        {
            if (CurrencyManager.Instance == null || SaveManager.Instance == null || SharedRunRecovery.Restoring) return;
            int slot = state.CrewSlot(state.Runner.LocalPlayer);
            if (slot < 0 || state.SaleCredits[slot] <= AppliedCredits) return;
            int total = state.SaleCredits[slot];
            int amount = (int)System.Math.Min((long)total - AppliedCredits,
                (long)int.MaxValue - CurrencyManager.Instance.Coins);
            AppliedCredits = total;
            CurrencyManager.Instance.AddCurrency(CurrencyType.Coins, amount);
            SharedRunRecovery.PersonalChanged();
        }
        public static int Price(PickableItem item)
        {
            var sellable = item.GetComponent<SellableItem>();
            if (sellable != null) return sellable.GetReward();
            var enemy = item.GetComponent<EnemyReward>();
            return enemy != null ? enemy.GetReward() : -1;
        }
        public static bool Sell(SellPoint point, PickableItem item)
        {
            if (!SharedRunContext.Active) return false;
            if (!SharedRunContext.Playing || item == null) return true;
            var state = SharedRunContext.State;
            var identity = item.GetComponent<WorldSpawnIdentity>();
            if (identity == null || !state.Items.TryGet(identity.Id, out var record) || record.Mode == 2 || record.Mode == 4) return true;
            bool freeAuthority = record.Owner == Fusion.PlayerRef.None && state.Object.HasStateAuthority;
            if ((!freeAuthority && SharedItemInteraction.Blocked) ||
                (!SharedItemsRuntime.Simulates(item) && !(freeAuthority && record.Mode == 3))) return true;
            double now = Time.realtimeSinceStartupAsDouble;
            if (requests.TryGetValue(identity.Id, out double next) && now < next) return true;
            int reward;
            if (point.SaleType == StoreType.Items)
            {
                var sellable = item.GetComponent<SellableItem>(); if (sellable == null) return true;
                reward = sellable.GetReward();
            }
            else { var sellable = item.GetComponent<EnemyReward>(); if (sellable == null) return true; reward = sellable.GetReward(); }
            ulong pointId = Key(point);
            Vector3 position = item.transform.position + LobbyNetworkAvatar.Origin;
            if (record.Owner == state.Runner.LocalPlayer)
                state.RPC_ItemState(identity.Id, record.Mode, position, item.transform.rotation, identity.Weapon?.CurrentAmmo ?? -1);
            else if (state.Object.HasStateAuthority) state.SampleFreeItem(identity.Id, position, item.transform.rotation);
            if (requests.Count >= 4096) requests.Clear();
            requests[identity.Id] = now + .5;
            state.RPC_SellItem(identity.Id, pointId, reward);
            return true;
        }
        public static bool Collect(MoneyBag bag)
        {
            if (SoloRunBranch.Active && bag.SalePoint != null)
                SoloRunBranch.RememberSale(Key(bag.SalePoint), 0);
            if (!SharedRunContext.Active) return false;
            if (!SharedRunContext.Playing || SharedItemInteraction.Blocked || bag.SalePoint == null) return true;
            var state = SharedRunContext.State;
            ulong id = Key(bag.SalePoint);
            if (state.Sales.TryGet(id, out var record) && record.Coins > 0) state.RPC_CollectSale(id, record.Revision);
            return true;
        }
    }
}
