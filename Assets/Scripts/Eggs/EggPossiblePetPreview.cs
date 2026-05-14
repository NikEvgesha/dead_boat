using UnityEngine;
using UnityEngine.UI;

public class EggPossiblePetPreview : MonoBehaviour
{
    [SerializeField] private Image _iconImage;
    [SerializeField] private Text _labelText;

    public void SetAnimal(AnimalDefinition animal)
    {
        Sprite icon = animal != null ? animal.icon : null;

        if (_iconImage != null)
        {
            _iconImage.sprite = icon;
            _iconImage.enabled = icon != null;
        }

        if (_labelText != null)
            _labelText.text = icon != null ? string.Empty : GetFallbackLabel(animal);
    }

    private static string GetFallbackLabel(AnimalDefinition animal)
    {
        string title = animal != null && !string.IsNullOrWhiteSpace(animal.title)
            ? animal.title
            : animal != null ? animal.animalId : "?";

        if (string.IsNullOrWhiteSpace(title))
            return "?";

        return title.Length <= 2 ? title : title.Substring(0, 1).ToUpperInvariant();
    }
}
