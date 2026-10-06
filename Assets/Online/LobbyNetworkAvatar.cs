using Fusion;
using UnityEngine;

namespace DeadBoat.Online
{
    // The existing first-person controller stays local. This object only represents it to other peers.
    [DefaultExecutionOrder(100)] // Anchor visuals after SharedBoatRuntime applies the boat snapshot.
    public sealed class LobbyNetworkAvatar : NetworkBehaviour
    {
        private static readonly System.Collections.Generic.List<LobbyNetworkAvatar> spawned = new();
        // Concrete enumerator avoids a scene scan and an array allocation per AI query.
        public static AvatarEnumerable All => new AvatarEnumerable();
        public readonly struct AvatarEnumerable
        {
            public Enumerator GetEnumerator() => new Enumerator(spawned.GetEnumerator());
        }
        public struct Enumerator
        {
            private System.Collections.Generic.List<LobbyNetworkAvatar>.Enumerator iterator;
            internal Enumerator(System.Collections.Generic.List<LobbyNetworkAvatar>.Enumerator value) => iterator = value;
            public LobbyNetworkAvatar Current => iterator.Current;
            public bool MoveNext()
            {
                while (iterator.MoveNext())
                    if (Current != null && Current.Object != null && Current.Object.IsValid) return true;
                return false;
            }
            public void Dispose() => iterator.Dispose();
        }

        public override void Despawned(NetworkRunner runner, bool hasState) => spawned.Remove(this);
        private void OnDestroy() => spawned.Remove(this);

        [SerializeField] private LobbyHeldItemCatalog itemCatalog;

        [Networked] private TickTimer AttackVisual { get; set; }
        [Networked] private int AttackStyle { get; set; }
        [Networked] private byte HeldItemId { get; set; }
        [Networked] public float Health { get; private set; }
        public Transform WorldTransform => visualRoot != null ? visualRoot : transform;
        private BoardController seatBoard;
        private Transform driverSeat;
        public Transform DrivingSeat
        {
            get
            {
                var state = SharedRunContext.State;
                if (Object == null || !Object.IsValid || !SharedRunContext.Playing || state.Runner != Runner ||
                    state.Driver != Object.StateAuthority) return null;
                var board = BoardController.Instance;
                if (seatBoard != board)
                {
                    seatBoard = board;
                    driverSeat = board != null ? board.GetComponentInChildren<DriverSeatTrigger>()?.driverSeatTransform : null;
                }
                return driverSeat;
            }
        }
        // The driver's logical location follows the already replicated boat;
        // its NetworkTransform can remain unchanged while the seat is occupied.
        public Vector3 LogicalPosition => DrivingSeat != null ? DrivingSeat.position + Origin : transform.position;

        private PlayerMovement localPlayer;
        private bool wasDriving;
        private NetworkTransform networkTransform;
        private ActiveItemManager activeItemManager;
        private PickableItem observedItem;
        private Renderer[] avatarRenderers;
        private Animator animator;
        private Transform rightArm;
        private Transform leftArm;
        private Transform handSocket;
        private GameObject heldVisual;
        private byte displayedItemId = byte.MaxValue;
        private float holdWeight;
        private Vector3 lastPosition;
        private Transform visualRoot;

        public static Vector3 Origin => SharedRunContext.Active && FixCoordinate.Instance != null
            ? Vector3.forward * FixCoordinate.Instance.BoardAddPos : Vector3.zero;

        public static void ReportAttack(int style)
        {
            foreach (var avatar in LobbyNetworkAvatar.All)
                if (avatar.Object != null && avatar.Object.IsValid && avatar.Object.HasStateAuthority &&
                    (!SharedRunContext.Active || avatar.Runner == SharedRunContext.State?.Runner))
                {
                    avatar.AttackStyle = style;
                    avatar.AttackVisual = TickTimer.CreateFromSeconds(avatar.Runner, 0.35f);
                    return;
                }
        }

        public override void Spawned()
        {
            if (!spawned.Contains(this)) spawned.Add(this);
            // Runs on every peer: remote instances must survive the lobby unload too.
            Runner.MakeDontDestroyOnLoad(gameObject);
            networkTransform = GetComponent<NetworkTransform>();
            Debug.Log($"[Lobby online] Avatar spawned; authority={Object.HasStateAuthority}");
            avatarRenderers = GetComponentsInChildren<Renderer>(true);
            animator = GetComponentInChildren<Animator>(true);
            foreach (var part in GetComponentsInChildren<Transform>(true))
            {
                if (part.name == "arm-right") rightArm = part;
                else if (part.name == "arm-left") leftArm = part;
            }

            if (rightArm != null)
            {
                handSocket = new GameObject("Held Item Socket").transform;
                handSocket.SetParent(rightArm, false);
                handSocket.localPosition = new Vector3(0f, -0.93f, 0f);
            }

            lastPosition = transform.position;
            // NetworkTransform carries logical coordinates. Move only the visual
            // children into this peer's rebased world; keep the network root intact.
            visualRoot = new GameObject("Local Avatar Visual").transform;
            var children = new System.Collections.Generic.List<Transform>();
            foreach (Transform child in transform) children.Add(child);
            visualRoot.SetParent(transform, false);
            foreach (var child in children) child.SetParent(visualRoot, true);
            if (Object.HasStateAuthority)
            {
                foreach (var avatarRenderer in avatarRenderers)
                    avatarRenderer.enabled = false;

                localPlayer = PlayerMovement.Instance;
            }
        }

        public override void FixedUpdateNetwork()
        {
            if (!Object.HasStateAuthority)
                return;

            if (localPlayer == null)
                localPlayer = PlayerMovement.Instance;

            if (localPlayer == null)
                return;

            bool driving = DrivingSeat != null;
            if (!driving)
            {
                // Resume at the exit position without interpolating from the
                // old, frozen position that may now be kilometres behind us.
                if (wasDriving && networkTransform != null)
                    networkTransform.Teleport(localPlayer.transform.position + Origin, localPlayer.transform.rotation);
                transform.SetPositionAndRotation(localPlayer.transform.position + Origin, localPlayer.transform.rotation);
            }
            wasDriving = driving;
            Health = PlayerStatsManager.Instance != null ? PlayerStatsManager.Instance.Health : 100;

            if (activeItemManager == null && Inventory.Instance != null)
                activeItemManager = Inventory.Instance.GetComponent<ActiveItemManager>();

            PickableItem activeItem = activeItemManager != null ? activeItemManager.Active : null;
            if (activeItem != observedItem)
            {
                observedItem = activeItem;
                HeldItemId = itemCatalog != null ? itemCatalog.FindId(activeItem) : (byte)0;
                Debug.Log($"[Lobby online] Held item changed: {activeItem?.name ?? "none"}; id={HeldItemId}");
            }
        }

        private void Update()
        {
            if (animator == null || Object == null || Object.HasStateAuthority)
                return;

            if (displayedItemId != HeldItemId)
                RefreshHeldVisual();

            var horizontalDelta = transform.position - lastPosition;
            horizontalDelta.y = 0f;
            var speed = horizontalDelta.magnitude / Mathf.Max(Time.deltaTime, 0.001f);
            animator.SetFloat("Speed", speed);
            lastPosition = transform.position;
        }

        private void LateUpdate()
        {
            if (visualRoot != null)
            {
                var seat = DrivingSeat;
                visualRoot.SetPositionAndRotation(seat != null ? seat.position : transform.position - Origin,
                    seat != null ? seat.rotation : transform.rotation);
            }
            if (Object == null || Object.HasStateAuthority || rightArm == null)
                return;

            holdWeight = Mathf.MoveTowards(holdWeight, HeldItemId == 0 ? 0f : 1f,
                Time.deltaTime * 4f);
            if (holdWeight <= 0f)
                return;

            float sway = Mathf.Sin(Time.time * 2.5f) * 1.5f;
            float remaining = AttackVisual.RemainingTime(Runner) ?? 0;
            float attack = remaining > 0 ? Mathf.Sin((1 - remaining / 0.35f) * Mathf.PI) : 0;
            var rightPose = Quaternion.Euler(-65f + sway + attack * (AttackStyle == 1 ? 70 : -15),
                attack * (AttackStyle == 1 ? 65 : 0), -6f);
            rightArm.localRotation = Quaternion.Slerp(rightArm.localRotation, rightPose, holdWeight);

            var entry = itemCatalog != null ? itemCatalog.Find(HeldItemId) : null;
            if (entry != null && entry.pose == LobbyHeldItemCatalog.HoldPose.TwoHanded && leftArm != null)
            {
                var leftPose = Quaternion.Euler(-62f + sway, 0f, 8f);
                leftArm.localRotation = Quaternion.Slerp(leftArm.localRotation, leftPose, holdWeight);
            }
        }

        private void RefreshHeldVisual()
        {
            displayedItemId = HeldItemId;
            if (heldVisual != null)
                Destroy(heldVisual);

            var entry = itemCatalog != null ? itemCatalog.Find(HeldItemId) : null;
            if (entry == null || entry.visualPrefab == null || handSocket == null)
                return;

            heldVisual = Instantiate(entry.visualPrefab, handSocket, false);
            heldVisual.transform.localPosition = entry.localPosition;
            heldVisual.transform.localRotation = Quaternion.Euler(entry.localEuler);
            heldVisual.transform.localScale = Vector3.one * entry.scale;
        }
    }
}
