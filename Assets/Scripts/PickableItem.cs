using System.Drawing;
using TMPro;
using UnityEngine;

public class PickableItem : MonoBehaviour
{
    //[SerializeField] private float _lerpSpeed = 15;
    [SerializeField] private ItemData _itemData;
    [SerializeField] private GameObject _visualObj;

    [SerializeField] private float _lerpSpeed = 10f;
    [SerializeField] private float _maxSpeed = 10f;

    public float damping = 0.1f;
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
    }

    public void Drop()
    {
        //transform.SetParent(null);
        Grabbed = false;
        _itemPoint = null;
        _rb.freezeRotation = false;
        //_rb.isKinematic = false;
        _rb.useGravity = true;
        _rb.velocity = _velocity;
    }

    private void FixedUpdate()
    {
        if (_itemPoint != null)
        {
            Vector3 targetVelocity = _rb.velocity;
            Vector3 predictedPosition = _itemPoint.position + targetVelocity * Time.deltaTime;

            float distance = Vector3.Distance(transform.position, predictedPosition);
            

            transform.position = Vector3.SmoothDamp(
                transform.position,
                predictedPosition,
                ref _velocity,
                damping,
                _lerpSpeed
            );
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


}
