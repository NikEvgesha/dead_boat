using Fusion;
using UnityEngine;

namespace DeadBoat.Online
{
    public static class SharedRunContext
    {
        public static bool Active { get; private set; }
        public static int Seed { get; private set; }
        public static int LevelId { get; private set; }
        public static SaveProvider Save { get; private set; }

        public static void Begin(int seed, int levelId, GameObject owner, SaveProvider persistentSave)
        {
            if (Active) return;
            Seed = seed;
            LevelId = levelId;
            var save = owner.AddComponent<SharedRunPreviewSave>();
            save.Persistent = persistentSave;
            Save = save;
            Active = true;
        }

        public static void End()
        {
            Active = false;
            if (Save != null) Object.Destroy(Save);
            Save = null;
        }

        public static RunRandom Random(string stream, int index) => new RunRandom(Seed, stream, index);
    }
}
