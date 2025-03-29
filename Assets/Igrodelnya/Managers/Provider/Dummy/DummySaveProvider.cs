using UnityEngine;

public class DummySaveProvider : SaveProvider
{
    public override void Initialize() { Debug.Log("DummySaveProvider initialized"); }
    public override float[] LoadVolume() => new float[] { 0.5f, 0.5f };
    public override void SaveVolume(float musicVolume, float soundVolume) { }
    public override void SaveScore(float score, int levelId) { }
    public override float LoadScore(int levelId) => 0;
    public override void SaveLevelUnlock(int id, bool unlocked) { }
    public override void SaveLevelWin(int id, bool win) { }
    public override void SaveProgress() { }
}
