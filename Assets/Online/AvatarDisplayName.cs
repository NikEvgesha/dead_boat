using System.Globalization;
using System.Text;
using MirraGames.SDK;

namespace DeadBoat.Online
{
    public static class AvatarDisplayName
    {
        public const int MaxLength = 24;

        public static string Local()
        {
            // Only cached provider data; no login prompts or extra Cloud requests.
            if (MirraSDK.IsInitialized && MirraSDK.Player.IsLoggedIn)
            {
                string name = MirraSDK.Player.DisplayName;
                if (!string.IsNullOrWhiteSpace(name)) return name;
            }
            return MirraSocialService.CachedDisplayName;
        }

        public static string Sanitize(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
            var cleaned = new StringBuilder(MaxLength);
            bool inTag = false, space = false;
            // Bound work even when a remote client supplies an unreasonable string.
            int end = System.Math.Min(raw.Length, 256);
            for (int i = 0; i < end; i++)
            {
                char c = raw[i];
                if (c == '<') { inTag = true; continue; }
                if (c == '>') { inTag = false; continue; }
                if (inTag) continue;
                if (char.IsWhiteSpace(c)) { space = cleaned.Length > 0; continue; }
                var category = char.GetUnicodeCategory(raw, i);
                if (category == UnicodeCategory.Control || category == UnicodeCategory.Format) continue;
                int count = char.IsHighSurrogate(c) && i + 1 < end && char.IsLowSurrogate(raw[i + 1]) ? 2 : 1;
                if (char.IsSurrogate(c) && count == 1) continue;
                if (space) { cleaned.Append(' '); space = false; }
                cleaned.Append(raw, i, count);
                i += count - 1;
            }
            string value = cleaned.ToString();
            if (value.Length <= MaxLength) return value;
            var elements = StringInfo.GetTextElementEnumerator(value);
            int length = 0;
            while (elements.MoveNext())
            {
                string element = elements.GetTextElement();
                if (length + element.Length > MaxLength) break;
                length += element.Length;
            }
            return value.Substring(0, length).TrimEnd();
        }

        public static string Resolve(string raw, int player, bool english)
        {
            string name = Sanitize(raw);
            return name.Length > 0 ? name : (english ? "Player " : "Игрок ") + System.Math.Max(1, player);
        }
    }
}
