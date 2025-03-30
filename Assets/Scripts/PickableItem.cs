using System.Drawing;
using TMPro;
using UnityEngine;
using UnityEngine.ProBuilder.Shapes;

public class PickableItem : MonoBehaviour
{
    //[SerializeField] private float _lerpSpeed = 15;
    [SerializeField] private ItemData _itemData;
    [SerializeField] private GameObject _visualObj;

    [SerializeField] private float _lerpSpeed = 10f;
    [SerializeField] private float _maxSpeed = 10f;
    [SerializeField] private float _damping = 0.1f;
    [SerializeField] private float _stopDistance = 0.05f;
    [SerializeField] private float _maxDistance = 8f;
    public bool Grabbed { get; private set; }

    private Outline _outline;
    private Rigidbody _rb;
    private BoxCollider _collider;
    private Transform _itemPoint;
    public bool _inGravitySource;


    private Vector3 _velocity;

    

    public ItemData Data { get { return _itemData; } }

    private void OnEnable()
    {
        _outline = GetComponent<Outline>();
        _collider = GetComponent<BoxCollider>();
        _rb = GetComponent<Rigidbody>();
    }


    public void OnFocus(bool focus)
    {
        _outline.enabled = focus;
    }

    public void PickUp(Transform point)
    {
        //transform.SetParent(point);
        Grabbed = true;
        _itemPoint = point;
        _rb.useGravity = false;
        _rb.freezeRotation = true;

        //_rb.isKinematic = true;

        _rb.drag = 10f;
        _rb.angularDrag = 10f;
    }

    public void Drop()
    {
        //transform.SetParent(null);
        Grabbed = false;
        _itemPoint = null;
        _rb.freezeRotation = false;
        _rb.useGravity = true;
        _rb.velocity = _velocity;

        //_rb.isKinematic = false;

        _rb.drag = 2f; // Reset drag
        _rb.angularDrag = 0.5f; // Default angular drag
    }

    private void FixedUpdate()
    {
        /*        if (_itemPoint != null)
                {
                    Vector3 targetVelocity = _rb.velocity;
                    Vector3 predictedPosition = _itemPoint.position + targetVelocity * Time.fixedDeltaTime;

                    float distance = Vector3.Distance(transform.position, predictedPosition);

                    transform.position = Vector3.SmoothDamp(
                        transform.position,
                        predictedPosition,
                        ref _velocity,
                        damping,
                        _lerpSpeed
                    );
                }*/


        if (Grabbed && _itemPoint != null)
        {

            // Calculate target position and velocity
            Vector3 targetPosition = _itemPoint.position;
            Vector3 positionDelta = targetPosition - transform.position;


            float distance = positionDelta.magnitude;

            if (distance > _maxDistance)
            {
                Grabbed = false;
                return;
            }


                // If close enough, snap to position and stop
                if (distance < _stopDistance)
            {
                transform.position = targetPosition;
                _rb.velocity = Vector3.zero;
                _velocity = Vector3.zero;
                return;
            }

            // Calculate target velocity with distance-based scaling
            Vector3 targetVelocity = positionDelta.normalized * Mathf.Min(distance * _lerpSpeed, _lerpSpeed);
            _velocity = Vector3.Lerp(_velocity, targetVelocity, _damping);

            // Apply additional damping based on distance
            float distanceDamping = Mathf.Clamp01(distance);
            _rb.velocity = Vector3.Lerp(_rb.velocity, _velocity * distanceDamping, Time.fixedDeltaTime * _lerpSpeed);

        }

    }


/*    private void OnGravityChanged(bool inGravitySource)
    {
        _inGravitySource = inGravitySource;
        if (!_inGravitySource) {
            _rb.useGravity = false;
            //_rb.velocity = Vector3.zero;
        } else
        {
            _rb.useGravity = true;
        }
    }*/


    public void PutToInventory()
    {
        if (Inventory.Instance.AddItem(this))
        {
            //SetVisibility(false);
            gameObject.SetActive(false);
        }
    }

    public void DropOutFromInventory(Transform dropOutPoint)
    {
        gameObject.SetActive(true);
        transform.SetParent(null); // TODO:  Objects Parent
        transform.position = dropOutPoint.position;
        //_rb.velocity = dropOutPoint.transform.forward;
    }


}
