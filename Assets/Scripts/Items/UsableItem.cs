using System;
using UnityEngine;

public class UsableItem : MonoBehaviour
{

    private bool _isActive = false;
    public bool IsActive { get { return _isActive; } }
    private PlayerItemPickUp _playerItemPickUp;
    public Action Use;
    public Action StopUse;
    public Action<bool> Active;
    private bool _canUse = true;
    public void SetActiveItem(bool active)
    {
        _isActive = active;
        Active?.Invoke(_isActive);
    }
    private void Start()
    {
        _playerItemPickUp = FindAnyObjectByType<PlayerItemPickUp>();
    }
    private void Update()
    {
        if (_isActive)
        {

            //Debug.Log("Нажата ли кнопка :" + PlayerInput.Instance.UseItem);
            if (PlayerInput.Instance.UseItem)
            {
                if (_playerItemPickUp.PickUpIsUse())
                    _canUse = false;
                
                if (_canUse)
                {
                    Use?.Invoke();
                }
            }
            else
            {
                _canUse = true;
                StopUse?.Invoke();
            }

        }
    }
    
}
