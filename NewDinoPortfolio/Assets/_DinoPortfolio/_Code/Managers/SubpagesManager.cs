using System;
using System.Collections.Generic;
using Dino.UtilityTools.Singleton;
using Sirenix.OdinInspector;
using UnityEngine;

public class SubpagesManager : Singleton<SubpagesManager>
{
    [SerializeField] private SubpageType currentSubpageType;
    [SerializeField] private List<SubpageData> subpageDataArray;
    
    public Action<SubpageType> OnSubpageChanged;

    public SubpageType CurrentSubpageType
    {
        get => currentSubpageType;
        set => currentSubpageType = value;
    }

    private void Start()
    {
        // DisableSubpagesExcept(currentSubpageType);
    }
    
    public void SetSubpageType(string value)
    {
        if (!Enum.TryParse(value, out SubpageType parsedSubpageType))
        {
            Debug.LogWarning($"Invalid subpage type: {value}");
            return;
        }
        SetCurrentSubpageType(parsedSubpageType);
    }
    
    
    
    [Button]
    public void SetCurrentSubpageType(SubpageType subpageType)
    {
        if(currentSubpageType == subpageType)
        {
            // Debug.Log($"Subpage type {subpageType} is already active.");
            return;
        }
        currentSubpageType = subpageType;
        HandleSubpageChange(currentSubpageType);
    }
    
    private void HandleSubpageChange(SubpageType subpageType)
    {
        // Handle the subpage change logic here
        foreach (var subpageData in subpageDataArray)
        {
            if (subpageData.subpageType == subpageType)
            {
                subpageData.subpageTransform.gameObject.SetActive(true);
                
                float targetYPosition = ResponsiveManager.Instance.CurrentDeviceType == DeviceType.Desktop
                    ? subpageData.desktopYPosition
                    : subpageData.mobileYPosition;
                
                CameraManager.Instance.AnimateCameraPosition(new Vector3(0, targetYPosition, 0), 1f, () =>
                {
                    // Debug.Log($"Camera animation to {subpageData.subpagePosition} completed.");
                    // DisableSubpagesExcept(subpageType);
                    OnSubpageChanged?.Invoke(currentSubpageType);
                });
                break;
            }
        }
        
    }
    
    
    private void DisableSubpagesExcept(SubpageType subpageType)
    {
        foreach (var subpageData in subpageDataArray)
        {
            if (subpageData.subpageType != subpageType)
            {
                // Debug.Log($"Disabling subpage: {subpageData.subpageType}");
                subpageData.subpageTransform.gameObject.SetActive(false);
            }
        }
    }
}

[System.Serializable]
public class SubpageData
{
    public SubpageType subpageType;
    public Transform subpageTransform;
    public float mobileYPosition;
    public float desktopYPosition;
}

public enum SubpageType
{
    LandingPage,
    Work,
    About,
    Services,
    Contact
}