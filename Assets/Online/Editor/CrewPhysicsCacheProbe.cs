using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace DeadBoat.Online.Editor
{
    public static class CrewPhysicsCacheProbe
    {
        public static string Run()
        {
            if (!SharedRunContext.Playing) throw new InvalidOperationException("shared level required");
            var runtime = UnityEngine.Object.FindAnyObjectByType<SharedItemsRuntime>();
            var state = SharedRunContext.State;
            var capture = (Action<SharedDepartureState>)Delegate.CreateDelegate(typeof(Action<SharedDepartureState>), runtime,
                typeof(SharedItemsRuntime).GetMethod("CaptureCrewPositions", BindingFlags.Instance | BindingFlags.NonPublic));
            var cached = (Func<PickableItem, bool, bool>)Delegate.CreateDelegate(typeof(Func<PickableItem, bool, bool>), runtime,
                typeof(SharedItemsRuntime).GetMethod("NearCrew", BindingFlags.Instance | BindingFlags.NonPublic));
            var avatars = LobbyNetworkAvatar.All.GetEnumerator();
            LobbyNetworkAvatar selected = null;
            while (avatars.MoveNext()) if (avatars.Current.Runner == state.Runner) { selected = avatars.Current; break; }
            avatars.Dispose();
            var item = UnityEngine.Object.FindObjectsByType<PickableItem>(FindObjectsSortMode.None)
                .First(x => x.Status == ItemStatus.Free && x.GetComponent<WorldSpawnIdentity>() != null);
            Vector3 position = item.transform.position;
            Vector3 avatarPosition = selected.transform.position;
            var driver = state.Driver;
            float health = selected.Health;
            try
            {
                capture(state);
                int cases = 0;
                foreach (var point in new[] { position, selected.LogicalPosition - LobbyNetworkAvatar.Origin,
                    selected.LogicalPosition - LobbyNetworkAvatar.Origin + Vector3.right * 1000 })
                {
                    item.transform.position = point;
                    foreach (bool previous in new[] { false, true })
                    {
                        Require(cached(item, previous) == Legacy(item, previous), "near/far/hysteresis differs"); cases++;
                    }
                }
                selected.transform.position += Vector3.right * 300;
                capture(state);
                Require(cached(item, false) == Legacy(item, false), "movement capture differs");
                item.transform.position = selected.LogicalPosition - LobbyNetworkAvatar.Origin + Vector3.right * (item.PhysicsActivationDistance + 2);
                Require(!cached(item, false) && cached(item, true), "hysteresis boundary preserved");
                item.transform.position = selected.LogicalPosition - LobbyNetworkAvatar.Origin;
                typeof(LobbyNetworkAvatar).GetProperty("Health").SetValue(selected, 0f);
                capture(state);
                Require(cached(item, false) && Legacy(item, false), "dead crew member dropped");
                typeof(LobbyNetworkAvatar).GetProperty("Health").SetValue(selected, health);
                typeof(SharedDepartureState).GetProperty("Driver").SetValue(state, selected.Object.StateAuthority);
                capture(state);
                item.transform.position = selected.LogicalPosition - LobbyNetworkAvatar.Origin;
                Require(cached(item, false) && Legacy(item, false), "driver seat must wake nearby physics");
                typeof(SharedDepartureState).GetProperty("Driver").SetValue(state, driver);
                selected.transform.position = avatarPosition;
                item.transform.position = position + Vector3.right * 10000;
                capture(state);
                for (int i = 0; i < 100; i++) { Legacy(item, false); cached(item, false); }
                const int calls = 20000;
                int first = 0, second = 0;
                var timer = Stopwatch.StartNew();
                for (int i = 0; i < calls; i++) if (Legacy(item, false)) first++;
                timer.Stop(); double legacyMs = timer.Elapsed.TotalMilliseconds;
                timer.Restart();
                for (int i = 0; i < calls; i++) if (cached(item, false)) second++;
                timer.Stop();
                Require(first == second, "benchmark visits differ");
                string result = $"[Crew physics cache] PASS: {cases} near/far/hysteresis cases, movement/dead/driver capture; {calls} same-item far queries legacyMs={legacyMs:F3} cachedMs={timer.Elapsed.TotalMilliseconds:F3}; microbenchmark, not whole-game FPS.";
                Debug.Log(result); return result;
            }
            finally
            {
                item.transform.position = position; selected.transform.position = avatarPosition;
                typeof(LobbyNetworkAvatar).GetProperty("Health").SetValue(selected, health);
                typeof(SharedDepartureState).GetProperty("Driver").SetValue(state, driver);
                capture(state);
            }
        }
        // Previous production calculation retained only as an A/B oracle.
        private static bool Legacy(PickableItem item, bool wasNearby)
        {
            var state = SharedRunContext.State;
            Vector3 position = item.transform.position + LobbyNetworkAvatar.Origin;
            float radius = item.PhysicsActivationDistance + (wasNearby ? 5 : 0);
            foreach (var avatar in LobbyNetworkAvatar.All)
                if (avatar.Runner == state.Runner && state.Includes(avatar.Object.StateAuthority) &&
                    (avatar.LogicalPosition - position).sqrMagnitude <= radius * radius) return true;
            return false;
        }
        private static void Require(bool value, string text) { if (!value) throw new InvalidOperationException(text); }
    }
}
