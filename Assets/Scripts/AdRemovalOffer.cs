using System;
using UnityEngine;
using UnityEngine.UI;

// Local offer only. No price or entitlement is invented when the product is absent.
public sealed class AdRemovalOffer : MonoBehaviour
{
    private AdsManager ads;
    private GameObject root, chip, panel;
    private Text title, description, buyLabel;
    private Button buy;
    private bool shown, open, ownsCursor, previousCursor, busy;
    private PlayerInput input;
    private float nextRefresh;
    public bool Visible => chip && chip.activeSelf;
    public bool IsOpen => open;
    private string L(string ru, string en) => LocalizationManager.Instance != null && LocalizationManager.Instance.CurrentLanguage?.StartsWith("En", StringComparison.OrdinalIgnoreCase) == true ? en : ru;
    private void Awake()
    {
        ads = GetComponent<AdsManager>(); ads.ShownAdClosed += OnShown;
        AdRemovalPolicy.Changed += Refresh;
    }
    private void OnShown(bool rewarded) { shown = true; if (!root) Build(); Refresh(); }
    private void Update()
    {
        if (input != PlayerInput.Instance)
        {
            if (input) input.AOpenWindow -= OtherWindow;
            input = PlayerInput.Instance; if (input) input.AOpenWindow += OtherWindow;
        }
        if (open && Input.GetKeyDown(KeyCode.Escape)) SetOpen(false);
        if (!shown || Time.unscaledTime < nextRefresh) return;
        nextRefresh = Time.unscaledTime + 1; Refresh();
    }
    private void OtherWindow(MonoBehaviour other) { if (other != this) SetOpen(false); }
    private void Refresh()
    {
        if (!root) return;
        if (AdRemovalPolicy.Owned) SetOpen(false);
        chip.SetActive(shown && !open && !AdRemovalPolicy.Owned);
        title.text = L("Отключение рекламы", "Remove ads");
        description.text = AdRemovalPolicy.Configured
            ? L("Автоматические рекламные паузы отключаются навсегда. Видео за награды остаются по желанию.", "Remove automatic ad breaks permanently. Reward videos remain optional.")
            : L("Предложение скоро появится в магазине.", "This offer is coming to the shop soon.");
        PurchaseData data = null;
        if (AdRemovalPolicy.Configured && PurchasesManager.Instance.PurchasesAvailable()) data = PurchasesManager.Instance.GetPurchaseData(AdRemovalPolicy.ProductId);
        bool available = data != null && !string.IsNullOrWhiteSpace(data.Price) && !string.IsNullOrWhiteSpace(data.CurrencyImageURL);
        buy.gameObject.SetActive(AdRemovalPolicy.Configured);
        buy.interactable = available && !busy && !AdRemovalPolicy.Owned;
        buyLabel.text = busy ? L("Подождите…", "Please wait…") : available ? data.Price + " " + data.CurrencyImageURL : L("Покупка недоступна", "Purchase unavailable");
    }
    public void SetOpen(bool value)
    {
        if (!root || open == value || (value && AdRemovalPolicy.Owned)) return;
        open = value; panel.SetActive(value);
        if (value)
        {
            input?.AOpenWindow?.Invoke(this);
            if (ControlManager.Instance) {previousCursor=ControlManager.Instance.CursorActive;ControlManager.Instance.CursorActive=true;ownsCursor=true;}
        }
        else if (ownsCursor)
        { if (ControlManager.Instance) ControlManager.Instance.CursorActive=previousCursor; ownsCursor=false; }
        Refresh();
    }
    private void Purchase()
    {
        if (busy || !AdRemovalPolicy.Configured || !buy.interactable) return;
        busy=true; Refresh();
        PurchasesManager.Instance.BuyPurchase(AdRemovalPolicy.ProductId, success => {
            if (!this) return; busy=false; if (success) SetOpen(false); Refresh();
        });
    }
    private void Build()
    {
        root=new GameObject("Ad removal offer",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));root.transform.SetParent(transform,false);
        var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=65;
        var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=.5f;
        chip=DeadBoat.Online.LobbyOnlineModeUI.CreateButton(root.transform,"AD ×",new Vector2(0,.5f),new Vector2(48,-65),new Vector2(82,64),()=>SetOpen(true)).gameObject;
        chip.GetComponentInChildren<Text>().text="AD ×";
        panel=new GameObject("Offer",typeof(RectTransform),typeof(Image));panel.transform.SetParent(root.transform,false);
        panel.GetComponent<RectTransform>().sizeDelta=new Vector2(600,330);panel.GetComponent<Image>().color=new Color(.09f,.15f,.20f,.98f);
        title=DeadBoat.Online.LobbyOnlineModeUI.CreateText(panel.transform,"Title",new Vector2(0,115),new Vector2(500,55),28);
        description=DeadBoat.Online.LobbyOnlineModeUI.CreateText(panel.transform,"Terms",new Vector2(0,20),new Vector2(520,120),23);
        var close=DeadBoat.Online.LobbyOnlineModeUI.CreateButton(panel.transform,"×",new Vector2(.5f,.5f),new Vector2(267,125),new Vector2(44,44),()=>SetOpen(false));close.GetComponentInChildren<Text>().text="×";
        buy=DeadBoat.Online.LobbyOnlineModeUI.CreateButton(panel.transform,"Buy",new Vector2(.5f,.5f),new Vector2(0,-105),new Vector2(440,56),Purchase);buyLabel=buy.GetComponentInChildren<Text>();
        panel.SetActive(false);
    }
    private void OnDisable() { SetOpen(false); }
    private void OnDestroy()
    {
        if (ads) ads.ShownAdClosed-=OnShown;AdRemovalPolicy.Changed-=Refresh;
        if (input) input.AOpenWindow-=OtherWindow;
        if(ownsCursor && ControlManager.Instance) ControlManager.Instance.CursorActive=previousCursor;
    }
}
