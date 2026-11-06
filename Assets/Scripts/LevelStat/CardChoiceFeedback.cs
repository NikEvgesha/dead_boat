using MoreMountains.Feedbacks;
using UnityEngine;
using UnityEngine.UI;

// One reusable confirmation icon outside the closing card panel; it never grants rewards.
public sealed class CardChoiceFeedback : MonoBehaviour
{
    private RectTransform visual;
    private CanvasGroup alpha;
    private Image icon;
    private MMF_Player player;
    public void Initialize(Transform canvas)
    {
        var go = new GameObject("FEEL chosen card", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        visual = (RectTransform)go.transform; visual.SetParent(canvas, false);
        visual.anchorMin = visual.anchorMax = new Vector2(.86f, .72f); visual.sizeDelta = new Vector2(90, 90);
        alpha = go.GetComponent<CanvasGroup>(); alpha.blocksRaycasts = false; alpha.interactable = false; alpha.alpha = 0;
        var backdrop = go.GetComponent<Image>(); backdrop.raycastTarget = false; backdrop.color = new Color(.08f, .1f, .15f, .9f);
        var image = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        var rect = (RectTransform)image.transform; rect.SetParent(visual, false);
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = Vector2.one * 10; rect.offsetMax = Vector2.one * -10;
        icon = image.GetComponent<Image>(); icon.raycastTarget = false; icon.preserveAspect = true;
        player = CardFeelFeedback.NewPlayer(transform, "FEEL confirmation");
        CardFeelFeedback.AddScale(player, visual, Vector3.one, .48f, new Keyframe(0, .75f), new Keyframe(.3f, 1.08f), new Keyframe(1, 1));
        CardFeelFeedback.AddAlpha(player, alpha, .48f, 1, 0);
        CardFeelFeedback.InitializePlayer(player); enabled = false;
    }
    public void Show(Sprite sprite, RareType rarity)
    {
        if (player == null || DeadBoat.Online.SharedAdProtection.IsShowing) return;
        player.StopFeedbacks(); visual.localScale = Vector3.one; alpha.alpha = 0;
        icon.sprite = sprite; icon.color = Color.white;
        visual.GetComponent<Image>().color = Color.Lerp(new Color(.08f, .1f, .15f), CardFeelFeedback.RarityColor(rarity), .45f);
        visual.SetAsLastSibling(); enabled = true; CardFeelFeedback.Play(player);
    }
    private void Update()
    {
        if (DeadBoat.Online.SharedAdProtection.IsShowing || !player.IsPlaying) { Stop(); enabled = false; }
    }
    private void Stop() { if (player != null) player.StopFeedbacks(); if (alpha != null) alpha.alpha = 0; }
    private void OnDisable() => Stop();
    private void OnDestroy() { if (visual != null) Destroy(visual.gameObject); }
}
