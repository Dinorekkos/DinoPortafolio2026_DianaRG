using Dino.UtilityTools.Singleton;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : Singleton<InputManager>
{
    #region Devices
    private Mouse _mouse;
    private Touchscreen _touchscreen;
    private bool _hasPreviousPointerPosition;
    private int _lastUpdatedFrame = -1;
    #endregion

    #region Type

    [Header("Input Type")]
    public InputType inputType;

    #endregion

    #region Pointer State
    public bool HasPointer { get; private set; }
    public bool PointerPressedThisFrame { get; private set; }
    public bool PointerReleasedThisFrame { get; private set; }
    public bool PointerIsPressed { get; private set; }
    public Vector2 PointerScreenPosition { get; private set; }
    public Vector2 PointerDelta { get; private set; }
    #endregion

    protected override void Awake()
    {
        base.Awake();
        Initialize();
    }

    void Initialize()
    {
        _mouse = Mouse.current;
        _touchscreen = Touchscreen.current;
        inputType = _touchscreen != null && _mouse == null ? InputType.Touch : InputType.Mouse;
    }

    private void Update()
    {
        RefreshInput();
    }

    public void RefreshInput()
    {
        if (_lastUpdatedFrame == Time.frameCount)
            return;

        _lastUpdatedFrame = Time.frameCount;

        if (_mouse == null)
            _mouse = Mouse.current;

        if (_touchscreen == null)
            _touchscreen = Touchscreen.current;

        Vector2 previousPosition = PointerScreenPosition;

        HasPointer = false;
        PointerPressedThisFrame = false;
        PointerReleasedThisFrame = false;
        PointerIsPressed = false;
        PointerDelta = Vector2.zero;

        if (TryReadTouchInput(out Vector2 touchPosition, out bool touchPressed, out bool touchPressedThisFrame, out bool touchReleasedThisFrame))
        {
            SetPointerState(InputType.Touch, touchPosition, touchPressed, touchPressedThisFrame, touchReleasedThisFrame, previousPosition);
            return;
        }

        if (TryReadMouseInput(out Vector2 mousePosition, out bool mousePressed, out bool mousePressedThisFrame, out bool mouseReleasedThisFrame))
        {
            SetPointerState(InputType.Mouse, mousePosition, mousePressed, mousePressedThisFrame, mouseReleasedThisFrame, previousPosition);
            return;
        }

        _hasPreviousPointerPosition = false;
    }

    private void SetPointerState(InputType detectedInputType, Vector2 position, bool isPressed, bool pressedThisFrame, bool releasedThisFrame, Vector2 previousPosition)
    {
        inputType = detectedInputType;
        HasPointer = true;
        PointerScreenPosition = position;
        PointerIsPressed = isPressed;
        PointerPressedThisFrame = pressedThisFrame;
        PointerReleasedThisFrame = releasedThisFrame;
        PointerDelta = _hasPreviousPointerPosition ? position - previousPosition : Vector2.zero;
        _hasPreviousPointerPosition = true;
    }

    private bool TryReadTouchInput(out Vector2 position, out bool isPressed, out bool pressedThisFrame, out bool releasedThisFrame)
    {
        position = Vector2.zero;
        isPressed = false;
        pressedThisFrame = false;
        releasedThisFrame = false;

        if (_touchscreen == null)
            return false;

        var primaryTouch = _touchscreen.primaryTouch;
        isPressed = primaryTouch.press.isPressed;
        pressedThisFrame = primaryTouch.press.wasPressedThisFrame;
        releasedThisFrame = primaryTouch.press.wasReleasedThisFrame;

        if (!isPressed && !pressedThisFrame && !releasedThisFrame)
            return false;

        position = primaryTouch.position.ReadValue();
        return true;
    }

    private bool TryReadMouseInput(out Vector2 position, out bool isPressed, out bool pressedThisFrame, out bool releasedThisFrame)
    {
        position = Vector2.zero;
        isPressed = false;
        pressedThisFrame = false;
        releasedThisFrame = false;

        if (_mouse == null)
            return false;

        position = _mouse.position.ReadValue();
        isPressed = _mouse.leftButton.isPressed;
        pressedThisFrame = _mouse.leftButton.wasPressedThisFrame;
        releasedThisFrame = _mouse.leftButton.wasReleasedThisFrame;
        return true;
    }

}

public enum InputType
{
    Touch,
    Mouse
}
