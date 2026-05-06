using System;
using UnityEngine;

[Serializable]
public class AnimalStageDefinition
{
    [Min(1)] public int stage = 1;
    public GameObject animalPrefab;
    public Color tint = Color.white;
    public GameObject mergeParticlesPrefab;
    public AnimalRunBuffs buffs = new();
}
