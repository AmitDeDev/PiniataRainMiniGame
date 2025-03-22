using System.Collections;
using UnityEngine;

public class ScreenShake : MonoBehaviour
{
    private Vector3 originalPos;
    
    public float defaultIntensity = 0.2f;
    public float defaultDuration = 0.3f;

    private void Awake()
    {
        originalPos = transform.localPosition;
    }

    public void Shake(float intensity, float duration)
    {
        StopAllCoroutines();

        StartCoroutine(DoShake(intensity, duration));
    }

    private IEnumerator DoShake(float intensity, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float offsetX = Random.Range(-1f, 1f) * intensity;
            float offsetY = Random.Range(-1f, 1f) * intensity;

            transform.localPosition = originalPos + new Vector3(offsetX, offsetY, 0f);

            yield return null;
        }
        
        transform.localPosition = originalPos;
    }
}