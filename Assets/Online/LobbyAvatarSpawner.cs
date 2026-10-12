using Fusion;
using UnityEngine;
using System.Linq;

namespace DeadBoat.Online
{
    public sealed class LobbyAvatarSpawner : SimulationBehaviour, IPlayerJoined
    {
        public NetworkObject AvatarPrefab { get; set; }

        void IPlayerJoined.PlayerJoined(PlayerRef player)
        { SpawnLocal(player); }

        public void SpawnLocal(PlayerRef player)
        { SpawnLocal(Runner, AvatarPrefab, player); }

        public static void SpawnLocal(NetworkRunner runner, NetworkObject avatarPrefab, PlayerRef player)
        {
            Debug.Log($"[Lobby online] Player joined: {player}; local={player == runner.LocalPlayer}");
            if (player != runner.LocalPlayer || avatarPrefab == null ||
                runner.TryGetPlayerObject(player, out _))
                return;

            var localPlayer = PlayerMovement.Instance;
            var position = localPlayer != null ? localPlayer.transform.position : Vector3.zero;
            var slot = Mathf.Clamp(runner.ActivePlayers.Count() - 1, 0, 9);
            position += Vector3.right * (slot * 2f);
            if (localPlayer != null && slot > 0)
            {
                var controller = localPlayer.GetComponent<CharacterController>();
                if (controller != null) controller.enabled = false;
                localPlayer.transform.position = position;
                if (controller != null) controller.enabled = true;
            }
            var rotation = localPlayer != null ? localPlayer.transform.rotation : Quaternion.identity;
            var avatar = runner.Spawn(avatarPrefab, position, rotation);
            if (avatar == null)
            {
                Debug.LogError("[Lobby online] Could not spawn player avatar.");
                return;
            }
            runner.SetPlayerObject(player, avatar);
        }
    }
}
