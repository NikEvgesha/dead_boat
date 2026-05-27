using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class AnimalLoadoutPointInfoCard : MonoBehaviour
{
    [SerializeField] private GameObject _root;
    [SerializeField] private Image _iconImage;
    [SerializeField] private Text _titleText;
    [SerializeField] private Text _stageText;
    [SerializeField] private Text _countText;
    [SerializeField] private Text _abilitiesHeaderText;
    [SerializeField] private Text _detailText;
    [SerializeField] private Text _emptyText;

    private void Awake()
    {
        ResolveReferences();
    }

    public void SetAnimal(AnimalDefinition definition, int stage, string detail)
    {
        ResolveReferences();
        SetRootActive(true);

        if (_iconImage != null)
        {
            _iconImage.sprite = definition != null ? definition.icon : null;
            _iconImage.enabled = _iconImage.sprite != null;
        }

        if (_titleText != null)
            _titleText.text = EggFeatureLocalization.AnimalTitle(definition);

        if (_stageText != null)
            _stageText.text = EggFeatureLocalization.StageLong(stage);

        if (_countText != null)
            _countText.text = "x1";

        if (_abilitiesHeaderText != null)
            _abilitiesHeaderText.text = EggFeatureLocalization.Text("UI/AnimalInventory/Buffs", "Abilities", "Abilities");

        if (_detailText != null)
            _detailText.text = FormatBuffText(detail);

        if (_emptyText != null)
            _emptyText.gameObject.SetActive(false);
    }

    public void SetEmpty()
    {
        ResolveReferences();
        SetRootActive(true);

        if (_iconImage != null)
        {
            _iconImage.sprite = null;
            _iconImage.enabled = false;
        }

        if (_titleText != null)
            _titleText.text = EggFeatureLocalization.Text("UI/AnimalLoadout/EmptySlot", "\u041f\u0443\u0441\u0442\u043e\u0439 \u0441\u043b\u043e\u0442", "Empty slot");

        if (_stageText != null)
            _stageText.text = string.Empty;

        if (_countText != null)
            _countText.text = string.Empty;

        if (_abilitiesHeaderText != null)
            _abilitiesHeaderText.text = EggFeatureLocalization.Text("UI/AnimalInventory/Buffs", "Abilities", "Abilities");

        if (_detailText != null)
            _detailText.text = EggFeatureLocalization.Text("UI/AnimalLoadout/HoldToSelect", "\u0423\u0434\u0435\u0440\u0436\u0438\u0432\u0430\u0439 E, \u0447\u0442\u043e\u0431\u044b \u0432\u044b\u0431\u0440\u0430\u0442\u044c", "Hold E to select");

        if (_emptyText != null)
        {
            _emptyText.gameObject.SetActive(true);
            _emptyText.text = "+";
        }
    }

    private void ResolveReferences()
    {
        if (_root == null)
            _root = gameObject;

        if (_iconImage == null)
            _iconImage = FindChildComponent<Image>("Icon");

        if (_titleText == null)
            _titleText = FindChildComponent<Text>("Title");

        if (_stageText == null)
            _stageText = FindChildComponent<Text>("Stage");

        if (_countText == null)
            _countText = FindChildComponent<Text>("Count");

        if (_abilitiesHeaderText == null)
            _abilitiesHeaderText = FindChildComponent<Text>("AbilitiesHeader");

        if (_detailText == null)
            _detailText = FindChildComponent<Text>("Details");

        if (_emptyText == null)
            _emptyText = FindChildComponent<Text>("EmptyIcon");
    }

    private T FindChildComponent<T>(string childName) where T : Component
    {
        Transform child = transform.Find(childName);
        return child != null ? child.GetComponent<T>() : null;
    }

    private void SetRootActive(bool active)
    {
        if (_root != null && _root.activeSelf != active)
            _root.SetActive(active);
    }

    private static string FormatBuffText(string detail)
    {
        if (string.IsNullOrWhiteSpace(detail))
            return EggFeatureLocalization.Text("UI/AnimalBuff/None", "\u041d\u0435\u0442 \u0431\u043e\u043d\u0443\u0441\u043e\u0432", "No buffs");

        return detail.Replace(", ", "\n");
    }
}
