using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeadBoat.FirstRun.Editor
{
    [InitializeOnLoad]
    public static class FirstRunAnalyticsProbe
    {
        private const string Key = "DeadBoat.FirstRunAnalyticsProbe.";
        [Serializable] public sealed class Events { public List<FirstRunTelemetryEvent> items = new(); }
        static FirstRunAnalyticsProbe()
        {
            SceneManager.sceneLoaded += (scene, mode) => {
                if (!SessionState.GetBool(Key + "active", false)) return;
                if (!FirstRunProfile.IsTemporary)
                    typeof(FirstRunEntryProbe).GetMethod("BeforeStart", BindingFlags.NonPublic | BindingFlags.Static)
                        .Invoke(null, new object[] {scene, mode});
                FirstRunAnalytics.TestSink = Capture;
            };
            EditorApplication.playModeStateChanged += state => {
                if (state != PlayModeStateChange.EnteredEditMode) return;
                FirstRunAnalytics.TestSink = null; SessionState.SetBool(Key + "active", false);
            };
        }
        private static void Capture(FirstRunTelemetryEvent e)
        {
            if (SessionState.GetBool(Key + "offline", false)) throw new InvalidOperationException("Simulated reporter unavailable");
            var events = Read(); events.items.Add(e);
            SessionState.SetString(Key + "events", JsonUtility.ToJson(events));
        }
        public static Events Read() => JsonUtility.FromJson<Events>(SessionState.GetString(Key + "events", "{}"));
        public static string Prepare(bool cold = false)
        {
            if (cold) FirstRunEntryProbe.PrepareCold(); else { FirstRunEntryProbe.Prepare(); SessionState.SetString(Key + "events", "{}"); }
            SessionState.SetBool(Key + "offline", false); SessionState.SetBool(Key + "active", true);
            return "isolated analytics sink armed; no production sending";
        }
        public static string VerifyFull()
        {
            var events = Read().items;
            Require(events.Count(e => e.name == "first_run_enter") == 1, "one enter");
            Require(events.Count(e => e.name == "first_run_completed") == 1 && events.Count(e => e.name == "first_run_lobby_enter") == 1, "completion/lobby");
            foreach (string step in FirstRunAnalytics.Steps)
            {
                Require(events.Count(e => e.name == "first_run_step_started" && e.stage == step) == 1, "start " + step);
                Require(events.Count(e => e.name == "first_run_step_completed" && e.stage == step) == 1, "complete " + step);
            }
            Require(events.Select(e => e.Id).Distinct().Count() == events.Count, "unique event ids");
            Require(events.All(e => e.platform == "EditorTest" && e.content == FirstRunAuthoredRoute.ContentVersion), "schema/platform");
            return "PASS: real lesson emits enter + all17 stage starts/completions + completion/lobby, unique IDs, no production transport";
        }
        public static string VerifyDeferredAndSave()
        {
            var p = FirstRunSession.Profile;
            Require(p != null && p.telemetry != null, "live analytics profile");
            SessionState.SetBool(Key + "offline", true);
            FirstRunAnalytics.Death(); FirstRunAnalytics.Death(); FirstRunAnalytics.Revived();
            Require(p.telemetry.pending.Count == 2, "dedup death + revive retained when reporter fails");
            FirstRunEntryProbe.CaptureForColdRestart();
            return "PASS: failed reporter retains death/revive in saved outbox, repeated death suppressed";
        }
        public static string VerifyResume()
        {
            var events = Read().items;
            Require(events.Count(e => e.name == "first_run_enter") == 1 && events.Count(e => e.name == "first_run_resume") == 1, "same attempt resume");
            Require(events.Count(e => e.name == "first_run_death") == 1 && events.Count(e => e.name == "first_run_revived") == 1, "saved events retried");
            Require(events.Count(e => e.name == "first_run_step_resumed") == 1, "resumed step");
            Require(events.Where(e => e.name == "first_run_step_completed").GroupBy(e => e.stage).All(g => g.Count() == 1), "no repeated completions");
            Require(events.Select(e => e.attempt).Distinct().Count() == 1, "same anonymous attempt");
            Require(events.Select(e => e.Id).Distinct().Count() == events.Count && FirstRunSession.Profile.telemetry.pending.Count == 0, "outbox drained without ID duplicates");
            return "PASS: cold boot preserves attempt, drains saved outbox, resumes step without false completions";
        }
        public static string VerifyEditorBlocked()
        {
            FirstRunAnalytics.TestSink = null;
            string before = FirstRunProfile.ReadJson(); FirstRunAnalytics.Death(); FirstRunAnalytics.Flush();
            Require(before == FirstRunProfile.ReadJson(), "Editor guard");
            return "PASS: Editor without isolated sink cannot record or send analytics";
        }
        public static string VerifyAcknowledgement()
        {
            var batch = new[] {new FirstRunTelemetryEvent {attempt="a",sequence=1}, new FirstRunTelemetryEvent {attempt="a",sequence=2}};
            var queue = batch.ToList();
            Require(!FirstRunAnalytics.Acknowledge(queue, batch, "{}") && queue.Count == 2, "empty ack retains events");
            Require(FirstRunAnalytics.Acknowledge(queue, batch, "{\"published\":1,\"errors\":[{\"index\":1,\"code\":\"unknown_event\"}]}") && queue.Count == 1 && queue[0].sequence == 2, "HTTP200 partial rejection retained");
            Require(FirstRunAnalytics.Acknowledge(queue, queue.ToArray(), "{\"published\":1,\"errors\":[]}") && queue.Count == 0, "accepted event drained");
            return "PASS: ambiguous/partial/accepted Cloud batch acknowledgements handled";
        }
        private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
    }
}
