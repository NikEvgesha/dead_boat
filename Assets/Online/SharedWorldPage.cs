using System.Collections;
using System.Collections.Generic;
using Fusion;

namespace DeadBoat.Online
{
    // Each page stays below Fusion's 32 KiB per-object allocation limit.
    public sealed class SharedWorldPage : NetworkBehaviour
    {
        private static readonly Dictionary<NetworkRunner, SortedDictionary<int, SharedWorldPage>> pages = new();
        private int registeredIndex;
        [Networked] public int Index { get; private set; }
        [Networked, Capacity(64)] public NetworkDictionary<ulong, SharedItemRecord> Items => default;
        [Networked, Capacity(64)] public NetworkDictionary<ulong, SharedEnemyRecord> Enemies => default;
        public void Initialize(int index) => Index = index;
        public override void Spawned()
        {
            Runner.MakeDontDestroyOnLoad(gameObject);
            if (!pages.TryGetValue(Runner, out var list)) pages[Runner] = list = new SortedDictionary<int, SharedWorldPage>();
            registeredIndex = Index;
            list[registeredIndex] = this;
        }
        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (pages.TryGetValue(runner, out var list))
            {
                list.Remove(registeredIndex);
                if (list.Count == 0) pages.Remove(runner);
            }
        }
        public static IEnumerable<SharedWorldPage> All(NetworkRunner runner)
        {
            if (runner == null || !pages.TryGetValue(runner, out var list)) yield break;
            foreach (var p in list.Values)
                if (p != null && p.Object != null && p.Object.IsValid) yield return p;
        }
        public static SharedWorldPage Get(NetworkRunner runner, int index)
        {
            if (runner == null || !pages.TryGetValue(runner, out var list)) return null;
            list.TryGetValue(index, out var page);
            return page != null && page.Object != null && page.Object.IsValid ? page : null;
        }
    }

    public sealed class SharedItemsTable : IEnumerable<KeyValuePair<ulong, SharedItemRecord>>
    {
        private readonly SharedDepartureState state;
        private readonly Dictionary<ulong, SharedWorldPage> locations = new();
        private NetworkRunner Runner => state.Runner;
        public SharedItemsTable(SharedDepartureState value) => state = value;
        public int Capacity => int.MaxValue;
        public int Count { get { int n = 0; foreach (var p in SharedWorldPage.All(Runner)) n += p.Items.Count; return n; } }
        private SharedWorldPage Page(ulong id)
        {
            if (locations.TryGetValue(id, out var cached) && cached != null && cached.Object != null &&
                cached.Object.IsValid && cached.Items.ContainsKey(id)) return cached;
            locations.Remove(id);
            foreach (var p in SharedWorldPage.All(Runner)) if (p.Items.ContainsKey(id))
            {
                // Remote cleanup is replicated independently; bound stale lookup entries.
                if (locations.Count >= 4096) locations.Clear();
                locations[id] = p;
                return p;
            }
            return null;
        }
        public bool ContainsKey(ulong id) => TryGet(id, out _);
        public bool TryGet(ulong id, out SharedItemRecord item)
        {
            var page = Page(id);
            if (page != null) return page.Items.TryGet(id, out item);
            item = default; return false;
        }
        public SharedItemRecord this[ulong id] => Page(id).Items[id];
        public void Set(ulong id, SharedItemRecord item)
        {
            var p = Page(id); if (p != null && p.Object.HasStateAuthority) p.Items.Set(id, item);
        }
        public void Add(ulong id, SharedItemRecord item)
        {
            if (!state.Object.HasStateAuthority || ContainsKey(id)) return;
            state.AvailablePage(false).Items.Add(id, item);
        }
        public IEnumerator<KeyValuePair<ulong, SharedItemRecord>> GetEnumerator()
        {
            foreach (var p in SharedWorldPage.All(Runner)) foreach (var item in p.Items) yield return item;
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
