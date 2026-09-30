using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeadBoat.Online
{
    // The transport/generation preview must not overwrite solo progress or grant
    // permanent rewards while authoritative interactions are still being implemented.
    public sealed class SharedRunPreviewSave : SaveProvider
    {
        public SaveProvider Persistent;
        private int distance = -1, levelId, fuel, coins, exp, playerLevel, upgrades;
        private float health = 100, playerOrigin, boatOrigin;
        private Stats stats = new Stats();
        private readonly Dictionary<WeaponType, int> ammo = new Dictionary<WeaponType, int>();
        private List<string> inventory = new List<string>();
        private int gems;
        private bool initializedGems;
        public override void Initialize() { }
        public override void SaveProgress() { }
        public override bool CheckProgress() => true;
        public override void SetSave(bool save) { }
        public override float[] LoadVolume() => Persistent.LoadVolume();
        public override void SaveVolume(float musicVolume, float soundVolume) => Persistent.SaveVolume(musicVolume, soundVolume);
        public override float LoadSensivity() => Persistent.LoadSensivity();
        public override void SaveSensivity(float sens) => Persistent.SaveSensivity(sens);
        public override float LoadScore(int levelId) => Persistent.LoadScore(levelId);
        public override void SaveScore(float score, int levelId) { }
        public override bool GetTutorialProgress() => true;
        public override void SaveTutorialProgress(bool endTutorial) { }
        public override int LoadQuestProgress() => 0;
        public override void SaveQuestProgress(int step) { }
        public override void SaveLevelUnlock(int id, bool unlocked) { }
        public override void SaveLevelWin(int id, bool win) { }
        public override void SaveGems(int amount) { gems = amount; initializedGems = true; }
        public override int LoadGems() => initializedGems ? gems : Persistent.LoadGems();
        public override void SaveAchievementProgress(AchievementType id, int progress) { }
        public override int LoadAchievementProgress(AchievementType id) => Persistent.LoadAchievementProgress(id);
        public override void SaveAchievementStatus(string id, bool progress) { }
        public override bool LoadAchievementStatus(string id) => Persistent.LoadAchievementStatus(id);
        public override void SaveLevelStatus(string lvlName, bool unlocked) { }
        public override bool LoadLevelStatus(string lvlName) => Persistent.LoadLevelStatus(lvlName);
        public override void SaveLobbyItem(string id) { }
        public override List<string> LoadLobbyItems() => new List<string>(Persistent.LoadLobbyItems());
        public override void ResetLobbyItems() { }
        public override void SaveDistance(int value) => distance = value;
        public override int LoadDistance() => distance;
        public override void SaveInventory(List<PickableItem> items)
        {
            inventory = new List<string>();
            foreach (var item in items)
                if (item != null && item.Data != null) inventory.Add(item.Data.Name);
        }
        public override List<string> LoadInventory() => new List<string>(inventory);
        public override void SaveBoardItem(List<PickableItem> items) { }
        public override List<SavedItem> LoadBoardItem() => new List<SavedItem>();
        public override void SavePlayerStats(int coin, float hp) { coins = coin; health = hp; }
        public override (int, float) LoadPlayerStats() => (coins, health);
        public override void SaveGameCoin(int coin) => coins = coin;
        public override int LoadGameCoin() => coins;
        public override void SavePlayerHealth(float value) => health = value;
        public override float LoadPlayerHealth() => health;
        public override void SavePlayerExperience(int value, int level) { exp = value; playerLevel = level; }
        public override (int, int) LoadPlayerExperience() => (exp, playerLevel);
        public override void SaveFuel(int value) => fuel = value;
        public override int LoadFuel() => fuel;
        public override void SaveAttachedItem(string id) { }
        public override void SaveAllAttachedItems(List<string> items) { }
        public override List<string> LoadAttachedItems() => new List<string>();
        public override void ResetAttachedItems() { }
        public override void SaveAmmo(WeaponType type, int amount) => ammo[type] = amount;
        public override int LoadAmmo(WeaponType type) => ammo.TryGetValue(type, out var value) ? value : 0;
        public override void SaveWins(int wins) { }
        public override int LoadWins() => Persistent.LoadWins();
        public override void SaveLevelId(int id) => levelId = id;
        public override int LoadLevelId() => levelId;
        public override void SaveRouletteDate(DateTime date) { }
        public override DateTime LoadRouletteDate() => Persistent.LoadRouletteDate();
        public override void SavePlayerFixPos(float value) => playerOrigin = value;
        public override float LoadPlayerFixPos() => playerOrigin;
        public override void SaveBoardFixPos(float value) => boatOrigin = value;
        public override float LoadBoardFixPos() => boatOrigin;
        public override void SaveLevelUpdate(Stats value) => stats = value;
        public override Stats LoadLevelUpdate() => stats;
        public override void SaveLevelUp(int count) => upgrades = count;
        public override int LoadLevelUp() => upgrades;
    }
}
