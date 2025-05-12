using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;


[CreateAssetMenu(menuName = "ScriptableObject/Achievement Database", fileName = "AchievementsList")]
public class AchievementsList : ScriptableObject
{
    [SerializeField] private List<Achievement> achievements;

    private Dictionary<string, Achievement> achievementsDict;

    public List<Achievement> Achievements => achievements;

    private void Awake()
    {
        achievementsDict = new();
        foreach (Achievement achievement in achievements)
        {
            achievementsDict.Add(achievement.id, achievement);
        }
    }
    public Achievement get(string id)
    {
        bool contains = achievementsDict.TryGetValue(id, out Achievement achievement);
        if (contains)
        {
            return achievement;
        }
        return null;
    }


}