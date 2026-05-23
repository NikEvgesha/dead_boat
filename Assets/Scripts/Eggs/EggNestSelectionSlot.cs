using System;
using UnityEngine;
using UnityEngine.UI;

public class EggNestSelectionSlot : MonoBehaviour
{
    [SerializeField] private LocalizationData _localizationData;
    [SerializeField] private Text _titleText;
    [SerializeField] private Text _countText;
    [SerializeField] private Text _durationText;
    [SerializeField] private Text _rarityText;
    [SerializeField] private Image _eggIconImage;
    [SerializeField] private Transform _possiblePetsRoot;
    [SerializeField] private GameObject _possiblePetTemplate;
    [SerializeField] private string _defaultRarity = "Common";
    [SerializeField] private string _rarityLocalizationPrefix = "Boost/RareType/";
    [SerializeField] private int _maxPossiblePets = 3;
    [SerializeField] private Button _button;
    [SerializeField] private GameObject _actionRoot;

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
        Init(definition, amount, onSelect, true);
    }

    public void Init(EggDefinition definition, int amount, Action<string> onSelect, bool canSelect)
    {
        _eggId = definition != null ? definition.eggId : string.Empty;
        _onSelect = onSelect;

        if (_titleText != null)
        {
            string titleKey = definition != null ? definition.title : string.Empty;
            _titleText.text = string.IsNullOrWhiteSpace(titleKey)
                ? _eggId
                : GetLocalizedText(titleKey, titleKey);
        }

        if (_countText != null)
            _countText.text = $"x{Mathf.Max(0, amount)}";

        if (_rarityText != null)
        {
            string rarity = string.IsNullOrWhiteSpace(definition?.rarity)
                ? _defaultRarity
                : definition.rarity;
            _rarityText.text = GetLocalizedText(_rarityLocalizationPrefix + rarity, rarity);
        }

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

        if (_actionRoot != null)
            _actionRoot.SetActive(canSelect);

        if (_button == null)
            return;

        _button.onClick.RemoveListener(OnButtonClicked);
        if (canSelect)
            _button.onClick.AddListener(OnButtonClicked);

        _button.interactable = canSelect && definition != null && amount > 0 && !string.IsNullOrWhiteSpace(_eggId);
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

    private string GetLocalizedText(string key, string fallback)
    {
        if (_localizationData == null || string.IsNullOrWhiteSpace(key))
            return fallback;

        string language = LocalizationManager.Instance != null
            ? LocalizationManager.Instance.CurrentLanguage
            : "Ru";

        return _localizationData.TryGetTranslation(key, language, out string value)
            ? value
            : fallback;
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
                SetPossiblePetPreview(item, result.animal);
                shown++;
            }
        }

        for (; shown < _maxPossiblePets; shown++)
        {
            GameObject item = Instantiate(_possiblePetTemplate, _possiblePetsRoot);
            item.SetActive(true);
            SetPossiblePetPreview(item, null);
        }
    }

    private static string GetAnimalDisplayText(AnimalDefinition animal)
    {
        string title = animal != null
            ? EggFeatureLocalization.AnimalTitle(animal)
            : "?";

        if (string.IsNullOrWhiteSpace(title))
            return "?";

        return title.Length <= 2 ? title : title.Substring(0, 1).ToUpperInvariant();
    }

    private static void SetPossiblePetPreview(GameObject item, AnimalDefinition animal)
    {
        if (item != null && item.TryGetComponent(out EggPossiblePetPreview preview))
        {
            preview.SetAnimal(animal);
            return;
        }

        SetPossiblePetVisualFallback(item, animal);
    }

    private static void SetPossiblePetVisualFallback(GameObject item, AnimalDefinition animal)
    {
        Image image = FindPossiblePetIcon(item);
        if (image != null)
        {
            image.sprite = animal != null ? animal.icon : null;
            image.enabled = image.sprite != null;
        }

        Text text = item != null ? item.GetComponentInChildren<Text>(true) : null;
        if (text != null)
            text.text = animal != null && animal.icon != null ? string.Empty : GetAnimalDisplayText(animal);
    }

    private static Image FindPossiblePetIcon(GameObject item)
    {
        if (item == null)
            return null;

        Transform imageZoo = FindChildRecursive(item.transform, "ImageZoo");
        if (imageZoo != null && imageZoo.TryGetComponent(out Image zooImage))
            return zooImage;

        Image[] images = item.GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; i++)
        {
            Image image = images[i];
            if (image != null && image.name != "Background")
                return image;
        }

        return null;
    }

    private static Transform FindChildRecursive(Transform root, string childName)
    {
        if (root == null)
            return null;

        if (root.name == childName)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindChildRecursive(root.GetChild(i), childName);
            if (found != null)
                return found;
        }

        return null;
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
