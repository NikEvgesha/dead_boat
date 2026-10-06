using Fusion;
using UnityEngine;

namespace DeadBoat.Online
{
    public sealed partial class SharedDepartureState
    {
        [Networked] public NetworkBool BoatInitialized { get; private set; }
        [Networked] public float BoatDistance { get; private set; }
        [Networked] public float BoatSpeed { get; private set; }
        [Networked] public float BoatFuel { get; private set; }
        [Networked] public float BoatMaxFuel { get; private set; }
        [Networked] public float BoatMaxSpeed { get; private set; }
        [Networked] private float BoatConsumption { get; set; }
        [Networked] private float BoatAcceleration { get; set; }
        [Networked] private float BoatCoast { get; set; }
        [Networked] private float BoatBrake { get; set; }
        [Networked] private float BoatEndDistance { get; set; }
        [Networked] public PlayerRef Driver { get; private set; }
        [Networked] private float DriverInput { get; set; }
        [Networked] private TickTimer DriverInputDeadline { get; set; }
        [Networked] public NetworkBool BoatFinished { get; private set; }

        internal static readonly Unity.Profiling.ProfilerMarker TickMarker = new("DeadBoat.SharedAuthority");
        private void TickBoat()
        {
            using var measured = TickMarker.Auto();
            EnsureWorldPages();
            ReleaseDisconnectedItems();
            TickWorld();
            TickEnemyEffects();
            var board = BoardController.Instance;
            if (board == null || !board.StartGame) return;
            if (!BoatInitialized)
            {
                if (BoatProfiles.Count != CrewCount) return;
                BoatDistance = board.TotalDistanceTraveled;
                BoatFuel = board.currentFuel;
                BoatMaxFuel = board.MaxFuel;
                BoatMaxSpeed = board.MaxSpeed;
                BoatConsumption = board.FuelConsumptionRate;
                BoatAcceleration = board.acceleration;
                BoatCoast = board.coastDeceleration;
                BoatBrake = board.brakeDeceleration;
                BoatEndDistance = board.SharedEndDistance;
                BoatInitialized = true;
            }
            if (BoatProfiles.Count == CrewCount) RefreshBoatBonuses(board);
            if (BoatFinished) return;
            bool present = false;
            foreach (var player in Runner.ActivePlayers) if (player == Driver) present = true;
            if (!present) Driver = PlayerRef.None;
            float input = Driver != PlayerRef.None && !DriverInputDeadline.ExpiredOrNotRunning(Runner)
                ? DriverInput : 0;
            SharedBoatMotion.Step(BoatSpeed, BoatFuel, BoatDistance, input, Runner.DeltaTime,
                BoatAcceleration, BoatMaxSpeed, BoatConsumption, BoatCoast, BoatBrake, BoatEndDistance,
                out float speed, out float fuel, out float distance);
            BoatSpeed = speed;
            BoatFuel = fuel;
            BoatDistance = distance;
            if (distance >= BoatEndDistance) BoatFinished = true;
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void RPC_Driver(bool occupy, RpcInfo info = default)
        {
            if (Phase != 3 || !Includes(info.Source) || BoatFinished) return;
            if (occupy && Driver == PlayerRef.None)
            {
                var seat = BoardController.Instance != null
                    ? BoardController.Instance.GetComponentInChildren<DriverSeatTrigger>() : null;
                if (seat == null || seat.driverSeatTransform == null) return;
                LobbyNetworkAvatar source = null;
                foreach (var avatar in LobbyNetworkAvatar.All)
                    if (avatar.Runner == Runner && avatar.Object.StateAuthority == info.Source) source = avatar;
                if (source == null || Vector3.Distance(source.LogicalPosition,
                    seat.driverSeatTransform.position + LobbyNetworkAvatar.Origin) > 4 + Mathf.Min(15, BoatSpeed * 0.2f)) return;
                Driver = info.Source;
                DriverInput = 0;
                DriverInputDeadline = default;
            }
            else if (!occupy && Driver == info.Source)
            {
                Driver = PlayerRef.None;
                DriverInput = 0;
            }
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void RPC_DriverInput(float input, RpcInfo info = default)
        {
            if (Phase != 3 || Driver == PlayerRef.None || info.Source != Driver ||
                float.IsNaN(input) || float.IsInfinity(input)) return;
            DriverInput = Mathf.Clamp(input, -1, 1);
            DriverInputDeadline = TickTimer.CreateFromSeconds(Runner, 0.75f);
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void RPC_AddFuel(float amount, RpcInfo info = default)
        {
            if (Phase != 3 || !BoatInitialized || !Includes(info.Source) ||
                float.IsNaN(amount) || float.IsInfinity(amount) || amount <= 0) return;
            BoatFuel = Mathf.Min(BoatFuel + amount, BoatMaxFuel);
        }
    }
}
