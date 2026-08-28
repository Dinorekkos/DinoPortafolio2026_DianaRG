using Dino.Utility.Audio;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Dino.Portfolio.Gameplay
{
    public class Interactable : MonoBehaviour
    {
        [Header("Drag and Tap Settings")]
        [SerializeField] private float outlineThickness = 0.75f;
        [SerializeField] private bool canDrag = true;
        [SerializeField] private float raycastDistance = 1000f;
        [SerializeField] private float dragStartThresholdPixels = 12f;
        [SerializeField] private bool canTapPush = true;
        [SerializeField] private float tapImpulseForce = 1.5f;
        [SerializeField] private float tapImpulseUpForce = 0.15f;

        private Material mat;
        private MeshRenderer meshRenderer;
        private Rigidbody rb;
        
        private bool _isSelected;
        private bool _isHovered;
        private bool _isPointerDown;
        private bool _isDragging;
        private bool _hasExceededDragThreshold;
        private bool _cachedIsKinematic;
        private bool _cachedUseGravity;
        private bool _hasCachedRigidbodyState;
        private Vector2 _pointerDownScreenPosition;
        private Plane _dragPlane;
        private Vector3 _dragOffset;

        private void Start()
        {
            Initialize();
        }

        private void Update()
        {
            HandleInput();
        }

        private void HandleInput()
        {
            InputManager inputManager = InputManager.Instance;
            inputManager.RefreshInput();

            if (!inputManager.HasPointer)
            {
                SetIsHovered(false);

                if (_isPointerDown)
                    CancelInteraction();

                return;
            }

            UpdateHover(inputManager);

            if (inputManager.PointerPressedThisFrame)
            {
                if (IsPointerOverThisInteractable(inputManager.PointerScreenPosition, out _, out RaycastHit hit))
                    BeginInteraction(inputManager.PointerScreenPosition, hit);
                else
                    SetIsSelected(false);
            }

            if (_isPointerDown && inputManager.PointerIsPressed)
                ContinueInteraction(inputManager.PointerScreenPosition);

            if (_isPointerDown && inputManager.PointerReleasedThisFrame)
                FinishInteraction(inputManager.PointerScreenPosition);
        }

        private void SetIsSelected(bool value)
        {
            if (_isSelected == value)
                return;

            _isSelected = value;
            UpdateOutlineState();
        }

        private void SetIsHovered(bool value)
        {
            if (_isHovered == value)
                return;

            _isHovered = value;
            UpdateOutlineState();
        }

        private void UpdateOutlineState()
        {
            if (_isSelected || _isHovered)
                SetOutline();
            else
                DisableOutline();
        }
        
        private void Initialize()
        {
            meshRenderer = GetComponentInChildren<MeshRenderer>();
            if (meshRenderer != null)
            {
                mat = meshRenderer.material;
            }

            rb = GetComponent<Rigidbody>();
            _isSelected = false;
            _isHovered = false;
            DisableOutline();
        }

        private void UpdateHover(InputManager inputManager)
        {
            if (inputManager.inputType != InputType.Mouse)
            {
                SetIsHovered(false);
                return;
            }

            if (inputManager.PointerIsPressed)
            {
                if (!_isPointerDown)
                    SetIsHovered(false);

                return;
            }

            SetIsHovered(IsPointerOverThisInteractable(inputManager.PointerScreenPosition, out _, out _));
        }

        private void BeginInteraction(Vector2 screenPosition, RaycastHit hit)
        {
            rb = hit.rigidbody != null ? hit.rigidbody : GetComponent<Rigidbody>();
            _isPointerDown = true;
            _isDragging = false;
            _hasExceededDragThreshold = false;
            _pointerDownScreenPosition = screenPosition;
            SetIsSelected(true);
            PlayClickSound();
        }

        private void ContinueInteraction(Vector2 screenPosition)
        {
            float distanceFromStart = Vector2.Distance(_pointerDownScreenPosition, screenPosition);
            if (!_hasExceededDragThreshold && distanceFromStart >= dragStartThresholdPixels)
            {
                _hasExceededDragThreshold = true;
                BeginDrag(screenPosition);
            }

            if (_isDragging)
                Drag(screenPosition);
        }

        private void FinishInteraction(Vector2 screenPosition)
        {
            if (IsTapGesture(screenPosition) && canTapPush && IsPointerOverThisInteractable(screenPosition, out Ray pointerRay, out RaycastHit hit))
                ApplyTapImpulse(pointerRay, hit);

            EndDrag();
            _isPointerDown = false;
            _hasExceededDragThreshold = false;
            SetIsSelected(false);
        }

        private void CancelInteraction()
        {
            EndDrag();
            _isPointerDown = false;
            _hasExceededDragThreshold = false;
            SetIsSelected(false);
        }

        private void BeginDrag(Vector2 screenPosition)
        {
            if (!canDrag)
                return;

            Camera interactionCamera = GetInteractionCamera();
            if (interactionCamera == null)
                return;

            Ray pointerRay = interactionCamera.ScreenPointToRay(screenPosition);
            Vector3 dragPosition = GetCurrentDragPosition();
            _dragPlane = new Plane(-interactionCamera.transform.forward, dragPosition);
            _isDragging = TryGetPointerWorldPosition(pointerRay, out Vector3 pointerWorldPosition);
            _dragOffset = _isDragging ? dragPosition - pointerWorldPosition : Vector3.zero;

            if (_isDragging)
                CacheRigidbodyStateForDrag();
        }

        private void Drag(Vector2 screenPosition)
        {
            Camera interactionCamera = GetInteractionCamera();
            if (interactionCamera == null)
                return;

            Ray pointerRay = interactionCamera.ScreenPointToRay(screenPosition);
            if (!TryGetPointerWorldPosition(pointerRay, out Vector3 pointerWorldPosition))
                return;

            Vector3 targetPosition = pointerWorldPosition + _dragOffset;
            if (rb != null)
                rb.MovePosition(targetPosition);
            else
                transform.position = targetPosition;
        }

        private void EndDrag()
        {
            if (_hasCachedRigidbodyState)
                RestoreRigidbodyStateAfterDrag();

            _isDragging = false;
        }

        private void ApplyTapImpulse(Ray pointerRay, RaycastHit hit)
        {
            Rigidbody targetRigidbody = hit.rigidbody != null ? hit.rigidbody : rb;
            if (targetRigidbody == null || targetRigidbody.isKinematic)
                return;

            Vector3 impulseDirection = (pointerRay.direction + Vector3.up * tapImpulseUpForce).normalized;
            targetRigidbody.AddForce(impulseDirection * tapImpulseForce, ForceMode.Impulse);
        }

        private bool IsTapGesture(Vector2 screenPosition)
        {
            float distanceFromStart = Vector2.Distance(_pointerDownScreenPosition, screenPosition);
            return !_hasExceededDragThreshold && distanceFromStart < dragStartThresholdPixels;
        }

        private Vector3 GetCurrentDragPosition()
        {
            return rb != null ? rb.position : transform.position;
        }

        private void CacheRigidbodyStateForDrag()
        {
            if (rb == null || _hasCachedRigidbodyState)
                return;

            _cachedIsKinematic = rb.isKinematic;
            _cachedUseGravity = rb.useGravity;
            _hasCachedRigidbodyState = true;
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        private void RestoreRigidbodyStateAfterDrag()
        {
            if (rb == null)
            {
                _hasCachedRigidbodyState = false;
                return;
            }

            rb.isKinematic = _cachedIsKinematic;
            rb.useGravity = _cachedUseGravity;
            _hasCachedRigidbodyState = false;
        }

        private void OnDisable()
        {
            if (_hasCachedRigidbodyState)
                RestoreRigidbodyStateAfterDrag();

            _isPointerDown = false;
            _isDragging = false;
            _hasExceededDragThreshold = false;
            SetIsHovered(false);
            SetIsSelected(false);
        }

        private bool IsPointerOverThisInteractable(Vector2 screenPosition, out Ray pointerRay, out RaycastHit hit)
        {
            pointerRay = default;
            hit = default;

            Camera interactionCamera = GetInteractionCamera();
            if (interactionCamera == null)
                return false;

            pointerRay = interactionCamera.ScreenPointToRay(screenPosition);
            if (!Physics.Raycast(pointerRay, out hit, raycastDistance))
                return false;

            return hit.transform == transform || hit.transform.IsChildOf(transform);
        }

        private bool TryGetPointerWorldPosition(Ray pointerRay, out Vector3 worldPosition)
        {
            worldPosition = Vector3.zero;

            if (!_dragPlane.Raycast(pointerRay, out float enter))
                return false;

            worldPosition = pointerRay.GetPoint(enter);
            return true;
        }

        private Camera GetInteractionCamera()
        {
            Camera camera = CameraManager.Instance.MainCamera;
            return camera != null ? camera : Camera.main;
        }

        // [Button]
        public void SetOutline()
        {
            if (mat == null)
                return;

            mat.SetFloat("_OutlineThickness", outlineThickness);
        }

        // [Button]
        public void DisableOutline()
        {
            if (mat == null)
                return;

            mat.SetFloat("_OutlineThickness", 0f);
        }
        
        private void PlayClickSound()
        {
            AudioManager.Instance.PlaySound("Tap");
        }

    }
}
