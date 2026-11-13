public sealed class Winter175MemorySave : DeadBoat.Online.SharedRunPreviewSave
{
    private bool tutorialComplete;
    public override bool GetTutorialProgress() => tutorialComplete;
    public override void SaveTutorialProgress(bool complete) => tutorialComplete = complete;
    public override void SaveVolume(float music, float sound) { }
    public override void SaveSensivity(float value) { }
}
