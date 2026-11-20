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

    public readonly struct ScheduledRunEvent
    {
        public readonly int LevelId;
        public readonly RunEventContent Content;
        public readonly string Scene;
        public readonly DateTimeOffset? StartUtc, EndUtc;

        public ScheduledRunEvent(int levelId, RunEventContent content, string scene,
            DateTimeOffset? startUtc, DateTimeOffset? endUtc)
        {
            LevelId = levelId;
            Content = content;
            Scene = scene;
            StartUtc = startUtc;
            EndUtc = endUtc;
        }

        public bool IsOpen(DateTimeOffset now) =>
            StartUtc is DateTimeOffset start && EndUtc is DateTimeOffset end &&
            start.Offset == TimeSpan.Zero && end.Offset == TimeSpan.Zero && start <= now && now < end;
    }

    public sealed class RunEventCalendar
    {
        // Content IDs are durable. Retain old entries and scenes for accepted runs when adding a new theme.
        public static readonly RunEventContent FirstWinterTheme = new() { EventId = 1, ThemeRevision = 1 };
        private static readonly ScheduledRunEvent Winter = new(3, FirstWinterTheme, "LevelEvent",
            startUtc: null, endUtc: null);
        public static RunEventCalendar Current { get; private set; } = new(() => DateTimeOffset.UtcNow, Winter);

        private readonly Func<DateTimeOffset> utcNow;
        private readonly ScheduledRunEvent[] events;

        public RunEventCalendar(Func<DateTimeOffset> utcNow, params ScheduledRunEvent[] events)
        {
            this.utcNow = utcNow;
            this.events = (ScheduledRunEvent[])events.Clone();
        }

        public bool TryAdmit(int levelId, out RunEventContent content)
        {
            content = default;
            bool scheduled = false;
            DateTimeOffset now = default;
            foreach (var entry in events)
            {
                if (entry.LevelId != levelId) continue;
                if (!scheduled) now = utcNow();
                scheduled = true;
                if (!entry.IsOpen(now)) continue;
                if (content.EventId != 0)
                {
                    content = default;
                    return false; // Ambiguous overlapping windows.
                }
                content = entry.Content;
            }
            return levelId >= 0 && (!scheduled || content.EventId != 0);
        }

        public bool IsOpenFor(int levelId, RunEventContent selected) =>
            TryAdmit(levelId, out var open) && open.Matches(selected);

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
