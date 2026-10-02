using System;
using Sirenix.OdinInspector;
using UnityEngine;

public class SubpagesManager : MonoBehaviour
{
    [SerializeField] private SubpageType currentSubpageType;
    [SerializeField] private Transform[] subpageTransforms;

    public SubpageType CurrentSubpageType
    {
        get => currentSubpageType;
        set => currentSubpageType = value;
    }

    private void Start()
    {
        SetCurrentSubpageType(currentSubpageType);
    }

    [Button]
    public void SetCurrentSubpageType(SubpageType subpageType)
    {
        currentSubpageType = subpageType;
        HandleSubpageChange(currentSubpageType);
    }
    
    private void HandleSubpageChange(SubpageType subpageType)
    {
        // Handle the subpage change logic here
        for (int i = 0; i < subpageTransforms.Length; i++)
        {
            if (i == (int)subpageType)
            {
                subpageTransforms[i].gameObject.SetActive(true);
            }
            else
            {
                subpageTransforms[i].gameObject.SetActive(false);
            }
        }
        
        
        
    }
}

public enum SubpageType
{
    LandingPage,
    Work,
    About,
    Services,
    Contact
}