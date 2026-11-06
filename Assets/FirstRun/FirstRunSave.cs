using System.Collections.Generic;
using DeadBoat.Online;

namespace DeadBoat.FirstRun
{
    // Run-only values reuse the existing isolated provider. Settings and persistent
    // meta data stay with Persistent; ordinary run slots are never overwritten.
    public sealed class FirstRunSave : SharedRunPreviewSave
    {
        public override int LoadGems() => Persistent.LoadGems();
        public override void SaveGems(int amount) => Persistent.SaveGems(amount);
        public void Restore(FirstRunCheckpoint c)
        {
            if (c == null) return;
            SaveDistance((int)c.distance); SaveFuel((int)c.fuel);
            SaveGameCoin(c.coins); SavePlayerHealth(c.health);
            SavePlayerExperience(c.exp, c.level); SaveLevelUpdate(c.cards);
            SaveAmmo(WeaponType.Pistol, c.pistol); SaveAmmo(WeaponType.Rifle, c.rifle); SaveAmmo(WeaponType.Shotgun, c.shotgun);
        }
        // The world transaction restores the original item instances and their ammo.
        public override List<string> LoadInventory() => new();
        public override List<string> LoadLobbyItems() => new();
        public override void SaveProgress() => Persistent.SaveProgress();
    }
}
