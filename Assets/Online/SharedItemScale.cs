using UnityEngine;

namespace DeadBoat.Online
{
    // World-space lengths of the item's local axes, independent of its current
    // inventory/boat parent. Zero in older sidecars means "unknown": keep the
    // generated fit (or the catalog size for a cold replica), never collapse it.
    public static class SharedItemScale
    {
        public static bool IsValid(Vector3 value) => Valid(value.x) && Valid(value.y) && Valid(value.z);
        private static bool Valid(float value) => !float.IsNaN(value) && !float.IsInfinity(value) && value != 0;

        public static Vector3 Capture(Transform target)
        {
            if (target == null) return Vector3.zero;
            Vector3 scale = target.lossyScale;
            return IsValid(scale) ? scale : Vector3.zero;
        }

        public static Vector3 Capture(PickableItem item)
        {
            if (item == null) return Vector3.zero;
            var identity = item.GetComponent<WorldSpawnIdentity>();
            var state = SharedRunContext.State;
            if (SharedRunContext.Playing && identity != null && state != null &&
                state.Items.TryGet(identity.Id, out var record) && IsValid(record.WorldScale)) return record.WorldScale;
            return Capture(item.transform);
        }

        public static Vector3 PresentationMultiplier(PickableItem item)
        {
            if (item == null) return Vector3.one;
            var catalog = SharedItemCatalog.Load();
            var prefab = catalog?.Find(catalog.Identify(item));
            Vector3 baseline = prefab != null ? Capture(prefab.transform) : Vector3.zero;
            Vector3 actual = Capture(item);
            if (!IsValid(baseline) || !IsValid(actual)) return Vector3.one;
            var ratio = new Vector3(actual.x / baseline.x, actual.y / baseline.y, actual.z / baseline.z);
            return IsValid(ratio) ? ratio : Vector3.one;
        }

        // Use the CURRENT composed size, not parent.lossyScale: child rotation
        // under a non-uniform parent can introduce shear. This scales each
        // matrix column to its requested length without multiplying the fit again.
        // Call after the final parent/rotation change.
        public static void Apply(Transform target, Vector3 worldScale)
        {
            if (target == null || !IsValid(worldScale)) return;
            Vector3 current = target.lossyScale;
            if (!IsValid(current) || (Mathf.Approximately(current.x, worldScale.x) &&
                Mathf.Approximately(current.y, worldScale.y) && Mathf.Approximately(current.z, worldScale.z))) return;
            Vector3 local = target.localScale;
            var next = new Vector3(local.x * (worldScale.x / current.x), local.y * (worldScale.y / current.y),
                local.z * (worldScale.z / current.z));
            if (IsValid(next)) target.localScale = next;
        }

        public static PickableItem CreateReplica(PickableItem prefab, Vector3 position, Quaternion rotation, Vector3 worldScale)
        {
            var item = Object.Instantiate(prefab, position, rotation);
            Apply(item.transform, worldScale);
            return item;
        }

        public static PickableItem CreateReplica(PickableItem prefab, Vector3 worldScale)
        {
            var item = Object.Instantiate(prefab);
            Apply(item.transform, worldScale);
            return item;
        }
    }
}
