using MirraGames.SDK;
using MirraGames.SDK.Common;
using System;
using UnityEngine;

public class MirraSDKPauseProvider : PauseProvider
{
    private bool _isPaused;
    private bool _isInitialized;

    public override bool IsPaused => _isPaused;

    public override void Initialize()
    {
        MirraSDK.WaitForProviders(() =>
        {
            _isInitialized = true;

            if (!IsGameplayAnalyticsEnabled())
                return;

            try
            {
                MirraSDK.Analytics.GameIsReady();
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"MirraSDKPauseProvider: failed to send GameIsReady ({exception.Message})");
            }
        });
    }

    public override void SetPause(bool paused, bool controlAudio = true)
    {
        if (_isPaused == paused) return;

        _isPaused = paused;

        if (_isInitialized && MirraSDK.IsInitialized)
        {
            try
            {
                MirraSDK.Time.Scale = paused ? 0f : 1f;

                if (controlAudio)
                    MirraSDK.Audio.Pause = paused;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"MirraSDKPauseProvider: failed to apply Mirra pause ({exception.Message})");
                ApplyUnityPause(paused, controlAudio);
            }
        }
        else
        {
            ApplyUnityPause(paused, controlAudio);
        }

        RaisePauseChanged(_isPaused);

        if (IsGameplayAnalyticsEnabled())
        {
            try
            {
                if (_isPaused)
                    MirraSDK.Analytics.GameplayStop();
                else
                    MirraSDK.Analytics.GameplayStart();
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"MirraSDKPauseProvider: failed to send gameplay analytics ({exception.Message})");
            }
        }

        Debug.Log($"MirraSDKPauseProvider: pause set to {_isPaused}");
    }

    private void ApplyUnityPause(bool paused, bool controlAudio)
    {
        Time.timeScale = paused ? 0f : 1f;

        if (controlAudio)
            AudioListener.pause = paused;
    }

    private bool IsGameplayAnalyticsEnabled()
    {
        if (!_isInitialized || !MirraSDK.IsInitialized)
            return false;

        try
        {
            PlatformType platform = MirraSDK.Platform.Current;
            return platform != PlatformType.Playgama && platform != PlatformType.PlaygamaBridge;
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"MirraSDKPauseProvider: gameplay analytics disabled ({exception.Message})");
            return false;
        }
    }
}
