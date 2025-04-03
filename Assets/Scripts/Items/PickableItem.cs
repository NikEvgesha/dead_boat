using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
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
    [SerializeField] private float _kinematicDistance = 30f;

    private ItemStatus _status;
    public ItemStatus Status => _status;
    public bool Grabbed { get; private set; }

    private Outline _outline;
    private Rigidbody _rb;
    private BoxCollider _collider;
    private Transform _itemPoint;
    private InfoUI _infoUI;
    private Transform _player;
    private ItemAttacher _attacher;
    private bool _attached;

    public bool Attached => _attached;


    private Vector3 _velocity;

    private HashSet<ItemTag> _tags;
    private bool _usable;
    private bool _useKinematicCheck = true;

    public HashSet<ItemTag> Tags => _tags;
    public ItemData Data { get { return _itemData; } }
    public bool Usable => _usable;


    private void OnEnable()
    {
        _outline = GetComponent<Outline>();
        _collider = GetComponent<BoxCollider>();
        _rb = GetComponent<Rigidbody>();
        _infoUI = GetComponent<InfoUI>();
        _attacher = GetComponent<ItemAttacher>();
        CheckTags();
        _infoUI.SetInfoText(Data.Name, _tags);
        if (_player != null)
            StartCoroutine(KinematicCheck());
    }

    private void Start()
    {
        _status = ItemStatus.Free;
        _player = GameManager.Instance.Player.transform;
        if (_player != null)
            StartCoroutine(KinematicCheck());
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }

    private IEnumerator KinematicCheck()
    {
        while (this.enabled)
        {
            yield return new WaitForSeconds(1);
            if (!_useKinematicCheck) continue;

            float distance = (transform.position - _player.position).magnitude;
            if (distance < _kinematicDistance && _rb.isKinematic)
                _rb.isKinematic = false;
            else if (distance > _kinematicDistance && !_rb.isKinematic)
                _rb.isKinematic = true;
            
        }
        yield return null;
    }


    public void OnFocus(bool focus)
    {
        _outline.enabled = focus;
        _infoUI.ShowInfo(focus);
        CheckPossibleActions(focus);
    }

    public void PickUp(Transform point)
    {
        if (_status != ItemStatus.Free) return;

        Grabbed = true;
        _itemPoint = point;
        _rb.useGravity = false;
        _rb.freezeRotation = true;
        _useKinematicCheck = false;
        _rb.drag = _drag;
        _rb.angularDrag = _drag;
        OnFocus(false);
        _attacher.CanAttach += CanAttachChange;

        _status = ItemStatus.Grabbed;
    }


    private void CanAttachChange(bool canAttach)
    {
        CheckPossibleActions();
    }

    public void Drop()
    {
        if (_status != ItemStatus.Grabbed && _status != ItemStatus.Attached) return;

        Grabbed = false;
        _itemPoint = null;
        _rb.freezeRotation = false;
        _rb.useGravity = true;
        _rb.velocity = _velocity * _dropVelocityMultiplier;
        _useKinematicCheck = true;
        _rb.drag = _dragOrigin;
        _rb.angularDrag = 0.5f;
        _attacher.CanAttach -= CanAttachChange;

        _status = ItemStatus.Free;
    }


    public void PutToInventory()
    {
        if (_status != ItemStatus.Free) return;

        if (Inventory.Instance.AddItem(this))
        {
            _useKinematicCheck = false;
            TrySetAttach(false);
            _status = ItemStatus.InInventory;
        }

    }

    public void DropOutFromInventory(Transform dropOutPoint)
    {
        if (_status != ItemStatus.InInventory) return;

        gameObject.SetActive(true);
        transform.SetParent(null);
        transform.position = dropOutPoint.position;
        _useKinematicCheck = true;

        _status = ItemStatus.Free;
    }

    private void FixedUpdate()
    {
        if (_status == ItemStatus.Grabbed && _itemPoint != null)
        {
            Vector3 targetPosition = _itemPoint.position;
            Vector3 positionDelta = targetPosition - transform.position;

            float distance = positionDelta.magnitude;

            if (distance > _maxDistance)
            {
                Grabbed = false;
                return;
            }

            if (distance < _stopDistance)
            {
                transform.position = targetPosition;
                _rb.velocity = Vector3.zero;
                _velocity = Vector3.zero;
                return;
            }

            Vector3 targetVelocity = positionDelta.normalized * Mathf.Min(distance * _lerpSpeed, _lerpSpeed);
            _velocity = Vector3.Lerp(_velocity, targetVelocity, _damping);

            float distanceDamping = Mathf.Clamp01(distance);
            _rb.velocity = Vector3.Lerp(_rb.velocity, _velocity * distanceDamping, Time.fixedDeltaTime * _lerpSpeed);

        }

    }


    private void CheckTags()
    {
        _tags = new();

        if (gameObject.GetComponent<FuelItem>() != null)
        {
            _tags.Add(ItemTag.Fuel);
        }

        if (gameObject.TryGetComponent<SellableItem>(out SellableItem sellableItem))
        {
            if (sellableItem.GetReward() < 10)
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


    public bool TrySetAttach(bool attach)
    {

        if (attach && _status != ItemStatus.Attached && _attacher.InAttachZone)
        {
            
            _attached = true;
            Drop();
            SetKinematic(true);
            _status = ItemStatus.Attached;
            _useKinematicCheck = false;
            return true;
        } else if (!attach && _status == ItemStatus.Attached)
        {
            SetKinematic(false);
            _useKinematicCheck = true;
            _attached = false;
            //Drop();
            _status = ItemStatus.Free;
            return true;
        }
        return false;
    }


    private void CheckPossibleActions(bool focus = true)
    {
        switch (_status)
        {
            case ItemStatus.Free:
                ControlUI.Instance.ShowAttachButton(focus);
                ControlUI.Instance.ShowPickUpButton(focus);
                ControlUI.Instance.ShowPutToInventoryButton(focus);
                break;
            case ItemStatus.Grabbed:
                ControlUI.Instance.ShowAttachButton(_attacher.InAttachZone);
                ControlUI.Instance.ShowPickUpButton(true);
                ControlUI.Instance.ShowPutToInventoryButton(false);
                break;
            case ItemStatus.InInventory:
                ControlUI.Instance.ShowAttachButton(false);
                ControlUI.Instance.ShowPickUpButton(false);
                ControlUI.Instance.ShowPutToInventoryButton(false);
                break;
            case ItemStatus.Attached:
                ControlUI.Instance.ShowAttachButton(focus);
                ControlUI.Instance.ShowPickUpButton(false);
                ControlUI.Instance.ShowPutToInventoryButton(false);
                break;
            default: break;
        }
    }



}
