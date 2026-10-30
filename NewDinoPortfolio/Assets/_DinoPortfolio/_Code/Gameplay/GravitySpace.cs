using Sirenix.OdinInspector;
using UnityEngine;

public class GravitySpace : MonoBehaviour
{
    [SerializeField] private float maxXSize = 1f;
    [SerializeField] private float maxYSize = 1f;
    [SerializeField] private float marginOffset = 2f;
    void Start()
    {
        ScaleWithScreenSize();
    }
    
    [Button]
    private void ScaleWithScreenSize()
    {
        // Get the current screen size
        float screenWidth = Screen.width;
        float screenHeight = Screen.height;

        // Calculate the aspect ratio
        float aspectRatio = screenWidth / screenHeight;
        // Limit the scale based on the maximum size
        aspectRatio = Mathf.Min(aspectRatio, maxXSize / maxYSize);

        // Adjust the scale of the object based on the aspect ratio
        transform.localScale = new Vector3(aspectRatio, 1f, 1f);
    }

}
