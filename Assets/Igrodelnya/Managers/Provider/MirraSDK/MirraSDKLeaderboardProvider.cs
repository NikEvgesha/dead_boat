using MirraGames.SDK;
using MirraGames.SDK.Common;
using System;
using UnityEngine;

public class MirraSDKLeaderboardProvider : LeaderboardProvider
{
    public override void LoadLB(LBName LBTag, int topAmount, bool includePlayer, Action<LBData> onLoad)
    {
        MirraSDK.Socials.InvokeLeaderboard("gems");
        MirraSDK.Socials.GetScoreTable(
            scoreTag: LBTag.ToString(),
            leadingPlayers: topAmount,
            includePlayer: includePlayer,
            playersAround: 1,
            onScoreTableResolve: (scoreTable) => {
                LBData data = new();
                data.LBName = LBTag;
                data.Records = new();
                //List<LBRecord> records = new();
                for (int i = 0; i < scoreTable.Count; i++)
                {
                    PlayerScore playerScore = scoreTable[i];
                    LBRecord rec = new LBRecord();
                    rec.Name = playerScore.DisplayName;
                    rec.Rank = playerScore.Position;
                    rec.Score = playerScore.Score;
                    rec.URL = playerScore.PictureURL;
                    data.Records.Add(rec);
                }

                Debug.Log("onScoreTableResolve : " + scoreTable.Count);
                onLoad(data);
            },
            onScoreTableError: () => {

                Debug.Log("onScoreTableError");
                onLoad(new LBData());
            }
        );
    }


    public override void SaveScore(string LBName, int score)
    {
        MirraSDK.Socials.SetScore(
            scoreTag: LBName,
            scoreValue: score);
        Debug.Log(LBName + " set score " + score);
    }
}