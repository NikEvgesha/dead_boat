using System;
using UnityEngine;
using UnityEngine.UI;

namespace DeadBoat.Online
{
    // Voluntary rewarded return. This does not impose a maximum crew separation.
    public sealed class SharedBoatReturn : MonoBehaviour
    {
        [SerializeField, Min(10)] private float offerDistance = 750;
        private readonly RaycastHit[] surfaces = new RaycastHit[24];
        private readonly Collider[] overlaps = new Collider[24];
        private static readonly Vector2[] deckPoints = {
            new(-.55f, -2), new(.55f, -2), new(-.55f, -4), new(.55f, -4), new(0, -1), new(0, -3)
        };
        private GameObject root, backdrop;
        private Button offer, watch;
        private Text offerLabel, title, message, watchLabel, cancelLabel;
        private bool opened, busy, earned;
        private int revision;
        private double nextRefresh;
        private string status;
        private ControlManager controls;
        private PauseManager pause;
        private bool previousMove, previousCursor;
        public bool Busy => busy;
        public bool HasEarnedReturn => earned;
        private static bool English => LocalizationManager.Instance != null &&
            (LocalizationManager.Instance.CurrentLanguage ?? "").StartsWith("en", StringComparison.OrdinalIgnoreCase);
        private static string T(string ru, string en) => English ? en : ru;

        private void Awake()
        {
            root = new GameObject("Return to boat UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.transform.SetParent(transform, false);
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            root.GetComponent<Canvas>().sortingOrder = 1050;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = .5f;
            offer = LobbyOnlineModeUI.CreateButton(root.transform, "BoatReturn", new Vector2(1, .5f),
                new Vector2(-135, -135), new Vector2(250, 68), OpenOffer);
            offerLabel = offer.GetComponentInChildren<Text>();
            offerLabel.fontSize = 18;
            backdrop = new GameObject("Boat return backdrop", typeof(RectTransform), typeof(Image));
            backdrop.transform.SetParent(root.transform, false);
            var full = backdrop.GetComponent<RectTransform>();
            full.anchorMin = Vector2.zero; full.anchorMax = Vector2.one;
            full.offsetMin = full.offsetMax = Vector2.zero;
            backdrop.GetComponent<Image>().color = new Color(0, 0, 0, .5f);
            var panel = new GameObject("Boat return offer", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(backdrop.transform, false);
            panel.GetComponent<RectTransform>().sizeDelta = new Vector2(560, 350);
            panel.GetComponent<Image>().color = new Color(.07f, .14f, .19f, .98f);
            title = LobbyOnlineModeUI.CreateText(panel.transform, "Title", new Vector2(0, 125), new Vector2(520, 55), 28);
            message = LobbyOnlineModeUI.CreateText(panel.transform, "Message", new Vector2(0, 30), new Vector2(510, 125), 19);
            message.supportRichText = false;
            watch = LobbyOnlineModeUI.CreateButton(panel.transform, "Watch", new Vector2(.5f, .5f),
                new Vector2(0, -72), new Vector2(480, 48), WatchAndReturn);
            watchLabel = watch.GetComponentInChildren<Text>();
            var cancel = LobbyOnlineModeUI.CreateButton(panel.transform, "Cancel", new Vector2(.5f, .5f),
                new Vector2(0, -130), new Vector2(480, 42), () => SetOpen(false));
            cancelLabel = cancel.GetComponentInChildren<Text>();
            offer.gameObject.SetActive(false); backdrop.SetActive(false);
        }
        private static bool InRun(SharedDepartureState state) => SharedRunContext.Playing && state != null &&
            SharedRunContext.State == state && state.BoatInitialized && !state.RunWon &&
            state.Includes(state.Runner.LocalPlayer) && BoardController.Instance != null && BoardController.Instance.StartGame &&
            PlayerMovement.Instance != null && PlayerStatsManager.Instance != null &&
            PlayerStatsManager.Instance.InGame && PlayerStatsManager.Instance.Health > 0;
        private bool FarEnough()
        {
            var delta = PlayerMovement.Instance.transform.position - BoardController.Instance.transform.position;
            delta.y = 0;
            return delta.sqrMagnitude >= offerDistance * offerDistance;
        }
        private bool CanOffer() => InRun(SharedRunContext.State) && !busy && !SharedAdProtection.IsShowing &&
            (PauseManager.Instance == null || !PauseManager.Instance.IsPaused) && (earned || FarEnough());
        private void Update()
        {
            if (opened && (!InRun(SharedRunContext.State) || SharedAdProtection.IsShowing)) SetOpen(false);
            if (opened && Input.GetKeyDown(KeyCode.Escape)) SetOpen(false);
            if (!opened && CanOffer() && Input.GetKeyDown(KeyCode.H)) OpenOffer();
            if (Time.realtimeSinceStartupAsDouble < nextRefresh) return;
            nextRefresh = Time.realtimeSinceStartupAsDouble + .2;
            offer.gameObject.SetActive(!opened && CanOffer());
            bool touch = ControlManager.Instance != null && ControlManager.Instance.UseTouchControl;
            offerLabel.text = earned ? T("Вернуться к лодке", "Return to boat") : T("К лодке · за рекламу", "Boat · watch an ad");
            if (!touch) offerLabel.text += " [H]";
            if (!opened) return;
            title.text = T("Вернуться к лодке?", "Return to the boat?");
            bool ready = earned || AdsManager.Instance.IsRewardedAdReady();
            watch.interactable = ready && !busy;
            message.text = status ?? (earned
                ? T("Просмотр засчитан. Можно повторить возврат без новой рекламы.", "Your ad reward is ready. Retry without another ad.")
                : !ready ? T("Реклама сейчас недоступна. Попробуйте позже.", "No ad is available right now. Try again later.")
                : T("Посмотрите рекламу, чтобы вернуться на палубу. Здоровье и содержимое сумки сохранятся. Переносимый груз сначала положите в сумку или на землю.",
                    "Watch an ad to return to the deck. Health and inventory stay unchanged. Put any carried cargo in your bag or on the ground first."));
            watchLabel.text = earned ? T("Вернуться", "Return") : T("Смотреть рекламу и вернуться", "Watch ad and return");
            cancelLabel.text = T("Отмена", "Cancel");
        }
        public void OpenOffer()
        {
            if (!CanOffer()) return;
            status = null;
            SetOpen(true);
            nextRefresh = 0;
        }
        private void SetOpen(bool value)
        {
            if (opened == value) return;
            opened = value;
            backdrop.SetActive(value);
            if (value)
            {
                controls = ControlManager.Instance;
                if (controls != null)
                {
                    previousMove = controls.MoveActive; previousCursor = controls.CursorActive;
                    controls.MoveActive = false; controls.CursorActive = true;
                }
                pause = PauseManager.Instance;
                if (pause != null) pause.SetPause(true);
                offer.gameObject.SetActive(false);
            }
            else
            {
                if (pause != null) pause.SetPause(false);
                if (controls != null) { controls.MoveActive = previousMove; controls.CursorActive = previousCursor; }
                pause = null; controls = null;
            }
        }
        private static bool Carrying(SharedDepartureState state)
        {
            var pickup = PlayerMovement.Instance.GetComponentInChildren<PlayerItemPickUp>();
            if (pickup != null && pickup.CarryingWorldItem) return true;
            // Also cover a claim received immediately before opening the panel.
            foreach (var entry in state.Items)
                if (entry.Value.Owner == state.Runner.LocalPlayer && entry.Value.Mode == 1) return true;
            return false;
        }
        public void WatchAndReturn()
        {
            var state = SharedRunContext.State;
            if (busy || !opened || !InRun(state)) return;
            if (Carrying(state))
            {
                status = T("Сначала положите переносимый предмет в сумку или на землю.", "Put the carried item in your bag or on the ground first.");
                nextRefresh = 0; return;
            }
            if (!TryDestination(state, out _)) { NoSpace(); return; }
            SetOpen(false);
            if (earned) { Return(state); return; }
            busy = true;
            int attempt = ++revision;
            try
            {
                AdsManager.Instance.ShowRewardedAd("return_to_boat", success =>
                {
                    if (this == null || attempt != revision) return;
                    busy = false;
                    if (!InRun(state)) return;
                    if (!success)
                    {
                        status = T("Возврат не выполнен: просмотр не подтверждён. Можно попробовать позже.", "Return cancelled: the ad reward was not confirmed. Try again later.");
                        SetOpen(true); nextRefresh = 0; return;
                    }
                    earned = true;
                    Return(state);
                });
            }
            catch (Exception e)
            {
                busy = false;
                Debug.LogWarning("[Boat return] Ad request failed: " + e.GetType().Name);
                if (InRun(state)) { status = T("Не удалось открыть рекламу. Попробуйте позже.", "Could not open the ad. Try again later."); SetOpen(true); nextRefresh = 0; }
            }
        }
        private void Return(SharedDepartureState state)
        {
            if (!InRun(state)) return;
            if (Carrying(state) || !TryDestination(state, out var destination)) { NoSpace(); return; }
            PlayerMovement.Instance.Teleport(destination);
            LobbyNetworkAvatar.SnapLocalPlayer(state.Runner);
            earned = false;
            status = null;
            Debug.Log("[Boat return] Rewarded return applied to local player.");
        }
        private void NoSpace()
        {
            status = earned ? T("На палубе пока нет свободного места. Повторите позже — реклама уже засчитана.", "The deck is occupied. Retry without another ad while this game remains open.")
                : T("На палубе пока нет свободного места. Попробуйте позже.", "The deck is occupied. Try again later.");
            SetOpen(true); nextRefresh = 0;
        }
        private bool TryDestination(SharedDepartureState state, out Vector3 destination)
        {
            var boat = BoardController.Instance;
            var player = PlayerMovement.Instance;
            var controller = player.GetComponent<CharacterController>();
            float scale = player.transform.lossyScale.y;
            float radius = controller != null ? controller.radius * Mathf.Max(player.transform.lossyScale.x, player.transform.lossyScale.z) : .35f;
            float height = controller != null ? Mathf.Max(controller.height * scale, 2 * radius) : 1.8f;
            float centerY = controller != null ? controller.center.y * scale : 0;
            Physics.SyncTransforms();
            foreach (var point in deckPoints)
            {
                Vector3 above = boat.transform.TransformPoint(new Vector3(point.x, 6, point.y));
                int count = Physics.RaycastNonAlloc(above, Vector3.down, surfaces, 12, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
                if (count == surfaces.Length) continue;
                float top = float.NegativeInfinity;
                for (int i = 0; i < count; i++)
                {
                    var hit = surfaces[i];
                    if (hit.normal.y > .65f && hit.collider.GetComponentInParent<BoardController>() == boat &&
                        hit.collider.GetComponentInParent<PickableItem>() == null) top = Mathf.Max(top, hit.point.y);
                }
                if (float.IsNegativeInfinity(top)) continue;
                var candidate = new Vector3(above.x, top - centerY + height * .5f + .08f, above.z);
                Vector3 center = candidate + Vector3.up * centerY;
                float stem = height * .5f - radius;
                int hits = Physics.OverlapCapsuleNonAlloc(center + Vector3.up * stem, center - Vector3.up * stem,
                    radius, overlaps, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
                bool clear = hits < overlaps.Length;
                for (int i = 0; i < hits && clear; i++)
                    if (!overlaps[i].transform.IsChildOf(player.transform)) clear = false;
                foreach (var avatar in LobbyNetworkAvatar.All)
                    if (clear && avatar.Runner == state.Runner && avatar.Object.StateAuthority != state.Runner.LocalPlayer &&
                        state.Includes(avatar.Object.StateAuthority) && avatar.Health > 0 &&
                        Vector3.Distance(avatar.LogicalPosition - LobbyNetworkAvatar.Origin, candidate) < radius * 2 + .25f) clear = false;
                if (clear) { destination = candidate; return true; }
            }
            destination = default; return false;
        }
        private void OnDestroy()
        {
            revision++;
            if (backdrop != null) SetOpen(false);
            if (root != null) Destroy(root);
        }
    }
}
