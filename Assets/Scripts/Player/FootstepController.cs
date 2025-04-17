using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(CharacterController))]
public class FootstepController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private AudioSource audioSource;                 // AudioSource, на котором будут играться шаги
    [SerializeField] private CharacterController controller;          // CharacterController игрока

    [Header("Settings")]
    [SerializeField] private float stepDistance = 2f;                 // дистанция между шагами (в метрах)
    [SerializeField] private float raycastDistance = 1.5f;            // длина луча вниз
    [SerializeField] private LayerMask groundMask;                    // слой для «земли»

    [Header("Pitch & Volume Variation")]
    [SerializeField][Range(0.6f, 1f)] private float minPitch = 0.95f;
    [SerializeField][Range(1f, 1.5f)] private float maxPitch = 1.05f;
    [SerializeField][Range(0.5f, 1f)] private float minVolume = 0.8f;
    [SerializeField][Range(0.5f, 1f)] private float maxVolume = 1f;

    [Header("Footstep Clips")]
    [SerializeField] private List<AudioClip> defaultClips;            // когда тип поверхности неизвестен
    [SerializeField] private List<AudioClip> woodClips;
    [SerializeField] private List<AudioClip> stoneClips;
    [SerializeField] private List<AudioClip> dirtClips;
    [SerializeField] private List<AudioClip> metalClips;

    private float accumulatedDistance = 0f;
    private Vector3 lastPosition;

    private void Start()
    {
        if (!controller) controller = GetComponent<CharacterController>();
        lastPosition = transform.position;
    }

    private void Update()
    {
        if (PlayerInput.Instance)
            if (PlayerInput.Instance.InTrain())
                return;
        // считаем, сколько метров прошли с прошлого кадра
        float delta = Vector3.Distance(transform.position, lastPosition);
        accumulatedDistance += delta;
        lastPosition = transform.position;

        // если на земле и прошли достаточно
        if (controller.isGrounded && accumulatedDistance >= stepDistance)
        {
            PlayFootstep();
            accumulatedDistance = 0f;
        }
    }

    private void PlayFootstep()
    {
        // 1) Raycast вниз
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, raycastDistance, groundMask))
        {
            // 2) Определяем список клипов по тегу или по PhysicsMaterial
            var mat = hit.collider.sharedMaterial;
            List<AudioClip> clipList = defaultClips;

            // Вариант A: по тегу
            switch (hit.collider.tag)
            {
                case "Wood": clipList = woodClips; break;
                case "Stone": clipList = stoneClips; break;
                case "Dirt": clipList = dirtClips; break;
                case "Metal": clipList = metalClips; break;
            }

            // Вариант B (альтернатива): по имени PhysicsMaterial
            // if (mat != null)
            // {
            //     if (mat.name.Contains("Wood"))  clipList = woodClips;
            //     else if (mat.name.Contains("Stone")) clipList = stoneClips;
            //     // и т.д.
            // }

            if (clipList == null || clipList.Count == 0) return;

            // 3) Выбираем рандомный клип
            AudioClip clip = clipList[Random.Range(0, clipList.Count)];

            // 4) Варьируем pitch и volume
            audioSource.pitch = Random.Range(minPitch, maxPitch);
            float volume = Random.Range(minVolume, maxVolume);

            // 5) Проигрываем
            audioSource.PlayOneShot(clip, volume);

            // 6) Сброс pitch, чтобы не влиял на другие звуки
            //audioSource.pitch = 1f;
        }
    }
}
