using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "animal_new", menuName = "ScriptableObject/Eggs/Animal Definition")]
public class AnimalDefinition : EggNamedDefinition
{
    public string animalId => Id;
    public string title;
    public Sprite icon;
    [Min(1)] public int maxStage = 3;
    public List<AnimalStageDefinition> stages = new();

    public AnimalStageDefinition GetStage(int stage)
    {
        int safeStage = Mathf.Max(1, stage);
        AnimalStageDefinition fallback = null;

        for (int i = 0; i < stages.Count; i++)
        {
            AnimalStageDefinition definition = stages[i];
            if (definition == null)
                continue;

            if (definition.stage == safeStage)
                return definition;

            if (fallback == null || definition.stage < fallback.stage)
                fallback = definition;
        }

        return fallback;
    }
}
