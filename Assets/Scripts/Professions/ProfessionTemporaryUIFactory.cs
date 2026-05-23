using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public static class ProfessionTemporaryUIFactory
{
    private const float ListButtonHeight = 88f;

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
        public RectTransform professionListRoot;
        public Button professionListButtonPrefab;
        public Button applyButton;
        public Button nextButton;
        public Button prevButton;
        public Button unlockRandomButton;
        public Button directBuyButton;
        public Button closeButton;
    }

    public static PanelRefs EnsurePanel(GameObject panel, UnityAction closeAction)
    {
        if (panel == null)
            return null;

        RectTransform panelRect = panel.GetComponent<RectTransform>();
        if (panelRect != null)
            Stretch(panelRect);

        Image background = panel.GetComponent<Image>();
        if (background == null)
            background = panel.AddComponent<Image>();
        background.color = new Color(0f, 0f, 0f, 0.55f);
        background.raycastTarget = true;

        GameObject body = EnsurePanelObject(
            panel.transform,
            "TemporaryProfessionBody",
            new Vector2(0.035f, 0.045f),
            new Vector2(0.965f, 0.955f),
            new Color(1f, 1f, 1f, 0.83f));
        AddOutline(body, new Color(1f, 1f, 1f, 0.18f), new Vector2(1.5f, -1.5f));

        GameObject listPanel = EnsurePanelObject(
            body.transform,
            "ProfessionListPanel",
            new Vector2(0.015f, 0.035f),
            new Vector2(0.32f, 0.965f),
            new Color(0.094f, 0.106f, 0.129f, 0.96f));
        AddOutline(listPanel, new Color(1f, 1f, 1f, 0.18f), new Vector2(1f, -1f));

        Text classHeader = EnsureText(
            listPanel.transform,
            "ClassHeader",
            new Vector2(0.02f, 0.885f),
            new Vector2(0.98f, 0.985f),
            TextAnchor.MiddleCenter,
            34,
            Color.white);
        classHeader.text = ProfessionLocalization.ClassHeader;

        ScrollRect listScroll = EnsureScroll(
            listPanel.transform,
            "ProfessionListScroll",
            new Vector2(0.018f, 0.018f),
            new Vector2(0.982f, 0.87f),
            out RectTransform listContent);
        ConfigureListContent(listContent);
        Button listTemplate = EnsureListButtonTemplate(listContent);

        GameObject detailPanel = EnsurePanelObject(
            body.transform,
            "ProfessionDetailPanel",
            new Vector2(0.35f, 0.19f),
            new Vector2(0.985f, 0.965f),
            new Color(0.094f, 0.106f, 0.129f, 0.96f));
        AddOutline(detailPanel, new Color(1f, 1f, 1f, 0.16f), new Vector2(1f, -1f));

        Text title = EnsureText(
            detailPanel.transform,
            "Title",
            new Vector2(0.06f, 0.82f),
            new Vector2(0.94f, 0.97f),
            TextAnchor.MiddleCenter,
            46,
            Color.white);

        Text status = EnsureText(
            detailPanel.transform,
            "Status",
            new Vector2(0.07f, 0.75f),
            new Vector2(0.32f, 0.83f),
            TextAnchor.MiddleLeft,
            20,
            new Color(1f, 0.86f, 0.36f));

        Image icon = EnsureIcon(detailPanel.transform);
        GameObject lockObject = EnsureLock(detailPanel.transform);

        Text description = EnsureText(
            detailPanel.transform,
            "Description",
            new Vector2(0.08f, 0.58f),
            new Vector2(0.92f, 0.70f),
            TextAnchor.MiddleCenter,
            30,
            Color.white);

        Text abilitiesHeader = EnsureText(
            detailPanel.transform,
            "AbilitiesHeader",
            new Vector2(0.08f, 0.51f),
            new Vector2(0.53f, 0.58f),
            TextAnchor.MiddleCenter,
            24,
            new Color(0.86f, 0.95f, 1f));
        abilitiesHeader.text = ProfessionLocalization.AbilitiesHeader;

        Text perks = EnsureText(
            detailPanel.transform,
            "Perks",
            new Vector2(0.08f, 0.06f),
            new Vector2(0.53f, 0.50f),
            TextAnchor.UpperLeft,
            22,
            new Color(0.78f, 0.87f, 0.96f));

        GameObject starterPanel = EnsurePanelObject(
            detailPanel.transform,
            "StarterItemsPanel",
            new Vector2(0.56f, 0.06f),
            new Vector2(0.92f, 0.58f),
            new Color(0.161f, 0.169f, 0.2f, 0.9f));
        AddOutline(starterPanel, new Color(1f, 1f, 1f, 0.12f), new Vector2(1f, -1f));

        Text starterHeader = EnsureText(
            starterPanel.transform,
            "StarterItemsHeader",
            new Vector2(0.04f, 0.80f),
            new Vector2(0.96f, 0.96f),
            TextAnchor.MiddleCenter,
            22,
            Color.white);
        starterHeader.text = ProfessionLocalization.StarterItemsHeader;

        Text starterItems = EnsureText(
            starterPanel.transform,
            "StarterItems",
            new Vector2(0.05f, 0.08f),
            new Vector2(0.95f, 0.76f),
            TextAnchor.UpperLeft,
            24,
            new Color(0.86f, 0.94f, 1f));

        GameObject actionBar = EnsurePanelObject(
            body.transform,
            "ActionBar",
            new Vector2(0.35f, 0.035f),
            new Vector2(0.985f, 0.155f),
            new Color(0.094f, 0.106f, 0.129f, 0.92f));

        Text message = EnsureText(
            actionBar.transform,
            "Message",
            new Vector2(0.02f, 0.58f),
            new Vector2(0.98f, 0.98f),
            TextAnchor.MiddleCenter,
            16,
            new Color(1f, 0.86f, 0.36f));

        Button applyButton = EnsureButton(
            actionBar.transform,
            "ApplyButton",
            ProfessionLocalization.ApplyButton,
            new Vector2(0.08f, 0.08f),
            new Vector2(0.31f, 0.52f),
            null);

        Button unlockRandomButton = EnsureButton(
            actionBar.transform,
            "UnlockRandomButton",
            ProfessionLocalization.RandomButton,
            new Vector2(0.34f, 0.08f),
            new Vector2(0.57f, 0.52f),
            null);

        Button directBuyButton = EnsureButton(
            actionBar.transform,
            "DirectBuyButton",
            ProfessionLocalization.BuyButton,
            new Vector2(0.60f, 0.08f),
            new Vector2(0.78f, 0.52f),
            null);

        Text unlockPrice = EnsureText(
            actionBar.transform,
            "UnlockPrice",
            new Vector2(0.80f, 0.08f),
            new Vector2(0.98f, 0.52f),
            TextAnchor.MiddleCenter,
            18,
            Color.white);

        Button prevButton = EnsureButton(actionBar.transform, "PrevButton", "<", Vector2.zero, Vector2.zero, null);
        Button nextButton = EnsureButton(actionBar.transform, "NextButton", ">", Vector2.zero, Vector2.zero, null);
        prevButton.gameObject.SetActive(false);
        nextButton.gameObject.SetActive(false);

        Button closeButton = EnsureButton(
            body.transform,
            "CloseButton",
            "\u00D7",
            new Vector2(0.94f, 0.895f),
            new Vector2(0.99f, 0.99f),
            closeAction);
        StyleCloseButton(closeButton);

        return new PanelRefs
        {
            titleText = title,
            descriptionText = description,
            starterItemsText = starterItems,
            perksText = perks,
            statusText = status,
            messageText = message,
            unlockPriceText = unlockPrice,
            icon = icon,
            lockObject = lockObject,
            professionListRoot = listContent,
            professionListButtonPrefab = listTemplate,
            applyButton = applyButton,
            nextButton = nextButton,
            prevButton = prevButton,
            unlockRandomButton = unlockRandomButton,
            directBuyButton = directBuyButton,
            closeButton = closeButton
        };
    }

    public static Button EnsureFloatingOpenButton(Canvas canvas, UnityAction openAction)
    {
        if (canvas == null)
            return null;

        Transform existing = canvas.transform.Find("TemporaryProfessionOpenButton");
        if (existing != null && existing.TryGetComponent(out Button existingButton))
        {
            EnsureUiSound(existingButton);
            existingButton.onClick.RemoveListener(openAction);
            existingButton.onClick.AddListener(openAction);
            return existingButton;
        }

        Button button = EnsureButton(canvas.transform, "TemporaryProfessionOpenButton", ProfessionLocalization.OpenButton, new Vector2(0.02f, 0.08f), new Vector2(0.18f, 0.16f), openAction);
        return button;
    }

    private static Image EnsureIcon(Transform parent)
    {
        GameObject iconObject = EnsurePanelObject(
            parent,
            "Icon",
            new Vector2(0.43f, 0.70f),
            new Vector2(0.57f, 0.80f),
            new Color(0.161f, 0.169f, 0.2f, 0.9f));

        Image image = iconObject.GetComponent<Image>();
        image.preserveAspect = true;
        return image;
    }

    private static GameObject EnsureLock(Transform parent)
    {
        GameObject root = EnsurePanelObject(
            parent,
            "LockObject",
            new Vector2(0.43f, 0.70f),
            new Vector2(0.57f, 0.80f),
            new Color(0.02f, 0.02f, 0.02f, 0.78f));

        Text label = EnsureText(root.transform, "Label", Vector2.zero, Vector2.one, TextAnchor.MiddleCenter, 20, Color.white);
        label.text = ProfessionLocalization.LockLabel;
        return root;
    }

    private static ScrollRect EnsureScroll(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, out RectTransform content)
    {
        GameObject root = EnsureRect(name, parent);
        RectTransform rect = root.GetComponent<RectTransform>();
        SetAnchors(rect, anchorMin, anchorMax);

        ScrollRect scrollRect = root.GetComponent<ScrollRect>();
        if (scrollRect == null)
            scrollRect = root.AddComponent<ScrollRect>();

        GameObject viewportObject = EnsurePanelObject(
            root.transform,
            "Viewport",
            Vector2.zero,
            Vector2.one,
            new Color(1f, 1f, 1f, 0.001f));
        Image viewportImage = viewportObject.GetComponent<Image>();
        viewportImage.raycastTarget = true;

        Mask mask = viewportObject.GetComponent<Mask>();
        if (mask == null)
            mask = viewportObject.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        GameObject contentObject = EnsureRect("Content", viewportObject.transform);
        content = contentObject.GetComponent<RectTransform>();
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.offsetMin = Vector2.zero;
        content.offsetMax = Vector2.zero;

        scrollRect.viewport = viewportObject.GetComponent<RectTransform>();
        scrollRect.content = content;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Elastic;
        scrollRect.scrollSensitivity = 24f;
        return scrollRect;
    }

    private static void ConfigureListContent(RectTransform content)
    {
        if (content == null)
            return;

        VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
        if (layout == null)
            layout = content.gameObject.AddComponent<VerticalLayoutGroup>();

        layout.childAlignment = TextAnchor.UpperCenter;
        layout.spacing = 4f;
        layout.padding = new RectOffset(0, 0, 0, 0);
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
        if (fitter == null)
            fitter = content.gameObject.AddComponent<ContentSizeFitter>();

        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    private static Button EnsureListButtonTemplate(RectTransform parent)
    {
        Button button = EnsureButton(parent, "ProfessionListButtonTemplate", string.Empty, new Vector2(0f, 1f), new Vector2(1f, 1f), null);
        RectTransform rect = button.GetComponent<RectTransform>();
        rect.pivot = new Vector2(0.5f, 1f);
        rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, ListButtonHeight);

        LayoutElement layoutElement = button.GetComponent<LayoutElement>();
        if (layoutElement == null)
            layoutElement = button.gameObject.AddComponent<LayoutElement>();

        layoutElement.minHeight = ListButtonHeight;
        layoutElement.preferredHeight = ListButtonHeight;
        layoutElement.flexibleWidth = 1f;
        layoutElement.flexibleHeight = 0f;

        Image image = button.GetComponent<Image>();
        if (image != null)
            image.color = new Color(0.094f, 0.106f, 0.129f, 0.96f);

        Text label = button.GetComponentInChildren<Text>(true);
        if (label != null)
        {
            label.fontSize = 34;
            label.resizeTextMinSize = 14;
            label.resizeTextMaxSize = 34;
            label.alignment = TextAnchor.MiddleCenter;
        }

        AddOutline(button.gameObject, new Color(1f, 1f, 1f, 0.18f), new Vector2(1f, -1f));
        if (button.GetComponent<EggSelectionSlotHover>() == null)
            button.gameObject.AddComponent<EggSelectionSlotHover>();

        button.gameObject.SetActive(false);
        return button;
    }

    private static Button EnsureButton(Transform parent, string name, string text, Vector2 anchorMin, Vector2 anchorMax, UnityAction action)
    {
        GameObject root = EnsureRect(name, parent);
        RectTransform rect = root.GetComponent<RectTransform>();
        SetAnchors(rect, anchorMin, anchorMax);

        Image image = root.GetComponent<Image>();
        if (image == null)
            image = root.AddComponent<Image>();
        image.color = new Color(0.161f, 0.169f, 0.2f, 0.96f);
        image.raycastTarget = true;

        Button button = root.GetComponent<Button>();
        if (button == null)
            button = root.AddComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.ColorTint;
        EnsureUiSound(button);

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
        colors.pressedColor = new Color(0.86f, 0.92f, 1f, 1f);
        colors.disabledColor = new Color(0.48f, 0.48f, 0.48f, 0.7f);
        button.colors = colors;

        Text label = EnsureText(root.transform, "Label", Vector2.zero, Vector2.one, TextAnchor.MiddleCenter, 18, Color.white);
        label.text = text;

        if (action != null)
        {
            button.onClick.RemoveListener(action);
            button.onClick.AddListener(action);
        }

        return button;
    }

    private static void EnsureUiSound(Button button)
    {
        if (button == null)
            return;

        UISound sound = button.GetComponent<UISound>();
        if (sound == null)
            sound = button.gameObject.AddComponent<UISound>();

        sound.Rebind();
    }

    private static Text EnsureText(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, TextAnchor alignment, int fontSize, Color color)
    {
        GameObject root = EnsureRect(name, parent);
        RectTransform rect = root.GetComponent<RectTransform>();
        SetAnchors(rect, anchorMin, anchorMax);

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
        text.raycastTarget = false;
        return text;
    }

    private static GameObject EnsurePanelObject(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Color color)
    {
        GameObject root = EnsureRect(name, parent);
        RectTransform rect = root.GetComponent<RectTransform>();
        SetAnchors(rect, anchorMin, anchorMax);

        Image image = root.GetComponent<Image>();
        if (image == null)
            image = root.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = true;
        return root;
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

    private static void Stretch(RectTransform rect)
    {
        SetAnchors(rect, Vector2.zero, Vector2.one);
    }

    private static void SetAnchors(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
    {
        if (rect == null)
            return;

        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void AddOutline(GameObject root, Color color, Vector2 distance)
    {
        if (root == null)
            return;

        UnityEngine.UI.Outline outline = root.GetComponent<UnityEngine.UI.Outline>();
        if (outline == null)
            outline = root.AddComponent<UnityEngine.UI.Outline>();

        outline.effectColor = color;
        outline.effectDistance = distance;
    }

    private static void StyleCloseButton(Button button)
    {
        if (button == null)
            return;

        Image image = button.GetComponent<Image>();
        if (image != null)
            image.color = new Color(0f, 0f, 0f, 0f);

        Text label = button.GetComponentInChildren<Text>(true);
        if (label != null)
        {
            label.fontSize = 42;
            label.resizeTextMaxSize = 42;
            label.color = Color.white;
        }
    }

    private static Font GetDefaultFont()
    {
        Font font = Resources.Load<Font>("Fonts/RussoOne-Regular");
        if (font != null)
            return font;

        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font != null)
            return font;

        return Resources.GetBuiltinResource<Font>("Arial.ttf");
    }
}
