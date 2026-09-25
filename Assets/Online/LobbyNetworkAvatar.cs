using Fusion;
using UnityEngine;

namespace DeadBoat.Online
{
    // The existing first-person controller stays local. This object only represents it to other peers.
    public sealed class LobbyNetworkAvatar : NetworkBehaviour
    {
        [SerializeField] private LobbyHeldItemCatalog itemCatalog;

        [Networked] private byte HeldItemId { get; set; }

        private PlayerMovement localPlayer;
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

        public override void Spawned()
        {
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

            transform.SetPositionAndRotation(localPlayer.transform.position, localPlayer.transform.rotation);

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
            if (Object == null || Object.HasStateAuthority || rightArm == null)
                return;

            holdWeight = Mathf.MoveTowards(holdWeight, HeldItemId == 0 ? 0f : 1f,
                Time.deltaTime * 4f);
            if (holdWeight <= 0f)
                return;

            float sway = Mathf.Sin(Time.time * 2.5f) * 1.5f;
            var rightPose = Quaternion.Euler(-65f + sway, 0f, -6f);
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
