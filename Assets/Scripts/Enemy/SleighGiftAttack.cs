using System.Collections.Generic;
using UnityEngine;

namespace DeadBoat.Events.NewYear
{
    /// <summary>A locked landing spot, a ballistic present and one damage pulse.</summary>
    public sealed class SleighGiftAttack : MonoBehaviour
    {
        [SerializeField] private Transform giftVisual;
        [SerializeField] private Material warningMaterial;
        [SerializeField] private bool icicle;
        private Transform attachedVisual;
        private SleighBossController owner;
        private Transform release, marker;
        private LineRenderer rim, countdown;
        private Vector3 startLocal;
        private Vector3 verticalStart;
        private float age, warningTime, flightTime, radius, size;
        private int damage;
        private bool launched, resolved;
        private readonly HashSet<PlayerStatsManager> victims = new();

        public bool Resolved => resolved;
        public bool Launched => launched;
        public Vector3 LandingPoint => marker ? marker.position : transform.position;
        public float Radius => radius;
        public int HitPlayers { get; private set; }
        public bool IsIcicle => icicle;

        public void Initialize(SleighBossController boss, Transform drop, RaycastHit surface,
            float delay, float duration, float blastRadius, int hitDamage, float giftSize, Transform attached = null)
        {
            owner = boss; release = drop;
            if(icicle)verticalStart=drop.position;
            attachedVisual = attached;
            warningTime = Mathf.Max(.5f, delay); flightTime = Mathf.Max(.5f, duration);
            radius = blastRadius; damage = hitDamage; size = giftSize;
            marker = new GameObject("Gift_Landing_Warning").transform;
            // Terrain/boat colliders may be scaled; preserve metre-sized warning rings.
            marker.SetParent(surface.collider.transform, true);
            marker.position = surface.point + surface.normal * .06f;
            marker.rotation = Quaternion.FromToRotation(Vector3.up, surface.normal);
            rim = MakeRing("Blast_Radius", .09f);
            countdown = MakeRing("Landing_Countdown", .14f);
            DrawRing(rim, radius); DrawRing(countdown, radius);
            giftVisual.gameObject.SetActive(false);
        }

        private LineRenderer MakeRing(string label, float width)
        {
            var go = new GameObject(label); go.layer = 2;
            go.transform.SetParent(marker, false);
            var line = go.AddComponent<LineRenderer>();
            line.sharedMaterial = warningMaterial;
            line.useWorldSpace = false; line.loop = true;
            line.positionCount = 64; line.widthMultiplier = width;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            return line;
        }

        private static void DrawRing(LineRenderer line, float r)
        {
            for (int i = 0; i < line.positionCount; i++)
            {
                float a = i * Mathf.PI * 2 / line.positionCount;
                line.SetPosition(i, new Vector3(Mathf.Cos(a) * r, .015f, Mathf.Sin(a) * r));
            }
        }

        private void Update()
        {
            if (!owner || owner.IsDead || !marker || !owner.CanSimulate) { Cancel(); return; }
            if (!owner.HasLivingTarget) return;
            age += Time.deltaTime;
            if (resolved)
            {
                float t = Mathf.Clamp01((age - warningTime - flightTime) / .3f);
                DrawRing(rim, Mathf.Lerp(.15f, radius, t));
                if (t >= 1) Cancel();
                return;
            }
            float progress = Mathf.Clamp01(age / (warningTime + flightTime));
            DrawRing(countdown, Mathf.Max(.12f, radius * (1 - progress)));
            rim.widthMultiplier = .09f + .025f * Mathf.Sin(age * 13);
            if (age < warningTime) return;
            if (!launched)
            {
                launched = true;
                owner.PlayThrowSound();
                startLocal = transform.InverseTransformPoint(icicle?verticalStart:release.position);
                if (attachedVisual) attachedVisual.gameObject.SetActive(false);
                giftVisual.gameObject.SetActive(true);
            }
            float flight = Mathf.Clamp01((age - warningTime) / flightTime);
            Vector3 from = transform.TransformPoint(startLocal);
            Vector3 to = LandingPoint + Vector3.up * size * .5f;
            if(icicle){to.x=from.x;to.z=from.z;}
            giftVisual.position = Vector3.Lerp(from, to, icicle ? flight*flight : flight) + Vector3.up * (4 * flight * (1 - flight) * (icicle ? 0 : 3.5f));
            if (!icicle) giftVisual.Rotate(new Vector3(75, 125, 45) * Time.deltaTime, Space.Self);
            if (flight >= 1) Impact();
        }

        private void Impact()
        {
            // resolved is set before callbacks: a present can never apply damage twice.
            resolved = true;
            giftVisual.gameObject.SetActive(false);
            countdown.gameObject.SetActive(false);
            Vector3 center = LandingPoint + Vector3.up * .4f;
            foreach (var hit in Physics.OverlapSphere(center, radius, 1 << LayerMask.NameToLayer("Player"), QueryTriggerInteraction.Collide))
            {
                var player = hit.GetComponentInParent<PlayerStatsManager>();
                if (!player || player.Health <= 0 || !victims.Add(player)) continue;
                Vector3 end = player.transform.position + Vector3.up * .8f;
                // Solid walls shelter players; trigger zones do not block a blast.
                bool blocked = false;
                foreach (var obstruction in Physics.RaycastAll(center, end - center, Vector3.Distance(center, end), ~0, QueryTriggerInteraction.Ignore))
                {
                    if (obstruction.collider.GetComponentInParent<PlayerStatsManager>() == player) continue;
                    if (obstruction.collider.transform.IsChildOf(owner.transform)) continue;
                    blocked = true; break;
                }
                if (blocked) continue;
                float before = player.Health;
                player.TakeDamage(damage);
                if (player.Health < before) HitPlayers++;
            }
            owner.RecordImpact(this);
        }

        public void Cancel()
        {
            if (marker) Destroy(marker.gameObject);
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (marker) Destroy(marker.gameObject);
        }
    }
}
