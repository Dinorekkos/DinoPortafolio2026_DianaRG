using System.Collections.Generic;
using Dino.Portfolio.Gameplay;
using Sirenix.OdinInspector;
using UnityEngine;

public class InteractablesHandler : MonoBehaviour
{
    public List<Interactable> interactables = new List<Interactable>();
    void Start()
    {
        GetInteractables();
        // SaveInteractablesPosition();
    }
    
    private void GetInteractables()
    {
        this.interactables.Clear();
        var interactables = FindObjectsOfType<Interactable>();
        foreach (var interactable in interactables)
        {
            Debug.Log($"Found interactable: {interactable.name}");
            this.interactables.Add(interactable);
        }
    }
    
    [Button]
    public void ConfigureInteractables()
    {
        foreach (var interactable in this.interactables)
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
    
    
    [Button]
    public void ResetInteractablesPosition()
    {
        foreach (var interactable in interactables)
        {
            Transform interactableTransform = interactable.AssetTransform;
            if (interactableTransform != null)
            {
                // Reset the position to the original position
                Rigidbody rb = interactableTransform.GetComponent<Rigidbody>();
                // If the interactable has a Rigidbody, set it to kinematic and disable gravity while resetting the position
                if (rb != null)
                {
                    rb.isKinematic = true;
                    rb.useGravity = false;
                }
                interactableTransform.localPosition = Vector3.zero;
                interactableTransform.localRotation = Quaternion.identity;
                
                // If the interactable has a Rigidbody, re-enable gravity and set it back to non-kinematic after resetting the position
                if (rb != null)
                {
                    rb.isKinematic = false;
                    rb.useGravity = true;
                }
                Debug.Log($"Reset {interactable.name} position to {interactableTransform.localPosition}");
                
            }
            else
            {
                Debug.LogWarning($"Interactable {interactable.name} does not have an AssetTransform assigned. Cannot reset position.");
            }
        }
    }

}
