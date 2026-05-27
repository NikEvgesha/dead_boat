using UnityEngine;

public class SeeInCamera : MonoBehaviour
{
    [SerializeField] private Camera _mainCamera;
    private Canvas _canvas;
    [SerializeField] int testVector;
    [SerializeField] Vector3 Vector3Test;

    private void OnEnable()
    {
        _canvas = gameObject.GetComponent<Canvas>();
        ResolveMainCamera();

        if (_canvas != null)
            _canvas.worldCamera = _mainCamera;
        
    }
    private void Update()
    {
        if (_mainCamera == null)
            ResolveMainCamera();

        if (_mainCamera == null)
            return;

        Vector3 testV = Vector3.zero;
        switch (testVector)
        {
            case 0:
                gameObject.transform.LookAt(_mainCamera.transform.position);
                Vector3Test = _mainCamera.transform.position;
                return;
            case 1:
                testV = Vector3.left;
                break;
            case 2:
                testV = Vector3.up;
                break;
            case 3:
                testV = Vector3.down;
                break;
            case 4:
                testV = Vector3.forward;
                break;
            case 5:
                testV = Vector3.back;
                break;
            case 6:
                testV = Vector3.one;
                break;
            case 7:
                testV = Vector3.zero;
                break;
            case 8:
                testV = Vector3.forward;
                break;
            case 9:
                testV = Vector3.negativeInfinity;
                break;
            case 10:
                testV = Vector3.positiveInfinity;
                break;
            case 11:
                testV = Vector3.right;
                break;
            case 12:
                FaceCamera();
                return;
            default:
                break;
        }
        gameObject.transform.LookAt(-_mainCamera.transform.position, testV);
        Vector3Test = _mainCamera.transform.position;
    }

    private void ResolveMainCamera()
    {
        _mainCamera = Camera.main;
        if (_mainCamera != null)
            return;

        Camera[] cameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
        foreach (Camera camera in cameras)
        {
            if (camera != null && camera.CompareTag("MainCamera"))
            {
                _mainCamera = camera;
                return;
            }
        }

        if (cameras.Length > 0)
            _mainCamera = cameras[0];
    }

    private void FaceCamera()
    {
        Vector3 directionFromCamera = transform.position - _mainCamera.transform.position;
        if (directionFromCamera.sqrMagnitude <= 0.0001f)
            return;

        transform.rotation = Quaternion.LookRotation(directionFromCamera, Vector3.up);
        Vector3Test = _mainCamera.transform.position;
    }
}
