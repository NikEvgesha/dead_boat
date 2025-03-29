using UnityEngine;

public class PickableItem : MonoBehaviour
{
    //[SerializeField] private float _lerpSpeed = 15;
    [SerializeField] private ItemData _itemData;
    [SerializeField] private GameObject _visualObj;
    public bool Grabbed { get; private set; }

    private Outline _outline;
    private Rigidbody _rb;
    private BoxCollider _collider;
    private Transform _itemPoint;
    public bool _inGravitySource;

    

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
        
        Grabbed = true;
        _itemPoint = point;
        _rb.useGravity = false;
        _rb.freezeRotation = true;
    }

    public void Drop()
    {
        Grabbed = false;
        _itemPoint = null;
        _rb.freezeRotation = false;
        if (_inGravitySource)
        {
            _rb.useGravity = true;
        } else
        {
            //_rb.velocity = Vector3.zero;
        }
        
        
    }

    [SerializeField] private float _lerpSpeed = 5f;      // Speed factor for movement
    [SerializeField] private float _maxSpeed = 10f;      // Cap on maximum speed
    [SerializeField] private float _dampingFactor = 10f; // Controls smoothness
    [SerializeField] private float _stopDistance = 0.1f;

    private void FixedUpdate()
    {
        if (_itemPoint != null)
        {
            Vector3 targetPosition = _itemPoint.position;
            Vector3 currentPosition = transform.position;
            Vector3 direction = (targetPosition - currentPosition).normalized;
            float distance = Vector3.Distance(currentPosition, targetPosition);

            // Smoothly adjust velocity based on distance
            float speed = Mathf.Clamp(distance * _lerpSpeed, 0f, _maxSpeed);
            Vector3 targetVelocity = direction * speed;

            // Apply velocity with damping to prevent overshooting
            _rb.velocity = Vector3.Lerp(_rb.velocity, targetVelocity, Time.fixedDeltaTime * _dampingFactor);

            // Optional: Stop movement if very close to target to prevent micro-twitching
            if (distance < _stopDistance)
            {
                _rb.velocity = Vector3.zero;
            }
        }
    }


    private void OnGravityChanged(bool inGravitySource)
    {
        _inGravitySource = inGravitySource;
        if (!_inGravitySource) {
            _rb.useGravity = false;
            //_rb.velocity = Vector3.zero;
        } else
        {
            _rb.useGravity = true;
        }
    }


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
    }


    /*    public void SetVisibility(bool visible)
        {
            _visualObj.SetActive(visible);
            _rb.isKinematic = !visible;
            _rb.detectCollisions = visible;
            _collider.enabled = visible;
            _gravityChecker.enabled = visible;
        }*/

}
