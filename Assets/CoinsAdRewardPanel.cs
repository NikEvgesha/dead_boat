using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CoinsAdRewardPanel : MonoBehaviour
{
    [SerializeField] private GameObject _panel;
    [SerializeField] private int _coinsAdReward;
    [SerializeField] private Text _rewardText;

    private bool _isOpen;

    private void Awake()
    {
        _rewardText.text = _coinsAdReward.ToString();
    }

    public void ToggleOpen()
    {
        _isOpen = !_isOpen;
        _panel.SetActive(_isOpen);
        ControlManager.Instance.CursorActive = _isOpen;
    }

    public void OnAdButtonClick()
    {

    }
}
