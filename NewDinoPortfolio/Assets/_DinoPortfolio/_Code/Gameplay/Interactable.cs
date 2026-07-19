using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Dino.Portfolio.Gameplay
{
    public class Interactable : MonoBehaviour
    {
        [SerializeField] private float outlineThickness = 0.75f;
        
        private Material mat;
        private MeshRenderer meshRenderer;
        private bool _isSelected;

        private void Start()
        {
            Initialize();
            
            
        }
        
        private void SetIsSelected(bool value)
        {
            _isSelected = value;
            if (_isSelected)
            {
                SetOutline();
            }
            else
            {
                DisableOutline();
            }
        }
        
        private void Initialize()
        {
            meshRenderer = GetComponentInChildren<MeshRenderer>();
            if (meshRenderer != null)
            {
                mat = meshRenderer.material;
            }
            
            SetIsSelected(false);
        }

        [Button]
        public void SetOutline()
        {
            mat.SetFloat("_OutlineThickness", outlineThickness);
        }

        [Button]
        public void DisableOutline()
        {
            mat.SetFloat("_OutlineThickness", 0f);
        }

    }
}