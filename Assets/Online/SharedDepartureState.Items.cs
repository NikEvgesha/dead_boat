using Fusion;
using UnityEngine;

namespace DeadBoat.Online
{
    public struct SharedItemRecord : INetworkStruct
    {
        public int Template, WeaponAmmo;
        public Vector3 Position;
        public Quaternion Rotation;
        public PlayerRef Owner;
        public Vector3 BoatLocalPosition;
        public Quaternion BoatLocalRotation;
        public float HomeZ;
        public NetworkBool Prunable, Dropped;
        public int Mode; // 0 free, 1 carried, 2 inventory, 3 attached, 4 consumed
    }

    public sealed partial class SharedDepartureState
    {
        private SharedItemsTable itemsTable;
        public SharedItemsTable Items => itemsTable ??= new SharedItemsTable(this);

        private void EnsureWorldPages()
        {
            if (SharedWorldPage.Get(Runner, 0) == null) SpawnPage(0);
        }
        private SharedWorldPage SpawnPage(int index) => Runner.Spawn(Resources.Load<NetworkObject>("Online/SharedWorldPage"),
            onBeforeSpawned: (_, obj) => obj.GetComponent<SharedWorldPage>().Initialize(index)).GetComponent<SharedWorldPage>();
        internal SharedWorldPage AvailablePage(bool enemies)
        {
            int index = 0;
            foreach (var p in SharedWorldPage.All(Runner))
            {
                index = Mathf.Max(index, p.Index + 1);
                if (enemies ? p.Enemies.Count < p.Enemies.Capacity : p.Items.Count < p.Items.Capacity) return p;
            }
            return SpawnPage(index);
        }

        public void RegisterItem(WorldSpawnIdentity identity)
        {
            if (!Object.HasStateAuthority || Items.ContainsKey(identity.Id) || Items.Count >= Items.Capacity) return;
            Items.Add(identity.Id, new SharedItemRecord
            {
                Template = identity.ItemTemplate != 0 ? identity.ItemTemplate :
                    SharedItemCatalog.Load()?.Identify(identity.GetComponent<PickableItem>()) ?? 0, WeaponAmmo = identity.GetComponentInChildren<RangedWeaponController>(true)?.CurrentAmmo ?? -1,
                Position = identity.transform.position + LobbyNetworkAvatar.Origin,
                Rotation = identity.transform.rotation, Owner = PlayerRef.None,
                HomeZ = identity.transform.position.z + LobbyNetworkAvatar.Origin.z, Prunable = identity.Prunable
            });
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void RPC_DropPersonalItem(ulong id, int template, Vector3 position, Quaternion rotation, int ammo, RpcInfo info = default)
        {
            if (Phase != 3 || !Includes(info.Source) || Items.ContainsKey(id) || !Finite(position) ||
                !Finite(new Vector3(rotation.x, rotation.y, rotation.z)) ||
                float.IsNaN(rotation.w) || float.IsInfinity(rotation.w) ||
                SharedItemCatalog.Load()?.Find(template) == null) return;
            LobbyNetworkAvatar source = null;
            foreach (var avatar in FindObjectsByType<LobbyNetworkAvatar>(FindObjectsSortMode.None))
                if (avatar.Runner == Runner && avatar.Object.StateAuthority == info.Source) source = avatar;
            if (source == null || Vector3.Distance(source.transform.position, position) > 20) return;
            Items.Add(id, new SharedItemRecord
            {
                Template = template, WeaponAmmo = Mathf.Clamp(ammo, -1, 10000), Position = position, Rotation = rotation,
                Owner = PlayerRef.None, HomeZ = position.z, Prunable = true, Dropped = true
            });
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void RPC_ClaimItem(ulong id, RpcInfo info = default)
        {
            if (Phase != 3 || !Includes(info.Source) || !Items.TryGet(id, out var item) ||
                (item.Mode != 0 && item.Mode != 3) || item.Owner != PlayerRef.None) return;
            if (item.Mode == 3 && BoardController.Instance != null)
            {
                item.Position = BoardController.Instance.transform.TransformPoint(item.BoatLocalPosition) + LobbyNetworkAvatar.Origin;
                item.Rotation = BoardController.Instance.transform.rotation * item.BoatLocalRotation;
            }
            LobbyNetworkAvatar avatar = null;
            foreach (var candidate in FindObjectsByType<LobbyNetworkAvatar>(FindObjectsSortMode.None))
                if (candidate.Runner == Runner && candidate.Object.StateAuthority == info.Source) avatar = candidate;
            if (avatar == null || Vector3.Distance(avatar.transform.position, item.Position) > 4 + BoatMovementAllowance(item.Position)) return;
            item.Owner = info.Source;
            item.Mode = 1;
            Items.Set(id, item);
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void RPC_ItemState(ulong id, int mode, Vector3 position, Quaternion rotation, int ammo = -1, RpcInfo info = default)
        {
            if (Phase != 3 || !Items.TryGet(id, out var item) || item.Owner != info.Source ||
                info.Source == PlayerRef.None || mode < 0 || mode > 4 || !Finite(position) ||
                !Finite(new Vector3(rotation.x, rotation.y, rotation.z)) ||
                float.IsNaN(rotation.w) || float.IsInfinity(rotation.w)) return;
            LobbyNetworkAvatar source = null;
            foreach (var avatar in FindObjectsByType<LobbyNetworkAvatar>(FindObjectsSortMode.None))
                if (avatar.Runner == Runner && avatar.Object.StateAuthority == info.Source) source = avatar;
            if (mode != 4 && (source == null || Vector3.Distance(source.transform.position, position) > 20)) return;
            if (mode == 3)
            {
                var boat = BoardController.Instance;
                if (boat == null) return;
                item.BoatLocalPosition = boat.transform.InverseTransformPoint(position - LobbyNetworkAvatar.Origin);
                item.BoatLocalRotation = Quaternion.Inverse(boat.transform.rotation) * rotation;
            }
            if (ammo >= 0 && ammo <= 10000) item.WeaponAmmo = ammo;
            if (mode == 0 && item.Mode == 2) item.Dropped = true;
            item.Mode = mode;
            item.Position = position;
            item.Rotation = rotation;
            if (mode == 0 || mode == 3) item.Owner = PlayerRef.None;
            Items.Set(id, item);
        }

        private void ReleaseDisconnectedItems()
        {
            foreach (var entry in Items)
            {
                var item = entry.Value;
                if (item.Owner == PlayerRef.None || item.Mode == 4) continue;
                bool present = false;
                foreach (var player in Runner.ActivePlayers) if (player == item.Owner) present = true;
                if (!present)
                {
                    item.Owner = PlayerRef.None;
                    item.Mode = 0;
                    Items.Set(entry.Key, item);
                }
            }
        }

        public void SampleFreeItem(ulong id, Vector3 position, Quaternion rotation)
        {
            if (!Object.HasStateAuthority || !Items.TryGet(id, out var item) || item.Owner != PlayerRef.None ||
                (item.Mode != 0 && item.Mode != 3)) return;
            if ((item.Position - position).sqrMagnitude < 0.0004f && Quaternion.Angle(item.Rotation, rotation) < 1) return;
            item.Position = position;
            item.Rotation = rotation;
            Items.Set(id, item);
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void RPC_BurnItem(ulong id, RpcInfo info = default)
        {
            if (Phase != 3 || !Items.TryGet(id, out var item) || item.Mode == 2 || item.Mode == 4 ||
                (item.Owner != info.Source && !(item.Owner == PlayerRef.None && info.Source == Object.StateAuthority))) return;
            if (!SharedItemsRuntime.TryFind(id, out var identity)) return;
            if (item.Mode == 3 && BoardController.Instance != null)
                item.Position = BoardController.Instance.transform.TransformPoint(item.BoatLocalPosition) + LobbyNetworkAvatar.Origin;
            var fuel = identity.GetComponent<FuelItem>();
            var deposit = BoardController.Instance != null
                ? BoardController.Instance.GetComponentInChildren<FuelDeposit>() : null;
            if (fuel == null || deposit == null ||
                Vector3.Distance(item.Position, deposit.transform.position + LobbyNetworkAvatar.Origin) > 6 + BoatMovementAllowance(item.Position)) return;
            item.Mode = 4;
            Items.Set(id, item);
            float amount = fuel.fuelValue * BoatFillMultiplier;
            BoatFuel = Mathf.Min(BoatMaxFuel, BoatFuel + amount);
        }

        private float BoatMovementAllowance(Vector3 position)
        {
            var boat = BoardController.Instance;
            return boat != null && Vector3.Distance(position, boat.transform.position + LobbyNetworkAvatar.Origin) < 40
                ? Mathf.Min(15, BoatSpeed * 0.2f) : 0;
        }

        private static bool Finite(Vector3 p) => !float.IsNaN(p.x) && !float.IsNaN(p.y) && !float.IsNaN(p.z) &&
            !float.IsInfinity(p.x) && !float.IsInfinity(p.y) && !float.IsInfinity(p.z);
    }
}
