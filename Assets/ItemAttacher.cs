using System;
using System.Collections.Generic;
using UnityEngine;

public class ItemAttacher : MonoBehaviour
{
    [SerializeField] private LayerMask _attachLayer;
    private bool _inAttachZone;

    HashSet<Collider> _zones = new();
    private int _zonesCounter = 0;


    public Action<bool> CanAttach;

    public bool InAttachZone => _inAttachZone;


    private void OnTriggerEnter(Collider other)
    {

        if ((_attachLayer | (1 << other.gameObject.layer)) == _attachLayer)
        {
            if (_zones.Contains(other))
                return;

            _zones.Add(other);
            _zonesCounter += 1;
            if (!_inAttachZone)
            {
                _inAttachZone = true;
                CanAttach?.Invoke(_inAttachZone);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if ((_attachLayer | (1 << other.gameObject.layer)) == _attachLayer)
        {
            if (!_zones.Contains(other))
                return;

            _zones.Remove(other);
            _zonesCounter -= 1;
            if (_inAttachZone && _zonesCounter <= 0)
            {
                _inAttachZone = false;
                CanAttach?.Invoke(_inAttachZone);
            }
        }
    }
}
