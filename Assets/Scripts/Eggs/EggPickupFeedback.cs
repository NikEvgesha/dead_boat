using System;
using UnityEngine;
using UnityEngine.UI;

// One local, non-blocking notification. It never writes meta progress itself.
public sealed class EggPickupFeedback : MonoBehaviour
{
    private static EggPickupFeedback instance;
    private RectTransform panel;
    private CanvasGroup group;
    private Text title, count, hint;
    private Image icon;
    private EggHatchingCatalog catalog;
    private int gained, total;
    private bool firstHint;
    private float remaining, duration, age;
    public int DisplayedTotal => total;
    public int DisplayedGain => gained;
    public bool ShowsFirstHint => firstHint && remaining > 0;
    public bool IsVisible => remaining > 0 && group != null && group.alpha > 0;

    public static void Show(EggPickupReceipt receipt)
    {
        if (!Application.isPlaying || receipt.Amount <= 0) return;
        try
        {
            if (instance == null)
            {
                var root = new GameObject("Egg pickup feedback");
                DontDestroyOnLoad(root);
                instance = root.AddComponent<EggPickupFeedback>();
            }
            instance.Present(receipt);
        }
        catch (Exception e) { Debug.LogWarning("[Egg pickup UI] Feedback failed: " + e.GetType().Name); }
    }
    private void Awake()
    {
        var canvas = gameObject.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 950;
        var scaler = gameObject.AddComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720); scaler.matchWidthOrHeight = .5f;
        group = gameObject.AddComponent<CanvasGroup>(); group.interactable = false; group.blocksRaycasts = false; group.alpha = 0;
        var box = new GameObject("Pickup", typeof(RectTransform), typeof(Image)); box.transform.SetParent(transform, false);
        panel = box.GetComponent<RectTransform>(); panel.anchorMin = panel.anchorMax = new Vector2(.5f, 0);
        panel.anchoredPosition = new Vector2(0, 155);
        var image = box.GetComponent<Image>(); image.color = new Color(.055f, .12f, .16f, .96f); image.raycastTarget = false;
        var iconObject = new GameObject("Egg icon", typeof(RectTransform), typeof(Image)); iconObject.transform.SetParent(panel, false);
        icon = iconObject.GetComponent<Image>(); icon.preserveAspect = true; icon.raycastTarget = false;
        var iconRect = iconObject.GetComponent<RectTransform>(); iconRect.sizeDelta = new Vector2(60, 60); iconRect.anchoredPosition = new Vector2(-215, 35);
        title = Label("Gain", new Vector2(30, 48), new Vector2(410, 42), 28); title.color = new Color(1, .84f, .3f);
        count = Label("Total", new Vector2(30, 13), new Vector2(410, 30), 20);
        hint = Label("First pickup hint", new Vector2(0, -43), new Vector2(490, 64), 18);
        catalog = Resources.Load<EggHatchingCatalog>("Eggs/EggHatchingCatalog");
        panel.gameObject.SetActive(false);
    }
    private Text Label(string label, Vector2 position, Vector2 size, int fontSize)
    {
        var go = new GameObject(label, typeof(RectTransform), typeof(Text)); go.transform.SetParent(panel, false);
        var rect = go.GetComponent<RectTransform>(); rect.anchoredPosition = position; rect.sizeDelta = size;
        var text = go.GetComponent<Text>(); text.font = Resources.Load<Font>("Fonts/RussoOne-Regular") ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize; text.color = Color.white; text.alignment = TextAnchor.MiddleCenter;
        text.supportRichText = false; text.raycastTarget = false;
        return text;
    }
    private void Present(EggPickupReceipt receipt)
    {
        bool continuing = remaining > 0;
        gained = continuing ? (int)Math.Min(int.MaxValue, (long)gained + receipt.Amount) : receipt.Amount;
        firstHint = receipt.FirstDiscovery || (continuing && firstHint);
        total = receipt.Total; duration = firstHint ? 7 : 3.5f; remaining = duration; age = 0;
        panel.sizeDelta = new Vector2(540, firstHint ? 170 : 100);
        title.rectTransform.anchoredPosition = new Vector2(30, firstHint ? 48 : 18);
        count.rectTransform.anchoredPosition = new Vector2(30, firstHint ? 13 : -18);
        icon.rectTransform.anchoredPosition = new Vector2(-215, firstHint ? 35 : 0);
        icon.sprite = catalog != null && catalog.TryGet(receipt.EggId, out var definition) ? definition.icon : null;
        icon.enabled = icon.sprite != null;
        hint.gameObject.SetActive(firstHint); panel.gameObject.SetActive(true);
        RefreshText();
    }
    private void RefreshText()
    {
        bool en = LocalizationManager.Instance != null && (LocalizationManager.Instance.CurrentLanguage ?? "").StartsWith("en", StringComparison.OrdinalIgnoreCase);
        title.text = en ? "Eggs: +" + gained : "Яйца: +" + gained;
        count.text = en ? "Eggs in storage: " + total : "Яиц в запасе: " + total;
        hint.text = en ? "Eggs are stored separately from your bag.\nReturn to the lobby to hatch them."
            : "Яйца хранятся отдельно от сумки.\nВылупление — после возвращения в лобби.";
    }
    private void Update()
    {
        if (remaining <= 0) return;
        if (DeadBoat.Online.SharedAdProtection.IsShowing) { group.alpha = 0; return; }
        float delta = Time.unscaledDeltaTime; remaining -= delta; age += delta;
        group.alpha = Mathf.Min(Mathf.Clamp01(age / .15f), Mathf.Clamp01(remaining / .4f));
        panel.localScale = Vector3.one * (1 + .08f * Mathf.Clamp01(1 - age / .3f));
        if (remaining <= 0) { group.alpha = 0; panel.gameObject.SetActive(false); }
    }
    private void OnDestroy() { if (instance == this) instance = null; }
}
