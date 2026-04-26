using UnityEngine;

public class HitTargetColor : MonoBehaviour
{
    [SerializeField] private Color hitColor = Color.green;
    [SerializeField] private float hitColorDuration = 0.3f;
    [SerializeField] private AudioClip hitSound;
    [SerializeField] private ScoreManager scoreManager;
    [SerializeField] private int scoreAmount = 1;

    private Renderer targetRenderer;
    private AudioSource audioSource;
    private Color originalColor;
    private bool isProcessingHit;

    private void Awake()
    {
        targetRenderer = GetComponent<Renderer>();
        audioSource = GetComponent<AudioSource>();
        originalColor = targetRenderer.material.color;
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

        yield return new WaitForSeconds(hitColorDuration);

        targetRenderer.material.color = originalColor;
        isProcessingHit = false;
    }
}
