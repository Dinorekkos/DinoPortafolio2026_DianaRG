using Sirenix.OdinInspector;
using UnityEngine;

public class Resize : MonoBehaviour
{
    [SerializeField] private float maxHeight = 3f; // Maximum height in world units
    
    void Start()
    {
        FitScreen();
    }

    [Button]
    private void FitScreen()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr == null) return;

        // Reset scale so calculation starts from base size
        transform.localScale = Vector3.one;

        // Get sprite dimensions in world units
        float spriteWidth = sr.sprite.bounds.size.x;
        float spriteHeight = sr.sprite.bounds.size.y; // Limit the height to a maximum of 1 unit

        // Calculate world height and width from the Orthographic Camera
        float worldHeight = Camera.main.orthographicSize * 2f;
        float worldWidth = worldHeight * Screen.width / Screen.height;

        float scaleX = worldWidth / spriteWidth;
        float scaleY = worldHeight / spriteHeight;
        // Scale the sprite to stretch across both dimensions
        transform.localScale = new Vector3(scaleX, Mathf.Clamp(scaleY, 0, maxHeight), 1f);
    }
}
