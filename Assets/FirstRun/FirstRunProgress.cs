using System;
namespace DeadBoat.FirstRun
{
    public enum FirstRunStage { Walk, PickUp, Stash, Retrieve, Fuel, Board, Sail, Threat, Return, Completed, Skipped }
    [Serializable]
    public sealed class FirstRunProgress
    {
        public const int Seed = 14052026, ContentVersion = 1;
        public int Version = ContentVersion;
        public FirstRunStage Stage;
        public float Distance;
        public int Hits, Health = 100;
        public bool Closed => Stage == FirstRunStage.Completed || Stage == FirstRunStage.Skipped;
        public bool Advance(FirstRunStage expected)
        {
            if (Closed || Stage != expected || expected >= FirstRunStage.Completed) return false;
            Stage++; return true;
        }
        public void Skip() { if (!Closed) Stage = FirstRunStage.Skipped; }
        public void Sail(float metres)
        {
            if (metres <= 0 || float.IsNaN(metres) || float.IsInfinity(metres)) return;
            if (Stage == FirstRunStage.Sail) { Distance = Math.Min(50, Distance + metres); if (Distance >= 50) Advance(FirstRunStage.Sail); }
            else if (Stage == FirstRunStage.Return) { Distance = Math.Min(180, Distance + metres); if (Distance >= 180) Advance(FirstRunStage.Return); }
        }
        public void Hit() { if (Stage == FirstRunStage.Threat && ++Hits >= 2) Advance(FirstRunStage.Threat); }
        public bool Damage(int amount)
        {
            if (Stage != FirstRunStage.Threat || amount <= 0) return false;
            Health = Math.Max(0, Health - amount);
            if (Health > 0) return false;
            Health = 100; Hits = 0; return true; // forgiving retry at the same checkpoint
        }
    }
}
