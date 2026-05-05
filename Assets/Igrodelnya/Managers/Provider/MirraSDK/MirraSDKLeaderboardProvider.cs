using MirraGames.SDK;
using MirraGames.SDK.Common;
using System;
using System.Collections.Generic;
using UnityEngine;

public class MirraSDKLeaderboardProvider : LeaderboardProvider
{
    public override void LoadLB(LBName LBTag, int topAmount, bool includePlayer, Action<LBData> onLoad)
    {
        if (!IsLeaderboardSupported())
        {
            onLoad?.Invoke(CreateEmptyData(LBTag));
            return;
        }

        MirraSDK.WaitForProviders(() =>
        {
            try
            {
                MirraSDK.Achievements.GetLeaderboard(
                    boardId: LBTag.ToString(),
                    onLeaderboard: (scoreTable) =>
                    {
                        LBData data = CreateEmptyData(LBTag);

                        if (scoreTable?.players != null)
                        {
                            foreach (PlayerScore player in scoreTable.players)
                            {
                                LBRecord rec = new LBRecord
                                {
                                    Name = player.displayName,
                                    Rank = player.position,
                                    Score = player.score,
                                    URL = player.profilePictureUrl
                                };
                                data.Records.Add(rec);
                            }
                        }

                        onLoad?.Invoke(data);
                    }
                );
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"MirraSDKLeaderboardProvider: failed to load leaderboard '{LBTag}' ({exception.Message})");
                onLoad?.Invoke(CreateEmptyData(LBTag));
            }
        });
    }

    public override void SaveScore(string LBName, int score)
    {
        if (!IsLeaderboardSupported())
            return;

        try
        {
            MirraSDK.Achievements.SetScore(
                boardId: LBName,
                score: score);
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"MirraSDKLeaderboardProvider: failed to save score for '{LBName}' ({exception.Message})");
        }
    }

    private static LBData CreateEmptyData(LBName name)
    {
        return new LBData
        {
            LBName = name,
            Records = new List<LBRecord>()
        };
    }

    private bool IsLeaderboardSupported()
    {
        if (!MirraSDK.IsInitialized)
            return false;

        try
        {
            DeploymentType deployment = MirraSDK.Platform.Deployment;
            PlatformType platform = MirraSDK.Platform.Current;

            if (deployment == DeploymentType.Editor ||
                platform == PlatformType.Editor ||
                platform == PlatformType.Localhost ||
                platform == PlatformType.Unknown ||
                platform == PlatformType.Playgama ||
                platform == PlatformType.PlaygamaBridge)
            {
                return false;
            }

            return true;
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"MirraSDKLeaderboardProvider: platform check failed ({exception.Message})");
            return false;
        }
    }
}
