using System;
using System.Collections.Generic;
using UnityEngine;


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

    [SerializeField] private float _drag = 10f;
    [SerializeField] private float _dragOrigin = 1f;
    [SerializeField] private float _dropVelocityMultiplier = 2f;

    public bool Grabbed { get; private set; }

    private Outline _outline;
    private Rigidbody _rb;
    private BoxCollider _collider;
    private Transform _itemPoint;
    private InfoUI _infoUI;
    public bool _inGravitySource;


    private Vector3 _velocity;

    private HashSet<ItemTag> _tags;
    private bool _usable;


    public HashSet<ItemTag> Tags => _tags;
    public ItemData Data { get { return _itemData; } }
    public bool Usable => _usable;


    private void OnEnable()
    {
        _outline = GetComponent<Outline>();
        _collider = GetComponent<BoxCollider>();
        _rb = GetComponent<Rigidbody>();
        _infoUI = GetComponent<InfoUI>();
        CheckTags();
        _infoUI.SetInfoText(Data.Name, _tags);
    }



    public void OnFocus(bool focus)
    {
        _outline.enabled = focus;
        _infoUI.ShowInfo(focus);
    }

    public void PickUp(Transform point)
    {
        //transform.SetParent(point);
        Grabbed = true;
        _itemPoint = point;
        _rb.useGravity = false;
        _rb.freezeRotation = true;

        //_rb.isKinematic = true;

        _rb.drag = _drag;
        _rb.angularDrag = _drag;
        OnFocus(false);
    }

    public void Drop()
    {
        //transform.SetParent(null);
        Grabbed = false;
        _itemPoint = null;
        _rb.freezeRotation = false;
        _rb.useGravity = true;
        _rb.velocity = _velocity * _dropVelocityMultiplier;

        //_rb.isKinematic = false;

        _rb.drag = _dragOrigin; // Reset drag
        _rb.angularDrag = 0.5f; // Default angular drag
    }


    public void PutToInventory()
    {
        if (Inventory.Instance.AddItem(this))
        {
            //SetVisibility(false);
            //gameObject.SetActive(false);
        }
    }

    public void DropOutFromInventory(Transform dropOutPoint)
    {
        gameObject.SetActive(true);
        transform.SetParent(null); // TODO:  Objects Parent
        transform.position = dropOutPoint.position;
        //_rb.velocity = dropOutPoint.transform.forward;
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



    private void CheckTags()
    {
        _tags = new();

        if (gameObject.GetComponent<FuelItem>() != null)
        {
            _tags.Add(ItemTag.Fuel);
        }

        if (gameObject.TryGetComponent<SellableItem>(out SellableItem sellableItem))
        {
            if (sellableItem.Cost < 10)
                _tags.Add(ItemTag.Trash);
            else
                _tags.Add(ItemTag.Valuable);
        }

        if (gameObject.TryGetComponent <UsableItem>(out UsableItem usableItem))
        {
            _usable = true;
        }

    }

    public void SetKinematic(bool kinematic)
    {
        _rb.isKinematic = kinematic;
    }


    public GameObject GetModel()
    {
        return _visualObj;
    }

}
