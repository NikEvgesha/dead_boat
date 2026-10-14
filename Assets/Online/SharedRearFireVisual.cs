using UnityEngine;
using UnityEngine.UI;

namespace DeadBoat.Online
{
    // Local, bounded effect; gameplay never depends on particles or colliders.
    public sealed class SharedRearFireVisual : MonoBehaviour
    {
        private GameObject fire, canvasObject;
        private ParticleSystem[] particles;
        private Text warning;
        private double refresh;
        private bool visible;

        private void Awake()
        {
            var prefab = Resources.Load<GameObject>("Online/RearFireVisual");
            if (prefab != null)
            {
                fire = Instantiate(prefab, transform);
                fire.name = "Rear fire (local visual only)";
                particles = fire.GetComponentsInChildren<ParticleSystem>(true);
                foreach (var ps in particles)
                {
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    ps.transform.localPosition = Vector3.zero;
                    ps.transform.localRotation = Quaternion.identity;
                    var main = ps.main;
                    main.maxParticles = 96; main.startLifetime = 1.2f;
                    main.startSpeed = 0; main.startSize3D = true;
                    main.startSizeX = new ParticleSystem.MinMaxCurve(1, 2);
                    main.startSizeY = new ParticleSystem.MinMaxCurve(3, 5);
                    main.startSizeZ = 1;
                    main.simulationSpace = ParticleSystemSimulationSpace.Local;
                    main.startColor = new ParticleSystem.MinMaxGradient(new Color(1, .12f, .01f), new Color(1, .7f, .03f));
                    var emission = ps.emission; emission.rateOverTime = 65;
                    var shape = ps.shape; shape.enabled = true; shape.shapeType = ParticleSystemShapeType.Box;
                    shape.scale = new Vector3(80, 1, 2);
                    var velocity = ps.velocityOverLifetime; velocity.enabled = true;
                    velocity.space = ParticleSystemSimulationSpace.Local;
                    velocity.x = new ParticleSystem.MinMaxCurve(0, 0);
                    velocity.y = new ParticleSystem.MinMaxCurve(1, 4);
                    velocity.z = new ParticleSystem.MinMaxCurve(0, 0);
                    var collision = ps.collision; collision.enabled = false;
                    var triggers = ps.trigger; triggers.enabled = false;
                    var lights = ps.lights; lights.enabled = false;
                }
                fire.SetActive(false);
            }
            canvasObject = new GameObject("Rear fire warning", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            canvasObject.transform.SetParent(transform, false);
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.GetComponent<Canvas>().sortingOrder = 950;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720); scaler.matchWidthOrHeight = .5f;
            warning = LobbyOnlineModeUI.CreateText(canvasObject.transform, "Warning", new Vector2(0, 170), new Vector2(680, 85), 24);
            warning.color = new Color(1, .35f, .08f); warning.raycastTarget = false;
            warning.supportRichText = false;
            canvasObject.SetActive(false);
        }

        private void Update()
        {
            var state = SharedRunContext.State;
            var player = PlayerMovement.Instance;
            bool active = SharedRunContext.Playing && state != null && state.BoatInitialized && !state.RunWon && player != null &&
                PlayerStatsManager.Instance != null && PlayerStatsManager.Instance.InGame && PlayerStatsManager.Instance.Health > 0;
            float gap = active ? player.transform.position.z + LobbyNetworkAvatar.Origin.z - state.RearFireZ : float.PositiveInfinity;
            bool show = active && gap < 120;
            if (show != visible)
            {
                visible = show; canvasObject.SetActive(show);
                if (fire != null)
                {
                    fire.SetActive(show);
                    if (show) foreach (var ps in particles) ps.Play();
                }
            }
            if (!show) return;
            // An 80 m patch follows the observer laterally. Far inside the zone,
            // show nearby flames rather than rendering a kilometre-wide wall.
            if (fire != null) fire.transform.position = new Vector3(player.transform.position.x, player.transform.position.y - 1,
                gap < -8 ? player.transform.position.z + 3 : state.RearFireZ - LobbyNetworkAvatar.Origin.z);
            if (Time.realtimeSinceStartupAsDouble < refresh) return;
            refresh = Time.realtimeSinceStartupAsDouble + .2;
            bool en = LocalizationManager.Instance != null && (LocalizationManager.Instance.CurrentLanguage ?? "").StartsWith("en", System.StringComparison.OrdinalIgnoreCase);
            string shortcut = ControlManager.Instance != null && ControlManager.Instance.UseTouchControl ? "" : " · H";
            warning.text = gap < 0
                ? (en ? "Fire! You lose 10 HP every second.\nUse the return to boat button" : "Огонь! Вы теряете 10 HP каждую секунду.\nНажмите кнопку возврата к лодке") + shortcut
                : (en ? "Fire behind you! Return to the boat." : "Позади огонь! Вернитесь к лодке.");
        }
        private void OnDestroy()
        {
            if (fire != null) Destroy(fire);
            if (canvasObject != null) Destroy(canvasObject);
        }
    }
}
