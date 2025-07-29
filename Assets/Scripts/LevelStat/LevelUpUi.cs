using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public struct RareBack
{
    public RareType Type;
    public Sprite sprite;
}
public class LevelUpUi : MonoBehaviour
{
    public static LevelUpUi Instance;
    [SerializeField] private GameObject _panel;
    [SerializeField] private List<LevelUpButtomUI> _buttons;
    [SerializeField] private List<RareBack> _rareBack;

    public RareBack GetRareBackUI(RareType rare) 
    {
        return _rareBack.Find(r => r.Type == rare);
    }

    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(Instance.gameObject);
        }
        _panel.SetActive(false);

    }
    public void NewLevel(List<ItemWithRare> items)
    {

        PauseManager.Instance.SetPause(true, false);
        ControlManager.Instance.CursorActive = true;
        _panel.SetActive(true);
        int i = 0;
        foreach (var item in items)
        {
            _buttons[i].AddItemInButton(item.Item, item.Rare);
            i++;
        }       
        
    }
    public void Close()
    {
        PauseManager.Instance.SetPause(false, false);
        ControlManager.Instance.CursorActive = false;
        _panel.SetActive(false);
    }

}

