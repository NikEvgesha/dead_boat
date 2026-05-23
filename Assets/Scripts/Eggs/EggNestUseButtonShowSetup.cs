using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class EggNestUseButtonShowSetup : MonoBehaviour
{
    [SerializeField] private EggNestPoint _nestPoint;
    [SerializeField] private UseButtonShow _useButtonShow;
    [SerializeField] private Canvas _infoCanvas;
    [SerializeField] private BuyTouchHandler _touchPanel;
    [SerializeField] private Image _openProgress;
    [SerializeField] private AudioSource _audioSource;

    private static readonly FieldInfo InfoCanvasField = typeof(UseButtonShow).GetField("_infoCanvas", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo TouchPanelField = typeof(UseButtonShow).GetField("_touchPanel", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo OpenProgressField = typeof(UseButtonShow).GetField("_openProgress", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo SourceField = typeof(UseButtonShow).GetField("_source", BindingFlags.Instance | BindingFlags.NonPublic);

    private void Awake()
    {
        ResolveReferences();
        ResolveUseButtonReferences();
        ConfigureUseButtonShow();
    }

    private void ResolveReferences()
    {
        if (_nestPoint == null)
            _nestPoint = GetComponent<EggNestPoint>();

        if (_useButtonShow == null)
            _useButtonShow = GetComponentInChildren<UseButtonShow>(true);

        if (_audioSource == null)
            _audioSource = GetComponent<AudioSource>();

        if (_audioSource == null)
            _audioSource = gameObject.AddComponent<AudioSource>();
    }

    private void ResolveUseButtonReferences()
    {
        if (_useButtonShow == null)
            return;

        if (_infoCanvas == null)
            _infoCanvas = _useButtonShow.GetComponentInChildren<Canvas>(true);

        if (_touchPanel == null && _infoCanvas != null)
            _touchPanel = _infoCanvas.GetComponentInChildren<BuyTouchHandler>(true);

        if (_openProgress == null && _infoCanvas != null)
            _openProgress = FindProgressImage(_infoCanvas.transform);
    }

    private void ConfigureUseButtonShow()
    {
        if (_useButtonShow == null || _nestPoint == null)
            return;

        InfoCanvasField?.SetValue(_useButtonShow, _infoCanvas);
        TouchPanelField?.SetValue(_useButtonShow, _touchPanel);
        OpenProgressField?.SetValue(_useButtonShow, _openProgress);
        SourceField?.SetValue(_useButtonShow, _audioSource);
        _useButtonShow.UseButtonShowBool = true;

        UnityEvent activate = _useButtonShow.Activate;
        if (activate == null)
        {
            activate = new UnityEvent();
            typeof(UseButtonShow).GetField("Activate", BindingFlags.Instance | BindingFlags.Public)?.SetValue(_useButtonShow, activate);
        }

        activate.RemoveListener(_nestPoint._Use);
        if (!HasPersistentUseListener(activate, _nestPoint))
            activate.AddListener(_nestPoint._Use);
    }

    private static bool HasPersistentUseListener(UnityEvent activate, EggNestPoint nestPoint)
    {
        if (activate == null || nestPoint == null)
            return false;

        for (int i = 0; i < activate.GetPersistentEventCount(); i++)
        {
            if (activate.GetPersistentTarget(i) == nestPoint &&
                activate.GetPersistentMethodName(i) == nameof(EggNestPoint._Use))
            {
                return true;
            }
        }

        return false;
    }

    private static Image FindProgressImage(Transform root)
    {
        if (root == null)
            return null;

        Image[] images = root.GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; i++)
        {
            if (images[i] != null && images[i].type == Image.Type.Filled)
                return images[i];
        }

        return null;
    }

}
