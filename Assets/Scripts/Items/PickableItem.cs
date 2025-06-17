using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class PickableItem : MonoBehaviour
{
    //[SerializeField] private float _lerpSpeed = 15;
    [SerializeField] private ItemData _itemData;
    [SerializeField] private GameObject _visualObj;

    [SerializeField] private Transform _sellPoint;

    [SerializeField] private float _lerpSpeed = 10f;
    //[SerializeField] private float _maxSpeed = 10f;
    [SerializeField] private float _damping = 0.1f;
    [SerializeField] private float _stopDistance = 0.05f;
    [SerializeField] private float _maxDistance = 8f;

    [SerializeField] private float _drag = 10f;
    [SerializeField] private float _dragOrigin = 1f;
    [SerializeField] private float _dropVelocityMultiplier = 2f;
    [SerializeField] private float _kinematicDistance = 30f;
    [SerializeField] private float _rotationSpeed = 40f;

    private ItemStatus _status = ItemStatus.Free;
    public ItemStatus Status => _status;
    public bool Grabbed { get; private set; }

    private Outline _outline;
    private Rigidbody _rb;
    [SerializeField] private BoxCollider _collider;
    private Transform _itemPoint;
    private InfoUI _infoUI;
    private Transform _player;
    private ItemAttacher _attacher;
    private bool _attached;

    private InventorySlot _currentSlot;
    public bool Attached => _attached;


    private Vector3 _velocity;

    private HashSet<ItemTag> _tags = new();
    private bool _usable;
    private bool _useKinematicCheck = true;
    private bool _componentsInitialized = false;

    public HashSet<ItemTag> Tags => _tags;
    public ItemData Data { get { return _itemData; } }
    public bool Usable => _usable;

    public Action PickUpItem;
    public Action PutItemToInventory;

    private bool _isFuel;
    private bool _isSellable;
    private bool _isRewarded;

    public bool IsFuel => _isFuel;
    public bool IsSellable => _isSellable;
    public bool IsRewarded => _isRewarded;
   

    private void OnEnable()
    {
        CheckComponents();
        _infoUI.SetInfoText(Data.Name, _tags);
        if (_player != null)
            StartCoroutine(KinematicCheck());

        _isFuel = HaveTag(ItemTag.Fuel);
        _isSellable = HaveTag(ItemTag.Valuable) || HaveTag(ItemTag.Trash);
        _isRewarded = HaveTag(ItemTag.Reward);
    }

    private void Start()
    {
        //_status = ItemStatus.Free;
        _player = PlayerManager.Instance.transform;
        if (_player != null)
            StartCoroutine(KinematicCheck());
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        ControlUI.Instance.HideItemHints();
    }

    public void CheckComponents()
    {
        if (_componentsInitialized) return;
        _outline = GetComponent<Outline>();
        _collider = GetComponent<BoxCollider>();
        _rb = GetComponent<Rigidbody>();
        _infoUI = GetComponent<InfoUI>();
        _attacher = GetComponent<ItemAttacher>();
        CheckTags();
        _componentsInitialized = true;
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
        CheckComponents();
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
        ControlUI.Instance.ShowRotateButtons(Grabbed);
        _outline.OutlineColor = Color.yellow;

        PickUpItem?.Invoke();
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
        ControlUI.Instance.ShowRotateButtons(Grabbed);
        _outline.OutlineColor = Color.white;
        _status = ItemStatus.Free;
    }


    public void PutToInventory()
    {
        CheckComponents();
        if (_status != ItemStatus.Free) return;
        if (_tags.Contains(ItemTag.Ammo))
        {
            if (this.GetComponent<AmmoItem>().AddAmmo())
            {
                Destroy(this.gameObject);
                return;
            }
        }
            if (_collider)
                _collider.enabled = false;

            OnFocus(false);
            tag = Tag.Inventory.ToString();
            this.gameObject.layer = (int)Layer.Inventory;
            _rb.interpolation = RigidbodyInterpolation.None;
            _useKinematicCheck = false;
            TrySetAttach(false);
            _status = ItemStatus.InInventory;
            PutItemToInventory?.Invoke();

    }

    public void DropOutFromInventory(Transform dropOutPoint)
    {
        if (_status != ItemStatus.InInventory) return;

        if (_collider)
            _collider.enabled = true;

        tag = Tag.Item.ToString();
        this.gameObject.layer = (int)Layer.Pickable;
        _rb.interpolation = RigidbodyInterpolation.Interpolate;
        _useKinematicCheck = true;

        transform.SetParent(null);
        transform.position = dropOutPoint.position;

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
                CheckRotation();
                return;
            }

            if (distance < _stopDistance)
            {
                transform.position = targetPosition;
                _rb.velocity = Vector3.zero;
                _velocity = Vector3.zero;
                CheckRotation();
                return;
            }

            Vector3 targetVelocity = positionDelta.normalized * Mathf.Min(distance * _lerpSpeed, _lerpSpeed);
            _velocity = Vector3.Lerp(_velocity, targetVelocity, _damping);

            float distanceDamping = Mathf.Clamp01(distance);
            _rb.velocity = Vector3.Lerp(_rb.velocity, _velocity * distanceDamping, Time.fixedDeltaTime * _lerpSpeed);

            //
            CheckRotation();




        }

    }


    private void CheckRotation() {
        if (PlayerInput.Instance.RotationY)
        {
            transform.RotateAround(transform.position, Vector3.up, Time.fixedDeltaTime * _rotationSpeed);
        }
        if (PlayerInput.Instance.RotationX)
        {
            transform.RotateAround(transform.position, Vector3.right, Time.fixedDeltaTime * _rotationSpeed);
        }
    }


    public void CheckTags()
    {

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

        if (gameObject.TryGetComponent<UsableItem>(out UsableItem usableItem))
        {
            _usable = true;
        }

        if (gameObject.GetComponent<AmmoItem>() != null)
        {
            _tags.Add(ItemTag.Ammo);
        }

        if (gameObject.GetComponent<EnemyReward>())
        {
            _tags.Add(ItemTag.Reward);
        }
        if (GetComponentInChildren<RangedWeaponController>() || GetComponentInChildren<MeleWeapon>())
        {
            _tags.Add(ItemTag.Weapon);
        }
    }

    public void SetKinematic(bool kinematic)
    {
        _rb.isKinematic = kinematic;
        if (_collider)
            _collider.enabled = !kinematic;
    }


    public GameObject GetModel()
    {
        return _visualObj;
    }
    public List<Vector3> GetSellPoint()
    {
        if (_sellPoint == null)
            return new List<Vector3>();

        Vector3 offsetPosition = _sellPoint.localPosition;
        Quaternion offsetRotationModel = _visualObj.transform.rotation;
        Quaternion offsetRotationPoint = _sellPoint.transform.localRotation;
        Vector3 offsetEulerRotation = offsetRotationPoint.eulerAngles;
        Vector3 offsetScale = _sellPoint.localScale;

        List<Vector3> offset = new List<Vector3>
        {
            offsetPosition,
            offsetEulerRotation,
            offsetScale
        };

        return offset;
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
            _outline.OutlineColor = Color.red;
            /*            if (transform.parent != null && transform.parent.TryGetComponent<BoardController>(out BoardController board))
                        {
                            SaveManager.Instance.SaveAttachedItem(Data.Name);
                            SaveManager.Instance.SaveInventory();
                        }*/

            return true;
        } else if (!attach && _status == ItemStatus.Attached)
        {
            SetKinematic(false);
            _useKinematicCheck = true;
            _attached = false;
            //Drop();
            _status = ItemStatus.Free;

            /*            if (transform.parent != null && transform.parent.TryGetComponent<BoardController>(out BoardController board))
                        {
                            SaveManager.Instance.DeleteAttachedItem(Data.Name);
                        }*/
            _outline.OutlineColor = Color.white;
            return true;
        }

            return false;
    }


    private void CheckPossibleActions(bool focus = true)
    {
        switch (_status)
        {
            case ItemStatus.Free:
                ControlUI.Instance.ShowAttachButton(focus && _attacher.InAttachZone);
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
    public void SetSlot( InventorySlot slot)
    {
        _currentSlot = slot;
    }
    public void Useble()
    {
        _currentSlot.UsebleActiveItem();
    }

    private void OnDestroy()
    {
        ControlUI.Instance.ShowAttachButton(false);
        ControlUI.Instance.ShowPickUpButton(false);
        ControlUI.Instance.ShowPutToInventoryButton(false);
    }

    public bool HaveTag(ItemTag tag)
    {
        return _tags.Contains(tag);
    }

}
