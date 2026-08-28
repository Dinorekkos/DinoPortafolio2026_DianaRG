using Dino.UtilityTools.Singleton;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : Singleton<InputManager>
{
    #region Touch Mobile
    private const float PinchSensitivity = 0.005f;
    private const float PinchThresholdPixels = 2f;
    private const float MinCameraSize = 0.5f;
    private const float MaxCameraSize = 8f;

    private Touchscreen _touchscreen;
    private float _previousPinchDistance;
    private bool _isPinching;
    #endregion

    #region Type

    [Header("Input Type")]
    public InputType inputType;

    #endregion
    
    
    protected override void Awake()
    {
        base.Awake();
        Initialize();
        
    }

    void Start()
    {
      
    }
    
    void Initialize()
    {
        _touchscreen = Touchscreen.current;
        if(_touchscreen == null)
        {
            Debug.LogWarning("Touchscreen not found. Touch input will not work.");
        }
        
    }

    private void Update()
    {
        if (_touchscreen == null)
            _touchscreen = Touchscreen.current;

        if (_touchscreen == null)
            return;

        if (TryGetTwoActiveTouchPositions(out Vector2 touchPosition1, out Vector2 touchPosition2))
        {
            DetectZoom(touchPosition1, touchPosition2);
        }
        else
        {
            _isPinching = false;
        }
    }

    //detect zoom input with 2 fingers in touch screen 
    void DetectZoom(Vector2 currentTouchPosition1, Vector2 currentTouchPosition2)
    {
        var distance = Vector2.Distance(currentTouchPosition1, currentTouchPosition2);

        if (!_isPinching)
        {
            _previousPinchDistance = distance;
            _isPinching = true;
            return;
        }

        float pinchDelta = distance - _previousPinchDistance;

        if (Mathf.Abs(pinchDelta) >= PinchThresholdPixels)
            DoZoom(pinchDelta * PinchSensitivity);

        _previousPinchDistance = distance;
    }
    private void DoZoom(float zoomFactor)
    {
        CameraManager cameraManager = CameraManager.Instance;

        if (cameraManager.MainCamera == null)
            return;

        float currentSize = cameraManager.MainCamera.orthographicSize;
        float newSize = Mathf.Clamp(currentSize - zoomFactor, MinCameraSize, MaxCameraSize);

        cameraManager.SetCameraSize(newSize);
    }
    private void OnEnable()
    {
        _previousPinchDistance = 0f;
        _isPinching = false;
    }

    private bool TryGetTwoActiveTouchPositions(out Vector2 touchPosition1, out Vector2 touchPosition2)
    {
        touchPosition1 = Vector2.zero;
        touchPosition2 = Vector2.zero;
        int activeTouches = 0;

        foreach (var touch in _touchscreen.touches)
        {
            if (!touch.press.isPressed)
                continue;

            if (activeTouches == 0)
                touchPosition1 = touch.position.ReadValue();
            else
            {
                touchPosition2 = touch.position.ReadValue();
                return true;
            }

            activeTouches++;
        }

        return false;
    }

}

public enum InputType
{
    Touch,
    Mouse
}