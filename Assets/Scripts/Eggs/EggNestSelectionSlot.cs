using System;
using UnityEngine;
using UnityEngine.UI;

public class EggNestSelectionSlot : MonoBehaviour
{
    [SerializeField] private Text _titleText;
    [SerializeField] private Text _countText;
    [SerializeField] private Text _durationText;
    [SerializeField] private Text _rarityText;
    [SerializeField] private Image _eggIconImage;
    [SerializeField] private Transform _possiblePetsRoot;
    [SerializeField] private GameObject _possiblePetTemplate;
    [SerializeField] private string _defaultRarity = "Common";
    [SerializeField] private int _maxPossiblePets = 3;
    [SerializeField] private Button _button;

    private string _eggId;
    private Action<string> _onSelect;

    public void BindTemporaryReferences(Text titleText, Text countText, Text durationText, Button button)
    {
        _titleText = titleText;
        _countText = countText;
        _durationText = durationText;
        _button = button;
    }

    public void Init(EggDefinition definition, int amount, Action<string> onSelect)
    {
        _eggId = definition != null ? definition.eggId : string.Empty;
        _onSelect = onSelect;

        if (_titleText != null)
        {
            _titleText.text = string.IsNullOrWhiteSpace(definition?.title)
                ? _eggId
                : definition.title;
        }

        if (_countText != null)
            _countText.text = $"x{Mathf.Max(0, amount)}";

        if (_rarityText != null)
            _rarityText.text = string.IsNullOrWhiteSpace(definition?.rarity)
                ? _defaultRarity
                : definition.rarity;

        if (_eggIconImage != null)
        {
            _eggIconImage.sprite = definition != null ? definition.icon : null;
            _eggIconImage.enabled = _eggIconImage.sprite != null;
        }

        if (_durationText != null)
        {
            _durationText.text = definition == null
                ? string.Empty
                : FormatSeconds(Mathf.Max(1, definition.incubationSeconds));
        }

        RebuildPossiblePets(definition);

        if (_button == null)
            return;

        _button.onClick.RemoveListener(OnButtonClicked);
        _button.onClick.AddListener(OnButtonClicked);
        _button.interactable = definition != null && amount > 0 && !string.IsNullOrWhiteSpace(_eggId);
    }

    private void OnDisable()
    {
        if (_button != null)
            _button.onClick.RemoveListener(OnButtonClicked);
    }

    private void OnButtonClicked()
    {
        if (string.IsNullOrWhiteSpace(_eggId))
            return;

        _onSelect?.Invoke(_eggId);
    }

    private void RebuildPossiblePets(EggDefinition definition)
    {
        if (_possiblePetsRoot == null || _possiblePetTemplate == null)
            return;

        for (int i = _possiblePetsRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = _possiblePetsRoot.GetChild(i);
            if (child == _possiblePetTemplate.transform)
                continue;

            Destroy(child.gameObject);
        }

        _possiblePetTemplate.SetActive(false);

        int shown = 0;
        if (definition != null && definition.hatchResults != null)
        {
            for (int i = 0; i < definition.hatchResults.Count && shown < _maxPossiblePets; i++)
            {
                EggHatchResult result = definition.hatchResults[i];
                if (result == null || result.animal == null)
                    continue;

                GameObject item = Instantiate(_possiblePetTemplate, _possiblePetsRoot);
                item.SetActive(true);
                SetPossiblePetVisual(item, result.animal);
                shown++;
            }
        }

        for (; shown < _maxPossiblePets; shown++)
        {
            GameObject item = Instantiate(_possiblePetTemplate, _possiblePetsRoot);
            item.SetActive(true);
            SetPossiblePetVisual(item, null);
        }
    }

    private static string GetAnimalDisplayText(AnimalDefinition animal)
    {
        string title = animal != null && !string.IsNullOrWhiteSpace(animal.title)
            ? animal.title
            : animal != null ? animal.animalId : "?";

        if (string.IsNullOrWhiteSpace(title))
            return "?";

        return title.Length <= 2 ? title : title.Substring(0, 1).ToUpperInvariant();
    }

    private static void SetPossiblePetVisual(GameObject item, AnimalDefinition animal)
    {
        Image image = item != null ? item.GetComponentInChildren<Image>(true) : null;
        if (image != null)
        {
            image.sprite = animal != null ? animal.icon : null;
            image.enabled = image.sprite != null;
        }

        Text text = item != null ? item.GetComponentInChildren<Text>(true) : null;
        if (text != null)
            text.text = animal != null && animal.icon != null ? string.Empty : GetAnimalDisplayText(animal);
    }

    private static string FormatSeconds(int totalSeconds)
    {
        int seconds = Mathf.Max(0, totalSeconds);
        int hours = seconds / 3600;
        int minutes = (seconds % 3600) / 60;
        int secs = seconds % 60;
        return $"{hours:00}:{minutes:00}:{secs:00}";
    }
}
