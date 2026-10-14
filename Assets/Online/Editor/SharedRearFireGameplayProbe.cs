using System;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;

namespace DeadBoat.Online.Editor
{
    public static class SharedRearFireGameplayProbe
    {
        public static async Task Check(SharedDepartureState state)
        {
            var ui = UnityEngine.Object.FindAnyObjectByType<SharedBoatReturn>();
            typeof(SharedBoatReturn).GetMethod("SetOpen", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(ui, new object[] { false });
            await Until(() => !state.AdProtected(state.Runner.LocalPlayer) && !SharedAdProtection.Protected, "prior ad grace");
            var stats = PlayerStatsManager.Instance;
            var player = PlayerMovement.Instance;
            var board = BoardController.Instance;
            var platform = new GameObject("Rear fire test floor");
            var point = new Vector3(board.transform.position.x, board.transform.position.y + 2,
                state.RearFireZ - LobbyNetworkAvatar.Origin.z - 5);
            platform.transform.position = point - Vector3.up * 3;
            platform.AddComponent<BoxCollider>().size = new Vector3(20, 1, 20);
            try
            {
                Require(!state.InRearFire(state.RearFireZ) && state.InRearFire(state.RearFireZ - .01f), "strict logical boundary");
                stats.RestoreRecoveryHealth(80);
                player.Teleport(point); LobbyNetworkAvatar.SnapLocalPlayer(state.Runner);
                await Until(() => stats.Health < 80, "first burn");
                Require(stats.Health == 70, "exactly 10 HP");
                await Task.Delay(250);
                Require(stats.Health == 70, "not per frame");
                await Until(() => stats.Health < 70, "second second");
                Require(stats.Health == 60, "second hit exactly 10");
                player.Teleport(board.transform.position + Vector3.up * 3);
                LobbyNetworkAvatar.SnapLocalPlayer(state.Runner);
                await Task.Delay(1400);
                Require(stats.Health == 60, "safe position stops damage");
                const long token = 600060;
                state.RPC_AdBegin(token);
                await Until(() => state.AdShowing(state.Runner.LocalPlayer), "ad ACK");
                player.Teleport(point); LobbyNetworkAvatar.SnapLocalPlayer(state.Runner);
                await Task.Delay(1400);
                Require(stats.Health == 60, "ad protection in fire");
                state.RPC_AdEnd(token);
                await Until(() => !state.AdShowing(state.Runner.LocalPlayer), "ad ends");
                await Task.Delay(700);
                Require(stats.Health == 60, "post-ad grace in fire");
                await Until(() => stats.Health < 60, "fire resumes after grace");
                Require(stats.Health == 50, "no accumulated hits after ad");
                player.Teleport(board.transform.position + Vector3.up * 3);
                LobbyNetworkAvatar.SnapLocalPlayer(state.Runner);
                Debug.Log("[Rear fire gameplay probe] PASS: boundary, 10 HP/second, safe exit, ad/grace, no catch-up burst.");
            }
            finally { UnityEngine.Object.Destroy(platform); }
        }
        private static void Require(bool value, string label) { if (!value) throw new InvalidOperationException(label); }
        private static async Task Until(Func<bool> condition, string label)
        {
            double end = Time.realtimeSinceStartupAsDouble + 12;
            while (!condition()) { Require(Time.realtimeSinceStartupAsDouble < end, "timeout: " + label); await Task.Delay(40); }
        }
    }
}
