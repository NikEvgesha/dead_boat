using UnityEngine;
using UnityEngine.UI;

/// <summary>Flat, reusable reward card. No runtime avatar, camera or render texture.</summary>
public sealed class ProfessionUnlockPresentation : MonoBehaviour
{
    private Image _portrait;
    private Text _title;
    private Text _continue;

    public static ProfessionUnlockPresentation Create(Transform parent)
    {
        var root = Rect("ProfessionUnlockReward", parent, Vector2.zero, Vector2.one);
        root.AddComponent<Image>().color = new Color(0, 0, 0, .88f);
        var view = root.AddComponent<ProfessionUnlockPresentation>();
        var card = Rect("Card", root.transform, new Vector2(.2f, .04f), new Vector2(.8f, .96f));
        card.AddComponent<Image>().color = new Color(.08f, .13f, .18f, 1);
        view._title = Label("Title", card.transform, new Vector2(.04f, .81f), new Vector2(.96f, .97f), 38);
        var image = Rect("Character", card.transform, new Vector2(.12f, .19f), new Vector2(.88f, .80f));
        view._portrait = image.AddComponent<Image>();
        view._portrait.preserveAspect = true;
        view._portrait.raycastTarget = false;
        var button = Rect("Continue", card.transform, new Vector2(.12f, .04f), new Vector2(.88f, .16f));
        var background = button.AddComponent<Image>();
        background.color = new Color(.15f, .72f, .20f, 1);
        var close = button.AddComponent<Button>();
        close.targetGraphic = background;
        close.onClick.AddListener(view.Hide);
        view._continue = Label("Label", button.transform, Vector2.zero, Vector2.one, 30);
        view.Hide();
        return view;
    }

    public void Show(ProfessionDefinition definition)
    {
        if (definition == null) return;
        _portrait.sprite = definition.icon;
        _portrait.enabled = definition.icon != null;
        _title.text = ProfessionLocalization.FormatUnlockedProfession(ProfessionLocalization.DefinitionTitle(definition));
        _continue.text = ProfessionLocalization.ContinueButton;
        transform.SetAsLastSibling();
        gameObject.SetActive(true);
    }

    public void Hide() => gameObject.SetActive(false);

    private static GameObject Rect(string name, Transform parent, Vector2 min, Vector2 max)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rect = (RectTransform)go.transform;
        rect.anchorMin = min; rect.anchorMax = max;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        return go;
    }

    private static Text Label(string name, Transform parent, Vector2 min, Vector2 max, int size)
    {
        var text = Rect(name, parent, min, max).AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = size; text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 12; text.resizeTextMaxSize = size;
        text.alignment = TextAnchor.MiddleCenter; text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }
}
