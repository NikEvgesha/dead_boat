using Fusion;
using UnityEngine;

namespace DeadBoat.Online
{
    // The existing first-person controller stays local. This object only represents it to other peers.
    public sealed class LobbyNetworkAvatar : NetworkBehaviour
    {
        private PlayerMovement localPlayer;
        private Renderer[] avatarRenderers;
        private Animator animator;
        private Vector3 lastPosition;

        public override void Spawned()
        {
            Debug.Log($"[Lobby online] Avatar spawned; authority={Object.HasStateAuthority}");
            avatarRenderers = GetComponentsInChildren<Renderer>(true);
            animator = GetComponentInChildren<Animator>(true);
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
        }

        private void Update()
        {
            if (animator == null || Object == null || Object.HasStateAuthority)
                return;

            var horizontalDelta = transform.position - lastPosition;
            horizontalDelta.y = 0f;
            var speed = horizontalDelta.magnitude / Mathf.Max(Time.deltaTime, 0.001f);
            animator.SetFloat("Speed", speed);
            lastPosition = transform.position;
        }
    }
}
