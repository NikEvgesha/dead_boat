using UnityEngine;

[DisallowMultipleComponent]
public sealed class EggFeatureWorldHintArrow : MonoBehaviour
{
    [SerializeField] private Transform _target;
    [SerializeField] private Vector3 _worldOffset = new(0f, 3f, 0f);
    [SerializeField, Min(0f)] private float _bobAmplitude = 0.25f;
    [SerializeField, Min(0f)] private float _bobSpeed = 2.5f;
    [SerializeField] private bool _faceCamera = true;

    private Camera _camera;
    private float _phase;

    private void OnEnable()
    {
        ResolveCamera();
        _phase = Random.value * Mathf.PI * 2f;
        RefreshPosition();
    }

    private void LateUpdate()
    {
        RefreshPosition();

        if (!_faceCamera)
            return;

        if (_camera == null)
            ResolveCamera();

        if (_camera == null)
            return;

        Vector3 directionFromCamera = transform.position - _camera.transform.position;
        if (directionFromCamera.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(directionFromCamera, Vector3.up);
    }

    private void RefreshPosition()
    {
        if (_target == null)
            return;

        float bob = _bobAmplitude > 0f
            ? Mathf.Sin(Time.unscaledTime * _bobSpeed + _phase) * _bobAmplitude
            : 0f;

        transform.position = _target.position + _worldOffset + Vector3.up * bob;
    }

    private void ResolveCamera()
    {
        _camera = Camera.main;
        if (_camera != null)
            return;

        Camera[] cameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
        if (cameras.Length > 0)
            _camera = cameras[0];
    }
}
