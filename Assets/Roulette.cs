using MirraGames.SDK;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public struct RouletteReward
{
    public RouletteRewardType rewardType;
    public int amount;
    public PickableItem item;
    public float weight;
}

public class Roulette : MonoBehaviour
{
    [SerializeField] private List<RouletteReward> _rewards = new();
    [SerializeField] private int _gemsPrice;
    [SerializeField] private float _startSpeed = 50f;
    [SerializeField] private float _spinDuration = 5f;
    [SerializeField] private AnimationCurve _speedCurve;

    [SerializeField] private GameObject _ui;
    [SerializeField] private GameObject _button;

    [SerializeField] private GameObject _wheel;
    [SerializeField] private Transform _rewardsParent;
    [SerializeField] private Transform _iconPoint;
    [SerializeField] private Text _priceText;
    

    [SerializeField] private Button _adButton;
    [SerializeField] private Button _gemsButton;
    [SerializeField] private GameObject _freePlayText;
    [SerializeField] private GameObject _unavailableText;
    [SerializeField] private GameObject _adIcon;
    [SerializeField] private Text _freePlayTimeText;




    private List<RouletteSlot> _slots;
    private float _rotateAngle;
    private bool _isOpen;
    private int _targetId;
    private bool _spinning;
    private float _spinProgress;
    private int _tillNextDay;
    private bool _freeAvailable;
    private float _targetAngle;
    private float _currentAngle;
    private float _currentSpinTime;


    private void Awake()
    {
        _slots = _rewardsParent.GetComponentsInChildren<RouletteSlot>().ToList();
        _rotateAngle = 360f / _slots.Count;
        _priceText.text = _gemsPrice.ToString();
    }

    private void Start()
    {
        LoadingManager.Instance.LocationChanged += ToggleButtonVisibility;
        for (int i = 0; i < _slots.Count; i++)
        {
            _slots[i].Init(_rewards[i]);
            _slots[i].transform.RotateAround(_wheel.transform.position, Vector3.forward, -i * _rotateAngle);

        }
        CheckFreeSpinAvailable();
    }

    private void OnDisable()
    {
        LoadingManager.Instance.LocationChanged -= ToggleButtonVisibility;
    }

    public void ToggleOpen()
    {
        _isOpen = !_isOpen;
        _ui.SetActive(_isOpen);
        if (!_isOpen)
            StopAllCoroutines();
    }


    private void ToggleButtonVisibility(Location location)
    {
        _button.SetActive(location == Location.Lobby);
    }

    public void OnPlayButtonClick()
    {
        if (_freeAvailable)
        {
            StartSpin();
            SwitchFreePlayButton(false);
        } else
        {
            AdsManager.Instance.ShowRewardedAd(
                "RouletteSpin",
                (success) =>
                {
                    if (success)
                        StartSpin();
            });
        }
    }

    public void OnGemsButtonClick()
    {
        if (CurrencyManager.Instance.CheckEnoughCurrency(CurrencyType.Gems, _gemsPrice))
        {
            CurrencyManager.Instance.RemoveCurrency(CurrencyType.Gems, _gemsPrice);
            StartSpin();
        }
    }


    private void StartSpin()
    {
        _adButton.interactable = false;
        _gemsButton.interactable = false;
        SaveManager.Instance.SaveRouletteDate(MirraSDK.Time.CurrentDate);

        _targetId = _rewards.IndexOf(GetRandomReward());

        if (_rewards[_targetId].rewardType == RouletteRewardType.Gems)
        {
            Debug.Log("roulette reward: " + _rewards[_targetId].amount + " gems");
        } else
        {
            Debug.Log("roulette reward: " + _rewards[_targetId].item.Data.Name);
        }



        _targetAngle = _targetId * _rotateAngle + 360 * UnityEngine.Random.Range(3, 6)+ UnityEngine.Random.Range(-_rotateAngle/4, _rotateAngle/4);
        Debug.Log("target angle: " + _targetId * _rotateAngle);

        _wheel.transform.rotation = Quaternion.Euler(Vector3.zero); 
        StartCoroutine(Spin());
        Debug.Log("Spin");
    }

    private RouletteReward GetRandomReward()
    {
        float totalWeight = 0f;

        foreach (RouletteReward reward in _rewards)
        {
            totalWeight += reward.weight;
        }

        float randomValue = UnityEngine.Random.Range(0f, totalWeight);

        foreach (RouletteReward reward in _rewards)
        {
            if (randomValue < reward.weight)
            {
                return reward;
            }

            randomValue -= reward.weight;
        }

        return _rewards[0];

    }


    private void CheckFreeSpinAvailable()
    {
        DateTime lastSpinDate = SaveManager.Instance.LoadRouletteDate();
        _freeAvailable = lastSpinDate != MirraSDK.Time.CurrentDate;
        SwitchFreePlayButton(_freeAvailable);

    }

    private void SwitchFreePlayButton(bool free)
    {
        _freePlayText.SetActive(free);
        _adIcon.SetActive(!free);
        _unavailableText.SetActive(!free);
        _freeAvailable = free;

        if (!free)
        {
            SetTime();
            StartCoroutine(FreeSpinTimer());
        }
        
    }

    private void SetTime()
    {

        _tillNextDay = 9999; // TODO
        _freePlayTimeText.text = String.Format(
            "{0}:{1}:{2}",
            (_tillNextDay / 24 / 60).ToString("D2"),
            (_tillNextDay % 24 / 60).ToString("D2"),
            (_tillNextDay % 60).ToString("D2")
        );
    }

    private IEnumerator FreeSpinTimer()
    {
        while (_tillNextDay > 0)
        {
            _tillNextDay -= 1;
            _freePlayTimeText.text = String.Format(
                "{0}:{1}:{2}",
                (_tillNextDay / 24 / 60).ToString("D2"),
                (_tillNextDay % 24 / 60).ToString("D2"),
                (_tillNextDay % 60).ToString("D2")
            );
            yield return new WaitForSeconds(1);
        }
        SwitchFreePlayButton(true);
    }

    private IEnumerator Spin()
    {
        _currentAngle = 0;
        _currentSpinTime = 0;
        _spinning = true;

        float t;
        float speed;
        float zAngle;

        while (_spinning)
        {
            _currentSpinTime += Time.deltaTime;

            if (_currentSpinTime > _spinDuration)
            {
                _currentSpinTime = _spinDuration;
                _spinning = false;
            }

            t = _currentSpinTime / _spinDuration;
            speed = _speedCurve.Evaluate(t);

            zAngle = Mathf.Lerp(0, _targetAngle, speed);
            _wheel.transform.rotation = Quaternion.Euler(new Vector3(0,0,zAngle));

            yield return null;
        }

        GiveReward();
        _adButton.interactable = true;
        _gemsButton.interactable = true;
    }

    private void GiveReward()
    {
        RouletteReward reward = _rewards[_targetId];

        if (reward.rewardType == RouletteRewardType.Gems)
        {
            CurrencyManager.Instance.AddCurrency(CurrencyType.Gems, reward.amount);
        } else
        {
            PickableItem item = Instantiate(reward.item);
            Inventory.Instance.AddItem(item);
            if (!item.HaveTag(ItemTag.Ammo))
                SaveManager.Instance.SaveLobbyItem(item.Data.Name);
        }

            Debug.Log("Roulette reward");
    }
}
