using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InfoUI : MonoBehaviour
{
    [SerializeField] GameObject _infoObj;
    [SerializeField] private Text _itemName;
    [SerializeField] private Text _itemTags;

    public void ShowInfo(bool visible)
    {
        _infoObj.SetActive(visible);
    }


    public void SetInfoText(string name, List<ItemTag> tags)
    {

        _itemName.text = LocalizationManager.Instance.LocalizationData.GetTranslation(name, LocalizationManager.Instance.CurrentLanguage, LocalizationKeyType.Item.ToString());
        _itemTags.text = "";
        foreach (ItemTag tag in tags)
        {
            string tagText = LocalizationManager.Instance.LocalizationData.GetTranslation(tag.ToString(), LocalizationManager.Instance.CurrentLanguage, LocalizationKeyType.Tag.ToString());
            _itemTags.text += tagText + "\n";
        }
    }



}
