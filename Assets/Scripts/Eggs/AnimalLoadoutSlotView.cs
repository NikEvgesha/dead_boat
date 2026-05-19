using System;
using UnityEngine;
using UnityEngine.UI;

public sealed class AnimalLoadoutSlotView : MonoBehaviour
{
    [SerializeField] private Button _button;
    [SerializeField] private Button _clearButton;
    [SerializeField] private Image _icon;
    [SerializeField] private Text _titleText;
    [SerializeField] private Text _stageText;
    [SerializeField] private Text _emptyText;

    private int _slotIndex;
    private Action<int> _onSelect;
    private Action<int> _onClear;

    public void Init(int slotIndex, PlacedAnimalState placed, EggHatchingManager manager, Action<int> onSelect, Action<int> onClear)
    {
        _slotIndex = slotIndex;
        _onSelect = onSelect;
        _onClear = onClear;

        if (_button != null)
        {
            _button.onClick.RemoveListener(HandleSelect);
            _button.onClick.AddListener(HandleSelect);
        }

        if (_clearButton != null)
        {
            _clearButton.onClick.RemoveListener(HandleClear);
            _clearButton.onClick.AddListener(HandleClear);
        }

        AnimalDefinition definition = null;
        bool hasAnimal = placed != null &&
                         manager != null &&
                         manager.TryGetAnimalDefinition(placed.animalId, out definition);

        if (hasAnimal)
        {
            if (_icon != null)
            {
                _icon.sprite = definition.icon;
                _icon.enabled = definition.icon != null;
            }

            if (_titleText != null)
                _titleText.text = string.IsNullOrWhiteSpace(definition.title) ? definition.animalId : definition.title;

            if (_stageText != null)
                _stageText.text = $"S{Mathf.Max(1, placed.stage)}";
        }
        else
        {
            if (_icon != null)
            {
                _icon.sprite = null;
                _icon.enabled = false;
            }

            if (_titleText != null)
                _titleText.text = string.Empty;

            if (_stageText != null)
                _stageText.text = string.Empty;
        }

        if (_emptyText != null)
        {
            _emptyText.gameObject.SetActive(!hasAnimal);
            _emptyText.text = "Select";
        }

        if (_clearButton != null)
            _clearButton.gameObject.SetActive(hasAnimal);
    }

    private void OnDisable()
    {
        if (_button != null)
            _button.onClick.RemoveListener(HandleSelect);

        if (_clearButton != null)
            _clearButton.onClick.RemoveListener(HandleClear);
    }

    private void HandleSelect()
    {
        _onSelect?.Invoke(_slotIndex);
    }

    private void HandleClear()
    {
        _onClear?.Invoke(_slotIndex);
    }
}
