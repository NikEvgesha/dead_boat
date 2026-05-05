using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public static class ProfessionTemporaryUIFactory
{
    public sealed class PanelRefs
    {
        public Text titleText;
        public Text descriptionText;
        public Text starterItemsText;
        public Text perksText;
        public Text statusText;
        public Text messageText;
        public Text unlockPriceText;
        public Image icon;
        public GameObject lockObject;
        public Button applyButton;
        public Button nextButton;
        public Button prevButton;
        public Button unlockRandomButton;
        public Button closeButton;
    }

    public static PanelRefs EnsurePanel(GameObject panel, UnityAction closeAction)
    {
        if (panel == null)
            return null;

        Image background = panel.GetComponent<Image>();
        if (background == null)
            background = panel.AddComponent<Image>();
        background.color = new Color(0.05f, 0.06f, 0.07f, 0.92f);

        RectTransform panelRect = panel.GetComponent<RectTransform>();
        if (panelRect != null)
        {
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
        }

        GameObject body = EnsureRect("TemporaryProfessionBody", panel.transform);
        RectTransform bodyRect = body.GetComponent<RectTransform>();
        bodyRect.anchorMin = new Vector2(0.14f, 0.1f);
        bodyRect.anchorMax = new Vector2(0.86f, 0.9f);
        bodyRect.offsetMin = Vector2.zero;
        bodyRect.offsetMax = Vector2.zero;

        Image bodyBackground = body.GetComponent<Image>();
        if (bodyBackground == null)
            bodyBackground = body.AddComponent<Image>();
        bodyBackground.color = new Color(0.11f, 0.12f, 0.13f, 0.96f);

        PanelRefs refs = new PanelRefs
        {
            titleText = EnsureText(body.transform, "Title", new Vector2(0.2f, 0.82f), new Vector2(0.8f, 0.95f), TextAnchor.MiddleCenter, 30, Color.white),
            statusText = EnsureText(body.transform, "Status", new Vector2(0.08f, 0.72f), new Vector2(0.32f, 0.8f), TextAnchor.MiddleLeft, 18, new Color(1f, 0.88f, 0.42f)),
            descriptionText = EnsureText(body.transform, "Description", new Vector2(0.08f, 0.52f), new Vector2(0.92f, 0.72f), TextAnchor.UpperLeft, 18, Color.white),
            starterItemsText = EnsureText(body.transform, "StarterItems", new Vector2(0.08f, 0.3f), new Vector2(0.46f, 0.5f), TextAnchor.UpperLeft, 16, new Color(0.82f, 0.94f, 1f)),
            perksText = EnsureText(body.transform, "Perks", new Vector2(0.54f, 0.3f), new Vector2(0.92f, 0.5f), TextAnchor.UpperLeft, 16, new Color(0.78f, 1f, 0.78f)),
            messageText = EnsureText(body.transform, "Message", new Vector2(0.2f, 0.2f), new Vector2(0.8f, 0.28f), TextAnchor.MiddleCenter, 16, new Color(1f, 0.88f, 0.42f)),
            unlockPriceText = EnsureText(body.transform, "UnlockPrice", new Vector2(0.58f, 0.08f), new Vector2(0.72f, 0.17f), TextAnchor.MiddleCenter, 18, Color.white),
            icon = EnsureIcon(body.transform),
            lockObject = EnsureLock(body.transform),
            prevButton = EnsureButton(body.transform, "PrevButton", "<", new Vector2(0.08f, 0.83f), new Vector2(0.16f, 0.93f), null),
            nextButton = EnsureButton(body.transform, "NextButton", ">", new Vector2(0.84f, 0.83f), new Vector2(0.92f, 0.93f), null),
            applyButton = EnsureButton(body.transform, "ApplyButton", "Apply", new Vector2(0.28f, 0.08f), new Vector2(0.44f, 0.17f), null),
            unlockRandomButton = EnsureButton(body.transform, "UnlockRandomButton", "Unlock", new Vector2(0.44f, 0.08f), new Vector2(0.58f, 0.17f), null),
            closeButton = EnsureButton(body.transform, "CloseButton", "X", new Vector2(0.9f, 0.9f), new Vector2(0.98f, 0.98f), closeAction)
        };

        return refs;
    }

    public static Button EnsureFloatingOpenButton(Canvas canvas, UnityAction openAction)
    {
        if (canvas == null)
            return null;

        Transform existing = canvas.transform.Find("TemporaryProfessionOpenButton");
        if (existing != null && existing.TryGetComponent(out Button existingButton))
        {
            existingButton.onClick.RemoveListener(openAction);
            existingButton.onClick.AddListener(openAction);
            return existingButton;
        }

        Button button = EnsureButton(canvas.transform, "TemporaryProfessionOpenButton", "Profession", new Vector2(0.02f, 0.08f), new Vector2(0.18f, 0.16f), openAction);
        return button;
    }

    private static Image EnsureIcon(Transform parent)
    {
        GameObject iconObject = EnsureRect("Icon", parent);
        RectTransform rect = iconObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.42f, 0.72f);
        rect.anchorMax = new Vector2(0.58f, 0.86f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = iconObject.GetComponent<Image>();
        if (image == null)
            image = iconObject.AddComponent<Image>();
        image.color = new Color(0.2f, 0.22f, 0.24f, 1f);
        return image;
    }

    private static GameObject EnsureLock(Transform parent)
    {
        GameObject root = EnsureRect("LockObject", parent);
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.34f, 0.72f);
        rect.anchorMax = new Vector2(0.66f, 0.82f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = root.GetComponent<Image>();
        if (image == null)
            image = root.AddComponent<Image>();
        image.color = new Color(0.02f, 0.02f, 0.02f, 0.75f);

        Text label = EnsureText(root.transform, "Label", Vector2.zero, Vector2.one, TextAnchor.MiddleCenter, 20, Color.white);
        label.text = "LOCKED";
        return root;
    }

    private static Button EnsureButton(Transform parent, string name, string text, Vector2 anchorMin, Vector2 anchorMax, UnityAction action)
    {
        GameObject root = EnsureRect(name, parent);
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = root.GetComponent<Image>();
        if (image == null)
            image = root.AddComponent<Image>();
        image.color = new Color(0.22f, 0.24f, 0.26f, 1f);

        Button button = root.GetComponent<Button>();
        if (button == null)
            button = root.AddComponent<Button>();
        button.targetGraphic = image;

        Text label = EnsureText(root.transform, "Label", Vector2.zero, Vector2.one, TextAnchor.MiddleCenter, 18, Color.white);
        label.text = text;

        if (action != null)
        {
            button.onClick.RemoveListener(action);
            button.onClick.AddListener(action);
        }

        return button;
    }

    private static Text EnsureText(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, TextAnchor alignment, int fontSize, Color color)
    {
        GameObject root = EnsureRect(name, parent);
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Text text = root.GetComponent<Text>();
        if (text == null)
            text = root.AddComponent<Text>();

        text.font = GetDefaultFont();
        text.alignment = alignment;
        text.fontSize = fontSize;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 10;
        text.resizeTextMaxSize = fontSize;
        text.color = color;
        return text;
    }

    private static GameObject EnsureRect(string name, Transform parent)
    {
        Transform existing = parent.Find(name);
        if (existing != null)
            return existing.gameObject;

        GameObject root = new GameObject(name, typeof(RectTransform));
        root.layer = parent.gameObject.layer;
        root.transform.SetParent(parent, false);
        return root;
    }

    private static Font GetDefaultFont()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font != null)
            return font;

        return Resources.GetBuiltinResource<Font>("Arial.ttf");
    }
}
