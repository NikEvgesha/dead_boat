using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
[CreateAssetMenu(menuName = "ScrptableObject/LevelData", fileName = "New Level")]
public class LevelList : ScriptableObject
{
    [SerializeField] private List<LevelData> _levels;

    public List<LevelData> Levels => _levels;
}