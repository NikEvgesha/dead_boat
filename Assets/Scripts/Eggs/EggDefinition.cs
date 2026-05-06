using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "egg_new", menuName = "ScriptableObject/Eggs/Egg Definition")]
public class EggDefinition : EggNamedDefinition
{
    public string eggId => Id;
    public string title;
    [Min(1)] public int incubationSeconds = 300;
    [Min(0)] public int skipCostGems = 10;
    public GameObject eggPreviewPrefab;
    public List<EggHatchResult> hatchResults = new();
}
