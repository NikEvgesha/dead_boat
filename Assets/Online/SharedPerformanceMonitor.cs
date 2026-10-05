using System;
using System.Globalization;
using Fusion.Statistics;
using UnityEngine;
using UnityEngine.Profiling;
using Unity.Profiling;

namespace DeadBoat.Online
{
    // Development builds/Editor only. No per-frame strings or expanding sample lists.
    // Raw Fusion bandwidth snapshot units are deliberately not labelled bytes/sec.
    public sealed class SharedPerformanceMonitor : MonoBehaviour
    {
        private readonly float[] samples = new float[4096];
        private readonly float[] sorted = new float[4096];
        private double start;
        private int count, frames, hitches;
        private float max;
        public string Stage { get; private set; } = "normal";
        public void BeginStage(string value) { Stage = value; ResetWindow(); }
        private ProfilerRecorder itemsCpu, enemiesCpu, authorityCpu;
        private double itemsNs, enemiesNs, authorityNs;
        private void OnEnable()
        {
            // Touch markers first so their static constructors register them before lookup.
            using (SharedItemsRuntime.UpdateMarker.Auto()) { }
            using (SharedEnemiesRuntime.UpdateMarker.Auto()) { }
            using (SharedDepartureState.TickMarker.Auto()) { }
            itemsCpu = ProfilerRecorder.StartNew(ProfilerCategory.Scripts, "DeadBoat.SharedItems", 1);
            enemiesCpu = ProfilerRecorder.StartNew(ProfilerCategory.Scripts, "DeadBoat.SharedEnemies", 1);
            authorityCpu = ProfilerRecorder.StartNew(ProfilerCategory.Scripts, "DeadBoat.SharedAuthority", 1);
            ResetWindow();
        }
        private void OnDisable() { itemsCpu.Dispose(); enemiesCpu.Dispose(); authorityCpu.Dispose(); }
        private void ResetWindow()
        {
            start = Time.realtimeSinceStartupAsDouble;
            count = frames = hitches = 0;
            max = 0;
            itemsNs = enemiesNs = authorityNs = 0;
        }
        private void Update()
        {
            if (!SharedRunContext.Playing) { ResetWindow(); return; }
            float ms = Time.unscaledDeltaTime * 1000;
            samples[count++ % samples.Length] = ms;
            frames++;
            if (itemsCpu.Valid) itemsNs += itemsCpu.LastValue;
            if (enemiesCpu.Valid) enemiesNs += enemiesCpu.LastValue;
            if (authorityCpu.Valid) authorityNs += authorityCpu.LastValue;
            max = Mathf.Max(max, ms);
            if (ms > 100) hitches++;
            double elapsed = Time.realtimeSinceStartupAsDouble - start;
            if (elapsed < 10) return;
            int n = Math.Min(count, samples.Length);
            Array.Copy(samples, sorted, n);
            Array.Sort(sorted, 0, n);
            var state = SharedRunContext.State;
            int players = 0, pages = 0, enemies = 0;
            foreach (var player in state.Runner.ActivePlayers) players++;
            foreach (var page in SharedWorldPage.All(state.Runner)) { pages++; enemies += page.Enemies.Count; }
            int bodies = 0, awakeBodies = 0;
            // Infrequent diagnostic scan, never used by the gameplay hot path.
            foreach (var body in FindObjectsByType<Rigidbody>(FindObjectsSortMode.None))
            {
                bodies++;
                if (!body.isKinematic && !body.IsSleeping()) awakeBodies++;
            }
            float rtt = -1, incoming = -1, outgoing = -1;
            if (state.Runner.TryGetFusionStatistics(out var statistics))
            {
                var values = statistics.SimulationSnapshot.Stats;
                if (values.TryGetValue(FusionStatType.RoundTripTime, out var v)) rtt = v * 1000;
                if (values.TryGetValue(FusionStatType.InBandwidth, out v)) incoming = v;
                if (values.TryGetValue(FusionStatType.OutBandwidth, out v)) outgoing = v;
            }
            Debug.Log(string.Format(CultureInfo.InvariantCulture,
                "[Coop perf] editor={0} authority={1} players={2} fps={3:F1} p95Ms={4:F1} maxMs={5:F1} hitches100ms={6} allocatedMB={7:F1} reservedMB={8:F1} items={9} enemyRecords={10} pages={11} routeSpan={12:F0} rttMs={13:F1} fusionInRaw={14:F1} fusionOutRaw={15:F1} retainedFrames={16}/{17} bodies={18} awakeDynamicBodies={19} itemsCpuMs={20:F3} enemiesCpuMs={21:F3} authorityCpuMs={22:F3} stage={23}",
                Application.isEditor, state.Object.HasStateAuthority, players, frames / elapsed,
                sorted[Math.Max(0, (int)Math.Ceiling(n * 0.95) - 1)], max, hitches,
                Profiler.GetTotalAllocatedMemoryLong() / 1048576.0,
                Profiler.GetTotalReservedMemoryLong() / 1048576.0, state.Items.Count,
                enemies, pages, state.WorldFront - state.WorldRear, rtt, incoming, outgoing, n, frames,
                bodies, awakeBodies, itemsCpu.Valid ? itemsNs / frames / 1000000 : -1,
                enemiesCpu.Valid ? enemiesNs / frames / 1000000 : -1,
                authorityCpu.Valid ? authorityNs / frames / 1000000 : -1, Stage));
            ResetWindow();
        }
    }
}
