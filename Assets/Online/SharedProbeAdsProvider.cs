#if UNITY_EDITOR
using System;
namespace DeadBoat.Online.Editor
{
    public sealed class SharedProbeAdsProvider : AdsProvider
    {
        private Action<bool> callback;
        public int requests;
        public override void Initialize() { }
        public override bool IsRewardedAdReady() => true;
        public override void ShowRewardedAd(string rewardId, Action<bool> onComplete) { requests++; callback = onComplete; }
        public override void ShowInterstitialAd(Action<bool> onComplete = null) { requests++; callback = onComplete; }
        public void CompleteTwice() { callback?.Invoke(true); callback?.Invoke(true); }
    }
}
#endif
