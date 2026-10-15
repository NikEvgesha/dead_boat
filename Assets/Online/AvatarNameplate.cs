using Fusion;
using UnityEngine;
using UnityEngine.UI;

namespace DeadBoat.Online
{
    // World-space UI uses normal scene depth and never participates in input raycasts.
    [DefaultExecutionOrder(110)]
    public sealed class AvatarNameplate : MonoBehaviour
    {
        public const float MaxDistance = 30;
        private LobbyNetworkAvatar avatar;
        private Camera view;
        private Canvas canvas;
        private CanvasGroup group;
        private Text label;
        private float nextSample;
        private NetworkString<_32> lastName;
        private bool hasName;
        private int lastPlayer;
        private bool lastEnglish;
        internal bool IsVisible => canvas != null && canvas.enabled;
        internal string Caption => label != null ? label.text : "";

        public void Initialize(LobbyNetworkAvatar owner)
        {
            avatar = owner;
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var rect = (RectTransform)transform;
            rect.sizeDelta = new Vector2(250, 42);
            rect.localScale = Vector3.one * .005f;
            group = gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false; group.interactable = false;
            var background = gameObject.AddComponent<Image>();
            background.color = new Color(.04f, .08f, .1f, .75f);
            background.raycastTarget = false;
            label = LobbyOnlineModeUI.CreateText(transform, "Player name", Vector2.zero, new Vector2(238, 40), 24);
            label.material = Graphic.defaultGraphicMaterial; // UI/Default respects world-canvas depth.
            label.supportRichText = false;
            label.resizeTextForBestFit = true; label.resizeTextMinSize = 14; label.resizeTextMaxSize = 24;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            canvas.enabled = false;
        }

        internal static bool CanShow(bool validRemote, Vector3 localOffset, float depth, bool menuOpen) =>
            validRemote && !menuOpen && depth > 0 && localOffset.sqrMagnitude <= MaxDistance * MaxDistance;

        private void LateUpdate()
        {
            if (avatar == null || avatar.Object == null || !avatar.Object.IsValid || avatar.Object.HasStateAuthority)
            { if (canvas != null) canvas.enabled = false; return; }
            if (Time.unscaledTime >= nextSample)
            {
                nextSample = Time.unscaledTime + .2f;
                // Reacquire after camera replacement and Lobby -> Run -> Lobby transitions.
                view = Camera.main;
                var name = avatar.DisplayName;
                int player = avatar.Object.StateAuthority.RawEncoded;
                bool english = LocalizationManager.Instance?.CurrentLanguage?.StartsWith("En", System.StringComparison.OrdinalIgnoreCase) == true;
                if (!hasName || !name.Equals(lastName) || player != lastPlayer || english != lastEnglish)
                {
                    label.text = AvatarDisplayName.Resolve(name.ToString(), player, english);
                    float width = Mathf.Clamp(label.preferredWidth + 18, 90, 250);
                    ((RectTransform)transform).sizeDelta = new Vector2(width, 42);
                    label.rectTransform.sizeDelta = new Vector2(width - 12, 40);
                    hasName = true; lastName = name; lastPlayer = player; lastEnglish = english;
                }
            }
            if (view == null) { canvas.enabled = false; return; }
            transform.position = avatar.WorldTransform.position + Vector3.up * 2.25f;
            Vector3 offset = transform.position - view.transform.position;
            float depth = Vector3.Dot(view.transform.forward, offset);
            canvas.enabled = CanShow(true, offset, depth, ControlManager.Instance != null && ControlManager.Instance.CursorActive);
            if (!canvas.enabled) return;
            canvas.worldCamera = view;
            transform.rotation = view.transform.rotation;
            float distance = offset.magnitude;
            // Roughly constant readable screen size at ordinary social distances; cap distant growth.
            transform.localScale = Vector3.one * (.001f * Mathf.Clamp(distance, 3f, 18f));
            group.alpha = Mathf.Clamp01((MaxDistance - distance) / 8f);
        }
    }
}
