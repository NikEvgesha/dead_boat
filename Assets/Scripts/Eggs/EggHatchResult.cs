using System;
using UnityEngine;

[Serializable]
public class EggHatchResult
{
    public AnimalDefinition animal;
    [Min(0)] public int weight = 1;

    public string AnimalId => animal != null ? animal.animalId : string.Empty;
}
