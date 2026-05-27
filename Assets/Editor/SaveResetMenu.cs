using System;
using MirraGames.SDK;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SaveResetMenu
{
    private const string MenuPath = "Tools/Debug/Reset Saves";

    [MenuItem(MenuPath)]
    private static void ResetSaves()
    {
        if (!EditorUtility.DisplayDialog(
                "Reset Saves",
                "Reset all local save data, egg progress, professions and ad cooldowns?",
                "Reset",
                "Cancel"))
        {
            return;
        }

        ResetMirraSaves();
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();

        EggFeatureStorage.Reset();
        EggAdRewardStorage.Reset();
        ProfessionStorage.Reset();

        Debug.Log("SaveResetMenu: saves reset.");

        if (EditorApplication.isPlaying)
            ReloadActiveScene();
    }

    private static void ResetMirraSaves()
    {
        try
        {
            if (!MirraSDK.IsInitialized)
                return;

            MirraSDK.Data.DeleteAll();
            MirraSDK.Data.Save();
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"SaveResetMenu: Mirra save reset failed ({exception.Message})");
        }
    }

    private static void ReloadActiveScene()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (!activeScene.IsValid())
            return;

        if (activeScene.buildIndex >= 0)
        {
            SceneManager.LoadScene(activeScene.buildIndex);
            return;
        }

        if (!string.IsNullOrWhiteSpace(activeScene.path))
            SceneManager.LoadScene(activeScene.path);
    }
}
