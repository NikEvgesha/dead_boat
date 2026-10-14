using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Fusion;
using UnityEditor;
using UnityEngine;

namespace DeadBoat.Online.Editor
{
    public static class SharedEnemyRuntimeProbe
    {
        public static string Result { get; private set; } = "not run";
        private static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        public static async void Run()
        {
            if (!EditorApplication.isPlaying || Result == "running") return;
            Result = "running";
            NetworkRunner first = null, second = null;
            GameObject owner = null;
            var objects = new List<GameObject>();
            var assets = new List<UnityEngine.Object>();
            float scale = Time.timeScale;
            EditorApplication.CallbackFunction clock = () => Time.timeScale = 1;
            EditorApplication.update += clock;
            try
            {
                Require(!SharedRunContext.Active && LoadingManager.Instance == null, "empty isolated scene");
                string room = "enemy-runtime-" + Guid.NewGuid().ToString("N");
                first = await Connect(room, 2701, true);
                SpawnAvatar(first);
                var state = first.Spawn(Resources.Load<NetworkObject>("Online/SharedDepartureState"),
                    onBeforeSpawned: (_, o) => o.GetComponent<SharedDepartureState>().Initialize(0, 2)).GetComponent<SharedDepartureState>();
                second = await Connect(room, 2702, false); SpawnAvatar(second);
                await Until(() => state.Phase == 2 && Replica(second)?.Phase == 2, "crew");
                var remote = Replica(second);
                state.RPC_SceneReady(); remote.RPC_SceneReady();
                await Until(() => state.Phase == 3 && remote.Phase == 3, "playing");
                owner = new GameObject("Enemy runtime isolated context");
                SharedRunContext.Begin(state.Seed, 0, owner, null, state);
                var runtime = owner.AddComponent<SharedEnemiesRuntime>();
                runtime.enabled = false; // Drive a reproducible 60 Hz / 10 Hz loop explicitly.
                var enemies = new List<WorldSpawnIdentity>();
                for (int i = 0; i < 2000; i++)
                {
                    var go = new GameObject("Non-enemy fixture"); objects.Add(go);
                    go.AddComponent<WorldSpawnIdentity>().Key = "test:non-enemy:" + i;
                }
                for (int i = 0; i < 100; i++)
                {
                    var go = new GameObject("Enemy fixture"); objects.Add(go);
                    var enemy = go.AddComponent<SharedProbeEnemy>(); enemy.SetHP(100);
                    var identity = go.AddComponent<WorldSpawnIdentity>(); identity.Key = "test:enemy-runtime:" + i;
                    enemies.Add(identity); state.SampleEnemy(identity, enemy);
                }
                await Until(() => remote.TryEnemy(enemies[99].Id, out _), "enemy snapshots");
                Benchmark(runtime, "authority");
                SetContext(remote);
                Benchmark(runtime, "proxy");
                var selected = enemies[0];
                SetEnemy(state, selected.Id, r => { r.Position = new Vector3(10, 0, 0); r.HP = 70; return r; });
                await Until(() => remote.TryEnemy(selected.Id, out var r) && r.HP == 70, "changed snapshot");
                var next = typeof(SharedEnemiesRuntime).GetField("nextSample", Private);
                next.SetValue(runtime, Time.realtimeSinceStartupAsDouble + 10);
                var animated = new GameObject("Animated enemy fixture"); objects.Add(animated);
                var clip = new AnimationClip(); assets.Add(clip);
                clip.SetCurve("", typeof(Transform), "localScale.x", AnimationCurve.Constant(0, 1, 1));
                var controller = new UnityEditor.Animations.AnimatorController(); assets.Add(controller);
                controller.AddLayer("Base Layer");
                var machine = controller.layers[0].stateMachine;
                var idle = machine.AddState("Idle"); idle.motion = clip; machine.defaultState = idle;
                var animator = animated.AddComponent<Animator>(); animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.runtimeAnimatorController = controller;
                var animatedEnemy = animated.AddComponent<SharedProbeEnemy>(); animatedEnemy.SetHP(100);
                var animatedIdentity = animated.AddComponent<WorldSpawnIdentity>(); animatedIdentity.Key = "test:animated-enemy";
                await Task.Delay(100); state.SampleEnemy(animatedIdentity, animatedEnemy);
                await Until(() => remote.TryEnemy(animatedIdentity.Id, out var r) && r.Animation != 0, "animation snapshot");
                Invoke(runtime); animator.Update(0);
                float animationStart = animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
                for (int i = 0; i < 15; i++) { Invoke(runtime); await Task.Delay(16); }
                Require(animator.GetCurrentAnimatorStateInfo(0).normalizedTime > animationStart + .1f,
                    "animation advances between identical snapshots");
                double end = Time.realtimeSinceStartupAsDouble + 3;
                while (Vector3.Distance(selected.transform.position, new Vector3(10, 0, 0) - LobbyNetworkAvatar.Origin) > .05f)
                {
                    Invoke(runtime); Require(Time.realtimeSinceStartupAsDouble < end, "frame interpolation"); await Task.Delay(16);
                }
                Require(selected.Enemy.SharedHP == 70, "proxy health changes between audits");
                SetEnemy(state, selected.Id, r => { r.HP = 0; return r; });
                await Until(() => remote.TryEnemy(selected.Id, out var r) && r.HP == 0, "death snapshot");
                Invoke(runtime); Invoke(runtime);
                Require(selected.Enemy.IsDead, "death applied");
                SetContext(state);
                Invoke(runtime);
                Require(state.TryEnemy(selected.Id, out var dead) && dead.HP == 0, "authority keeps replicated death");
                Result = "PASS: 100 enemies / 2000 other identities, frame interpolation/animation, changed HP/death, authority switch";
                Debug.Log("[Enemy runtime probe] " + Result);
            }
            catch (Exception e) { Result = "FAIL: " + e; Debug.LogWarning("[Enemy runtime probe] " + Result); }
            finally
            {
                foreach (var go in objects) if (go != null) UnityEngine.Object.Destroy(go);
                foreach (var asset in assets) if (asset != null) UnityEngine.Object.Destroy(asset);
                if (SharedRunContext.Active) SharedRunContext.End();
                if (owner != null) UnityEngine.Object.Destroy(owner);
                foreach (var r in new[] { first, second }) if (r != null) try { await r.Shutdown(); } catch (Exception) { }
                EditorApplication.update -= clock; Time.timeScale = scale;
            }
        }
        private static void Benchmark(SharedEnemiesRuntime runtime, string stage)
        {
            var next = typeof(SharedEnemiesRuntime).GetField("nextSample", Private);
            Invoke(runtime); // Warm caches and role transition outside the timed loop.
            var watch = System.Diagnostics.Stopwatch.StartNew();
            for (int i = 0; i < 200; i++)
            {
                next.SetValue(runtime, i % 6 == 0 ? 0d : Time.realtimeSinceStartupAsDouble + 1);
                Invoke(runtime);
            }
            watch.Stop();
            Debug.Log($"[Enemy runtime benchmark] {stage}: 200 calls / 34 audits / 100 enemies / 2000 non-enemies = {watch.Elapsed.TotalMilliseconds:F3} ms");
        }
        private static void Invoke(SharedEnemiesRuntime runtime) => typeof(SharedEnemiesRuntime).GetMethod("LateUpdate", Private).Invoke(runtime, null);
        private static void SetContext(SharedDepartureState state) => typeof(SharedRunContext).GetProperty("State").SetValue(null, state);
        private static void SetEnemy(SharedDepartureState state, ulong id, Func<SharedEnemyRecord, SharedEnemyRecord> update)
        {
            foreach (var page in SharedWorldPage.All(state.Runner))
                if (page.Enemies.TryGet(id, out var r)) { page.Enemies.Set(id, update(r)); return; }
            throw new InvalidOperationException("enemy page missing");
        }
        private static async Task<NetworkRunner> Connect(string room, long id, bool create) =>
            await (Task<NetworkRunner>)typeof(SharedAdProtocolProbe).GetMethod("Connect", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { room, id, create });
        private static void SpawnAvatar(NetworkRunner runner) => runner.SetPlayerObject(runner.LocalPlayer,
            runner.Spawn(AssetDatabase.LoadAssetAtPath<NetworkObject>("Assets/Online/Avatars/LobbyNetworkAvatar.prefab")));
        private static SharedDepartureState Replica(NetworkRunner runner) => UnityEngine.Object.FindObjectsByType<SharedDepartureState>(FindObjectsSortMode.None).FirstOrDefault(s => s.Runner == runner);
        private static void Require(bool value, string label) { if (!value) throw new InvalidOperationException(label); }
        private static async Task Until(Func<bool> condition, string label)
        {
            double end = Time.realtimeSinceStartupAsDouble + 20;
            while (!condition()) { Require(Time.realtimeSinceStartupAsDouble < end, "timeout: " + label); await Task.Delay(50); }
        }
    }
}
