using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Fusion;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DeadBoat.Online.Editor
{
    // Empty Play scene, two real Cloud peers. No SaveManager, owner-key writes,
    // gameplay scene, ads, recovery UI or Player Build. Physics is paused.
    public static class SharedItemScaleNetworkProbe
    {
        public static string Result { get; private set; } = "not run";
        private static bool running;
        private static Stopwatch clock;
        private static readonly MethodInfo ItemAudit = typeof(SharedItemsRuntime).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic);

        public static async Task RunAsync()
        {
            if (running) throw new InvalidOperationException("Scale network probe already running.");
            if (!EditorApplication.isPlaying || SaveManager.Instance != null || LoadingManager.Instance != null ||
                GameManager.Instance != null || SharedRunContext.Active || SoloRunBranch.Active ||
                Object.FindObjectsByType<NetworkRunner>(FindObjectsSortMode.None).Length != 0)
                throw new InvalidOperationException("Requires an empty isolated Play scene, without live runs/managers/runners.");

            running = true; Result = "running"; clock = Stopwatch.StartNew();
            var previousMode = NetworkProjectConfig.Global.PeerMode;
            float previousScale = Time.timeScale;
            var previousSocial = Object.FindAnyObjectByType<MirraSocialService>();
            NetworkRunner first = null, second = null;
            GameObject fixture = null;
            var ids = new HashSet<ulong>();
            bool passed = false;
            try
            {
                Time.timeScale = 0;
                NetworkProjectConfig.Global.PeerMode = NetworkProjectConfig.PeerModes.Multiple;
                var catalog = SharedItemCatalog.Load();
                var entry = catalog.entries.Single(e => e.prefab != null && e.prefab.name == "Loot203_T10_Coffer");
                var avatarPrefab = AssetDatabase.LoadAssetAtPath<NetworkObject>("Assets/Online/Avatars/LobbyNetworkAvatar.prefab");
                Require(avatarPrefab != null && ItemAudit != null, "probe prefabs/method available");
                // Load assets and fit before establishing connections; disk IO is not a network timeout test.
                fixture = new GameObject("Scale264 isolated network fixture");
                fixture.AddComponent<GameManager>().enabled = false; // Awake only; never Start/save initialization.
                var localization = fixture.AddComponent<LocalizationManager>(); localization.enabled = false;
                var managers = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Managers.prefab");
                var localizationData = managers.GetComponentInChildren<LocalizationManager>(true).LocalizationData;
                Require(localizationData != null, "read-only production localization fixture");
                typeof(LocalizationManager).GetField("localizationData", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(localization, localizationData);
                typeof(LocalizationManager).GetField("currentLanguage", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(localization, "Ru");
                var runtime = fixture.AddComponent<SharedItemsRuntime>(); runtime.enabled = false;
                var slot = new GameObject("Coffer fit slot"); slot.transform.SetParent(fixture.transform, false);
                var fitted = SharedItemScale.CreateReplica(entry.prefab, Vector3.zero, Quaternion.identity, Vector3.zero);
                fitted.transform.SetParent(fixture.transform, true); Quiesce(fitted);
                LocationContentSpawner.PlaceItemAtSpawnPoint(fitted.gameObject, new LocationSpawnPoint
                { spawnTransform = slot.transform, alignVisualBase = true, maxVisualFootprint = .72f });
                Vector3 canonical = SharedItemScale.Capture(fitted.transform);
                Near(Width(fitted), .72f, "native production fit width");
                float fittedColliderWidth = ColliderWidth(fitted);
                var identity = fitted.gameObject.AddComponent<WorldSpawnIdentity>();
                identity.ItemTemplate = entry.id; identity.Key = "test:scale264-" + Guid.NewGuid().ToString("N");
                ulong sourceId = identity.Id; ids.Add(sourceId);

                string room = "scale264-" + Guid.NewGuid().ToString("N");
                first = SharedReadyBarrierProbe.NewRunner(); await Budget(Start(first, room), "first Cloud connection");
                var authority = first.Spawn(Resources.Load<NetworkObject>("Online/SharedDepartureState"),
                    onBeforeSpawned: (_, obj) => obj.GetComponent<SharedDepartureState>().Initialize(0, 2)).GetComponent<SharedDepartureState>();
                second = SharedReadyBarrierProbe.NewRunner(); await Budget(Start(second, room), "second Cloud connection");
                await Until(() => authority.Phase == 2 && Replica(second) != null && Replica(second).Phase == 2, "sealed crew");
                var remote = Replica(second);
                first.SetPlayerObject(first.LocalPlayer, first.Spawn(avatarPrefab));
                second.SetPlayerObject(second.LocalPlayer, second.Spawn(avatarPrefab));
                await Until(() => first.ActivePlayers.All(p => first.TryGetPlayerObject(p, out var obj) && obj != null) &&
                    second.ActivePlayers.All(p => second.TryGetPlayerObject(p, out var obj) && obj != null), "avatars on both peers");
                // Companion profile reads are unrelated to this fixture; no loadout/save operations run.
                foreach (var companions in Object.FindObjectsByType<AvatarCompanions>(FindObjectsSortMode.None)) companions.enabled = false;
                authority.RPC_SceneReady(); remote.RPC_SceneReady();
                await Until(() => authority.Phase == 3 && remote.Phase == 3, "ready barrier");
                authority.RegisterItem(identity);
                await Until(() => remote.Items.TryGet(sourceId, out var r) && Same(r.WorldScale, canonical), "registration scale over wire");
                remote.RPC_ClaimItem(sourceId);
                await Until(() => authority.Items[sourceId].Owner == second.LocalPlayer && remote.Items[sourceId].Mode == 1, "remote claim");
                remote.RPC_ItemState(sourceId, 2, Vector3.zero, Quaternion.identity, 7);
                await Until(() => authority.Items[sourceId].Mode == 2 && remote.Items[sourceId].WeaponAmmo == 7, "inventory/ammo transport");
                Near(remote.Items[sourceId].WorldScale, canonical, "claim/inventory keeps fit");
                remote.RPC_RecoveryItem(1, 0, new SharedRecoveryItem
                { WorldId = sourceId, Template = entry.id, Ammo = 7, WorldScale = canonical });
                remote.RPC_RecoveryCommit(new SharedPersonalData { Revision = 1, ItemCount = 1, Health = 100 });
                int bagKey = remote.CrewSlot(second.LocalPlayer) * SharedDepartureState.RecoverySlots;
                await Until(() => authority.PersonalItems.TryGet(bagKey, out var bag) && Same(bag.WorldScale, canonical) &&
                    remote.Personal.TryGet(second.LocalPlayer, out var data) && data.Revision == 1, "atomic recovery bag scale over wire");
                Require(remote.PersonalItems[bagKey].Ammo == 7, "bag ammo field retained");
                remote.RPC_ItemState(sourceId, 0, Vector3.right, Quaternion.identity, 3);
                await Until(() => authority.Items[sourceId].Mode == 0 && authority.Items[sourceId].Owner == PlayerRef.None &&
                    remote.Items[sourceId].WeaponAmmo == 3, "drop and updated ammo field");
                Near(remote.Items[sourceId].WorldScale, canonical, "drop keeps fit");

                // Runtime is bound to one proxy world, as in the other multi-peer protocol fixtures.
                SharedRunContext.Begin(remote.Seed, remote.LevelId, fixture, null, remote);
                SharedRunContext.Independent.enabled = false;
                ulong droppedId = WorldSpawnIdentity.IdForKey("drop:scale264-" + Guid.NewGuid().ToString("N")); ids.Add(droppedId);
                remote.RPC_DropPersonalItem(droppedId, entry.id, Vector3.right * 2, Quaternion.identity, 5, false, canonical);
                await Until(() => authority.Items.ContainsKey(droppedId) && remote.Items.ContainsKey(droppedId), "personal drop scale publication");
                Near(authority.Items[droppedId].WorldScale, canonical, "personal drop RPC fit");
                ItemAudit.Invoke(runtime, null); // Actual cold dropped discovery factory, before any item Start.
                Require(SharedItemsRuntime.TryFind(droppedId, out var cold) && cold.Item != null, "cold dropped discovery created replica");
                Quiesce(cold.Item);
                CheckReplica(cold, canonical, fittedColliderWidth, "cold dropped discovery");
                var coldObject = cold.gameObject;
                cold.PhysicsNearby = false; // Controlled distant-focus fixture; no live player movement here.
                Require(SharedRunContext.Independent.Audit(new[] { 10000f }), "independent suspension");
                await Until(() => coldObject == null, "deferred replica removal");
                Require(!SharedItemsRuntime.TryFind(droppedId, out _) && remote.Items.ContainsKey(droppedId), "suspension keeps canonical record");
                Require(SharedRunContext.Independent.Audit(new[] { 0f }), "independent restoration");
                Require(SharedItemsRuntime.TryFind(droppedId, out var restored) && restored.Item != null, "independent factory created replica");
                Quiesce(restored.Item);
                CheckReplica(restored, canonical, fittedColliderWidth, "independent restoration");
                for (int i = 0; i < 5; i++) ItemAudit.Invoke(runtime, null);
                CheckReplica(restored, canonical, fittedColliderWidth, "repeat proxy application");

                await Budget(first.Shutdown(), "former authority shutdown");
                await Until(() => remote.Object.HasStateAuthority && SharedWorldPage.All(second).All(p => p.Object.HasStateAuthority), "authority/page takeover");
                Near(remote.Items[sourceId].WorldScale, canonical, "takeover original scale");
                Near(remote.Items[droppedId].WorldScale, canonical, "takeover dropped scale");
                Require(remote.Items[sourceId].WeaponAmmo == 3 && remote.Items[droppedId].WeaponAmmo == 5 &&
                    Same(remote.PersonalItems[bagKey].WorldScale, canonical), "takeover preserves ammo fields and bag scale");
                Result = "PASS: two Cloud peers, native coffer fit, world/carry/inventory/drop scale and ammo fields, atomic bag scale, actual cold dropped factory, independent suspend/restore, repeated proxy apply, authority/page takeover. NOT TESTED: recovery UI/coordinator, real weapon firing, inventory UI, full scene/reconnect, Physics simulation or WebGL.";
                passed = true;
            }
            catch (Exception error) { Result = "FAIL: " + error; throw; }
            finally
            {
                SharedRunContext.End();
                foreach (var item in Object.FindObjectsByType<WorldSpawnIdentity>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    if (ids.Contains(item.Id))
                    {
                        typeof(WorldSpawnIdentity).GetProperty("RemovingReplica", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(item, true);
                        Object.Destroy(item.gameObject);
                    }
                if (fixture != null) Object.Destroy(fixture);
                try { await Stop(second); await Stop(first); }
                finally
                {
                    NetworkProjectConfig.Global.PeerMode = previousMode;
                    Time.timeScale = previousScale;
                    if (previousSocial == null)
                    {
                        var created = Object.FindAnyObjectByType<MirraSocialService>();
                        if (created != null) Object.Destroy(created.gameObject);
                    }
                    running = false;
                }
                if (passed && !Result.StartsWith("PASS", StringComparison.Ordinal))
                    throw new InvalidOperationException(Result);
                if (passed) UnityEngine.Debug.Log("[Item scale network264] " + Result);
            }
        }

        private static async Task Start(NetworkRunner runner, string room)
        {
            var config = NetworkProjectConfig.Deserialize(NetworkProjectConfig.Serialize(NetworkProjectConfig.Global));
            config.PrefabTable = NetworkProjectConfig.Global.PrefabTable; config.PeerMode = NetworkProjectConfig.PeerModes.Multiple;
            var start = await runner.StartGame(new StartGameArgs
            {
                GameMode = GameMode.Shared, Config = config, SessionName = room,
                CustomPhotonAppSettings = SharedRunRecovery.Settings("eu"),
                AuthValues = new Photon.Realtime.AuthenticationValues(Guid.NewGuid().ToString("N")),
                CustomLobbyName = "deadboat-scale264-probe-v1", PlayerCount = 2, IsVisible = false,
                SceneManager = runner.GetComponent<LobbySceneManager>(), ObjectProvider = runner.GetComponent<NetworkObjectProviderDefault>()
            });
            Require(start.Ok, "Cloud connect " + start.ShutdownReason);
        }
        private static async Task Budget(Task task, string label)
        {
            int left = Math.Max(1, 100000 - (int)clock.ElapsedMilliseconds);
            if (await Task.WhenAny(task, Task.Delay(left)) != task)
            {
                _ = task.ContinueWith(t => { _ = t.Exception; }, TaskContinuationOptions.OnlyOnFaulted);
                throw new TimeoutException("Scale264 global work deadline: " + label);
            }
            await task;
        }
        private static async Task Until(Func<bool> condition, string label)
        {
            while (!condition())
            { Require(clock.ElapsedMilliseconds < 100000, "global work deadline: " + label); await Task.Delay(25); }
        }
        private static async Task Stop(NetworkRunner runner)
        {
            if (runner == null) return;
            var root = runner.gameObject;
            try
            {
                var shutdown = runner.Shutdown();
                int left = Math.Max(1, Math.Min(8000, 119000 - (int)clock.ElapsedMilliseconds));
                if (await Task.WhenAny(shutdown, Task.Delay(left)) == shutdown) await shutdown;
                else
                {
                    _ = shutdown.ContinueWith(t => { _ = t.Exception; }, TaskContinuationOptions.OnlyOnFaulted);
                    Result = "FAIL: transport cleanup timeout; " + Result;
                }
            }
            catch (Exception error) { Result = "FAIL: transport cleanup " + error.GetType().Name + "; " + Result; }
            finally { if (root != null) Object.Destroy(root); }
        }
        private static SharedDepartureState Replica(NetworkRunner runner) => Object.FindObjectsByType<SharedDepartureState>(FindObjectsSortMode.None).FirstOrDefault(s => s.Runner == runner);
        private static void Quiesce(PickableItem item)
        {
            foreach (var behaviour in item.GetComponentsInChildren<MonoBehaviour>(true)) behaviour.enabled = false;
            foreach (var body in item.GetComponentsInChildren<Rigidbody>(true)) body.isKinematic = true;
        }
        private static void CheckReplica(WorldSpawnIdentity item, Vector3 scale, float colliderWidth, string label)
        { Near(SharedItemScale.Capture(item.transform), scale, label + " scale"); Near(Width(item.Item), .72f, label + " visual width"); Near(ColliderWidth(item.Item), colliderWidth, label + " collider width"); }
        private static float Width(PickableItem item)
        {
            var renderers = item.GetComponentsInChildren<Renderer>(true); Require(renderers.Length > 0, "coffer renderers");
            var bounds = renderers[0].bounds; foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
            return Mathf.Max(bounds.size.x, bounds.size.z);
        }
        private static float ColliderWidth(PickableItem item)
        { Physics.SyncTransforms(); var collider = item.GetComponent<BoxCollider>(); Require(collider != null, "coffer BoxCollider"); return Mathf.Max(collider.bounds.size.x, collider.bounds.size.z); }
        private static bool Same(Vector3 a, Vector3 b) => (a - b).sqrMagnitude < .000001f;
        private static void Near(Vector3 a, Vector3 b, string label) => Require(Same(a, b), label + ": " + a + " != " + b);
        private static void Near(float a, float b, string label) => Require(Mathf.Abs(a - b) < .0005f, label + ": " + a + " != " + b);
        private static void Require(bool value, string label) { if (!value) throw new InvalidOperationException(label); }
    }
}
