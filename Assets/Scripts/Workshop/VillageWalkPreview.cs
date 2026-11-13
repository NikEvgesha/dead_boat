using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// A standalone inspection scene: no game services or persistent save are started.
[DefaultExecutionOrder(-10000)]
public sealed class VillageWalkPreview : MonoBehaviour
{
    public GameObject leftBankPrefab;
    public GameObject rightBankPrefab;
    public GameObject authoredLocation;
    public Transform ground;
    public Camera overview;
    public CharacterController controllerTemplate;

    readonly List<GameObject> pickups = new();
    GameObject session;
    CharacterController player;
    Camera view;
    int bank;
    float yaw, pitch, vertical;
    Vector3 start;
    float startYaw;
    int collected;

    void OnEnable()
    {
        // Recover the existing scene objects after a live script reload; private
        // runtime references and the readonly list are not serialized by Unity.
        if (!Application.isPlaying) return;
        var existingSession = transform.Find("Village bank walk");
        var existingController = transform.Find("Village walk controller");
        if (!session && existingSession) session = existingSession.gameObject;
        if (!player && existingController) player = existingController.GetComponent<CharacterController>();
        if (!view && player) view = player.GetComponentInChildren<Camera>();
        if (!session || !player || !view) return;
        if (pickups.Count == 0)
            pickups.AddRange(session.transform.Cast<Transform>()
                .Where(t => t.GetComponent<PickableItem>()).Select(t => t.gameObject));
        var location = session.GetComponentInChildren<LocationContentSpawner>();
        bank = location && location.name.Contains("RightBank") ? 1 : 0;
        collected = pickups.Count(p => p && !p.activeSelf);
        yaw = player.transform.eulerAngles.y;
        pitch = Mathf.DeltaAngle(0, view.transform.localEulerAngles.x);
        vertical = 0;
        startYaw = bank == 0 ? 270 : 0;
        var bounds = ground.GetComponent<Renderer>().bounds;
        start = bank == 0 ? new Vector3(bounds.max.x - 11, .3f, 8) : new Vector3(77.24f, .3f, bounds.min.z + 11);
    }

    void Awake()
    {
        if (authoredLocation) authoredLocation.SetActive(false);
        if (overview) overview.enabled = false;
        RenderSettings.fog = false;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(.65f, .70f, .75f);
        var walker = new GameObject("Village walk controller");
        walker.transform.SetParent(transform, false);
        player = walker.AddComponent<CharacterController>();
        player.height = controllerTemplate.height;
        player.radius = controllerTemplate.radius;
        player.center = controllerTemplate.center;
        player.stepOffset = controllerTemplate.stepOffset;
        player.skinWidth = controllerTemplate.skinWidth;
        player.slopeLimit = controllerTemplate.slopeLimit;
        view = new GameObject("Village walk camera").AddComponent<Camera>();
        view.transform.SetParent(walker.transform, false);
        view.transform.localPosition = Vector3.up * 1.6f;
        view.nearClipPlane = .05f;
        view.farClipPlane = 250;
        view.fieldOfView = 70;
        view.clearFlags = CameraClearFlags.SolidColor;
        view.backgroundColor = new Color(.48f, .63f, .72f);
        view.gameObject.AddComponent<AudioListener>();
        SetBank(0);
    }

    void Start()
    {
        var diagnostics = FindAnyObjectByType<DeadBoat.Online.MenuInputDiagnostics>();
        if (diagnostics) diagnostics.enabled = false;
    }

    public void SetBank(int index)
    {
        if (session) { session.SetActive(false); Destroy(session); }
        bank = index == 0 ? 0 : 1;
        pickups.Clear(); collected = 0;
        session = new GameObject("Village bank walk");
        session.transform.SetParent(transform, false);
        session.SetActive(false);
        var location = Instantiate(bank == 0 ? leftBankPrefab : rightBankPrefab, session.transform);
        location.name = bank == 0 ? "Village_LeftBank" : "Village_RightBank";
        // Retain the gameplay configuration in the actual prefabs. The walk only
        // displays their geometry and loot, without invoking enemy/save systems.
        foreach (var behaviour in location.GetComponentsInChildren<MonoBehaviour>(true))
            if (behaviour) behaviour.enabled = false;
        var content = location.GetComponent<LocationContentSpawner>();
        var random = new DeadBoat.Online.RunRandom(255, "village-walk", bank);
        foreach (var point in content.itemSpawnPoints)
        {
            var prefab = content.itemCollection.GetRandomItem(point.allowedType, point.allowedSize, random);
            if (!prefab || !point.spawnTransform) continue;
            var model = Instantiate(prefab.gameObject, point.spawnTransform.position, point.spawnTransform.rotation, session.transform);
            LocationContentSpawner.PlaceItemAtSpawnPoint(model, point);
            foreach (var behaviour in model.GetComponentsInChildren<MonoBehaviour>(true))
                if (behaviour) behaviour.enabled = false;
            foreach (var collider in model.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            foreach (var body in model.GetComponentsInChildren<Rigidbody>(true)) { body.isKinematic = true; body.useGravity = false; }
            foreach (var light in model.GetComponentsInChildren<Light>(true)) light.enabled = false;
            pickups.Add(model);
        }
        var renderers = location.GetComponentsInChildren<MeshRenderer>().Where(r => r.enabled).ToArray();
        var bounds = renderers[0].bounds;
        foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
        ground.position = new Vector3(bounds.center.x, -.5f, bounds.center.z);
        ground.localScale = new Vector3(bounds.size.x + 20, 1, bounds.size.z + 20);
        start = bank == 0 ? new Vector3(bounds.max.x - 1, .3f, 8) : new Vector3(77.24f, .3f, bounds.min.z + 1);
        startYaw = bank == 0 ? 270 : 0;
        session.SetActive(true);
        ResetPosition();
    }

    public void ResetPosition()
    {
        player.enabled = false;
        player.transform.SetPositionAndRotation(start, Quaternion.Euler(0, startYaw, 0));
        player.enabled = true;
        yaw = startYaw; pitch = vertical = 0;
        view.transform.localRotation = Quaternion.identity;
        Physics.SyncTransforms();
    }

    void Update()
    {
        if (!player || !view) return;
        if (Input.GetMouseButton(1))
        {
            yaw += Input.GetAxis("Mouse X") * 2.4f;
            pitch = Mathf.Clamp(pitch - Input.GetAxis("Mouse Y") * 2.4f, -80, 80);
            player.transform.rotation = Quaternion.Euler(0, yaw, 0);
            view.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
        }
        if (player.isGrounded) vertical = Input.GetKeyDown(KeyCode.Space) ? 5.2f : -1.2f;
        else vertical -= 19 * Time.deltaTime;
        var direction = Vector3.ClampMagnitude(new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")), 1);
        float speed = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ? 7 : 4;
        player.Move((player.transform.TransformDirection(direction) * speed + Vector3.up * vertical) * Time.deltaTime);
        if (player.transform.position.y < -3) ResetPosition();
        if (Input.GetKeyDown(KeyCode.E))
        {
            var item = Nearby();
            if (item) { item.SetActive(false); collected++; }
        }
    }

    GameObject Nearby() => !player ? null : pickups.Where(p => p && p.activeSelf)
        .Where(p => Vector3.Distance(p.transform.position, player.transform.position + Vector3.up * .8f) < 2.2f)
        .OrderBy(p => (p.transform.position - player.transform.position).sqrMagnitude).FirstOrDefault();

    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(12, 12, 700, 122), GUI.skin.box);
        GUILayout.Label("Прогулка по деревне · " + (bank == 0 ? "левый берег" : "правый берег") + " · без боя");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Левый берег") && bank != 0) SetBank(0);
        if (GUILayout.Button("Правый берег") && bank != 1) SetBank(1);
        if (GUILayout.Button("К началу")) ResetPosition();
        GUILayout.EndHorizontal();
        GUILayout.Label("WASD — идти · Shift — бег · ПКМ + мышь — обзор · Пробел — прыжок");
        GUILayout.Label("Собрано " + collected + " / " + pickups.Count + (Nearby() ? " · E — подобрать предмет" : " · E — подбор рядом с предметом"));
        GUILayout.EndArea();
    }
}
