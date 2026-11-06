using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using DeadBoat.Online;
using MirraGames.SDK;
using MirraGames.SDK.Common;
using Plugins.MirraCloud.Core.Services.Analytics.Dto;
using UnityEngine;

namespace DeadBoat.FirstRun
{
    [Serializable] public sealed class FirstRunTelemetry
    {
        public string attempt, stage;
        public int content, sequence, started, completed, dropped;
        public float activeSeconds;
        public bool closed, lobby;
        public List<FirstRunTelemetryEvent> pending = new();
    }
    [Serializable] public sealed class FirstRunTelemetryEvent
    {
        public string name, attempt, stage, reason, platform;
        public int sequence, content;
        public long utc;
        public float activeSeconds;
        public string Id => attempt + ":" + sequence;
        public Dictionary<string, object> Parameters() => new() {
            {"event_id", Id}, {"attempt_id", attempt}, {"step", stage ?? "loading"},
            {"reason", reason ?? ""}, {"platform", platform}, {"content_version", content},
            {"seed", FirstRunAuthoredRoute.Seed}, {"sequence", sequence},
            {"active_seconds", Mathf.RoundToInt(activeSeconds)}, {"occurred_at_utc", utc}
        };
    }

    // Own game adapter; both SDKs are unchanged. MirraWeb's EventsReporter is a fallback.
    public sealed class FirstRunAnalytics : MonoBehaviour
    {
        public static readonly string[] Steps = {
            "pickup", "sell", "collect_money", "buy_fuel", "pickup_purchase", "refuel", "steer",
            "sail_first", "first_fight", "egg", "first_card", "sail_second", "supplies",
            "second_fight", "second_card", "sail_boss", "boss"
        };
        private static FirstRunProfileData profile;
        private static bool ready, dead;
        private static FirstRunAnalytics pump;
        private static bool sending;
        private static float retryAt;
        private float nextFlush;
#if UNITY_EDITOR
        // Only isolated tests can capture events; Editor never reaches production analytics.
        public static Action<FirstRunTelemetryEvent> TestSink;
#endif
        private static bool Enabled
        {
            get
            {
#if UNITY_EDITOR
                return TestSink != null && FirstRunProfile.IsTemporary;
#else
                if (!MirraSDK.IsInitialized) return false;
                var platform = MirraSDK.Platform.Current;
                return platform != PlatformType.Editor && platform != PlatformType.Localhost && platform != PlatformType.Unknown;
#endif
            }
        }
        private static FirstRunTelemetry Data => profile?.telemetry;
        public static void Boot(FirstRunProfileData value, GameObject owner)
        {
            profile = value; ready = false; dead = false;
            if (!Enabled) return;
            value.telemetry ??= new FirstRunTelemetry();
            Data.pending ??= new List<FirstRunTelemetryEvent>();
            if (pump == null) pump = owner.AddComponent<FirstRunAnalytics>();
            if (value.status != FirstRunStatus.Running) { Flush(); return; }
            bool fresh = string.IsNullOrEmpty(Data.attempt) || Data.content != FirstRunAuthoredRoute.ContentVersion;
            if (fresh) NewAttempt();
            Emit(fresh ? "first_run_enter" : "first_run_resume", fresh ? "new_attempt" : "restart");
        }
        private static void NewAttempt()
        {
            Data.attempt = Guid.NewGuid().ToString("N"); Data.content = FirstRunAuthoredRoute.ContentVersion;
            Data.stage = "loading"; Data.sequence = Data.started = Data.completed = 0;
            Data.activeSeconds = 0; Data.closed = Data.lobby = false;
        }
        public static void Ready(FirstRunLesson lesson, int intro)
        {
            if (!Enabled || Data == null || profile.status != FirstRunStatus.Running) return;
            ready = true;
            dead = PlayerStatsManager.Instance != null && PlayerStatsManager.Instance.Health <= 0;
            int index = Index(lesson, intro);
            if (index < 0) return;
            Data.stage = Steps[index];
            int mask = 1 << index;
            if ((Data.started & mask) != 0) Emit("first_run_step_resumed");
            else { Data.started |= mask; Emit("first_run_step_started"); }
        }
        public static int Index(FirstRunLesson lesson, int intro) => lesson == FirstRunLesson.Intro
            ? Mathf.Clamp(intro, 0, 6) : lesson == FirstRunLesson.Completed ? -1 : (int)lesson + 6;
        public static void Observe(FirstRunLesson lesson, int intro)
        {
            if (!Enabled || !ready || Data == null || profile.status != FirstRunStatus.Running || Data.closed) return;
            int index = Index(lesson, intro);
            string next = index >= 0 ? Steps[index] : "completed";
            if (Data.stage == next) return;
            CompleteStep();
            Data.stage = next;
            if (index >= 0 && (Data.started & (1 << index)) == 0)
            { Data.started |= 1 << index; Emit("first_run_step_started"); }
        }
        private static void CompleteStep()
        {
            int index = Array.IndexOf(Steps, Data.stage);
            if (index < 0 || (Data.completed & (1 << index)) != 0) return;
            Data.completed |= 1 << index; Emit("first_run_step_completed");
        }
        public static void Death()
        {
            if (!Enabled || !ready || dead || Data == null || Data.closed) return;
            dead = true; Emit("first_run_death");
        }
        public static void Revived()
        {
            if (!Enabled || !dead || Data == null || Data.closed) return;
            dead = false; Emit("first_run_revived");
        }
        public static void Close(bool completed)
        {
            if (!Enabled || Data == null || Data.closed || string.IsNullOrEmpty(Data.attempt)) return;
            if (completed) CompleteStep();
            Data.closed = true;
            Emit(completed ? "first_run_completed" : "first_run_skipped", completed ? "victory" : "explicit_lobby_exit");
        }
        public static void Retry(FirstRunProfileData next)
        {
            if (!Enabled || Data == null) { profile = next; return; }
            Emit("first_run_retry", "restart_lesson");
            next.telemetry = Data;
            profile = next;
            ready = false; dead = false; NewAttempt(); Emit("first_run_enter", "retry");
        }
        public static void LobbyEntered()
        {
            if (!Enabled || Data == null || !Data.closed || Data.lobby) return;
            Data.lobby = true; Emit("first_run_lobby_enter");
        }
        private static void Emit(string name, string reason = "")
        {
            if (Data.pending.Count >= 128) { Data.pending.RemoveAt(0); Data.dropped++; }
            Data.pending.Add(new FirstRunTelemetryEvent {
                name = name, attempt = Data.attempt, stage = Data.stage, reason = reason,
                content = Data.content, sequence = ++Data.sequence, activeSeconds = Data.activeSeconds,
                utc = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                platform = Application.isEditor ? "EditorTest" : MirraSDK.Platform.Current.ToString()
            });
            FirstRunProfile.Write(profile);
            Flush();
        }
        public static void Flush()
        {
            if (!Enabled || Data?.pending == null || Data.pending.Count == 0) return;
            int sent = 0;
            try
            {
                while (Data.pending.Count > 0 && sent < 8)
                {
                    var e = Data.pending[0];
#if UNITY_EDITOR
                    TestSink(e);
#else
                    if (!sending && Time.unscaledTime >= retryAt) _ = SendCloudBatch();
                    break; // Only an acknowledged, accepted batch may leave the durable outbox.
#endif
#if UNITY_EDITOR
                    Data.pending.RemoveAt(0); sent++;
#endif
                }
            }
            catch (Exception e) { Debug.LogWarning("[First run analytics] Deferred: " + e.GetType().Name); }
            if (sent > 0) FirstRunProfile.Write(profile);
        }
        [Serializable] private sealed class BatchReply { public int published; public List<BatchError> errors; }
        [Serializable] private sealed class BatchError { public int index; public string code; public string error; }
        public static bool Acknowledge(List<FirstRunTelemetryEvent> queue, FirstRunTelemetryEvent[] batch, string body)
        {
            if (string.IsNullOrEmpty(body)) return false;
            BatchReply reply;
            try { reply = JsonUtility.FromJson<BatchReply>(body); } catch { return false; }
            if (reply == null || reply.published < 0) return false;
            var errors = reply.errors ?? new List<BatchError>();
            if (errors.Any(e => e == null || e.index < 0 || e.index >= batch.Length) ||
                errors.Select(e => e.index).Distinct().Count() != errors.Count ||
                reply.published + errors.Count != batch.Length) return false;
            var rejected = new HashSet<int>(errors.Select(e => e.index));
            for (int i = 0; i < batch.Length; i++)
                if (!rejected.Contains(i)) queue.RemoveAll(e => e.Id == batch[i].Id);
            return true;
        }
        private static async Task SendCloudBatch()
        {
            sending = true;
            var captured = Data;
            try
            {
                var social = MirraSocialService.Instance;
                if (!await social.ConnectAsync()) return;
                if (!ReferenceEquals(captured, Data) || !Enabled) return;
                var batch = captured.pending.Take(8).ToArray();
                var dto = batch.Select(e => new BatchEventItemDto {
                    EventKey = e.name, EventName = e.name,
                    Date = DateTimeOffset.FromUnixTimeSeconds(e.utc).UtcDateTime.ToString("o", CultureInfo.InvariantCulture),
                    Parameters = e.Parameters().ToDictionary(p => p.Key, p => Convert.ToString(p.Value, CultureInfo.InvariantCulture))
                }).ToList();
                var op = social.Sdk.Analytics.SendBatchAsync(dto);
                await op.Task();
                if (!ReferenceEquals(captured, Data) || !Enabled || !op.Result.IsSuccess) return;
                // HTTP200 can reject items. Missing/ambiguous bodies are not acknowledgement.
                if (Acknowledge(captured.pending, batch, op.Result.ResponseBody)) FirstRunProfile.Write(profile);
            }
            catch (Exception e) { Debug.LogWarning("[First run analytics] Cloud deferred: " + e.GetType().Name); }
            finally { sending = false; retryAt = Time.unscaledTime + 30; }
        }
        private void Update()
        {
            if (!Enabled) return;
            // Foreground dwell includes thinking time in a paused card/menu screen.
            if (ready && !dead && Data != null && !Data.closed && Application.isFocused)
                Data.activeSeconds += Time.unscaledDeltaTime;
            if (Time.unscaledTime < nextFlush) return;
            nextFlush = Time.unscaledTime + 2; Flush();
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            profile = null; ready = dead = false; pump = null;
            sending = false; retryAt = 0;
#if UNITY_EDITOR
            TestSink = null;
#endif
        }
    }
}
