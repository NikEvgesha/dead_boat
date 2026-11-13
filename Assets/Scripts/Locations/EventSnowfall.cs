using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(ParticleSystem))]
public sealed class EventSnowfall : MonoBehaviour
{
    private ParticleSystem flakes;
    private Camera observer;
    private Vector3 lastPosition;

    private void Awake() => flakes = GetComponent<ParticleSystem>();

    private void LateUpdate()
    {
        if (!observer || !observer.isActiveAndEnabled)
            observer = Camera.main;
        if (!observer) return;

        var position = observer.transform.position + Vector3.up * 9f;
        if (flakes.isPlaying && (position - lastPosition).sqrMagnitude > 45f * 45f)
            flakes.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
        transform.position = position;
        lastPosition = position;

        if (!flakes.isPlaying)
            flakes.Play();
    }
}
