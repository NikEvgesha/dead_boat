#if UNITY_EDITOR
using System;
namespace DeadBoat.Online.Editor
{
    public sealed class SharedProbePauseProvider : PauseProvider
    {
        public bool Paused, FailNextResume;
        public Action<bool> BeforeSet;
        public override void Initialize() { }
        public override bool IsPaused => Paused;
        public override void SetPause(bool paused, bool controlAudio = true)
        {
            BeforeSet?.Invoke(paused);
            if (!paused && FailNextResume) { FailNextResume = false; throw new InvalidOperationException("injected resume failure"); }
            Paused = paused;
        }
    }
}
#endif
