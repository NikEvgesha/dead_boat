using UnityEngine;

namespace DeadBoat.Online
{
    [CreateAssetMenu(menuName = "Dead Boat/Online/Mirra Lobby Chat Settings")]
    public sealed class MirraLobbyChatSettings : ScriptableObject
    {
        // Enable only after testing the Cloud template's mandatory server moderation.
        public bool enabledForPilot;
        public bool PilotEnabled
        {
            get
            {
#if DEADBOAT_MIRRA_GAME_CHAT_PILOT
                return true;
#else
                return enabledForPilot;
#endif
            }
        }
        public string templateKey = "deadboat-lobby-v1";
        public string profanityGroupKey = "deadboat-chat";
    }
}
