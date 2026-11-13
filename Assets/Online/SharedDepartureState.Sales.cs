using Fusion;
using UnityEngine;

namespace DeadBoat.Online
{
    public struct SharedSaleRecord : INetworkStruct
    {
        public int Coins, Revision;
        public Vector3 Position;
    }

    public sealed partial class SharedDepartureState
    {
        [Networked, Capacity(128)] public NetworkDictionary<ulong, SharedSaleRecord> Sales => default;
        [Networked, Capacity(4)] public NetworkArray<int> SaleCredits => default;

        private bool SaleActorNear(PlayerRef player, Vector3 position)
        {
            foreach (var avatar in LobbyNetworkAvatar.All)
                if (avatar.Runner == Runner && avatar.Object.StateAuthority == player)
                    return Vector3.Distance(avatar.LogicalPosition, position) <= 8;
            return false;
        }

        // Prices retain the seller's existing profession/card/animal calculation.
        // This Shared-mode authority makes the transaction unique; it is not a
        // trusted backend economy or anti-cheat boundary.
        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void RPC_SellItem(ulong itemId, ulong pointId, int reward, RpcInfo info = default)
        {
            if (Phase != 3 || !Includes(info.Source) || reward < 0 || reward > 100000 ||
                !Items.TryGet(itemId, out var item) || item.Mode == 2 || item.Mode == 4 ||
                (item.Owner != info.Source && !(item.Owner == PlayerRef.None && info.Source == Object.StateAuthority)) ||
                !SharedSalesRuntime.TryPoint(pointId, out var point)) return;
            if (!CanAct(info.Source) && !(item.Owner == PlayerRef.None && info.Source == Object.StateAuthority)) return;
            Vector3 pointPosition = point.transform.position + LobbyNetworkAvatar.Origin;
            Vector3 itemPosition = item.Mode == 3 && BoardController.Instance != null
                ? BoardController.Instance.transform.TransformPoint(item.BoatLocalPosition) + LobbyNetworkAvatar.Origin : item.Position;
            if (Vector3.Distance(itemPosition, pointPosition) > 8 ||
                (item.Owner != PlayerRef.None && !SaleActorNear(info.Source, itemPosition))) return;
            var prefab = SharedItemCatalog.Load()?.Find(item.Template);
            var actual = prefab;
            if (actual == null && SharedItemsRuntime.TryFind(itemId, out var identity)) actual = identity.Item;
            if (actual == null || (point.SaleType == StoreType.Items ? actual.GetComponent<SellableItem>() == null
                : actual.GetComponent<EnemyReward>() == null)) return;
            if (item.Owner == PlayerRef.None && item.SalePrice > 0) reward = item.SalePrice - 1;
            bool exists = Sales.TryGet(pointId, out var sale);
            if ((!exists && Sales.Count >= Sales.Capacity) || sale.Coins > int.MaxValue - reward || sale.Revision == int.MaxValue) return;
            sale.Coins += reward; sale.Revision++; sale.Position = pointPosition;
            if (exists) Sales.Set(pointId, sale); else Sales.Add(pointId, sale);
            item.Mode = 4; item.Owner = PlayerRef.None;
            Items.Set(itemId, item);
            // The catalog asset has not run PickableItem.OnEnable/CheckTags.
            // Use the same reward threshold as CheckTags, with the seller's price.
            int tag = point.SaleType == StoreType.Enemies ? (int)ItemTag.Reward
                : reward < 10 ? (int)ItemTag.Trash : (int)ItemTag.Valuable;
            RPC_SaleFeedback(pointId, tag, info.Source);
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void RPC_CollectSale(ulong pointId, int revision, RpcInfo info = default)
        {
            int slot = CrewSlot(info.Source);
            if (Phase != 3 || !CanAct(info.Source) || slot < 0 || !Sales.TryGet(pointId, out var sale) ||
                sale.Revision != revision || sale.Coins <= 0 || sale.Revision == int.MaxValue ||
                !SaleActorNear(info.Source, sale.Position) || SaleCredits[slot] > int.MaxValue - sale.Coins) return;
            int amount = sale.Coins;
            SaleCredits.Set(slot, SaleCredits[slot] + amount);
            sale.Coins = 0; sale.Revision++;
            Sales.Set(pointId, sale);
            // This survives a refresh before the collecting client receives its
            // visual acknowledgement, and stale progress cannot overwrite it.
            if (Personal.TryGet(info.Source, out var data))
            {
                ReconcileSaleCredits(info.Source, ref data);
                Personal.Set(info.Source, data);
            }
            RPC_SaleCollectedFeedback(pointId, revision, info.Source);
        }

        private void ReconcileSaleCredits(PlayerRef player, ref SharedPersonalData data)
        {
            int slot = CrewSlot(player);
            if (slot < 0) return;
            int total = SaleCredits[slot];
            int acknowledged = Mathf.Clamp(data.SaleCredits, 0, total);
            data.Coins = (int)System.Math.Min(int.MaxValue, (long)data.Coins + total - acknowledged);
            data.SaleCredits = total;
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_SaleFeedback(ulong pointId, int tag, PlayerRef seller)
        {
            if (Runner.LocalPlayer == seller && SharedSalesRuntime.TryPoint(pointId, out var point))
                point.NotifySharedSale((ItemTag)tag);
        }
        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_SaleCollectedFeedback(ulong pointId, int revision, PlayerRef collector)
        {
            if (Runner.LocalPlayer == collector && SharedSalesRuntime.TryPoint(pointId, out var point) && point.CheckpointBag != null)
                point.CheckpointBag.ConfirmSharedCollection(revision);
        }
    }
}
