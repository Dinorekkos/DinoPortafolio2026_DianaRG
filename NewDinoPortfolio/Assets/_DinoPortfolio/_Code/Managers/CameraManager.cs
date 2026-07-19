using System;
using Dino.UtilityTools.Singleton;
using UnityEngine;

public class CameraManager : Singleton<CameraManager>
{
    [SerializeField] private float mobileSize = 2.8f;
    [SerializeField] private float tabletSize = 2.0f;
    [SerializeField] private float desktopSize = 1f;
   
    [SerializeField] private Camera _mainCamera;
    
    public Camera MainCamera => _mainCamera;

    protected override void Awake()
    {
        base.Awake();
        
    }

    void Start()
    {
        Initialize();
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
                _mainCamera.orthographicSize = mobileSize;
                break;
            case DeviceType.Tablet:
                _mainCamera.orthographicSize = tabletSize;
                break;
            case DeviceType.Desktop:
                _mainCamera.orthographicSize = desktopSize;
                break;
            default:
                Debug.LogWarning("Unknown device type. Using default camera size.");
                _mainCamera.orthographicSize = desktopSize;
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
}
