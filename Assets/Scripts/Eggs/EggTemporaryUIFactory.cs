using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public static class EggTemporaryUIFactory
{
    private const string TemplateRootName = "_TemporaryEggUITemplates";

    public static DynamicGridSpawner EnsureGrid(GameObject panel, string name)
    {
        if (panel == null)
            return null;

        Transform existing = panel.transform.Find(name);
        if (existing != null && existing.TryGetComponent(out DynamicGridSpawner existingGrid))
            return existingGrid;

        GameObject root = CreateRectObject(name, panel.transform);
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.08f, 0.18f);
        rect.anchorMax = new Vector2(0.92f, 0.78f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        VerticalLayoutGroup layout = root.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = root.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        return root.AddComponent<DynamicGridSpawner>();
    }

    public static GameObject EnsureEmptyState(GameObject panel, string text)
    {
        if (panel == null)
            return null;

        Transform existing = panel.transform.Find("TemporaryEmptyState");
        if (existing != null)
            return existing.gameObject;

        GameObject root = CreateRectObject("TemporaryEmptyState", panel.transform);
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.08f, 0.36f);
        rect.anchorMax = new Vector2(0.92f, 0.58f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Text label = root.AddComponent<Text>();
        label.font = GetDefaultFont();
        label.alignment = TextAnchor.MiddleCenter;
        label.resizeTextForBestFit = true;
        label.resizeTextMinSize = 14;
        label.resizeTextMaxSize = 26;
        label.color = Color.white;
        label.text = text;

        return root;
    }

    public static Text EnsureHeader(GameObject panel, string name, string prefix)
    {
        if (panel == null)
            return null;

        Transform existing = panel.transform.Find(name);
        if (existing != null && existing.TryGetComponent(out Text existingText))
            return existingText;

        GameObject root = CreateRectObject(name, panel.transform);
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.08f, 0.8f);
        rect.anchorMax = new Vector2(0.92f, 0.92f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Text label = root.AddComponent<Text>();
        label.font = GetDefaultFont();
        label.alignment = TextAnchor.MiddleCenter;
        label.resizeTextForBestFit = true;
        label.resizeTextMinSize = 14;
        label.resizeTextMaxSize = 28;
        label.color = Color.white;
        label.text = prefix;
        return label;
    }

    public static Button EnsureCloseButton(GameObject panel, UnityAction closeAction)
    {
        if (panel == null)
            return null;

        Transform existing = panel.transform.Find("TemporaryCloseButton");
        if (existing != null && existing.TryGetComponent(out Button existingButton))
        {
            existingButton.onClick.RemoveListener(closeAction);
            existingButton.onClick.AddListener(closeAction);
            return existingButton;
        }

        GameObject root = CreateRectObject("TemporaryCloseButton", panel.transform);
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.86f, 0.84f);
        rect.anchorMax = new Vector2(0.96f, 0.94f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image background = root.AddComponent<Image>();
        background.color = new Color(0.18f, 0.18f, 0.18f, 0.95f);

        Button button = root.AddComponent<Button>();
        button.targetGraphic = background;
        button.onClick.AddListener(closeAction);

        Text label = CreateSlotText("Label", root.transform, TextAnchor.MiddleCenter, 22, Color.white);
        RectTransform labelRect = label.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        label.text = "X";

        return button;
    }

    public static EggNestSelectionSlot EnsureEggSlotTemplate(MonoBehaviour owner)
    {
        GameObject slot = CreateSlotTemplate(owner, "TemporaryEggSelectionSlot");
        EggNestSelectionSlot component = slot.GetComponent<EggNestSelectionSlot>();
        if (component != null)
            return component;

        component = slot.AddComponent<EggNestSelectionSlot>();
        BindSlotCommon(slot, out Text title, out Text count, out Text detail, out Button button);
        component.BindTemporaryReferences(title, count, detail, button);
        return component;
    }

    public static AnimalPlacementSelectionSlot EnsureAnimalSlotTemplate(MonoBehaviour owner)
    {
        GameObject slot = CreateSlotTemplate(owner, "TemporaryAnimalSelectionSlot");
        AnimalPlacementSelectionSlot component = slot.GetComponent<AnimalPlacementSelectionSlot>();
        if (component != null)
            return component;

        component = slot.AddComponent<AnimalPlacementSelectionSlot>();
        BindSlotCommon(slot, out Text title, out Text count, out Text detail, out Button button);
        component.BindTemporaryReferences(title, count, detail, button);
        return component;
    }

    public static string FormatHeader(string prefix, string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return prefix;

        return $"{prefix}: {id}";
    }

    private static GameObject CreateSlotTemplate(MonoBehaviour owner, string name)
    {
        Transform templateRoot = EnsureTemplateRoot(owner);
        Transform existing = templateRoot.Find(name);
        if (existing != null)
            return existing.gameObject;

        GameObject slot = CreateRectObject(name, templateRoot);
        slot.SetActive(true);

        RectTransform rect = slot.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(360f, 78f);

        Image background = slot.AddComponent<Image>();
        background.color = new Color(0.12f, 0.12f, 0.12f, 0.92f);

        Button button = slot.AddComponent<Button>();
        button.targetGraphic = background;
        button.transition = Selectable.Transition.ColorTint;

        HorizontalLayoutGroup layout = slot.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(14, 14, 10, 10);
        layout.spacing = 12f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;

        return slot;
    }

    private static void BindSlotCommon(GameObject slot, out Text title, out Text count, out Text detail, out Button button)
    {
        title = CreateSlotText("Title", slot.transform, TextAnchor.MiddleLeft, 18, Color.white);
        count = CreateSlotText("Count", slot.transform, TextAnchor.MiddleCenter, 16, new Color(1f, 0.92f, 0.45f));
        detail = CreateSlotText("Detail", slot.transform, TextAnchor.MiddleRight, 15, new Color(0.8f, 0.9f, 1f));
        button = slot.GetComponent<Button>();
    }

    private static Text CreateSlotText(string name, Transform parent, TextAnchor alignment, int fontSize, Color color)
    {
        GameObject root = CreateRectObject(name, parent);
        Text text = root.AddComponent<Text>();
        text.font = GetDefaultFont();
        text.alignment = alignment;
        text.fontSize = fontSize;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 11;
        text.resizeTextMaxSize = fontSize;
        text.color = color;
        text.raycastTarget = false;

        LayoutElement layout = root.AddComponent<LayoutElement>();
        layout.minWidth = name == "Title" ? 150f : 82f;
        layout.preferredWidth = name == "Title" ? 180f : 96f;
        return text;
    }

    private static Transform EnsureTemplateRoot(MonoBehaviour owner)
    {
        Transform existing = owner.transform.Find(TemplateRootName);
        if (existing != null)
            return existing;

        GameObject root = new GameObject(TemplateRootName, typeof(RectTransform));
        root.transform.SetParent(owner.transform, false);
        root.SetActive(false);
        return root.transform;
    }

    private static GameObject CreateRectObject(string name, Transform parent)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        gameObject.layer = parent != null ? parent.gameObject.layer : 0;
        gameObject.transform.SetParent(parent, false);
        return gameObject;
    }

    private static Font GetDefaultFont()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font != null)
            return font;

        return Resources.GetBuiltinResource<Font>("Arial.ttf");
    }
}
