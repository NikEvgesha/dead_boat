using UnityEngine;
using System.Collections.Generic;
using System;


// Главный менеджер рекламы
public class AdsManager : MonoBehaviour
{
    private static AdsManager _instance;
    public static AdsManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("AdsManager");
                _instance = go.AddComponent<AdsManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    [SerializeField] private List<AdsProvider> adsProviders = new List<AdsProvider>(); // Список активных провайдеров

    public Action AdClosed;
    public event Action<bool> ShownAdClosed;
    private AdsProvider observedProvider;
    private Action openedHandler;
    private bool providerOpened, requestRewarded;

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        //DontDestroyOnLoad(gameObject);

        // Инициализация всех провайдеров
        InitializeProviders();
        if (GetComponent<AdRemovalOffer>() == null) gameObject.AddComponent<AdRemovalOffer>();
    }


    private void OnDisable() => Finish(request, false);

    private PauseManager adPause;
    private ControlManager adControl;
    private bool active;
    private bool releasing;
    private int request;
    private double deadline;
    private Action<bool> completion;
    private void Update()
    {
        if (active && Time.realtimeSinceStartupAsDouble >= deadline) Finish(request, false);
    }
    private void Finish(int id, bool result)
    {
        if (!active || id != request) return;
        active = false;
        if (observedProvider != null) observedProvider.AdOpened -= openedHandler;
        observedProvider = null; openedHandler = null;
        bool shown = providerOpened; bool rewarded = requestRewarded; providerOpened = false;
        var callback = completion;
        completion = null;
        var pause = adPause;
        var control = adControl;
        adPause = null;
        adControl = null;
        // Detach request-owned references before any external callback can start another ad.
        releasing = true;
        try
        {
            ReleaseSafely(() => DeadBoat.Online.SharedAdProtection.End(), "protection");
            if (pause != null) ReleaseSafely(() => pause.SetAdPause(false), "pause");
            if (control != null) ReleaseSafely(() => control.SetAdCursor(false), "cursor");
        }
        finally { releasing = false; }
        NotifySafely(AdClosed, "closed");
        NotifySafely(callback, result, "completion");
        if (shown) NotifySafely(ShownAdClosed, rewarded, "shown");
    }
    private static void ReleaseSafely(Action action, string stage)
    {
        try { action(); }
        catch (Exception exception) { Debug.LogWarning("[Ads] " + stage + " failed: " + exception.GetType().Name); }
    }
    private static void NotifySafely(Action handlers, string stage)
    {
        if (handlers == null) return;
        foreach (Action handler in handlers.GetInvocationList()) ReleaseSafely(handler, stage);
    }
    private static void NotifySafely(Action<bool> handlers, bool value, string stage)
    {
        if (handlers == null) return;
        foreach (Action<bool> handler in handlers.GetInvocationList()) ReleaseSafely(() => handler(value), stage);
    }
    private async void Show(AdsProvider provider, bool rewarded, string rewardId, Action<bool> callback)
    {
        if (this == null || !isActiveAndEnabled || active || releasing) { NotifySafely(callback, false, "completion"); return; }
        active = true;
        int id = ++request;
        providerOpened = false; requestRewarded = rewarded; observedProvider = provider;
        openedHandler = () => { if (active && request == id) providerOpened = true; };
        provider.AdOpened += openedHandler;
        completion = callback;
        deadline = Time.realtimeSinceStartupAsDouble + DeadBoat.Online.SharedDepartureState.AdMaximumSeconds;
        try
        {
            bool protectedByRun = await DeadBoat.Online.SharedAdProtection.PrepareAsync();
            if (!active || id != request) return;
            if (!protectedByRun) { Finish(id, false); return; }
            adPause = PauseManager.Instance;
            adControl = ControlManager.Instance;
            if (adControl != null) adControl.SetAdCursor(true);
            if (adPause != null) adPause.SetAdPause(true);
            if (rewarded) provider.ShowRewardedAd(rewardId, result => Finish(id, result));
            else provider.ShowInterstitialAd(result => Finish(id, result));
        }
        catch (Exception exception)
        {
            Debug.LogWarning("[Ads] Request failed: " + exception.GetType().Name);
            Finish(id, false);
        }
    }

    private static bool RewardedReady(AdsProvider provider)
    {
        if (provider == null) return false;
        try { return provider.IsRewardedAdReady(); }
        catch (Exception exception)
        {
            Debug.LogWarning("[Ads] Readiness failed: " + exception.GetType().Name);
            return false;
        }
    }

    private void InitializeProviders()
    {
        foreach (var provider in adsProviders)
        {
            provider.Initialize();
            //Debug.Log($"Initialized ads provider: {provider.GetType().Name}");
        }
    }

    // Проверка готовности rewarded-рекламы
    public bool IsRewardedAdReady()
    {
        if (this == null || !isActiveAndEnabled || active || releasing) return false;
        if (adsProviders.Count == 0)
        {
            Debug.LogWarning("No ads providers configured!");
            return false;
        }

        foreach (var provider in adsProviders)
        {
            if (RewardedReady(provider))
            {
                return true;
            }
        }
        return false;
    }

    // Показ rewarded-рекламы через первый готовый провайдер
    public void ShowRewardedAd(string rewardId, Action<bool> onComplete)
    {
        if (this == null || !isActiveAndEnabled || active || releasing) { NotifySafely(onComplete, false, "completion"); return; }
        foreach (var provider in adsProviders)
            if (RewardedReady(provider))
            { Show(provider, true, rewardId, onComplete); return; }
        NotifySafely(onComplete, false, "completion");
    }

    public void ShowInterstitialAd(Action<bool> onComplete = null)
    {
        if (AdRemovalPolicy.Owned) { NotifySafely(onComplete, false, "completion"); return; }
        if (this == null || !isActiveAndEnabled || active || releasing) { NotifySafely(onComplete, false, "completion"); return; }
        foreach (var provider in adsProviders)
            if (provider != null) { Show(provider, false, null, onComplete); return; }
        NotifySafely(onComplete, false, "completion");
    }

    public void AddProvider(AdsProvider provider)
    {
        if (!adsProviders.Contains(provider))
        {
            adsProviders.Add(provider);
            provider.Initialize();
        }
    }
}