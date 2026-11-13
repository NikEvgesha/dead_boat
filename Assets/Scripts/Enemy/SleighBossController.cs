using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeadBoat.Events.NewYear
{
    /// <summary>Local sleigh encounter with gift volleys and low icicle passes.</summary>
    public sealed class SleighBossController : EnemyCore
    {
        public enum FlightState { Waiting, Approach, Bombardment, Descending, Recovery, Climbing, Defeated, IcicleApproach, IcicleDrop, IcicleRetreat, RadialApproach, RadialBurst }
        [SerializeField] private Transform visualRoot, dropAnchor, engine;
        [SerializeField] private SleighGiftAttack[] gifts;
        [SerializeField] private int startingHealth = 600;
        [SerializeField] private float engageDistance = 28;
        [SerializeField] private float recoveryDuration = 4.5f;
        [SerializeField, Min(1)] private int volleysBeforeRecovery = 3;
        [SerializeField, Min(.1f)] private float flightSpeedMultiplier = 2;
        [SerializeField, Min(1)] private int giftsPerVolley = 5;
        [SerializeField] private float bombardmentDuration = 5;
        [SerializeField] private float timeBetweenGifts = .45f;
        [SerializeField] private AudioSource laughSource, throwSource;
        [SerializeField] private AudioClip[] laughClips;
        [SerializeField] private AudioClip throwClip;
        [SerializeField] private bool showHud = true;
        [Header("Low icicle pass")]
        [SerializeField] private SleighGiftAttack icicleProjectile;
        [SerializeField] private Transform[] hangingIcicles;
        [SerializeField, Min(1)] private int icicleAttackInterval = 3;
        [SerializeField] private float icicleHoverHeight = 2.8f;
        [SerializeField] private float icicleDropDuration = 3.2f;
        [SerializeField] private int icicleDamage = 18;
        [SerializeField, Range(0,1)] private float radialAttackChance = .2f;
        private int passDrops;
        public int RadialVolleysThrown { get; private set; }

        private readonly List<SleighGiftAttack> activeGifts = new();
        private Transform arena;
        private PlayerStatsManager target;
        private Vector3 legStart, legEnd, tangent, engineScale;
        private float timer, legDuration, flightClock;
        private int pattern, cycle, airborneVolleys;
        private float nextLaughTime;
        private bool released;
        private bool finalEncounter;
        private int attacksStarted;
        private Quaternion icicleFacing;
        private GUIStyle titleStyle, hintStyle;

        public FlightState State { get; private set; }
        public int VolleysThrown { get; private set; }
        public int GiftsImpacted { get; private set; }
        public int PlayerHits { get; private set; }
        public int LaughsPlayed { get; private set; }
        public int ThrowSoundsPlayed { get; private set; }
        public int IcicleVolleysThrown { get; private set; }
        public int IciclesImpacted { get; private set; }
        public int AttacksStarted => attacksStarted;
        public bool CanSimulate => !DeadBoat.Online.SharedRunContext.Active;
        public bool HasLivingTarget => target && target.InGame && target.Health > 0;
        public event Action<SleighGiftAttack> GiftImpacted;
        public event Action Defeated;

        protected override void Awake()
        {
            base.Awake();
            enemyType = EnemyType.None;
            SetHP(startingHealth);
            if (engine) engineScale = engine.localScale;
        }

        private void Start()
        {
            var anchor = new GameObject("Sleigh_Encounter_Origin");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(anchor, gameObject.scene);
            arena = anchor.transform; arena.position = transform.position;
            State = FlightState.Waiting;
            target = FindAnyObjectByType<PlayerStatsManager>();
            if (finalEncounter) Engage();
        }

        private void Update()
        {
            if (!arena || !CanSimulate) return;
            flightClock += Time.deltaTime;
            if (isDead)
            {
                timer += Time.deltaTime;
                visualRoot.localRotation = Quaternion.Euler(-12, timer * 55, Mathf.Min(65, timer * 30));
                visualRoot.localScale = Vector3.one * Mathf.Max(0, 1 - timer / 2.2f);
                if (timer >= 2.2f) visualRoot.gameObject.SetActive(false);
                return;
            }
            if (!target) target = FindAnyObjectByType<PlayerStatsManager>();
            if (!HasLivingTarget) { CancelGifts(); RestoreIcicles(); return; }
            if (State == FlightState.Waiting)
            {
                visualRoot.localPosition = Vector3.up * Mathf.Sin(flightClock * 1.6f) * .16f;
                // Altitude must not make an apparently nearby flying boss passive.
                Vector3 separation = target.transform.position - transform.position;
                if (Vector3.ProjectOnPlane(separation, Vector3.up).sqrMagnitude <= engageDistance * engageDistance &&
                    Mathf.Abs(separation.y) <= 25) Engage();
                return;
            }
            timer += Time.deltaTime;
            switch (State)
            {
                case FlightState.Approach:
                    FlyLeg();
                    if (timer >= legDuration)
                    {
                        released = false;
                        // Keep coasting over the arena while presents leave the stern.
                        BeginLeg(FlightState.Bombardment, transform.position + transform.forward * 4,
                            bombardmentDuration, Vector3.up * .35f);
                    }
                    break;
                case FlightState.Bombardment:
                    FlyLeg();
                    if (!released) { ReleaseVolley(); released = true; }
                    if (timer >= legDuration)
                    {
                        if (airborneVolleys >= volleysBeforeRecovery) { airborneVolleys = 0; BeginRecovery(); }
                        else { AdvancePattern(); BeginApproach(); }
                    }
                    break;
                case FlightState.Descending:
                    FlyLeg();
                    if (timer >= legDuration) { State = FlightState.Recovery; timer = 0; }
                    break;
                case FlightState.Recovery:
                    SetVisual(Mathf.Sin(timer * 2) * 3, 0);
                    if (timer >= recoveryDuration)
                    { AdvancePattern(); BeginApproach(); }
                    break;
                case FlightState.Climbing:
                    FlyLeg();
                    if (timer >= legDuration) BeginApproach();
                    break;
                case FlightState.IcicleApproach:
                    FlyLeg();
                    if (timer >= legDuration)
                    {
                        transform.rotation = icicleFacing;
                        BeginLeg(FlightState.IcicleDrop,FromArena(legEnd)+icicleFacing*Vector3.forward*12,4,Vector3.zero);
                    }
                    break;
                case FlightState.IcicleDrop:
                    FlyLeg();
                    while(passDrops<3 && timer >= 1+passDrops)ReleaseIcicles();
                    if (timer >= legDuration)
                    {
                        PlayLaugh(true);
                        Vector3 away = Vector3.ProjectOnPlane(transform.position - target.transform.position, Vector3.up).normalized;
                        if (away.sqrMagnitude < .1f) away = -target.transform.forward;
                        BeginLeg(FlightState.IcicleRetreat, target.transform.position + away * 18 + Vector3.up * 14, 3.2f, Vector3.up * 4);
                    }
                    break;
                case FlightState.RadialApproach:
                    FlyLeg();
                    if(timer>=legDuration){State=FlightState.RadialBurst;timer=0;released=false;}
                    break;
                case FlightState.RadialBurst:
                    SetVisual(0,0);
                    if(!released&&timer>=.65f){ReleaseRadialGifts();released=true;}
                    if(timer>=2.2f){PlayLaugh(true);BeginLeg(FlightState.IcicleRetreat,target.transform.position-transform.forward*16+Vector3.up*14,3.2f,Vector3.up*3);}
                    break;
                case FlightState.IcicleRetreat:
                    FlyLeg();
                    if (timer >= legDuration) { RestoreIcicles(); airborneVolleys = 0; cycle++; BeginApproach(); }
                    break;
            }
            if (engine)
            {
                float power = State == FlightState.Approach || State == FlightState.Climbing || State == FlightState.IcicleRetreat ? 1.2f : .85f;
                engine.localScale = engineScale * (power + .08f * Mathf.Sin(flightClock * 11));
            }
            activeGifts.RemoveAll(g => !g);
        }

        public void Engage()
        {
            if (isDead || !CanSimulate || !arena || !HasLivingTarget || State != FlightState.Waiting) return;
            PlayLaugh();
            BeginApproach();
        }

        private void AdvancePattern() { pattern = (pattern + 1) % 3; cycle++; }

        public void BeginFinalEncounter()
        {
            finalEncounter = true;
            if (arena) Engage();
        }

        private void PlayLaugh(bool force = false)
        {
            if (!laughSource || laughClips == null || laughClips.Length == 0 || (!force && Time.time < nextLaughTime)) return;
            var clip = laughClips[VolleysThrown % laughClips.Length];
            if (!clip) return;
            laughSource.pitch = .97f + .03f * (VolleysThrown % 3);
            laughSource.PlayOneShot(clip);
            nextLaughTime = Time.time + 6;
            LaughsPlayed++;
        }

        public void PlayThrowSound()
        {
            if (!throwSource || !throwClip || isDead || !CanSimulate) return;
            throwSource.pitch = 1.06f - pattern * .08f;
            throwSource.PlayOneShot(throwClip);
            ThrowSoundsPlayed++;
        }

        private Vector3 InArena(Vector3 world) => arena.InverseTransformPoint(world);
        private Vector3 FromArena(Vector3 local) => arena.TransformPoint(local);

        private void BeginApproach()
        {
            attacksStarted++;
            if (UnityEngine.Random.value < radialAttackChance && BeginRadialAttack()) return;
            if (icicleProjectile && hangingIcicles != null && hangingIcicles.Length >= 3 &&
                attacksStarted % Mathf.Max(1,icicleAttackInterval) == 0 && BeginIciclePass()) return;
            Vector3 center = target.transform.position;
            float side = cycle % 2 == 0 ? 1 : -1;
            float distance = pattern == 0 ? 6 : pattern == 1 ? 11 : 17;
            Vector3 end = center + new Vector3(distance * side, 11 + pattern * 1.2f, 3);
            BeginLeg(FlightState.Approach, end, 3.2f, new Vector3(0, 5, -9 * side));
        }

        private bool BeginIciclePass()
        {
            Vector3 direction = Vector3.ProjectOnPlane(target.transform.forward,Vector3.up).normalized;
            if(direction.sqrMagnitude<.1f)direction=Vector3.forward;
            var center=target.transform.position;
            if(!FindSurface(center,out var surface))return false;
            float height=Mathf.Max(6,icicleHoverHeight*2.5f);
            var start=surface.point-direction*6+Vector3.up*height;
            var finish=surface.point+direction*6+Vector3.up*height;
            // A straight pass needs clearance along its entire hull, not only its endpoints.
            for(int i=0;i<=8;i++)
                foreach(var solid in Physics.OverlapBox(Vector3.Lerp(start,finish,i/8f),new Vector3(1.4f,.8f,2.4f),Quaternion.LookRotation(direction),~0,QueryTriggerInteraction.Ignore))
                    if(!solid.transform.IsChildOf(transform)&&!solid.GetComponentInParent<PlayerStatsManager>())return false;
            RestoreIcicles();icicleFacing=Quaternion.LookRotation(direction);passDrops=0;
            BeginLeg(FlightState.IcicleApproach,start,4,Vector3.up*2);return true;
        }

        private void ReleaseIcicles()
        {
            if(passDrops>=3)return;
            int i=passDrops++;
            // Snapshot the release point: each icicle falls vertically, with no homing.
            var origin=dropAnchor.position;
            if(!FindSurface(new Vector3(origin.x,target.transform.position.y,origin.z),out var ground))return;
            var ice=Instantiate(icicleProjectile,arena);
            ice.Initialize(this,dropAnchor,ground,.5f,.85f,.72f,icicleDamage,.7f,hangingIcicles[i]);
            activeGifts.Add(ice);
            if(i==0)IcicleVolleysThrown++;
        }

        private bool BeginRadialAttack()
        {
            if(gifts==null||gifts.Length<3)return false;
            // Reuse the existing obstacle-aware low landing search.
            BeginRecovery(false);
            if(State!=FlightState.Descending)return false;
            State=FlightState.RadialApproach;return true;
        }

        private void ReleaseRadialGifts()
        {
            var center=transform.position;
            var planned=new List<Vector3>();
            for(int i=0;i<8;i++){
                float angle=i*Mathf.PI/4;
                var candidate=center+new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*3.5f;
                candidate.y=target.transform.position.y;
                if(!TryGiftSurface(candidate,planned,1.8f,out var ground))continue;
                planned.Add(ground.point);
                var gift=Instantiate(gifts[i%3],arena);
                gift.Initialize(this,dropAnchor,ground,.75f+i*.06f,1,1.65f,17,.8f);
                activeGifts.Add(gift);
            }
            RadialVolleysThrown++;VolleysThrown++;
        }

        private void RestoreIcicles()
        {
            if (hangingIcicles == null) return;
            foreach(var ice in hangingIcicles) if(ice) ice.gameObject.SetActive(true);
        }

        private void BeginRecovery(bool selectNextOnFailure = true)
        {
            Vector3 desired = target.transform.position;
            bool found = false;
            for (int i = 0; i < 12; i++)
            {
                float angle = target.transform.eulerAngles.y + i * 30;
                Vector3 candidate = target.transform.position + Quaternion.Euler(0,angle,0) * Vector3.forward * 4;
                if (!FindSurface(candidate, out var ground)) continue;
                bool blocked = false;
                foreach (var solid in Physics.OverlapBox(ground.point + Vector3.up * 1.8f, new Vector3(1.6f,1.5f,2.6f), Quaternion.identity, ~0, QueryTriggerInteraction.Ignore))
                {
                    if (solid.GetComponentInParent<PlayerStatsManager>() || solid.transform.IsChildOf(transform)) continue;
                    blocked = true; break;
                }
                if (blocked) continue;
                desired = ground.point; found = true; break;
            }
            // Stay airborne if buildings leave no room for the full hull.
            if (!found) { if(selectNextOnFailure){AdvancePattern(); BeginApproach();} return; }
            // Low enough to reach the hull with a normal melee swing.
            BeginLeg(FlightState.Descending, desired + Vector3.up * .85f, 2.5f, Vector3.up * 2);
        }

        private void BeginLeg(FlightState state, Vector3 end, float duration, Vector3 bend)
        {
            State = state; timer = 0;
            legDuration = state == FlightState.Bombardment || state == FlightState.IcicleDrop ? duration : duration / Mathf.Max(.1f,flightSpeedMultiplier);
            legStart = InArena(transform.position); legEnd = InArena(end); tangent = bend;
        }

        private void FlyLeg()
        {
            float t = Mathf.Clamp01(timer / legDuration);
            float ease = State==FlightState.IcicleDrop ? t : t * t * t * (t * (t * 6 - 15) + 10);
            Vector3 local = Vector3.Lerp(legStart, legEnd, ease) + tangent * Mathf.Sin(Mathf.PI * ease);
            if (State == FlightState.Descending || State == FlightState.RadialApproach)
            {
                float u = 1 - ease;
                local = u*u*u*legStart + 3*u*u*ease*(legStart + Vector3.up*3) +
                    3*u*ease*ease*(legEnd + Vector3.up*9) + ease*ease*ease*legEnd;
            }
            Vector3 next = FromArena(local);
            Vector3 velocity = next - transform.position;
            if (velocity.sqrMagnitude > .00001f)
            {
                Vector3 flat = Vector3.ProjectOnPlane(velocity, Vector3.up);
                if (flat.sqrMagnitude > .00001f)
                {
                    float turn = Vector3.SignedAngle(transform.forward, flat, Vector3.up);
                    float turningSpeed = State == FlightState.Bombardment ? 120 : 120 * flightSpeedMultiplier;
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(flat), turningSpeed * Time.deltaTime);
                    SetVisual(Mathf.Clamp(-turn * .4f, -19, 19), Mathf.Clamp(-velocity.y * 100, -13, 13));
                }
            }
            transform.position = next;
        }

        private void SetVisual(float bank, float pitch)
        {
            visualRoot.localRotation = Quaternion.Slerp(visualRoot.localRotation, Quaternion.Euler(pitch, 0, bank), Time.deltaTime * 4);
            visualRoot.localPosition = Vector3.up * Mathf.Sin(flightClock * 2.4f) * .12f;
        }

        private void ReleaseVolley()
        {
            if (gifts == null || gifts.Length < 3) return;
            Vector3 center = target.transform.position;
            Vector3 right = Vector3.ProjectOnPlane(target.transform.right, Vector3.up).normalized;
            Vector3 forward = Vector3.ProjectOnPlane(target.transform.forward, Vector3.up).normalized;
            float radius = pattern == 0 ? 1.65f : pattern == 1 ? 2.35f : 3.3f;
            float spacing = radius * 2 + 1.2f;
            var planned = new List<Vector3>();
            for (int i = 0; i < giftsPerVolley; i++)
            {
                Vector3 offset = Vector3.zero;
                if (i > 0)
                {
                    if (pattern == 1) offset = forward * ((i + 1) / 2) * (i % 2 == 1 ? 1 : -1) * spacing;
                    else
                    {
                        float angle = (i - 1) * 90 + (pattern == 2 ? 45 : 0);
                        offset = (right * Mathf.Cos(angle*Mathf.Deg2Rad) + forward * Mathf.Sin(angle*Mathf.Deg2Rad)) * spacing;
                    }
                }
                bool placed = TryGiftSurface(center + offset, planned, spacing, out var ground);
                // A river edge or hole must not stack the remaining presents on one mark.
                for(int candidate=0;!placed&&candidate<48;candidate++)
                {
                    float angle = candidate * 137.5f * Mathf.Deg2Rad;
                    float distance = spacing * (1 + candidate / 12);
                    placed = TryGiftSurface(center + (right*Mathf.Cos(angle)+forward*Mathf.Sin(angle))*distance, planned, spacing, out ground);
                }
                if (!placed) continue;
                planned.Add(ground.point);
                var gift = Instantiate(gifts[pattern], arena);
                gift.Initialize(this, dropAnchor, ground, 1.05f + i * timeBetweenGifts,
                    1.05f + pattern * .25f, radius,
                    pattern == 0 ? 10 : pattern == 1 ? 17 : 26,
                    pattern == 0 ? .65f : pattern == 1 ? 1 : 1.5f);
                activeGifts.Add(gift);
            }
            VolleysThrown++;
            airborneVolleys++;
            PlayLaugh();
        }

        private bool TryGiftSurface(Vector3 desired, List<Vector3> planned, float spacing, out RaycastHit ground)
        {
            if (!FindSurface(desired,out ground)) return false;
            foreach(var point in planned)
                if(Vector3.ProjectOnPlane(point-ground.point,Vector3.up).sqrMagnitude < spacing*spacing*.95f) return false;
            return true;
        }

        public bool FindSurface(Vector3 near, out RaycastHit surface)
        {
            surface = default;
            float best = float.PositiveInfinity;
            foreach (var hit in Physics.RaycastAll(near + Vector3.up * 2, Vector3.down, 9, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.normal.y < .55f || hit.collider.GetComponentInParent<PlayerStatsManager>() ||
                    hit.collider.transform.IsChildOf(transform) || hit.collider.gameObject.layer == 4 || hit.distance >= best) continue;
                best = hit.distance; surface = hit;
            }
            return surface.collider;
        }

        public void RecordImpact(SleighGiftAttack gift)
        { if(gift.IsIcicle) IciclesImpacted++; else GiftsImpacted++; PlayerHits += gift.HitPlayers; GiftImpacted?.Invoke(gift); }

        public override void TakeDamage(int damage)
        {
            if (damage <= 0 || isDead || !CanSimulate) return;
            base.TakeDamage(damage);
            if (!isDead) Engage();
        }

        protected override void Die()
        {
            // A review encounter never awards a level win or persistent boss rewards.
            isDead = true; currentHP = 0; State = FlightState.Defeated; timer = 0;
            StopAllCoroutines(); CancelGifts();
            if (laughSource) laughSource.Stop();
            if (throwSource) throwSource.Stop();
            foreach (var collider in GetComponentsInChildren<Collider>()) collider.enabled = false;
            Defeated?.Invoke();
        }

        private void CancelGifts()
        {
            foreach (var gift in activeGifts) if (gift) gift.Cancel();
            activeGifts.Clear();
        }

        private void OnDisable()
        {
            CancelGifts();
            RestoreIcicles();
            if (laughSource) laughSource.Stop();
            if (throwSource) throwSource.Stop();
        }
        private void OnDestroy() { CancelGifts(); if (arena) Destroy(arena.gameObject); }

        private void OnGUI()
        {
            if (!showHud || !CanSimulate) return;
            if (titleStyle == null)
            {
                titleStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 21, fontStyle = FontStyle.Bold };
                hintStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 16, wordWrap = true };
            }
            float width = Mathf.Min(520, Screen.width - 30), left = (Screen.width - width) / 2;
            const float top = 182;
            GUI.Box(new Rect(left, top, width, 105), GUIContent.none);
            GUI.Label(new Rect(left, top + 3, width, 27), "Похититель подарков", titleStyle);
            Color prior = GUI.color;
            GUI.color = new Color(.18f, .09f, .12f); GUI.DrawTexture(new Rect(left + 20, top + 35, width - 40, 12), Texture2D.whiteTexture);
            GUI.color = new Color(.88f, .22f, .31f); GUI.DrawTexture(new Rect(left + 20, top + 35, (width - 40) * Mathf.Clamp01(currentHP / (float)startingHealth), 12), Texture2D.whiteTexture);
            GUI.color = prior;
            string hint = isDead ? "Сани повержены!" : State == FlightState.Waiting ? "Приблизьтесь — сани сами начнут атаку" :
                State == FlightState.IcicleApproach || State == FlightState.IcicleDrop ? "Три сосульки по линии — уходите в сторону!" :
                State == FlightState.RadialApproach || State == FlightState.RadialBurst ? "Подарки вокруг саней! Отойдите от кругов взрыва" :
                State == FlightState.IcicleRetreat ? "Сани улетают — готовьтесь к следующему заходу" :
                State == FlightState.Recovery || State == FlightState.Descending ? "Двигатель остывает — атакуйте сани!" :
                State == FlightState.Bombardment ? (pattern == 0 ? "Пять красных подарков" : pattern == 1 ? "Пять синих подарков по линии" : "Пять больших золотых подарков") + " — выходите из кругов!" : "Сани заходят на бросок";
            GUI.Label(new Rect(left + 8, top + 53, width - 16, 46), hint, hintStyle);
        }
    }
}
