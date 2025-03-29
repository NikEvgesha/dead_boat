using System;
using System.Collections.Generic;
using UnityEngine;

public class GravityChecker : MonoBehaviour
{
    public bool InGravitySource { get; private set; }

    public Action<bool> GravityChanged;

    private HashSet<Collider> _gravityPlatforms;
    private int _gravitySourceCounter = 0;


    private void OnEnable()
    {
        _gravitySourceCounter = 0;
        _gravityPlatforms = new();
        InGravitySource = false;
    }

    private void OnTriggerEnter(Collider other)
    {

        if (other.tag == "GravityPlatform")
        {
            if (_gravityPlatforms.Contains(other))
                return;

            _gravityPlatforms.Add(other);
            _gravitySourceCounter += 1;
            if (!InGravitySource)
            {
                InGravitySource = true;
                GravityChanged?.Invoke(InGravitySource);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.tag == "GravityPlatform")
        {
            if (!_gravityPlatforms.Contains(other))
                return;

            _gravityPlatforms.Remove(other);
            _gravitySourceCounter -= 1;
            if (InGravitySource && _gravitySourceCounter <= 0)
            {
                InGravitySource = false;
                GravityChanged?.Invoke(InGravitySource);
            }
        }
    }


}
