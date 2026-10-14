#if UNITY_EDITOR
using System;
namespace DeadBoat.Online.Editor
{
    public sealed class SharedProbeAdsProvider : AdsProvider
    {
        public Action<bool> callback;
        public bool ready = true, throwOnShow;
        public Action onShow;
        public int requests;
        public override void Initialize() { }
        public override bool IsRewardedAdReady() => ready;
        public override void ShowRewardedAd(string rewardId, Action<bool> onComplete) { requests++; callback = onComplete; onShow?.Invoke(); if (throwOnShow) throw new InvalidOperationException("injected provider failure"); }
        public override void ShowInterstitialAd(Action<bool> onComplete = null) { requests++; callback = onComplete; onShow?.Invoke(); if (throwOnShow) throw new InvalidOperationException("injected provider failure"); }
        public void CompleteTwice() { callback?.Invoke(true); callback?.Invoke(true); }
    }
}
#endif
