using System;
using System.Collections;
using UnityEngine;

namespace DeadBoat.Online
{
    // City IDs occupy a separate LOCAL archive namespace, not network IDs.
    public sealed class WorldCityRegion : MonoBehaviour
    {
        public const int Namespace = 1000000000;
        public GameObject Prefab { get; private set; }
        public CitySpawner Owner { get; private set; }
        public int Index { get; private set; }
        public float LogicalZ { get; private set; }
        public bool Ready { get; private set; }
        private bool tracked;
        public static bool TryIndex(string key, out int index)
        {
            index = 0;
            if (key == null || !key.StartsWith("city:", StringComparison.Ordinal)) return false;
            int end = 5;
            while (end < key.Length && char.IsDigit(key[end])) end++;
            if (end == 5 || end < key.Length && key[end] != ':' && key[end] != '/' ||
                !int.TryParse(key.Substring(5, end - 5), out int z) || z < 0 || z >= Namespace) return false;
            index = Namespace + z; return true;
        }
        public void Initialize(CitySpawner owner, GameObject prefab, float z, bool restoring = false)
        {
            if (!TryIndex("city:" + Mathf.RoundToInt(z), out int index)) throw new InvalidOperationException("Invalid city coordinate");
            Owner = owner; Prefab = prefab; LogicalZ = z; Index = index;
            var anchor = GetComponent<WorldGenerationAnchor>() ?? gameObject.AddComponent<WorldGenerationAnchor>();
            anchor.Key = "city:" + Mathf.RoundToInt(z);
            tracked = SharedRunContext.Membership.Begin(index, z);
            if (!tracked && restoring) throw new InvalidOperationException("City membership budget");
            int ordinal = 0;
            foreach (var content in GetComponentsInChildren<LocationContentSpawner>(true))
            {
                string key = anchor.Key + ":" + ordinal++;
                content.InitializeStableGeneration(key);
                if (restoring) content.RestoreStableGeneration();
            }
            foreach (var item in GetComponentsInChildren<PickableItem>(true))
            {
                if (item.GetComponentInParent<LocationContentSpawner>(true) != null ||
                    item.GetComponentInParent<EnemyCore>(true) != null || item.GetComponentInParent<StorePoint>(true) != null ||
                    item.GetComponent<WorldSpawnIdentity>() != null) continue;
                string key = "authored:" + gameObject.scene.name + ":" + WorldSpawnIdentity.StablePath(item.transform);
                if (restoring || SharedRunContext.State.Items.ContainsKey(WorldSpawnIdentity.IdForKey(key)))
                    WorldRegionRestore.InitializeItem(item.gameObject, key, null);
                else item.gameObject.AddComponent<WorldSpawnIdentity>().Key = key;
            }
            if (restoring) { SharedRunContext.Membership.Seal(index); Ready = tracked; }
        }
        private IEnumerator Start()
        {
            if (Ready || !tracked) yield break;
            yield return null; // all child Start methods finish their deterministic generation first
            SharedRunContext.Membership?.Seal(Index); Ready = true;
        }
    }
}
