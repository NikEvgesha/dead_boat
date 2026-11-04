using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace DeadBoat.Online.Editor
{
    public static class CompanionProbe
    {
        public static string Validate()
        {
            var trail = new CompanionTrail();
            trail.Reset(Vector3.zero);
            for (int i = 1; i <= 20; i++) trail.Observe(Vector3.forward * i * .25f);
            Require(Vector3.Distance(trail.Sample(1.3f, Vector3.forward), Vector3.forward * 3.7f) < .001f, "distance history");
            int count = trail.Count;
            for (int i = 0; i < 500; i++) trail.Observe(Vector3.forward * 5);
            Require(trail.Count == count, "stationary trail grows");
            for (int i = 21; i < 1000; i++) trail.Observe(Vector3.forward * i * .25f);
            Require(trail.Count == 128, "unbounded trail");
            trail.Observe(Vector3.forward * 5000);
            Require(trail.Count == 1 && (trail.Sample(1.3f, Vector3.forward) - Vector3.forward * 4998.7f).sqrMagnitude < .001f, "teleport smear");
            trail.Reset(Vector3.forward * 300);
            Require(trail.Count == 1, "explicit origin reset");
            var parent = new GameObject("Temporary companion visual validation"); parent.SetActive(false);
            int models = 0;
            try
            {
                var catalog = Resources.Load<EggHatchingCatalog>("Eggs/EggHatchingCatalog");
                foreach (var animal in catalog.Animals)
                foreach (var stage in animal.stages)
                {
                    if (stage.animalPrefab == null) continue;
                    var visual = CompanionVisual.Create(stage.animalPrefab, stage.tint, parent.transform);
                    Require(visual.GetComponentsInChildren<Renderer>(true).Length > 0, "missing mesh " + animal.animalId);
                    Require(visual.GetComponentsInChildren<MonoBehaviour>(true).Length == 0 &&
                        visual.GetComponentsInChildren<Collider>(true).Length == 0 &&
                        visual.GetComponentsInChildren<Rigidbody>(true).Length == 0 &&
                        visual.GetComponentsInChildren<AudioSource>(true).Length == 0, "gameplay component leaked");
                    foreach (var skin in visual.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                        foreach (var bone in skin.bones) Require(bone != null && bone.IsChildOf(visual.transform), "external bone reference");
                    UnityEngine.Object.DestroyImmediate(visual); models++;
                }
                Require(models > 0, "empty catalog");
            }
            finally { UnityEngine.Object.DestroyImmediate(parent); }
            return "[Companions] PASS bounded distance trail/stationary/teleport/reset; render-only models=" + models;
        }
        public static async Task RunShared(SharedDepartureState authority, SharedDepartureState remote)
        {
            var runner = authority.Runner;
            var first = runner.GetPlayerObject(runner.LocalPlayer).GetComponent<LobbyNetworkAvatar>();
            var view = first.GetComponent<AvatarCompanions>();
            typeof(AvatarCompanions).GetField("nextLoadout", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(view, double.MaxValue);
            var contextOwner = new GameObject("Temporary companion run context");
            SharedRunContext.Begin(authority.Seed, authority.LevelId, contextOwner, null, authority);
            try
            {
            first.Companions.Set(0, new CompanionRecord { Animal = "paca", Stage = 3 });
            first.Companions.Set(1, new CompanionRecord { Animal = "dog", Stage = 1 });
            first.Companions.Set(2, new CompanionRecord { Animal = "cow", Stage = 2 });
            LobbyNetworkAvatar copy = null;
            await Until(() => {
                var obj = remote.Runner.GetPlayerObject(runner.LocalPlayer);
                if (obj == null) return false;
                copy = obj.GetComponent<LobbyNetworkAvatar>();
                return copy.Companions[0].Animal.ToString() == "paca" && copy.Companions[0].Stage == 3 &&
                    copy.Companions[1].Animal.ToString() == "dog" && copy.Companions[2].Stage == 2;
            });
            await Until(() => ((GameObject[])typeof(AvatarCompanions).GetField("models", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(view)).All(x => x != null));
            var rendered = (GameObject[])typeof(AvatarCompanions).GetField("models", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(view);
            for (int i = 0; i < 3; i++)
                Require(rendered[i].activeInHierarchy && rendered[i].GetComponentsInChildren<MonoBehaviour>(true).Length == 0 &&
                    rendered[i].GetComponentsInChildren<Collider>(true).Length == 0 &&
                    rendered[i].transform.position.z < first.LogicalPosition.z, "runtime companion visual/placement");
            first.Companions.Set(0, new CompanionRecord { Animal = "cow", Stage = 2 });
            first.Companions.Set(2, default);
            await Until(() => copy.Companions[0].Animal.ToString() == "cow" && copy.Companions[2].Animal.ToString() == "");
            Debug.Log("[Companions] PASS two Photon peers and live renderer: three IDs/stages/order, change/removal, visible render-only followers behind player; no per-pet transforms");
            }
            finally { SharedRunContext.End(); UnityEngine.Object.Destroy(contextOwner); }
        }
        private static async Task Until(Func<bool> check)
        {
            double end = Time.realtimeSinceStartupAsDouble + 8;
            while (!check()) { if (Time.realtimeSinceStartupAsDouble > end) throw new TimeoutException("companion replication"); await Task.Delay(25); }
        }
        private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    }
}
