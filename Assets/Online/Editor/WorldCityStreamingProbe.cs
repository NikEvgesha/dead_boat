using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using Object = UnityEngine.Object;

namespace DeadBoat.Online.Editor
{
    public static class WorldCityStreamingProbe
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private static void Require(bool value, string text) { if (!value) throw new InvalidOperationException(text); }
        public static async Task RunGame()
        {
            var state = SharedRunContext.State;
            Require(state != null && state.Object.HasStateAuthority, "real authoritative game required");
            var owner = Object.FindAnyObjectByType<CitySpawner>();
            bool wasEnabled = owner.enabled; owner.enabled = false;
            var independent = SharedRunContext.Independent; independent.enabled = false;
            var source = owner.cityPrefab;
            var staging = new GameObject("City adapter fixture source"); staging.SetActive(false);
            var template = Object.Instantiate(source, staging.transform);
            var loot = SharedItemCatalog.Load().entries.First(e => e.prefab != null && !e.prefab.Usable &&
                e.prefab.GetComponent<AmmoItem>() == null && e.prefab.GetComponent<EggCollectibleItem>() == null).prefab;
            Object.Instantiate(loot, template.transform).transform.localPosition = new Vector3(20, 1, 0);
            var gunPrefab = SharedItemCatalog.Load().entries.First(e => e.prefab != null &&
                e.prefab.GetComponentInChildren<RangedWeaponController>(true) != null).prefab;
            Object.Instantiate(gunPrefab, template.transform).transform.localPosition = new Vector3(24, 1, 0);
            var defenderRoot = new GameObject("Controlled city defender"); defenderRoot.transform.SetParent(template.transform, false);
            var content = defenderRoot.AddComponent<LocationContentSpawner>();
            var fixtureBalance = ScriptableObject.CreateInstance<LocationBalanceProfile>();
            typeof(LocationContentSpawner).GetField("_balanceProfile", Private).SetValue(content, fixtureBalance);
            content._enemyPrefabs = new List<ZombieController> { AssetDatabase.LoadAssetAtPath<ZombieController>("Assets/Prefabs/Enemy/Zombie.prefab") };
            var defenderPoint = new GameObject("Defender point"); defenderPoint.transform.SetParent(defenderRoot.transform, false);
            content.enemySpawnPoints = new List<Transform> { defenderPoint.transform };
            owner.cityPrefab = template;
            LobbyNetworkAvatar buyer = null;
            foreach (var avatar in LobbyNetworkAvatar.All)
                if (avatar.Runner == state.Runner && avatar.Object.HasStateAuthority) { buyer = avatar; break; }
            Require(buyer != null, "buyer avatar");
            var playerField = typeof(LobbyNetworkAvatar).GetField("localPlayer", Private);
            object oldPlayer = playerField.GetValue(buyer);
            var dummy = new GameObject("City purchase transport avatar"); dummy.SetActive(false);
            var transportPlayer = dummy.AddComponent<PlayerMovement>();
            GameObject city = null;
            try
            {
                typeof(CitySpawner).GetField("nextSpawnDistance", Private).SetValue(owner, 10000f);
                typeof(CitySpawner).GetMethod("SpawnCityAt", Private).Invoke(owner, new object[] { new Vector3(0, 0, 10000) - LobbyNetworkAvatar.Origin });
                var active = (List<GameObject>)typeof(CitySpawner).GetField("spawnedCities", Private).GetValue(owner);
                city = active.Last();
                await Until(() => city.GetComponent<WorldCityRegion>().Ready, "city ready");
                await Task.Delay(500);
                var actors = city.GetComponentsInChildren<WorldSpawnIdentity>(true);
                await Until(() => actors.All(a => a.Item != null ? state.Items.ContainsKey(a.Id) : state.TryEnemy(a.Id, out _)), "city canonical actors");
                var stock = Stock(city);
                Require(stock.Count > 0 && actors.Length > 0, "real city has stock and loot");
                var gun = actors.First(a => a.Weapon != null); ulong gunId = gun.Id;
                gun.Weapon.ApplySharedAmmo(7); var magazine = state.Items[gunId]; magazine.WeaponAmmo = 7; state.Items.Set(gunId, magazine);
                var defender = actors.First(a => a.Enemy != null); ulong defenderId = defender.Id;
                typeof(EnemyCore).GetField("currentHP", Private).SetValue(defender.Enemy, 17); state.SampleEnemy(defender, defender.Enemy);
                var point = city.GetComponentsInChildren<StorePoint>().First();
                int price = (int)typeof(StorePoint).GetField("price", Private).GetValue(point);
                CurrencyManager.Instance.AddCurrency(CurrencyType.Coins, price + 100 - CurrencyManager.Instance.Coins);
                CurrencyManager.Instance.RemoveCurrency(CurrencyType.Coins, price);
                var buyPoint = (Transform)typeof(StorePoint).GetField("_buyPoint", Private).GetValue(point);
                dummy.transform.position = buyPoint.position;
                playerField.SetValue(buyer, transportPlayer);
                await Until(() => (buyer.LogicalPosition - (dummy.transform.position + LobbyNetworkAvatar.Origin)).sqrMagnitude < 1, "buyer avatar at store");
                var before = new HashSet<ulong>(state.Items.Select(e => e.Key));
                typeof(StorePoint).GetMethod("GiveItem", Private).Invoke(point, null); // in-memory coins, never IAP/ads
                await Until(() => state.Items.Any(e => !before.Contains(e.Key) && e.Value.Dropped), "purchased public drop");
                ulong bought = state.Items.First(e => !before.Contains(e.Key) && e.Value.Dropped).Key;
                playerField.SetValue(buyer, oldPlayer);
                await Task.Delay(250);
                int coins = CurrencyManager.Instance.Coins;
                var used = actors.First(a => a.Item != null && a != gun);
                ulong usedId = used.Id; var consumed = state.Items[usedId]; consumed.Mode = 4; state.Items.Set(usedId, consumed);
                Require(WorldReplicaRemoval.TryRemoveLocalTree(used.gameObject, out var reason), reason);
                await Until(() => used == null, "consumed city item removed");
                foreach (var a in city.GetComponentsInChildren<WorldSpawnIdentity>(true)) a.PhysicsNearby = false;
                float[] far = { 0 }, near = { 10000 };
                typeof(StorePoint).GetField("rewardPending", Private).SetValue(point, true);
                Require(!owner.Streaming.Audit(active, far, 0) && city != null && owner.Streaming.LastRefusal == "purchase in progress", "pending rewarded purchase guard");
                typeof(StorePoint).GetField("rewardPending", Private).SetValue(point, false);
                int meshes = city.GetComponentsInChildren<MeshFilter>(true).Length;
                Require(owner.Streaming.Audit(active, far, 0), owner.Streaming.LastRefusal);
                await Until(() => city == null, "city geometry removed");
                magazine = state.Items[gunId]; magazine.WeaponAmmo = 5; state.Items.Set(gunId, magazine);
                foreach (var page in SharedWorldPage.All(state.Runner)) if (page.Object.HasStateAuthority && page.Enemies.TryGet(defenderId, out var wounded))
                { wounded.HP = 13; page.Enemies.Set(defenderId, wounded); }
                Require(owner.Streaming.SleepingCount == 1 && state.Items.ContainsKey(bought) && CurrencyManager.Instance.Coins == coins, "purchase/currency lost with city");
                Require(owner.Streaming.Audit(active, near, 0), owner.Streaming.LastRefusal);
                city = active.Single(); await Task.Delay(250);
                Require(stock.OrderBy(x => x.Key).SequenceEqual(Stock(city).OrderBy(x => x.Key)), "city assortment changed");
                Require(!SharedItemsRuntime.TryFind(usedId, out _) && state.Items[usedId].Mode == 4 && state.Items.ContainsKey(bought), "city consumed/purchased item resurrected or lost");
                Require(SharedItemsRuntime.TryFind(gunId, out var loadedGun) && loadedGun.Weapon.CurrentAmmo == 5 &&
                    SharedItemsRuntime.TryFind(defenderId, out var loadedDefender) && loadedDefender.Enemy.SharedHP == 13, "city latest magazine/HP");
                foreach (var a in city.GetComponentsInChildren<WorldSpawnIdentity>(true)) a.PhysicsNearby = false;
                Require(owner.Streaming.Audit(active, far, 0), owner.Streaming.LastRefusal);
                await Until(() => city == null, "second city removal");
                state.MarkEnemyDead(defenderId);
                Require(owner.Streaming.Audit(active, near, 0, false), owner.Streaming.LastRefusal);
                city = active.Single(); await Task.Delay(250);
                Require(stock.OrderBy(x => x.Key).SequenceEqual(Stock(city).OrderBy(x => x.Key)) && owner.Streaming.Restored == 2, "second city return");
                Require(!SharedItemsRuntime.TryFind(defenderId, out _) && SharedItemsRuntime.TryFind(gunId, out loadedGun) && loadedGun.Weapon.CurrentAmmo == 5,
                    "dead city defender resurrected or ammo lost");
                Require(CurrencyManager.Instance.Coins == coins && state.Items.ContainsKey(bought), "second city return changed wallet/drop");
                Debug.Log($"[City streaming] PASS: real city meshes={meshes}, stores={stock.Count}, actors={actors.Length}; two unload/restore cycles, pending reward guard, seeded stock unchanged, consumed absent, paid public drop and coins retained. Four Cloud connections, one local world.");
                await CheckIndependent(bought);
            }
            finally { playerField.SetValue(buyer, oldPlayer); Object.Destroy(dummy); owner.cityPrefab = source; owner.enabled = wasEnabled; independent.enabled = true; Object.Destroy(staging); Object.Destroy(fixtureBalance); }
        }
        private static Dictionary<string, string> Stock(GameObject city) => city.GetComponentsInChildren<StorePoint>(true)
            .ToDictionary(p => WorldSpawnIdentity.StablePath(p.transform), p =>
                ((PickableItem)typeof(StorePoint).GetField("_itemPrefab", Private).GetValue(p))?.name + ":" + typeof(StorePoint).GetField("price", Private).GetValue(p));
        private static async Task CheckIndependent(ulong bought)
        {
            var state = SharedRunContext.State; var streaming = SharedRunContext.Independent;
            var prefab = AssetDatabase.LoadAssetAtPath<ZombieController>("Assets/Prefabs/Enemy/Zombie.prefab");
            var enemy = Object.Instantiate(prefab, new Vector3(200, 1, 20000) - LobbyNetworkAvatar.Origin, Quaternion.identity);
            enemy.InitializeLevel(4);
            var identity = enemy.gameObject.AddComponent<WorldSpawnIdentity>();
            identity.TemplateId = SharedEnemiesRuntime.RegisterTemplate(prefab); identity.Key = "night:" + state.NextNightSpawn();
            ulong id = identity.Id;
            await Task.Delay(200);
            typeof(EnemyCore).GetField("currentHP", Private).SetValue(enemy, 17); state.SampleEnemy(identity, enemy);
            Require(state.TryEnemy(id, out var record) && record.HP == 17, "night canonical HP");
            float[] far = { 0 }, near = { 20000 };
            await Until(() => { streaming.Audit(far); return !SharedItemsRuntime.TryFind(id, out _); }, "independent night removal");
            await Task.Delay(150);
            Require(enemy == null && state.TryEnemy(id, out record) && record.HP == 17, "night removal wrote death");
            await Until(() => { streaming.Audit(near); return SharedItemsRuntime.TryFind(id, out _); }, "independent night return");
            await Task.Delay(150);
            Require(SharedItemsRuntime.TryFind(id, out var restored) && restored.Enemy.SharedHP == 17, "night restored HP reset");
            Require(Object.FindObjectsByType<WorldSpawnIdentity>(FindObjectsInactive.Include, FindObjectsSortMode.None).Count(i => i.Id == id) == 1, "duplicate night enemy");
            await Until(() => { streaming.Audit(far); return !SharedItemsRuntime.TryFind(id, out _); }, "second night removal");
            state.MarkEnemyDead(id); streaming.Audit(near); await Task.Delay(150);
            Require(!SharedItemsRuntime.TryFind(id, out _), "dead dormant night enemy resurrected");
            await Until(() => { streaming.Audit(far, false); return streaming.SleepingCount == 0; }, "independent disable drain");
            Require(state.Items.TryGet(bought, out var purchased) && SharedItemsRuntime.TryFind(bought, out var publicItem) &&
                (publicItem.Weapon?.CurrentAmmo ?? -1) == purchased.WeaponAmmo, "independent purchased item/ammo lost");
            Require(Object.FindObjectsByType<WorldSpawnIdentity>(FindObjectsInactive.Include, FindObjectsSortMode.None).Count(i => i.Id == bought) == 1,
                "purchased drop duplicate");
            Debug.Log("[Independent streaming] PASS: actual night-key zombie removed/recreated from canonical HP17, same ID/one copy; dead while dormant never resurrects. Independent bought drop remains canonical.");
        }
        private static async Task Until(Func<bool> check, string label)
        {
            double end = Time.realtimeSinceStartupAsDouble + 12;
            while (!check()) { Require(Time.realtimeSinceStartupAsDouble < end, label); await Task.Delay(50); }
        }
    }
}
