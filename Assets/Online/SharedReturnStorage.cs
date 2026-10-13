using System.Runtime.InteropServices;
using UnityEngine;

namespace DeadBoat.Online
{
    // Small reconnect metadata only. WebGL PlayerPrefs can move with the build URL;
    // browser storage stays on the game's origin and commits synchronously.
    internal static class SharedReturnStorage
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern string DB_ReturnStorage_Get(string key);
        [DllImport("__Internal")] private static extern int DB_ReturnStorage_Set(string key, string value);
        public const string Backend = "browser-origin";
#else
        public const string Backend = "PlayerPrefs";
#endif
        public static string Read(string key)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            string stored = DB_ReturnStorage_Get(key);
            // Empty is a deletion marker: never revive an old URL's ticket.
            if (stored != null) return stored;
            string legacy = PlayerPrefs.GetString(key, "");
            if (!string.IsNullOrEmpty(legacy)) DB_ReturnStorage_Set(key, legacy);
            return legacy;
#else
            return PlayerPrefs.GetString(key, "");
#endif
        }

        public static bool Write(string key, string value)
        {
            PlayerPrefs.SetString(key, value);
            PlayerPrefs.Save();
#if UNITY_WEBGL && !UNITY_EDITOR
            bool durable = DB_ReturnStorage_Set(key, value) != 0;
            if (!durable) Debug.LogWarning("[Shared recovery storage] Browser write unavailable; using PlayerPrefs fallback.");
            return durable;
#else
            return true;
#endif
        }

        public static void Delete(string key)
        {
            // Keep an explicit empty value on the origin; remove the local legacy copy.
#if UNITY_WEBGL && !UNITY_EDITOR
            if (DB_ReturnStorage_Set(key, "") == 0)
                Debug.LogWarning("[Shared recovery storage] Browser deletion marker unavailable.");
#endif
            PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
        }
    }
}
