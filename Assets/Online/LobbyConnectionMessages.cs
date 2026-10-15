namespace DeadBoat.Online
{
    public static class LobbyConnectionMessages
    {
        public static LobbyConnectionFailure Classify(string reason, string message, bool noNetwork)
        {
            if (noNetwork) return LobbyConnectionFailure.NoNetwork;
            string details = (reason + " " + message).ToLowerInvariant();
            if (details.Contains("ccu") || details.Contains("capacity")) return LobbyConnectionFailure.Capacity;
            if (details.Contains("gameisfull") || details.Contains("roomisfull") ||
                details.Contains("game full") || details.Contains("room full")) return LobbyConnectionFailure.RoomFull;
            if (details.Contains("full")) return LobbyConnectionFailure.Capacity;
            if (details.Contains("timeout") || details.Contains("timed out")) return LobbyConnectionFailure.Timeout;
            return LobbyConnectionFailure.Other;
        }
        public static bool CanFallbackAfterInvite(LobbyConnectionFailure failure) =>
            failure == LobbyConnectionFailure.Other || failure == LobbyConnectionFailure.RoomFull;
        public static string Describe(LobbyConnectionFailure failure, bool english)
        {
            switch (failure)
            {
                case LobbyConnectionFailure.NoNetwork:
                    return english ? "No network. Play offline or reconnect later." : "Нет сети. Играйте офлайн или подключитесь позже.";
                case LobbyConnectionFailure.Capacity:
                    return english ? "Online is full. Try again later; offline play is available." : "Сейчас нет мест. Попробуйте позже; офлайн доступен.";
                case LobbyConnectionFailure.RoomFull:
                    return english ? "This crew or lobby is full. Choose another one." : "Этот экипаж или лобби заполнены. Выберите другой.";
                case LobbyConnectionFailure.Timeout:
                    return english ? "Connection timed out. You can retry." : "Сервер не ответил. Можно попробовать ещё раз.";
                default:
                    return english ? "Offline. You can reconnect at any time." : "Офлайн. Можно подключиться в любой момент.";
            }
        }
    }
}
