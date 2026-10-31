using System;
using System.Globalization;
using DG.Tweening;
using Dino.UtilityTools.Singleton;
using UnityEngine;

public class CameraManager : Singleton<CameraManager>
{
    [Header("Mobile Settings")]
    [SerializeField] private float mobileSize = 2.8f;
    [SerializeField] private Vector3 mobilePosition = new Vector3(0, 0, 0);
    [Header("Tablet Settings")]
    [SerializeField] private float tabletSize = 2.0f;
    [Header("Desktop Settings")]
    [SerializeField] private float desktopSize = 1f;
    [SerializeField] private Vector3 desktopPosition = new Vector3(0, 0, 0);
    [Header("Web Scroll Settings")]
    [SerializeField, Min(1f)] private float pixelsPerUnityUnit = 400f;
   
    [SerializeField] private Camera _mainCamera;
    private Vector3 initialCameraPosition;
    private float currentWebScrollPixels;
    private float appliedWebScrollOffset;
    private bool hasReceivedWebScroll;
    
    public Camera MainCamera => _mainCamera;

    protected override void Awake()
    {
        base.Awake();
        
    }

    void Start()
    {
        Initialize();
        ResponsiveManager.Instance.OnScreenSizeChanged.AddListener(Initialize);
    }

    void Initialize()
    {
        _mainCamera = Camera.main;
        if(_mainCamera == null)
        {
            Debug.LogError("Main Camera not found in the scene.");
            return;
        }

        Vector3 positionWithoutWebScroll = _mainCamera.transform.position;
        positionWithoutWebScroll.y += appliedWebScrollOffset;
        
        DeviceType deviceType = ResponsiveManager.Instance.CurrentDeviceType;
        switch (deviceType) 
        {
            case DeviceType.Mobile:
                SetCameraSize(mobileSize);
                SetCameraPosition(mobilePosition);
                break;
            case DeviceType.Tablet:
                SetCameraSize(tabletSize);
                SetCameraPosition(positionWithoutWebScroll);
                break;
            case DeviceType.Desktop:
                SetCameraSize(desktopSize);
                SetCameraPosition(desktopPosition);
                break;
            default:
                Debug.LogWarning("Unknown device type. Using default camera size.");
                SetCameraSize(desktopSize);
                SetCameraPosition(desktopPosition);
                break;
        }

        initialCameraPosition = _mainCamera.transform.position;
        ApplyWebScrollPosition();
        
        Debug.Log($"Camera orthographic size set to {_mainCamera.orthographicSize} for device type {deviceType}");
        
    }
    
    public void SetCameraSize(float size)
    {
        if(_mainCamera != null)
        {
            _mainCamera.orthographicSize = size;
            Debug.Log($"Camera orthographic size manually set to {size}");
        }
        else
        {
            Debug.LogError("Main Camera not found. Cannot set size.");
        }
    }
    
    public void SetCameraPosition(string positionString)
    {
        string[] parts = positionString.Split(',');
        if (parts.Length != 3)
        {
            Debug.LogError($"Invalid position string: {positionString}. Expected format: 'x,y,z'");
            return;
        }

        if (float.TryParse(parts[0], out float x) &&
            float.TryParse(parts[1], out float y) &&
            float.TryParse(parts[2], out float z))
        {
            SetCameraPosition(new Vector3(x, y, z));
        }
        else
        {
            Debug.LogError($"Failed to parse position string: {positionString}");
        }
    }

    public void SetCameraPositionFromWebScroll(string scrollPixelsString)
    {
        if (!float.TryParse(scrollPixelsString, NumberStyles.Float, CultureInfo.InvariantCulture, out float scrollPixels)
            || float.IsNaN(scrollPixels) || float.IsInfinity(scrollPixels))
        {
            Debug.LogWarning($"Invalid web scroll position: {scrollPixelsString}");
            return;
        }

        currentWebScrollPixels = Mathf.Max(0f, scrollPixels);
        hasReceivedWebScroll = true;
        ApplyWebScrollPosition();
    }

    private void ApplyWebScrollPosition()
    {
        if (!hasReceivedWebScroll || _mainCamera == null)
        {
            return;
        }

        appliedWebScrollOffset = currentWebScrollPixels / pixelsPerUnityUnit;
        Vector3 position = initialCameraPosition;
        position.y -= appliedWebScrollOffset;
        _mainCamera.transform.position = position;
    }

    public void SetCameraPosition(Vector3 position)
    {
        if(_mainCamera != null)
        {
            _mainCamera.transform.position = position;
            Debug.Log($"Camera position manually set to {position}");
        }
        else
        {
            Debug.LogError("Main Camera not found. Cannot set position.");
        }
    }
    
    public void AnimateCameraPosition(Vector3 targetPosition, float duration, Action onComplete = null)
    {
        if(_mainCamera != null)
        {
            Vector3 fixedPos = new Vector3(_mainCamera.transform.position.x,targetPosition.y ,_mainCamera.transform.position.z );
            _mainCamera.transform.DOMove(fixedPos, duration).SetEase(Ease.InOutSine).OnComplete(() =>
            {
                onComplete?.Invoke();
            });
            Debug.Log($"Animating camera position to {targetPosition} over {duration} seconds");
        }
        else
        {
            Debug.LogError("Main Camera not found. Cannot animate position.");
        }
    }
}
