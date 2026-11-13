using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FootstepController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private CharacterController controller;
    [Header("Settings")]
    [SerializeField] private float stepDistance = 2f;
    [SerializeField] private float raycastDistance = 1.5f;
    [SerializeField] private LayerMask groundMask;
    [Header("Pitch & Volume Variation")]
    [SerializeField, Range(.6f, 1f)] private float minPitch = .95f;
    [SerializeField, Range(1f, 1.5f)] private float maxPitch = 1.05f;
    [SerializeField, Range(.5f, 1f)] private float minVolume = .8f;
    [SerializeField, Range(.5f, 1f)] private float maxVolume = 1f;
    [Header("Footstep Clips")]
    [SerializeField] private List<AudioClip> defaultClips;
    [SerializeField] private List<AudioClip> woodClips, stoneClips, dirtClips, metalClips;

    private readonly RaycastHit[] surfaceHits = new RaycastHit[24];
    private Vector3 lastPosition;
    private Transform lastParent;
    private float accumulatedDistance;
    public int StepsPlayed { get; private set; }

    private void Start()
    {
        if (!controller) controller = GetComponent<CharacterController>();
        if (!audioSource) audioSource = GetComponent<AudioSource>();
        ResetDistance();
    }

    private void ResetDistance()
    {
        lastParent = transform.parent;
        lastPosition = transform.localPosition;
        accumulatedDistance = 0;
    }

    private void Update()
    {
        if (!controller || !controller.enabled || Time.deltaTime <= 0 ||
            (PlayerInput.Instance && PlayerInput.Instance.InTrain()))
        { ResetDistance(); return; }

        if (lastParent != transform.parent) { ResetDistance(); return; }
        Vector3 displacement = transform.parent
            ? transform.parent.TransformVector(transform.localPosition - lastPosition)
            : transform.position - lastPosition;
        lastPosition = transform.localPosition;
        // Horizontal travel avoids extra steps from gravity, stair risers and teleports.
        float distance = new Vector2(displacement.x, displacement.z).magnitude;
        if (!controller.isGrounded || distance > Mathf.Max(3, stepDistance * 2))
        { accumulatedDistance = 0; return; }
        accumulatedDistance += distance;
        if (accumulatedDistance < Mathf.Max(.1f, stepDistance)) return;
        if (PlayFootstep()) accumulatedDistance %= Mathf.Max(.1f, stepDistance);
    }

    private bool TryGetSurface(out RaycastHit surface)
    {
        surface = default;
        // Imported POI solids use Ground, Default and Location. Probe the actual support.
        var feet = new Vector3(controller.bounds.center.x, controller.bounds.min.y, controller.bounds.center.z);
        int count = Physics.RaycastNonAlloc(feet + Vector3.up * .35f, Vector3.down, surfaceHits,
            Mathf.Max(.6f, raycastDistance), Physics.DefaultRaycastLayers | groundMask.value,
            QueryTriggerInteraction.Ignore);
        float nearest = float.PositiveInfinity;
        float normalY = Mathf.Cos(Mathf.Min(85, controller.slopeLimit + 5) * Mathf.Deg2Rad);
        for (int i = 0; i < count; i++)
        {
            var hit = surfaceHits[i];
            if (!hit.collider || hit.collider.transform.IsChildOf(transform) ||
                hit.collider.GetComponentInParent<EnemyCore>() || hit.normal.y < normalY || hit.distance >= nearest) continue;
            nearest = hit.distance;
            surface = hit;
        }
        return nearest < float.PositiveInfinity;
    }

    private bool PlayFootstep()
    {
        if (!audioSource || !TryGetSurface(out var hit)) return false;
        List<AudioClip> clips = defaultClips;
        switch (hit.collider.tag)
        {
            case "Wood": clips = woodClips; break;
            case "Stone": clips = stoneClips; break;
            case "Dirt": clips = dirtClips; break;
            case "Metal": clips = metalClips; break;
        }
        if (clips == null || clips.Count == 0) clips = defaultClips;
        if (clips == null || clips.Count == 0) return false;
        var clip = clips[Random.Range(0, clips.Count)];
        if (!clip) return false;
        audioSource.pitch = Random.Range(minPitch, maxPitch);
        audioSource.PlayOneShot(clip, Random.Range(minVolume, maxVolume));
        StepsPlayed++;
        return true;
    }
}