using MoreMountains.Feedbacks;
using MoreMountains.Tools;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Only the inner visual is animated. The Button, layout and Animator's Point stay untouched.
public sealed class CardFeelFeedback : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler,
    IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    private Transform visual;
    private CanvasGroup opacity, frame;
    private Image[] edges;
    private Vector3 baseline;
    private float baselineAlpha;
    private MMF_Player entrance, pulse;
    private Selectable button;
    private bool hovered, selected, adWasShowing;
    private double lastPulse = double.NegativeInfinity;
    public static Color RarityColor(RareType rare)
    {
        switch (rare)
        {
            case RareType.Uncommon: return new Color(.3f, 1, .25f);
            case RareType.Rare: return new Color(.3f, .55f, 1);
            case RareType.Epic: return new Color(.8f, .4f, 1);
            case RareType.Legendary: return new Color(1, .7f, .15f);
            case RareType.Mythic: return new Color(1, .3f, .4f);
            default: return new Color(.85f, .9f, 1);
        }
    }
    public void Initialize(Transform target)
    {
        if (visual != null || target == null) return;
        baseline = target.localScale;
        button = GetComponent<Selectable>();
        opacity = target.GetComponent<CanvasGroup>();
        if (opacity == null) opacity = target.gameObject.AddComponent<CanvasGroup>();
        baselineAlpha = opacity.alpha;
        visual = target;
        var frameObject = new GameObject("FEEL rarity border", typeof(RectTransform), typeof(CanvasGroup));
        var rect = (RectTransform)frameObject.transform;
        rect.SetParent(target, false); rect.SetAsLastSibling();
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
        frame = frameObject.GetComponent<CanvasGroup>(); frame.blocksRaycasts = false; frame.interactable = false;
        edges = new Image[4];
        for (int i = 0; i < 4; i++)
        {
            var edge = new GameObject("Border " + i, typeof(RectTransform), typeof(Image));
            var r = (RectTransform)edge.transform; r.SetParent(rect, false);
            bool horizontal = i < 2;
            r.anchorMin = horizontal ? new Vector2(0, i) : new Vector2(i - 2, 0);
            r.anchorMax = horizontal ? new Vector2(1, i) : new Vector2(i - 2, 1);
            r.sizeDelta = horizontal ? new Vector2(-10, 3) : new Vector2(3, -10);
            r.anchoredPosition = Vector2.zero;
            edges[i] = edge.GetComponent<Image>(); edges[i].raycastTarget = false;
        }
        entrance = NewPlayer(transform, "FEEL entrance");
        AddScale(entrance, target, baseline, .2f, new Keyframe(0, .94f), new Keyframe(.65f, 1.018f), new Keyframe(1, 1));
        AddAlpha(entrance, opacity, .2f, .72f * baselineAlpha, baselineAlpha);
        pulse = NewPlayer(transform, "FEEL press");
        AddScale(pulse, target, baseline, .14f, new Keyframe(0, 1), new Keyframe(.3f, .965f), new Keyframe(1, 1));
        AddAlpha(pulse, frame, .2f, .95f, .35f);
        InitializePlayer(entrance); InitializePlayer(pulse);
        ResetVisual();
    }
    public void Present(RareType rarity)
    {
        if (visual == null) return;
        ResetVisual();
        foreach (var edge in edges) edge.color = RarityColor(rarity);
        if (!DeadBoat.Online.SharedAdProtection.IsShowing && isActiveAndEnabled) Play(entrance);
    }
    public void Press()
    {
        if (visual == null || !isActiveAndEnabled || (button != null && !button.IsInteractable()) ||
            DeadBoat.Online.SharedAdProtection.IsShowing || Time.unscaledTimeAsDouble - lastPulse < .06) return;
        lastPulse = Time.unscaledTimeAsDouble;
        ResetVisual(); Play(pulse);
    }
    public void ResetVisual()
    {
        if (visual == null || opacity == null || frame == null) return;
        entrance?.StopFeedbacks(); pulse?.StopFeedbacks();
        visual.localScale = baseline; opacity.alpha = baselineAlpha;
        frame.alpha = hovered || selected ? .65f : .35f;
    }
    private void Update()
    {
        bool ad = DeadBoat.Online.SharedAdProtection.IsShowing;
        if (ad && !adWasShowing) ResetVisual();
        adWasShowing = ad;
    }
    private void OnDisable() { hovered = selected = false; adWasShowing = false; ResetVisual(); }
    public void OnPointerDown(PointerEventData data) { if (data.button == PointerEventData.InputButton.Left) Press(); }
    public void OnPointerEnter(PointerEventData data) { hovered = true; Focus(); }
    public void OnPointerExit(PointerEventData data) { hovered = false; Focus(); }
    public void OnSelect(BaseEventData data) { selected = true; Focus(); }
    public void OnDeselect(BaseEventData data) { selected = false; Focus(); }
    private void Focus() { if (frame != null && !DeadBoat.Online.SharedAdProtection.IsShowing) frame.alpha = hovered || selected ? .65f : .35f; }

    internal static MMF_Player NewPlayer(Transform parent, string name)
    {
        var go = new GameObject(name); go.SetActive(false); go.transform.SetParent(parent, false);
        var p = go.AddComponent<MMF_Player>();
        p.InitializationMode = MMFeedbacks.InitializationModes.Script;
        p.AutoPlayOnEnable = p.AutoPlayOnStart = false;
        p.PlayerTimescaleMode = p.ForcedTimescaleMode = TimescaleModes.Unscaled;
        p.ForceTimescaleMode = true; p.StopFeedbacksOnDisable = true;
        p.RestoreInitialValuesOnDisable = false; p.CanPlayWhileAlreadyPlaying = false;
        go.SetActive(true); return p;
    }
    internal static void InitializePlayer(MMF_Player p)
    {
        var random = Random.state;
        try { p.Initialization(); } finally { Random.state = random; }
    }
    internal static void Play(MMF_Player p)
    {
        var random = Random.state;
        try { p.PlayFeedbacks(); } finally { Random.state = random; }
    }
    internal static void AddScale(MMF_Player p, Transform target, Vector3 original, float seconds, params Keyframe[] keys)
    {
        var f = new MMF_Scale(); p.AddFeedback(f);
        f.AnimateScaleTarget = target; f.Mode = MMF_Scale.Modes.Absolute; f.AnimateScaleDuration = seconds;
        f.RemapCurveZero = 0; f.RemapCurveOne = 1;
        f.AnimateScaleTweenX = Curve(keys, original.x);
        f.AnimateScaleTweenY = Curve(keys, original.y);
        f.AnimateScaleTweenZ = Curve(keys, original.z);
    }
    private static MMTweenType Curve(Keyframe[] keys, float scale)
    {
        var copy = new Keyframe[keys.Length];
        for (int i = 0; i < keys.Length; i++) copy[i] = new Keyframe(keys[i].time, keys[i].value * scale);
        return new MMTweenType(new AnimationCurve(copy));
    }
    internal static void AddAlpha(MMF_Player p, CanvasGroup target, float seconds, float from, float to)
    {
        var f = new MMF_CanvasGroup(); p.AddFeedback(f);
        f.TargetCanvasGroup = target; f.Duration = seconds; f.Mode = MMF_FeedbackBase.Modes.OverTime;
        f.RemapZero = 0; f.RemapOne = 1;
        f.AlphaCurve = new MMTweenType(AnimationCurve.EaseInOut(0, from, 1, to));
    }
}
