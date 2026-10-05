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
        public static SharedDepartureState State { get; private set; }
        public static bool Playing => Active && State != null && State.Object != null &&
            State.Object.IsValid && State.Phase == 3;

        public static void Begin(int seed, int levelId, GameObject owner, SaveProvider persistentSave,
            SharedDepartureState state = null)
        {
            if (Active) return;
            Seed = seed;
            LevelId = levelId;
            State = state;
            var save = owner.AddComponent<SharedRunPreviewSave>();
            save.Persistent = persistentSave;
            Save = save;
            Active = true;
        }

        public static void End()
        {
            Active = false;
            State = null;
            if (Save != null) Object.Destroy(Save);
            Save = null;
        }

        public static RunRandom Random(string stream, int index) => new RunRandom(Seed, stream, index);
    }
}
