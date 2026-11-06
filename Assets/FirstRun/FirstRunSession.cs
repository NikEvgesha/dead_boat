using System;
using DeadBoat.Online;
using UnityEngine;

namespace DeadBoat.FirstRun
{
    public static class FirstRunSession
    {
        public const string SceneName = "FirstRunAuthored";
        public static FirstRunSave Save { get; private set; }
        public static FirstRunProfileData Profile { get; private set; }
        public static bool Active => Save != null;
        public static bool Restoring { get; internal set; }
        public static FirstRunCheckpoint Checkpoint => Active ? Profile?.checkpoint : null;
        public static bool CanCapture => Active && !Restoring && Profile.status == FirstRunStatus.Running;
        public static bool TryBoot(SaveManager manager, bool pendingReturn)
        {
            if (Active) return true;
#if UNITY_EDITOR
            // Other gameplay probes must never create a real profile record.
            if (manager.PersistentProvider is SharedRunPreviewSave && !FirstRunProfile.IsTemporary) return false;
#endif
            var persistent = manager.PersistentProvider;
            bool existing = pendingReturn || persistent.CheckProgress() || persistent.GetTutorialProgress() || persistent.LoadDistance() >= 0 || persistent.LoadGems() > 0 || persistent.LoadWins() > 0 || persistent.LoadPlayerExperience().Item2 > 0;
            var previous = FirstRunProfile.Read();
            var profile = FirstRunProfile.Resolve(previous, existing);
            if (previous == null) FirstRunProfile.Write(profile);
            if (profile.status != FirstRunStatus.Running)
            {
                if (profile.status != FirstRunStatus.Legacy && !persistent.CheckProgress())
                {
                    persistent.SetSave(true); persistent.SaveTutorialProgress(true); persistent.SaveProgress();
                }
                return false;
            }
            Profile = profile;
            Restoring = profile.checkpoint != null;
            Save = manager.gameObject.AddComponent<FirstRunSave>();
            Save.Persistent = persistent;
            Save.Restore(profile.checkpoint);
            return true;
        }
        public static void Persist(FirstRunCheckpoint checkpoint)
        {
            if (!CanCapture || checkpoint == null || !checkpoint.Valid()) return;
            Profile.checkpoint = checkpoint;
            FirstRunProfile.Write(Profile);
        }
        public static void Close(bool completed)
        {
            if (!Active || Profile.status != FirstRunStatus.Running) return;
            Profile.status = completed ? FirstRunStatus.Completed : FirstRunStatus.Skipped;
            Profile.checkpoint = null;
            FirstRunProfile.Write(Profile);
            Save.Persistent.SetSave(true);
            Save.Persistent.SaveTutorialProgress(true);
            Save.Persistent.SaveProgress();
        }
        public static void Restart()
        {
            if (!Active) return;
            var persistent = Save.Persistent;
            var owner = Save.gameObject;
            UnityEngine.Object.Destroy(Save);
            Save = owner.AddComponent<FirstRunSave>(); Save.Persistent = persistent;
            Profile = new FirstRunProfileData { status = FirstRunStatus.Running };
            Restoring = true;
            FirstRunProfile.Write(Profile);
        }
        public static void End()
        {
            Restoring = false;
            var save = Save; Save = null; Profile = null;
            if (save != null) UnityEngine.Object.Destroy(save);
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { Save = null; Profile = null; Restoring = false; }
    }
}
