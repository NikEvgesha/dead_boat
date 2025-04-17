using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(NavMeshAgent))]
public class ZombieFootstepController : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float stepDistance = 1.8f;       // расстояние между шагами
    [SerializeField] private float raycastDistance = 1.2f;    // длина луча вниз
    [SerializeField] private LayerMask groundMask;            // слой «земли»

    [Header("Pitch & Volume Variation")]
    [SerializeField][Range(0.8f, 1.2f)] private float minPitch = 0.9f;
    [SerializeField][Range(0.8f, 1.2f)] private float maxPitch = 1.1f;
    [SerializeField][Range(0.6f, 1f)] private float minVolume = 0.7f;
    [SerializeField][Range(0.6f, 1f)] private float maxVolume = 1f;

    [Header("Footstep Clips")]
    [SerializeField] private List<AudioClip> defaultClips;
    [SerializeField] private List<AudioClip> woodClips;
    [SerializeField] private List<AudioClip> stoneClips;
    [SerializeField] private List<AudioClip> dirtClips;
    [SerializeField] private List<AudioClip> metalClips;

    private AudioSource audioSource;
    private NavMeshAgent agent;
    private float accumulatedDistance = 0f;
    private Vector3 lastPosition;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        agent = GetComponent<NavMeshAgent>();
        audioSource.spatialBlend = 1f;   // 3D-звук
    }

    private void Start()
    {
        lastPosition = transform.position;
    }

    private void Update()
    {
        // если зомби стоит на месте — не считаем
        if (agent.velocity.magnitude < 0.1f)
        {
            accumulatedDistance = 0f;
            lastPosition = transform.position;
            return;
        }

        // считаем пройденный путь
        float delta = Vector3.Distance(transform.position, lastPosition);
        accumulatedDistance += delta;
        lastPosition = transform.position;

        if (accumulatedDistance >= stepDistance)
        {
            PlayFootstep();
            accumulatedDistance = 0f;
        }
    }

    private void PlayFootstep()
    {
        // Raycast вниз
        if (!Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, out RaycastHit hit, raycastDistance, groundMask))
            return;

        // Выбираем список по тегу
        List<AudioClip> clips = defaultClips;
        switch (hit.collider.tag)
        {
            case "Wood": clips = woodClips; break;
            case "Stone": clips = stoneClips; break;
            case "Dirt": clips = dirtClips; break;
            case "Metal": clips = metalClips; break;
        }

        if (clips == null || clips.Count == 0) return;

        // Случайный клип
        AudioClip clip = clips[Random.Range(0, clips.Count)];

        // Варьируем pitch и volume напрямую
        float originalPitch = audioSource.pitch;
        float originalVol = audioSource.volume;
        audioSource.pitch = Random.Range(minPitch, maxPitch);
        audioSource.volume = Random.Range(minVolume, maxVolume);

        audioSource.PlayOneShot(clip);

        // Восстанавливаем
        audioSource.pitch = originalPitch;
        audioSource.volume = originalVol;
    }
}
