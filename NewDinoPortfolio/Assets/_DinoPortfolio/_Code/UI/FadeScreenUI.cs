using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;

public class FadeScreenUI : MonoBehaviour
{
    [SerializeField] private GameObject parentCanvas;
    [SerializeField] CanvasGroup fadeScreenCanvasGroup;
    [SerializeField] float fadeDuration = 1f;
    [SerializeField] Color fadeColor = Color.black;
    [SerializeField] Image fadeImage;
   
    float _alpha = 1f;
    void Start()
    {
        parentCanvas.SetActive(true);
        fadeImage.color = fadeColor;
        fadeScreenCanvasGroup.alpha = _alpha;
        FadeOut();
    }
    
    
    [Button]
    public void FadeIn()
    {
        fadeScreenCanvasGroup.DOFade(1f, fadeDuration).OnComplete(() =>
        {
            Debug.Log("Fade in complete");
        });
    }
    
    [Button] 
    public void FadeOut()
    {
        fadeScreenCanvasGroup.DOFade(0f, fadeDuration).OnComplete(() =>
        {
            Debug.Log("Fade out complete");
        });
    }
    
    
    
}
