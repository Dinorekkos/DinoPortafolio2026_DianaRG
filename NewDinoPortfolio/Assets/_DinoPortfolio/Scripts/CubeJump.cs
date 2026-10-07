using System.Collections;
using UnityEngine;
namespace DianaPortfolio {
 public sealed class CubeJump : MonoBehaviour {
  [SerializeField] private float height = 10f;
  [SerializeField] private float duration = 1f;
  private Vector3 startPosition;
  private Coroutine routine;
  private void Start() { Jump(); }
  public void Jump() { if (routine != null) StopCoroutine(routine); startPosition = transform.position; routine = StartCoroutine(Animate()); }
  private IEnumerator Animate() {
   float elapsed = 0f;
   while (elapsed < duration) { elapsed += Time.deltaTime; transform.position = startPosition + Vector3.up * JumpOffset(elapsed / duration, height); yield return null; }
   transform.position = startPosition; routine = null;
  }
  public static float JumpOffset(float t, float jumpHeight) { return Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI) * jumpHeight; }
 }
}