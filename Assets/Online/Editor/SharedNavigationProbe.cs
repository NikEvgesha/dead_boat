using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;

namespace DeadBoat.Online.Editor
{
    public static class SharedNavigationProbe
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        public static string Run()
        {
            Require(EditorApplication.isPlaying && SaveManager.Instance?.PersistentProvider is SharedRunPreviewSave, "isolated real level required");
            CheckSchedule();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy/Zombie.prefab").GetComponent<ZombieController>();
            var player = PlayerMovement.Instance.transform;
            Vector3 originalPosition = player.position;
            using var fixture = new NavigationFixture(prefab.GetComponent<NavMeshAgent>().agentTypeID);
            Require(NavMesh.SamplePosition(fixture.Center, out var hit, 3, NavMesh.AllAreas), "ground fixture required");
            var enemy = Object.Instantiate(prefab, hit.position, Quaternion.identity);
            enemy.enabled = false;
            var health = new Dictionary<LobbyNetworkAvatar, float>();
            var state = SharedRunContext.State;
            bool shared = SharedRunContext.Playing;
            var driver = shared ? state.Driver : default;
            int warnings = 0;
            Application.LogCallback log = (message, trace, type) =>
            {
                if (type == LogType.Warning && message.Contains(enemy.name)) warnings++;
            };
            Application.logMessageReceived += log;
            try
            {
                if (shared)
                {
                    // Make nearest-target selection unambiguous, then restore within
                    // this same frame before any network tick can publish the fixture.
                    foreach (var avatar in LobbyNetworkAvatar.All)
                        if (avatar.Runner == state.Runner)
                        {
                            health.Add(avatar, avatar.Health);
                            typeof(LobbyNetworkAvatar).GetProperty("Health").SetValue(avatar, avatar.Object.HasStateAuthority ? 100f : 0f);
                        }
                    typeof(SharedDepartureState).GetProperty("Driver").SetValue(state, Fusion.PlayerRef.None);
                }
                player.position = hit.position + Vector3.back * 8;
                var agent = enemy.GetComponent<NavMeshAgent>();
                agent.enabled = true;
                Require(agent.Warp(hit.position) && agent.isOnNavMesh, "fixture agent binding");
                var update = (Action)Delegate.CreateDelegate(typeof(Action), enemy, typeof(ZombieController).GetMethod("Update", Private));
                var requests = typeof(ZombieController).GetField("pathRequests", Private);
                int Count() => (int)requests.GetValue(enemy);
                // Same live chase/agent/crew in both loops. This is a synchronous
                // microbenchmark, not 2000 simulated frames or whole-game FPS.
                var see = (Func<float, bool>)Delegate.CreateDelegate(typeof(Func<float, bool>), enemy, typeof(ZombieController).GetMethod("CheckSee", Private));
                var project = (Func<Vector3, Vector3>)Delegate.CreateDelegate(typeof(Func<Vector3, Vector3>), enemy, typeof(ZombieController).GetMethod("GetNavMeshTarget", Private));
                var attack = (Action<float>)Delegate.CreateDelegate(typeof(Action<float>), enemy, typeof(ZombieController).GetMethod("HandleAttack", Private));
                var selectedTarget = SharedEnemiesRuntime.Target(enemy.transform, player);
                typeof(ZombieController).GetField("target", Private).SetValue(enemy, selectedTarget);
                var legacy = Stopwatch.StartNew();
                for (int i = 0; i < 2000; i++)
                {
                    selectedTarget = SharedEnemiesRuntime.Target(enemy.transform, selectedTarget);
                    float distance = Vector3.Distance(enemy.transform.position, selectedTarget.position);
                    if (!see(distance)) continue;
                    agent.SetDestination(project(selectedTarget.position));
                    attack(distance);
                }
                legacy.Stop();
                agent.ResetPath();
                var timer = Stopwatch.StartNew();
                for (int i = 0; i < 2000; i++) update();
                timer.Stop();
                Require(Count() == (shared ? 1 : 2000), "same-frame native request count");
                player.position += Vector3.right * 0.1f;
                int before = Count(); update();
                Require(Count() == before + (shared ? 0 : 1), "small movement cadence");
                player.position += Vector3.forward * 3;
                before = Count(); update();
                Require(Count() == before + 1 && (agent.destination - project(player.position)).sqrMagnitude < 0.04f,
                    "large movement updates the requested destination");
                if (shared)
                {
                    var field = typeof(ZombieController).GetField("sharedPath", Private);
                    object schedule = field.GetValue(enemy);
                    typeof(SharedEnemyPathSchedule).GetField("next", Private).SetValue(schedule, Time.time - 1);
                    field.SetValue(enemy, schedule);
                    before = Count(); update();
                    Require(Count() == before + 1, "elapsed deadline replans");
                }
                agent.enabled = false;
                enemy.transform.position = Vector3.one * 1000000;
                player.position = enemy.transform.position + Vector3.back * 8;
                before = Count(); update(); update();
                Require(Count() == before && !agent.enabled, "unbound agent never receives path/attack updates");
                if (shared) Require(warnings == 1, "failed binding retries are bounded");
                enemy.transform.position = hit.position;
                player.position = hit.position + Vector3.back * 8;
                typeof(ZombieController).GetField("nextNavMeshProbe", Private).SetValue(enemy, Time.time - 1);
                update();
                Require(agent.enabled && agent.isOnNavMesh && Count() == before + 1, "agent recovers when mesh is available");
                string result = $"[Navigation probe] PASS shared={shared}; 2000 live-chase calls legacyMs={legacy.Elapsed.TotalMilliseconds:F3} updateMs={timer.Elapsed.TotalMilliseconds:F3}; requests={(shared ? 1 : 2000)}; movement/deadline/offmesh/rebind; policy cases PASS; microbenchmark, not FPS";
                UnityEngine.Debug.Log(result);
                return result;
            }
            finally
            {
                Application.logMessageReceived -= log;
                player.position = originalPosition;
                foreach (var pair in health) if (pair.Key != null) typeof(LobbyNetworkAvatar).GetProperty("Health").SetValue(pair.Key, pair.Value);
                if (shared) typeof(SharedDepartureState).GetProperty("Driver").SetValue(state, driver);
                enemy.gameObject.SetActive(false);
                Object.Destroy(enemy.gameObject);
            }
        }

        private static void CheckSchedule()
        {
            var schedule = new SharedEnemyPathSchedule();
            Require(schedule.ShouldRefresh(1, Vector3.zero, Vector3.zero, 0), "initial path");
            Require(!schedule.ShouldRefresh(1, Vector3.right, Vector3.zero, .19f), "small movement retains path");
            Require(schedule.ShouldRefresh(1, Vector3.right, Vector3.zero, .2f), "deadline");
            Require(schedule.ShouldRefresh(2, Vector3.right, Vector3.zero, .21f), "new target immediately");
            Require(schedule.ShouldRefresh(2, Vector3.right * 3, Vector3.zero, .22f), "two metre movement immediately");
            Require(schedule.ShouldRefresh(2, Vector3.right * 3, Vector3.forward * 2000, .23f), "origin shift immediately");
            schedule.Reset();
            Require(schedule.ShouldRefresh(2, Vector3.right * 3, Vector3.forward * 2000, .24f), "takeover/wake resets path");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        internal sealed class NavigationFixture : IDisposable
        {
            public readonly Vector3 Center = new(100000, 10000, 0);
            private NavMeshData data;
            private NavMeshDataInstance instance;
            public NavigationFixture(int agentType)
            {
                try
                {
                    var sources = new List<NavMeshBuildSource> { new()
                    {
                        shape = NavMeshBuildSourceShape.Box,
                        transform = Matrix4x4.TRS(Center, Quaternion.identity, Vector3.one),
                        size = new Vector3(40, 1, 40), area = 0
                    } };
                    data = NavMeshBuilder.BuildNavMeshData(NavMesh.GetSettingsByID(agentType), sources,
                        new Bounds(Center, new Vector3(50, 10, 50)), Vector3.zero, Quaternion.identity);
                    Require(data != null, "fixture navmesh built");
                    instance = NavMesh.AddNavMeshData(data);
                    Require(instance.valid, "fixture navmesh registered");
                }
                catch { Dispose(); throw; }
            }
            public void Dispose()
            {
                if (instance.valid) instance.Remove();
                if (data != null) Object.Destroy(data);
            }
        }
    }
}
