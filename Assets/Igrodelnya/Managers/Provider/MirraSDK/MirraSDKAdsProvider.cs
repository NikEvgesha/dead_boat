
//#if MIRRA_SDK_ENABLED
using System;
using UnityEngine;
using MirraGames.SDK;  // добавили пространство имён SDK

public class MirraSDKAdsProvider : AdsProvider
{
    public override void Initialize()
    {
        // В MirraSDK нет явной инициализации Ads-модуля,
        // но логируем факт подключения провайдера
        Debug.Log("MirraSDKAdsProvider initialized");
    }

    public override bool IsRewardedAdReady()
    {
        return MirraSDK.Ads.IsRewardedReady;
    }

    public override void ShowRewardedAd(string rewardId, Action<bool> onComplete)
    {
        if (!MirraSDK.Ads.IsRewardedReady)
        {
            Debug.LogWarning("MirraSDK: Rewarded ad not ready");
            onComplete?.Invoke(false);
            return;
        }

        // Используем упрощённый InvokeRewarded с одним коллбэком onClose
        MirraSDK.Ads.InvokeRewarded(
            rewardTag: rewardId,
            onClose: (success) =>
            {
                if (success)
                    Debug.Log($"MirraSDK: Rewarded ad succeeded (tag = {rewardId})");
                else
                    Debug.LogWarning($"MirraSDK: Rewarded ad closed without reward (tag = {rewardId})");
                
                onComplete?.Invoke(success);
            }
        );
    }

    public override void ShowInterstitialAd()
    {
        if (!MirraSDK.Ads.IsInterstitialReady)
        {
            Debug.LogWarning("MirraSDK: Interstitial ad not ready");
            return;
        }

        // Правильные имена параметров: onOpen и onClose
        MirraSDK.Ads.InvokeInterstitial(
            onOpen: () =>
            {
                Debug.Log("MirraSDK: Interstitial ad opened");
            },
            onClose: () =>
            {
                Debug.Log("MirraSDK: Interstitial ad closed");
            }
        );
    }

    private void OnDestroy()
    {
        // Ничего не подписывали — нечего и очищать
    }
}
//#endif
