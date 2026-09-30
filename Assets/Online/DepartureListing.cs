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

        private DepartureListing(string sessionName, int levelId, int playerCount, int targetPlayers)
        {
            SessionName = sessionName;
            LevelId = levelId;
            PlayerCount = playerCount;
            TargetPlayers = targetPlayers;
        }

        public static bool TryFromSession(SessionInfo session, out DepartureListing listing)
        {
            listing = null;
            if (session == null || !session.IsOpen || !session.IsVisible ||
                session.PlayerCount < 1 || session.PlayerCount >= session.MaxPlayers ||
                session.Properties == null ||
                !TryInt(session.Properties, "d", out int departureVersion) || departureVersion != 1 ||
                !TryInt(session.Properties, "l", out int levelId) || levelId < 0 ||
                !TryInt(session.Properties, "t", out int targetPlayers) ||
                targetPlayers < 2 || targetPlayers > 4 || session.MaxPlayers != targetPlayers)
                return false;

            listing = new DepartureListing(session.Name, levelId, session.PlayerCount, targetPlayers);
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
