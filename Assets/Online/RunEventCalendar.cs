using System;
using Fusion;

namespace DeadBoat.Online
{
    [Serializable]
    public struct RunEventContent : INetworkStruct
    {
        public int EventId;
        public int ThemeRevision;

        public bool Matches(RunEventContent other) =>
            EventId == other.EventId && ThemeRevision == other.ThemeRevision;
    }

    public enum EventWindow { OrdinaryLevel, Hidden, Upcoming, Open }

    public readonly struct ScheduledRunEvent
    {
        public readonly int LevelId;
        public readonly RunEventContent Content;
        public readonly string Scene;
        public readonly DateTimeOffset? StartUtc, EndUtc;
        public readonly bool CrewReady;

        public ScheduledRunEvent(int levelId, RunEventContent content, string scene,
            DateTimeOffset? startUtc, DateTimeOffset? endUtc, bool crewReady)
        {
            LevelId = levelId;
            Content = content;
            Scene = scene;
            StartUtc = startUtc;
            EndUtc = endUtc;
            CrewReady = crewReady;
        }

        public bool HasDates =>
            StartUtc is DateTimeOffset start && EndUtc is DateTimeOffset end &&
            start.Offset == TimeSpan.Zero && end.Offset == TimeSpan.Zero && start < end;

        public bool IsOpen(DateTimeOffset now) => HasDates && StartUtc <= now && now < EndUtc;
    }

    public sealed class RunEventCalendar
    {
        // Content IDs are durable. Retain old entries and scenes for accepted runs when adding a new theme.
        public static readonly RunEventContent FirstWinterTheme = new() { EventId = 1, ThemeRevision = 1 };
        private static readonly ScheduledRunEvent Winter = new(3, FirstWinterTheme, "LevelEvent",
            startUtc: null, endUtc: null, crewReady: false); // Enable with the shared sleigh encounter (DB-ON-263).
        public static RunEventCalendar Current { get; private set; } = new(() => DateTimeOffset.UtcNow, Winter);

        private readonly Func<DateTimeOffset> utcNow;
        private readonly ScheduledRunEvent[] events;

        public RunEventCalendar(Func<DateTimeOffset> utcNow, params ScheduledRunEvent[] events)
        {
            this.utcNow = utcNow;
            this.events = (ScheduledRunEvent[])events.Clone();
        }

        public EventWindow GetWindow(int levelId, out ScheduledRunEvent window)
        {
            window = default;
            var state = EventWindow.OrdinaryLevel;
            DateTimeOffset now = default;
            foreach (var entry in events)
            {
                if (entry.LevelId != levelId) continue;
                if (state == EventWindow.OrdinaryLevel)
                {
                    now = utcNow();
                    state = EventWindow.Hidden;
                }
                if (!entry.HasDates || entry.Content.EventId == 0 || entry.EndUtc <= now) continue;
                if (entry.StartUtc > now)
                {
                    if (state == EventWindow.Hidden || (state == EventWindow.Upcoming && entry.StartUtc < window.StartUtc))
                    {
                        window = entry;
                        state = EventWindow.Upcoming;
                    }
                    continue;
                }
                if (state == EventWindow.Open)
                {
                    window = default;
                    return EventWindow.Hidden; // Ambiguous overlapping windows.
                }
                window = entry;
                state = EventWindow.Open;
            }
            return state;
        }

        public bool TryAdmit(int levelId, out RunEventContent content, bool forCrew = false)
        {
            content = default;
            var state = GetWindow(levelId, out var window);
            if (state == EventWindow.OrdinaryLevel) return levelId >= 0;
            if (state != EventWindow.Open || (forCrew && !window.CrewReady)) return false;
            content = window.Content;
            return true;
        }

        public bool IsOpenFor(int levelId, RunEventContent selected, bool forCrew = false) =>
            TryAdmit(levelId, out var open, forCrew) && open.Matches(selected);

        public bool TryGetEventLevel(string scene, out int levelId)
        {
            foreach (var entry in events)
                if (entry.Scene == scene) { levelId = entry.LevelId; return true; }
            levelId = -1;
            return false;
        }

        // A confirmed run resolves its exact content independently of today's window.
        public bool TryResolve(int levelId, RunEventContent content, out string eventScene)
        {
            eventScene = null;
            if (levelId < 0) return false;
            bool scheduled = false;
            foreach (var entry in events)
            {
                if (entry.LevelId != levelId) continue;
                scheduled = true;
                if (!entry.Content.Matches(content)) continue;
                eventScene = entry.Scene;
                return true;
            }
            return !scheduled && content.Matches(default);
        }

        public bool TryResolveSaved(int levelId, ref RunEventContent content, out string eventScene)
        {
            // Before the calendar, every persisted LevelId 3 was this exact winter scene.
            if (levelId == Winter.LevelId && content.Matches(default)) content = FirstWinterTheme;
            return TryResolve(levelId, content, out eventScene);
        }

#if UNITY_EDITOR
        public static RunEventCalendar ReplaceForEditorTest(RunEventCalendar replacement)
        {
            var previous = Current;
            Current = replacement;
            return previous;
        }
#endif
    }
}
