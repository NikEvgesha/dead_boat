using System;

// Scene identity only. Loading routes stay serialized in LoadingManager until migration is ready.
public static class LobbySceneRole
{
    public const string Legacy = "Lobby";
    public const string IslandWorkshop = "LobbyIslandWorkshop";

    public static bool IsLobby(string sceneName) =>
        string.Equals(sceneName, Legacy, StringComparison.Ordinal) ||
        string.Equals(sceneName, IslandWorkshop, StringComparison.Ordinal);
}
