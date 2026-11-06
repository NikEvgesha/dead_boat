using System;
using System.Collections.Generic;
using MirraGames.SDK;
using UnityEngine;

namespace DeadBoat.FirstRun
{
    public enum FirstRunStatus { Running, Completed, Skipped, Legacy }
    [Serializable] public sealed class FirstRunProfileData
    {
        public int schema = 1, content = FirstRunAuthoredRoute.ContentVersion;
        public FirstRunStatus status;
        public FirstRunCheckpoint checkpoint;
        public FirstRunTelemetry telemetry;
    }
    [Serializable] public sealed class FirstRunItemData
    {
        public string id, name;
        public int template, ammo = -1, supply = -1;
        public Vector3 position;
        public Quaternion rotation;
        public bool bag, equipped, attached, boat;
    }
    [Serializable] public sealed class FirstRunShopData { public string path, item; }
    [Serializable] public sealed class FirstRunSaleData { public string path; public int coins; }
    [Serializable] public sealed class FirstRunCheckpoint
    {
        public FirstRunLesson lesson;
        public int intro, choices, coins, exp, level, pistol, rifle, shotgun, bossHP = -1;
        public float distance, fuel, health;
        public Vector3 player;
        public Stats cards;
        public bool egg, ammo, shot;
        public int[] guards = new int[2];
        public List<FirstRunItemData> items = new();
        public List<string> removed = new();
        public List<FirstRunShopData> shops = new();
        public List<FirstRunSaleData> sales = new();
        public bool Valid()
        {
            if ((int)lesson < 0 || lesson > FirstRunLesson.Completed || intro < 0 || intro > 7 || choices < 0 || choices > 2 ||
                float.IsNaN(distance) || float.IsInfinity(distance) || distance < 0 || distance > 910 ||
                float.IsNaN(health + fuel + player.sqrMagnitude) || float.IsInfinity(health + fuel + player.sqrMagnitude) || health < 0 || fuel < 0 ||
                !cards.Save || guards == null || guards.Length != 2 ||
                items == null || items.Count > 256 || removed == null || shops == null || sales == null) return false;
            var ids = new HashSet<string>();
            foreach (var item in items)
                if (item == null || string.IsNullOrEmpty(item.id) || !ids.Add(item.id) ||
                    float.IsNaN(item.position.sqrMagnitude) || float.IsInfinity(item.position.sqrMagnitude)) return false;
            return true;
        }
    }
    public static class FirstRunProfile
    {
        public const string Key = "DeadBoat.FirstRun.Profile.v2";
#if UNITY_EDITOR
        private static TemporaryStorage temporary;
        public static bool IsTemporary => temporary != null;
        public static IDisposable UseTemporaryStorage(string json = "")
        {
            if (temporary != null) throw new InvalidOperationException("Profile test scope already active");
            return temporary = new TemporaryStorage { Json = json };
        }
        private sealed class TemporaryStorage : IDisposable
        {
            public string Json;
            public void Dispose() { if (temporary == this) temporary = null; }
        }
#endif
        public static string ReadJson()
        {
#if UNITY_EDITOR
            if (temporary != null) return temporary.Json;
#endif
            // The SDK's data provider is profile-specific. Do not merge a browser's
            // unscoped local value into another signed-in platform account.
            return MirraSDK.IsInitialized ? MirraSDK.Data.GetString(Key, "") : PlayerPrefs.GetString(Key, "");
        }
        public static FirstRunProfileData Read()
        {
            string json = ReadJson();
            if (string.IsNullOrEmpty(json)) return null;
            try
            {
                var data = JsonUtility.FromJson<FirstRunProfileData>(json);
                if (data == null ||
                    !Enum.IsDefined(typeof(FirstRunStatus), data.status)) throw new FormatException("Unsupported lesson save");
                if (data.schema != 1) return new FirstRunProfileData {status = FirstRunStatus.Legacy};
                // Changing lesson content can restart an unfinished checkpoint,
                // never reopen a lesson that the profile has already closed.
                if (data.content != FirstRunAuthoredRoute.ContentVersion) { data.content = FirstRunAuthoredRoute.ContentVersion; data.checkpoint = null; }
                if (data.checkpoint != null && !data.checkpoint.Valid()) data.checkpoint = null;
                return data;
            }
            catch (Exception)
            {
                // An existing lesson record is not a pristine profile. Retry the
                // lesson safely rather than writing a normal run over its checkpoint.
                return new FirstRunProfileData { status = FirstRunStatus.Running };
            }
        }
        public static void Write(FirstRunProfileData data)
        {
            string json = JsonUtility.ToJson(data);
#if UNITY_EDITOR
            if (temporary != null) { temporary.Json = json; return; }
#endif
            if (MirraSDK.IsInitialized) { MirraSDK.Data.SetString(Key, json); MirraSDK.Data.Save(); }
            else { PlayerPrefs.SetString(Key, json); PlayerPrefs.Save(); }
        }
        public static FirstRunProfileData Resolve(FirstRunProfileData data, bool existingProfile)
        {
            return data ?? new FirstRunProfileData { status = existingProfile ? FirstRunStatus.Legacy : FirstRunStatus.Running };
        }
    }
}
