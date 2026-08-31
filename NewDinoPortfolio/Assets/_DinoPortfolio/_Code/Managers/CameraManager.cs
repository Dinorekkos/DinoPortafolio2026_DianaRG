using System;
using Dino.UtilityTools.Singleton;
using UnityEngine;

public class CameraManager : Singleton<CameraManager>
{
    [SerializeField] private float mobileSize = 2.8f;
    [SerializeField] private Vector3 mobilePosition = new Vector3(0, 0, 0);
    [SerializeField] private float tabletSize = 2.0f;
    [SerializeField] private float desktopSize = 1f;
    [SerializeField] private Vector3 desktopPosition = new Vector3(0, 0, 0);
   
    [SerializeField] private Camera _mainCamera;
    
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
        
        DeviceType deviceType = ResponsiveManager.Instance.CurrentDeviceType;
        switch (deviceType) 
        {
            case DeviceType.Mobile:
                SetCameraSize(mobileSize);
                SetCameraPosition(mobilePosition);
                break;
            case DeviceType.Tablet:
                SetCameraSize(tabletSize);
                // SetCameraPosition(mobilePosition);
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
}
