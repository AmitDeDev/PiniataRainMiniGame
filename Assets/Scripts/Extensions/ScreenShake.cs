using System.Collections;
using UnityEngine;

/// <summary>
/// Attach this to your Main Camera. 
/// Call Shake(intensity, duration) to do a quick screen shake.
/// </summary>
public class ScreenShake : MonoBehaviour
{
    private Vector3 originalPos;

    // You can keep default intensity/duration if you want
    public float defaultIntensity = 0.2f;
    public float defaultDuration = 0.3f;

    private void Awake()
    {
        originalPos = transform.localPosition;
    }

    public void Shake(float intensity, float duration)
    {
        // If you want to be sure no multiple shakes run simultaneously, 
        // you can stop them first:
        StopAllCoroutines();

        StartCoroutine(DoShake(intensity, duration));
    }

    private IEnumerator DoShake(float intensity, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            // random offset
            float offsetX = Random.Range(-1f, 1f) * intensity;
            float offsetY = Random.Range(-1f, 1f) * intensity;

            transform.localPosition = originalPos + new Vector3(offsetX, offsetY, 0f);

            yield return null;
        }

        // Reset position
        transform.localPosition = originalPos;
    }
}