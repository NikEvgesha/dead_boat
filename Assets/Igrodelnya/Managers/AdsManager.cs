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
    }


    private void OnDisable() => Finish(request, false);

    private PauseManager adPause;
    private ControlManager adControl;
    private bool previousCursor;
    private bool active;
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
        var callback = completion;
        completion = null;
        DeadBoat.Online.SharedAdProtection.End();
        AdClosed?.Invoke();
        callback?.Invoke(result);
    }
    private async void Show(AdsProvider provider, bool rewarded, string rewardId, Action<bool> callback)
    {
        if (active) { callback?.Invoke(false); return; }
        active = true;
        int id = ++request;
        completion = callback;
        deadline = Time.realtimeSinceStartupAsDouble + DeadBoat.Online.SharedDepartureState.AdMaximumSeconds;
        try
        {
            bool protectedByRun = await DeadBoat.Online.SharedAdProtection.PrepareAsync();
            if (!active || id != request) return;
            if (!protectedByRun) { Finish(id, false); return; }
            adPause = PauseManager.Instance;
            adControl = ControlManager.Instance;
            if (adControl != null) { previousCursor = adControl.CursorActive; adControl.CursorActive = true; }
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
        if (active) return false;
        if (adsProviders.Count == 0)
        {
            Debug.LogWarning("No ads providers configured!");
            return false;
        }

        foreach (var provider in adsProviders)
        {
            if (provider != null && provider.IsRewardedAdReady())
            {
                return true;
            }
        }
        return false;
    }

    // Показ rewarded-рекламы через первый готовый провайдер
    public void ShowRewardedAd(string rewardId, Action<bool> onComplete)
    {
        if (active) { onComplete?.Invoke(false); return; }
        foreach (var provider in adsProviders)
            if (provider != null && provider.IsRewardedAdReady())
            { Show(provider, true, rewardId, onComplete); return; }
        onComplete?.Invoke(false);
    }

    public void ShowInterstitialAd(Action<bool> onComplete = null)
    {
        if (active) { onComplete?.Invoke(false); return; }
        foreach (var provider in adsProviders)
            if (provider != null) { Show(provider, false, null, onComplete); return; }
        onComplete?.Invoke(false);
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