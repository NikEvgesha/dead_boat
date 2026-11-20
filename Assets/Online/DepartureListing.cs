using System.Collections.Generic;
using Fusion;

namespace DeadBoat.Online
{
    public sealed class DepartureListing
    {
        public string SessionName { get; }
        public int LevelId { get; }
        public int PlayerCount { get; }
        public int TargetPlayers { get; }
        public RunEventContent Content { get; }

        public static Dictionary<string, SessionProperty> PropertiesFor(int levelId, int seats, RunEventContent content) =>
            new()
            {
                { "d", 2 }, { "l", levelId }, { "t", seats }, { "ev", content.EventId }, { "rev", content.ThemeRevision }
            };

        private DepartureListing(string sessionName, int levelId, int playerCount, int targetPlayers, RunEventContent content)
        {
            SessionName = sessionName;
            LevelId = levelId;
            PlayerCount = playerCount;
            TargetPlayers = targetPlayers;
            Content = content;
        }

        public static bool TryFromSession(SessionInfo session, out DepartureListing listing)
        {
            listing = null;
            if (session == null || !session.IsOpen || !session.IsVisible ||
                session.PlayerCount < 1 || session.PlayerCount >= session.MaxPlayers ||
                session.Properties == null ||
                !TryInt(session.Properties, "d", out int departureVersion) || departureVersion != 2 ||
                !TryInt(session.Properties, "l", out int levelId) || levelId < 0 ||
                !TryInt(session.Properties, "t", out int targetPlayers) ||
                targetPlayers < 2 || targetPlayers > 4 || session.MaxPlayers != targetPlayers)
                return false;

            if (!TryInt(session.Properties, "ev", out int eventId) || !TryInt(session.Properties, "rev", out int revision))
                return false;
            var content = new RunEventContent { EventId = eventId, ThemeRevision = revision };
            if (!RunEventCalendar.Current.IsOpenFor(levelId, content)) return false;
            listing = new DepartureListing(session.Name, levelId, session.PlayerCount, targetPlayers, content);
            return true;
        }

        private static bool TryInt(IReadOnlyDictionary<string, SessionProperty> properties, string key, out int value)
        {
            value = 0;
            if (!properties.TryGetValue(key, out SessionProperty property) || !property.IsInt)
                return false;
            value = (int)property.PropertyValue;
            return true;
        }
    }
}
