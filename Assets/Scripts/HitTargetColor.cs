using UnityEngine;

public class HitTargetColor : MonoBehaviour
{
    [SerializeField] private Color hitColor = Color.green;
    [SerializeField] private float hitColorDuration = 0.3f;
    [SerializeField] private float hitScaleMultiplier = 1.08f;
    [SerializeField] private float hitScaleDuration = 0.18f;
    [SerializeField] private AudioClip hitSound;
    [SerializeField] private ScoreManager scoreManager;
    [SerializeField] private int scoreAmount = 1;

    private Renderer targetRenderer;
    private AudioSource audioSource;
    private Color originalColor;
    private Vector3 originalScale;
    private bool isProcessingHit;

    private void Awake()
    {
        targetRenderer = GetComponent<Renderer>();
        audioSource = GetComponent<AudioSource>();
        originalColor = targetRenderer.material.color;
        originalScale = transform.localScale;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (isProcessingHit)
        {
            return;
        }

        if (!collision.gameObject.CompareTag("Throwable"))
        {
            return;
        }

        StartCoroutine(ProcessHit(collision.gameObject));
    }

    private System.Collections.IEnumerator ProcessHit(GameObject hitObject)
    {
        isProcessingHit = true;
        targetRenderer.material.color = hitColor;

        if (audioSource != null && hitSound != null)
        {
            audioSource.PlayOneShot(hitSound);
        }

        if (scoreManager != null)
        {
            scoreManager.AddScore(scoreAmount);
        }

        ResettableObject resettableObject = hitObject.GetComponent<ResettableObject>();
        if (resettableObject != null)
        {
            resettableObject.ResetToStart();
        }

        yield return AnimateHitScale();

        float remainingColorDuration = hitColorDuration - hitScaleDuration;
        if (remainingColorDuration > 0f)
        {
            yield return new WaitForSeconds(remainingColorDuration);
        }

        targetRenderer.material.color = originalColor;
        transform.localScale = originalScale;
        isProcessingHit = false;
    }

    private System.Collections.IEnumerator AnimateHitScale()
    {
        if (hitScaleMultiplier <= 1f || hitScaleDuration <= 0f)
        {
            yield break;
        }

        Vector3 hitScale = originalScale * hitScaleMultiplier;
        float halfDuration = hitScaleDuration * 0.5f;

        yield return ScaleOverTime(originalScale, hitScale, halfDuration);
        yield return ScaleOverTime(hitScale, originalScale, halfDuration);
    }

    private System.Collections.IEnumerator ScaleOverTime(Vector3 fromScale, Vector3 toScale, float duration)
    {
        if (duration <= 0f)
        {
            transform.localScale = toScale;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            transform.localScale = Vector3.Lerp(fromScale, toScale, t);
            yield return null;
        }

        transform.localScale = toScale;
    }
}
