using UnityEngine;

public class Rotatable : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 10f;
    
    [SerializeField] private float maxRotationX = 45f;
    [SerializeField] private float minRotationX = 0f;
    
    [SerializeField] private float maxRotationY = 45f;
    [SerializeField] private float minRotationY = -45f;
    
    
    void Start()
    {
        
    }

    void Update()
    {
        Vector2 mousePosition = InputManager.Instance.PointerScreenPosition;
        RotateObject(mousePosition);
    }
    
    private void RotateObject(Vector2 mousePosition)
    {
        Vector3 objectPosition = Camera.main.WorldToScreenPoint(transform.position);
        
     
    }
}
