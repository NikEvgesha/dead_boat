using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using Unity.Profiling;
using Object = UnityEngine.Object;

namespace DeadBoat.Online.Editor
{
    // Isolates native path submissions across REAL frames, with identical moving
    // goals and stationary agents. Not a gameplay/FPS benchmark of a whole fight.
    public static class SharedNavigationCrowdProbe
    {
        public static async Task<string> Run()
        {
            if (!EditorApplication.isPlaying || !(SaveManager.Instance?.PersistentProvider is SharedRunPreviewSave))
                throw new InvalidOperationException("isolated Play required");
            using var fixture = new SharedNavigationProbe.NavigationFixture(0);
            var agents = new List<NavMeshAgent>();
            var lines = new List<string>();
            try
            {
                if (!NavMesh.SamplePosition(fixture.Center, out var floor, 3, NavMesh.AllAreas))
                    throw new InvalidOperationException("fixture floor missing");
                for (int i = 0; i < 32; i++)
                {
                    var go = new GameObject("Temporary navigation benchmark agent");
                    go.SetActive(false);
                    var agent = go.AddComponent<NavMeshAgent>(); agents.Add(agent);
                    go.transform.position = floor.position + new Vector3((i % 8) * 1.5f - 5.25f, 0, 6 + i / 8);
                    agent.speed = 0; agent.radius = .3f;
                    go.SetActive(true);
                    if (!agent.isOnNavMesh) throw new InvalidOperationException("agent binding failed");
                }
                // ABBA order reduces a simple warmup/order bias.
                foreach (bool scheduled in new[] { false, true, true, false })
                {
                    foreach (var agent in agents) agent.ResetPath();
                    var schedules = new SharedEnemyPathSchedule[agents.Count];
                    int submissions = 0, frames = 0, previous = Time.frameCount;
                    double milliseconds = 0;
                    var timer = new Stopwatch();
                    using var manager = ProfilerRecorder.StartNew(ProfilerCategory.Ai, "NavMeshManager", 1);
                    double managerNs = 0;
                    double deadline = Time.realtimeSinceStartupAsDouble + 30;
                    while (frames < 90)
                    {
                        if (!EditorApplication.isPlaying || Time.realtimeSinceStartupAsDouble >= deadline)
                            throw new TimeoutException("navigation crowd frame wait");
                        await Task.Delay(1);
                        if (Time.frameCount == previous) continue;
                        previous = Time.frameCount;
                        if (manager.Valid) managerNs += manager.LastValue;
                        Vector3 goal = floor.position + Vector3.back * 8 + Vector3.right * (Mathf.Sin(frames * .015f) * 3);
                        timer.Restart();
                        for (int i = 0; i < agents.Count; i++)
                        {
                            if (scheduled && !schedules[i].ShouldRefresh(1, goal, Vector3.zero, Time.time)) continue;
                            if (!NavMesh.SamplePosition(goal, out var projected, 2, NavMesh.AllAreas) ||
                                !agents[i].SetDestination(projected.position))
                                throw new InvalidOperationException("native path request rejected");
                            submissions++;
                        }
                        timer.Stop(); milliseconds += timer.Elapsed.TotalMilliseconds;
                        frames++;
                    }
                    if (!scheduled && submissions != 2880 || scheduled && submissions >= 2880)
                        throw new InvalidOperationException("unexpected path request count");
                    lines.Add(string.Format(CultureInfo.InvariantCulture,
                        "scheduled={0} frames={1} requests={2} submitMeanMs={3:F4} managerMeanMs={4:F4}",
                        scheduled, frames, submissions, milliseconds / frames,
                        manager.Valid ? managerNs / frames / 1000000 : -1));
                }
                string result = "[Navigation crowd] PASS 32 agents / 90 frames per stage / ABBA\n" + string.Join("\n", lines);
                UnityEngine.Debug.Log(result);
                System.IO.File.WriteAllText("Logs/NavigationCrowd-20261102.log", result);
                return result;
            }
            finally
            {
                foreach (var agent in agents) if (agent != null)
                {
                    agent.gameObject.SetActive(false);
                    Object.Destroy(agent.gameObject);
                }
            }
        }
    }
}
