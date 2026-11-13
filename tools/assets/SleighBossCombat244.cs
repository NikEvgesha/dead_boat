// Run with Unity MCP script-execute (full code), method Run. Uses isolated gameplay only.
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;
using DeadBoat.Events.NewYear;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class SleighBossCombat244
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    const string Report = "Docs/locations/previews/sleigh-combat244.txt";
    static void Check(bool ok, string message)
    { if (!ok) throw new InvalidOperationException(message); }
    static async Task Until(Func<bool> condition, string message, double seconds = 15)
    {
        double end = EditorApplication.timeSinceStartup + seconds;
        while (!condition() && EditorApplication.timeSinceStartup < end) await Task.Delay(50);
        Check(condition(), message);
    }
    static SleighGiftAttack[] Pending() => Object.FindObjectsByType<SleighGiftAttack>(FindObjectsSortMode.None).Where(g => !g.Resolved).ToArray();
    static void Move(PlayerMovement player, Vector3 point)
    {
        var capsule = player.GetComponent<CharacterController>(); capsule.enabled = false;
        player.transform.position = point; capsule.enabled = true; Physics.SyncTransforms();
    }

    public static async Task<string> Run()
    {
        Check(EditorApplication.isPlaying, "Use Play Mode");
        await Until(()=>SaveManager.Instance&&PlayerMovement.Instance&&Object.FindAnyObjectByType<SleighBossController>(),"Review gameplay did not initialize",45);
        Check(SaveManager.Instance.PersistentProvider is DeadBoat.Online.SharedRunPreviewSave, "Requires isolated save");
        var boss = Object.FindAnyObjectByType<SleighBossController>();
        Check(boss && boss.State == SleighBossController.FlightState.Waiting, "Needs fresh waiting boss");
        var player = PlayerMovement.Instance;
        var health = PlayerStatsManager.Instance;
        var active = Object.FindAnyObjectByType<ActiveItemManager>();
        var item = Inventory.Instance.GetItems().First(i => i && i.GetComponentInChildren<RangedWeaponController>(true));
        active.SwitchActiveItem(item);
        var rifle = item.GetComponentInChildren<RangedWeaponController>(true);
        Check(rifle, "Review rifle must be equipped");
        var lines = new List<string> { "DB-ON-244 local Play Mode combat audit, " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            "Scene=" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name + "; save=" + SaveManager.Instance.PersistentProvider.GetType().Name };
        var visited = new HashSet<SleighBossController.FlightState>();
        var impacts = new List<string>();
        Action<SleighGiftAttack> observe = g => impacts.Add(g.name + ": radius=" + g.Radius + " hits=" + g.HitPlayers + " playerHP=" + health.Health);
        boss.GiftImpacted += observe;
        bool enabled = player.enabled;
        bool inventoryEnabled = Inventory.Instance.enabled, activeEnabled = active.enabled, inputEnabled = PlayerInput.Instance.enabled;
        Inventory.Instance.enabled = false; active.enabled = false; PlayerInput.Instance.enabled = false;
        foreach (var name in new[] { "_attack", "_healing", "_reload" }) typeof(PlayerInput).GetField(name, Private)?.SetValue(PlayerInput.Instance,false);
        player.enabled = false;
        try
        {
            PauseManager.Instance.SetPause(false);
            ControlManager.Instance.ForceGameplayCursor();
            await Task.Delay(200);
            rifle.ApplySharedAmmo(1);
            typeof(RangedWeaponController).GetField("_isShooting",Private).SetValue(rifle,false);
            typeof(RangedWeaponController).GetField("_isReloading",Private).SetValue(rifle,false);
            // Invoke the equipped weapon's real use handler: raycast, cards, HP and ammo.
            Camera.main.transform.LookAt(boss.GetComponent<Collider>().bounds.center);
            Physics.SyncTransforms();
            Check(Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out var aimed, 80) && aimed.collider.GetComponentInParent<SleighBossController>() == boss, "Boss must be visible to a real weapon ray");
            int before = boss.SharedHP, ammo = rifle.CurrentAmmo;
            lines.Add("Gun preflight: allowed="+typeof(RangedWeaponController).GetProperty("CardActionsAllowed",Private).GetValue(rifle)+" mask="+((LayerMask)typeof(RangedWeaponController).GetField("hitLayers",Private).GetValue(rifle)).value+" bossLayer="+boss.gameObject.layer+" enabled="+rifle.enabled+" active="+item.gameObject.activeInHierarchy);
            typeof(RangedWeaponController).GetMethod("UseUpdate", Private).Invoke(rifle, null);
            await Task.Delay(150);
            lines.Add("Gun result: HP="+boss.SharedHP+" ammo="+rifle.CurrentAmmo+" state="+boss.State);
            Check(boss.SharedHP < before, "Equipped rifle did not damage sleigh");
            Check(rifle.CurrentAmmo == ammo - 1, "Real shot did not consume ammo");
            lines.Add("PASS equipped rifle use: boss HP " + before + " -> " + boss.SharedHP + "; ammo " + ammo + " -> " + rifle.CurrentAmmo);
            Check(boss.State != SleighBossController.FlightState.Waiting, "Shooting must start encounter");
            await Until(() => Pending().Length > 0, "First AI volley missing");
            var first = Pending();
            Check(first.All(g => !g.Launched), "Landing warning must precede flight");
            foreach(var g in first)
            {
                var marker=Resources.FindObjectsOfTypeAll<Transform>().Where(t=>t.gameObject.scene.IsValid()&&t.name=="Gift_Landing_Warning")
                    .OrderBy(t=>Vector3.Distance(t.position,g.LandingPoint)).First();
                var ring=marker.GetComponentsInChildren<LineRenderer>().First(l=>l.name=="Blast_Radius");
                float worldRadius=Vector3.Distance(marker.position,ring.transform.TransformPoint(ring.GetPosition(0)));
                Check(Mathf.Abs(worldRadius-g.Radius)<.01f,"Warning scale differs from actual blast radius");
            }
            lines.Add("PASS warning radius remains in metres on scaled East_Bank collider (25,0.5,53)");
            lines.Add("PASS red volley: visible warnings before launch; gifts=" + first.Length);
            // Look at a neighbouring mark from the real camera eye, so the full ring fits.
            Camera.main.transform.LookAt(first.OrderByDescending(g => Vector3.Distance(g.LandingPoint, player.transform.position)).First().LandingPoint);
            ScreenCapture.CaptureScreenshot("Docs/locations/previews/sleigh-red-warning244.png");
            float initialHealth = health.Health;
            await Until(() => boss.GiftsImpacted >= first.Length, "First gifts did not impact");
            Check(Mathf.Approximately(initialHealth - health.Health, 10), "One red impact must damage once, even with several player colliders");
            lines.Add("PASS red impact once per player: HP " + initialHealth + " -> " + health.Health);
            await Until(() => boss.State == SleighBossController.FlightState.Recovery, "No reachable recovery pause");
            visited.Add(boss.State);
            Check(boss.FindSurface(boss.transform.position, out var floor), "No floor under recovery");
            float clearance = boss.GetComponent<Collider>().bounds.min.y - floor.point.y;
            Check(clearance < 1.1f && clearance > .1f, "Hull is outside melee reach or under floor: " + clearance);
            lines.Add("PASS low recovery: hull bottom above actual floor=" + clearance.ToString("F3") + " m; distance=" + Vector3.Distance(player.transform.position,boss.transform.position).ToString("F2"));

            await Until(() => boss.VolleysThrown >= 2 && Pending().Length > 0, "Second AI volley missing", 18);
            var blue = Pending();
            Check(blue.All(g => g.name.Contains("Blue")), "Second pattern is not blue");
            Vector3[] locked = blue.Select(g => g.LandingPoint).ToArray();
            float beforeDodge = health.Health;
            Vector3 dodge = player.transform.position + Vector3.forward * 9;
            Check(boss.FindSurface(dodge, out var dodgeFloor), "No dodge floor");
            Move(player, dodgeFloor.point + Vector3.up * .08f);
            await Task.Delay(300);
            Check(blue.Select((g,i) => Vector3.Distance(g.LandingPoint,locked[i]) < .01f).All(b => b), "Warnings chased player");
            int expected = boss.GiftsImpacted + blue.Length;
            await Until(() => boss.GiftsImpacted >= expected, "Blue impacts missing");
            Check(Mathf.Approximately(beforeDodge,health.Health), "Dodging blue markers did not avoid damage");
            lines.Add("PASS blue locked marks + dodge: " + blue.Length + " gifts, HP unchanged=" + health.Health);
            await Until(() => boss.VolleysThrown >= 3 && Pending().Length > 0, "Third AI volley missing", 18);
            var gold = Pending();
            Check(gold.Length == 1 && gold[0].name.Contains("Gold") && Mathf.Approximately(gold[0].Radius,3.3f), "Gold pattern size/radius mismatch");
            float beforeGold = health.Health;
            expected = boss.GiftsImpacted + 1;
            await Until(() => boss.GiftsImpacted >= expected, "Gold impact missing");
            Check(Mathf.Approximately(beforeGold-health.Health,26), "Gold present damage mismatch");
            lines.Add("PASS gold large impact: radius=3.3 m, HP " + beforeGold + " -> " + health.Health);
            // Start a fresh live warning before death; all hazards must disappear.
            await Until(() => boss.VolleysThrown >= 4 && Pending().Length > 0, "Fourth volley missing", 18);
            boss.TakeDamage(boss.SharedHP);
            await Task.Delay(400);
            Check(boss.IsDead && boss.State == SleighBossController.FlightState.Defeated, "Boss did not die");
            Check(Pending().Length == 0, "Live gifts remain after boss defeat");
            float afterDeath = health.Health;
            await Task.Delay(2500);
            Check(Mathf.Approximately(afterDeath,health.Health), "Damage continued after defeat");
            Check(!boss.GetComponent<Collider>().enabled, "Defeated boss collider remains active");
            lines.Add("PASS defeat during volley: all gifts cancelled, collider off, no post-death damage");
            var meleeItem=Inventory.Instance.GetItems().First(i=>i&&i.GetComponentInChildren<MeleWeapon>(true));
            active.SwitchActiveItem(meleeItem);
            await Task.Delay(250);
            var specimen=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(SleighBossReview244.BossPath)).GetComponent<SleighBossController>();
            specimen.enabled=false;
            Vector3 forward=Vector3.ProjectOnPlane(player.transform.forward,Vector3.up).normalized;
            specimen.transform.position=player.transform.position+forward*3.25f+Vector3.up*.77f;
            Camera.main.transform.rotation=Quaternion.LookRotation(forward);
            Physics.SyncTransforms();
            int meleeBefore=specimen.SharedHP;
            var melee=meleeItem.GetComponentInChildren<MeleWeapon>(true);
            typeof(MeleWeapon).GetMethod("UseUpdate",Private).Invoke(melee,null);
            try
            {
                await Until(()=>specimen.SharedHP<meleeBefore,"Real sword swing did not reach low sleigh hull",3);
                lines.Add("PASS real sword swing + trigger: low hull HP "+meleeBefore+" -> "+specimen.SharedHP);
            }
            finally { Object.Destroy(specimen.gameObject); }
            lines.AddRange(impacts);
            lines.Add("Limit: local Editor simulation only; manual balance, WebGL, replication and final-boss rewards not accepted.");
            File.WriteAllLines(Report, lines);
            return string.Join("\n", lines);
        }
        catch (Exception e)
        {
            lines.Add("FAIL " + e.Message); lines.AddRange(impacts); File.WriteAllLines(Report, lines); throw;
        }
        finally { player.enabled = enabled; Inventory.Instance.enabled=inventoryEnabled; active.enabled=activeEnabled; PlayerInput.Instance.enabled=inputEnabled; boss.GiftImpacted -= observe; }
    }
}
