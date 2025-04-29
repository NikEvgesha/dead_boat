using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireballController : MonoBehaviour
{
    [SerializeField] private ParticleSystem _particle;

    private void Start()
    {
        _particle.Stop();
    }

    public void BurstParticles()
    {
        _particle.Stop();
        _particle.Play();
    }
}
