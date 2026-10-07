using System.Collections.Generic;

namespace DeadBoat.Online
{
    // Local presentation preference for this running game only. No moderation API calls.
    public sealed class ChatLocalVisibility
    {
        public static ChatLocalVisibility Session { get; } = new();
        private const int Limit = 64;
        private readonly HashSet<string> hidden = new();
        public IReadOnlyCollection<string> HiddenSenders => hidden;
        public bool IsHidden(string sender) => sender != null && hidden.Contains(sender);

        public bool SetHidden(string sender, bool value)
        {
            if (string.IsNullOrEmpty(sender)) return false;
            if (!value) { hidden.Remove(sender); return true; }
            if (hidden.Contains(sender)) return true;
            if (hidden.Count >= Limit) return false;
            hidden.Add(sender);
            return true;
        }
    }
}
