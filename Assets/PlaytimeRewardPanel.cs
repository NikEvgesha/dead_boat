using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct PlaytimeReward
{
    public int amount;
    public int playtimeMinutes;
}

public class PlaytimeRewardPanel : MonoBehaviour
{
    [SerializeField] private List<PlaytimeReward> _rewards;
    [SerializeField] private DynamicGridSpawner _grid;
    [SerializeField] private PlaytimeRewardSlot _slotPrefab;
    [SerializeField] private GameObject _panel;
    [SerializeField] private GameObject _button;

    private bool _isOpen = false;
    private List<PlaytimeRewardSlot> _slots = new();


    private void Start()
    {
        LoadingManager.Instance.LocationChanged += ToggleButtonVisibility;
        foreach (PlaytimeReward reward in _rewards)
        {
            PlaytimeRewardSlot slot = _grid.SpawnObject<PlaytimeRewardSlot>(_slotPrefab.gameObject);
            _slots.Add(slot);
            slot.Init(reward);
        }
    }

    public void ToggleOpen()
    {
        _isOpen = !_isOpen;
        _panel.SetActive(_isOpen);
    }

    private void ToggleButtonVisibility(Location location)
    {
        _button.SetActive(location == Location.Lobby);

/*        if (location != Location.Lobby) { 
            foreach (PlaytimeRewardSlot slot in _slots)
            {
                slot.ResetReward();
            }
        }*/

    }
}
