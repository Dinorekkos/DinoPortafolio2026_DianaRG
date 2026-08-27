using Dino.Portfolio.Gameplay;
using Sirenix.OdinInspector;
using UnityEngine;

public class AssetConfigurator : MonoBehaviour
{
    void Start()
    {
        
        
        
    }
    
    [Button]
    public void ConfigureAssets()
    {
        var interactables = FindObjectsOfType<Interactable>();
        foreach (var interactable in interactables)
        {
            MeshCollider meshCollider = interactable.GetComponentInChildren<MeshCollider>();
            //if the interactable has more than one mesh collider, remove all but the first one
            var meshColliders = interactable.GetComponentsInChildren<MeshCollider>();
            if (meshColliders.Length > 1)
            {
                for (int i = 1; i < meshColliders.Length; i++)
                {
                    Destroy(meshColliders[i]);
                }
            }

            meshCollider.convex = true;
            Debug.Log($"Configured {interactable.name} with convex MeshCollider.");
            
            
        }
    }

}
