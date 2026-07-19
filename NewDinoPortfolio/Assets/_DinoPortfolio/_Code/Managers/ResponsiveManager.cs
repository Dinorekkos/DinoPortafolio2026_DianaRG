using System;
using Dino.UtilityTools.Singleton;
using UnityEngine;
using UnityEngine.Events;

public class ResponsiveManager : Singleton<ResponsiveManager>
{  
    
    #region private Fields
    private Vector2 _lastScreenSize;
    #endregion

    #region public Properties
    public ScreenOrientation CurrentOrientation => GetScreenOrientation();
    public DeviceType CurrentDeviceType { get => GetDeviceTypeByResolution(Screen.width, Screen.height); }
    public bool IsPortrait() => Screen.width < Screen.height;
    public bool IsLandscape() => Screen.width >= Screen.height;
    public Vector2 CurrentScreenSize => new Vector2(Screen.width, Screen.height);
    public UnityEvent OnScreenSizeChanged { get; private set; } = new UnityEvent();

    #endregion
    
    #region Unity Methods
    
    private void OnEnable()
    {
        _lastScreenSize = new Vector2(Screen.width, Screen.height);
        Application.onBeforeRender += CheckScreenSizeChange;
    }
    private void OnDisable()
    {
        Application.onBeforeRender -= CheckScreenSizeChange;
    }

    protected override void Awake()
    {
        base.Awake();
    }

    private void Start()
    {
        // Debug.Log(CurrentScreenSize);
        // Debug.Log(CurrentOrientation);
        Debug.Log(CurrentDeviceType);
    }
    #endregion
    
    #region Private Methods
    private void CheckScreenSizeChange()
    {
        Vector2 currentScreenSize = new Vector2(Screen.width, Screen.height);
        if (_lastScreenSize != currentScreenSize)
        {
            _lastScreenSize = currentScreenSize;
            OnScreenSizeChanged?.Invoke();
            Debug.Log($"Screen size changed: {currentScreenSize.x}x{currentScreenSize.y} Orientation: {(IsPortrait() ? "Portrait" : "Landscape")}");
            Debug.Log($"Device type: {CurrentDeviceType}");
        }
    }
    private ScreenOrientation GetScreenOrientation()
    {
        return IsPortrait() ? ScreenOrientation.Portrait : ScreenOrientation.Landscape;
    }
    private DeviceType GetDeviceTypeByResolution(int width, int height)
    {
        const int tabletMinDimension = 600;
        const int desktopMinWidth = 1024;
        const int desktopMinHeight = 600;
        const float phoneAspectRatio = 2.0f;

        float aspectRatio = (float)Math.Max(width, height) / Math.Min(width, height);
        int minDimension = Math.Min(width, height);
        bool isLandscape = width >= height;

        if (aspectRatio >= phoneAspectRatio)
            return DeviceType.Mobile;
        // Desktop breakpoint: common PC/web layouts start from 1024px wide in landscape.
        else if (isLandscape && width >= desktopMinWidth && height >= desktopMinHeight)
            return DeviceType.Desktop;
        else if (minDimension >= tabletMinDimension && aspectRatio < phoneAspectRatio)
            return DeviceType.Tablet;
        else
            return DeviceType.Mobile;
    }
    #endregion
}

public enum ScreenOrientation
{
    Portrait,
    Landscape
}
public enum DeviceType
{
    Mobile,
    Tablet,
    Desktop
}
