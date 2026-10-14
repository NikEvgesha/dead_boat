using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
namespace DeadBoat.Online.Editor
{
    public static class SharedBoatReturnGameplayProbe
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        public static async Task Check(SharedDepartureState state)
        {
            var manager = AdsManager.Instance;
            var field = typeof(AdsManager).GetField("adsProviders", Private);
            var original = field.GetValue(manager);
            var fakeObject = new GameObject("Boat return fake ads");
            var fake = fakeObject.AddComponent<SharedProbeAdsProvider>();
            var ui = UnityEngine.Object.FindAnyObjectByType<SharedBoatReturn>();
            GameObject blocker = null, disposed = null;
            try
            {
                Require(ui != null, "runtime return UI exists");
                field.SetValue(manager, new System.Collections.Generic.List<AdsProvider> { fake });
                fake.onShow = () => Require(state.AdShowing(state.Runner.LocalPlayer), "authority ACK before return ad");
                var player = PlayerMovement.Instance;
                var board = BoardController.Instance;
                var stats = PlayerStatsManager.Instance;
                CurrencyManager.Instance.AddCurrency(CurrencyType.Coins, 321 - CurrencyManager.Instance.Coins);
                var cards = JsonUtility.FromJson<Stats>(JsonUtility.ToJson(LevelStatManager.Instance.Stats));
                cards.HP = 35; cards.MoveSpeedMult = 1.3f;
                LevelStatManager.Instance.RestoreRecovery(cards, 2);
                PlayerAmmoManager.Instance.RestoreRecovery(31, 17, 9);
                stats.RestoreRecoveryHealth(63);
                var bag = Inventory.Instance.GetInventoryList().ToArray();
                string cardSnapshot = JsonUtility.ToJson(LevelStatManager.Instance.Stats);
                float hp = stats.Health;
                player.Teleport(board.transform.position + Vector3.back * 800 + Vector3.up * 2);
                await Task.Delay(400);
                fake.ready = false;
                ui.OpenOffer();
                await Task.Delay(250);
                var watch = (UnityEngine.UI.Button)typeof(SharedBoatReturn).GetField("watch", Private).GetValue(ui);
                Require(!watch.interactable && fake.requests == 0, "unavailable ad is explained and disabled");
                typeof(SharedBoatReturn).GetMethod("SetOpen", Private).Invoke(ui, new object[] { false });
                fake.ready = true;
                state.Items.Add(9900781, new SharedItemRecord { Owner = state.Runner.LocalPlayer, Mode = 1 });
                ui.OpenOffer(); ui.WatchAndReturn();
                Require(fake.requests == 0 && !ui.Busy, "physical cargo blocks return before ad");
                var pending = state.Items[9900781]; pending.Owner = Fusion.PlayerRef.None; pending.Mode = 4;
                state.Items.Set(9900781, pending);
                ui.WatchAndReturn();
                await Until(() => fake.requests == 1, "failed ad opened");
                fake.callback(false);
                Require(!ui.Busy && FlatDistance(player.transform.position, board.transform.position) > 750, "failed ad does not teleport");
                await Task.Delay(100);
                ui.WatchAndReturn();
                await Until(() => fake.requests == 2, "rewarded return opened");
                typeof(SharedDepartureState).GetProperty("BoatDistance").SetValue(state, state.BoatDistance + 10);
                await Task.Delay(350);
                blocker = new GameObject("Temporary occupied deck fixture");
                blocker.transform.position = board.transform.position + Vector3.up * 2 + Vector3.back * 2;
                blocker.AddComponent<BoxCollider>().size = new Vector3(20, 8, 20);
                fake.CompleteTwice();
                Require(ui.HasEarnedReturn && FlatDistance(player.transform.position, board.transform.position) > 750,
                    "occupied deck retains credit without teleport");
                blocker.SetActive(false);
                ui.WatchAndReturn();
                await Until(() => FlatDistance(player.transform.position, board.transform.position) < 6, "return to current moving boat");
                Require(fake.requests == 2 && !ui.HasEarnedReturn, "retry consumes credit without another ad");
                await Task.Delay(250);
                Require(stats.Health == hp && CurrencyManager.Instance.Coins == 321 &&
                    JsonUtility.ToJson(LevelStatManager.Instance.Stats) == cardSnapshot &&
                    Inventory.Instance.GetInventoryList().SequenceEqual(bag) && PlayerAmmoManager.Instance.GetAmmo(WeaponType.Pistol) == 31 &&
                    PlayerAmmoManager.Instance.GetAmmo(WeaponType.Rifle) == 17 && PlayerAmmoManager.Instance.GetAmmo(WeaponType.Shotgun) == 9,
                    "unchanged HP, wallet, cards, bag and ammo");
                Require(SharedAdProtection.Protected && Time.timeScale == 1, "post-ad protection and shared clock");
                player.Teleport(board.transform.position + Vector3.back * 800 + Vector3.up * 2);
                await Task.Delay(400);
                fake.CompleteTwice();
                Require(FlatDistance(player.transform.position, board.transform.position) > 750, "duplicate cannot repeat return");
                await Until(() => !SharedAdProtection.IsShowing && !PauseManager.Instance.IsPaused, "previous modal/ad released");
                disposed = new GameObject("Disposed return UI fixture");
                var removed = disposed.AddComponent<SharedBoatReturn>();
                removed.OpenOffer(); removed.WatchAndReturn();
                await Until(() => fake.requests == 3, "pending callback before teardown; hp=" + stats.Health +
                    " paused=" + PauseManager.Instance.IsPaused + " showing=" + SharedAdProtection.IsShowing + " active=" + stats.InGame);
                UnityEngine.Object.Destroy(disposed);
                await Task.Delay(100);
                fake.CompleteTwice();
                Require(FlatDistance(player.transform.position, board.transform.position) > 750, "late callback after teardown ignored");
                ui.OpenOffer();
                Debug.Log("[Boat return probe] PASS: not-ready, carry gate, failed ad, ACK, moving boat, credit/retry, personal state, duplicate/late callback, clock/protection.");
            }
            finally
            {
                field.SetValue(manager, original);
                if (blocker != null) UnityEngine.Object.Destroy(blocker);
                if (disposed != null) UnityEngine.Object.Destroy(disposed);
                UnityEngine.Object.Destroy(fakeObject);
            }
        }
        private static float FlatDistance(Vector3 a, Vector3 b) { a.y = b.y = 0; return Vector3.Distance(a, b); }
        private static void Require(bool condition, string label) { if (!condition) throw new InvalidOperationException(label); }
        private static async Task Until(Func<bool> test, string label)
        {
            double end = Time.realtimeSinceStartupAsDouble + 10;
            while (!test()) { Require(Time.realtimeSinceStartupAsDouble < end, "timeout: " + label); await Task.Delay(50); }
        }
    }
}
