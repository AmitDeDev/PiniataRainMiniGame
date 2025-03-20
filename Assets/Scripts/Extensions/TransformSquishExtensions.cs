using DG.Tweening;
using UnityEngine;

public static class TransformSquishExtensions
{
    /// <summary>
    /// Applies a quick squish/bounce animation to a 2D transform (e.g. your Piñata) via DOTween.
    /// </summary>
    public static void Squish(this Transform target, float scaleFactor, float duration)
    {
        Vector3 originalScale = target.localScale;

        // Scale down, then back up with an easing
        target.DOScale(originalScale * scaleFactor, duration).OnComplete(() =>
        {
            target.DOScale(originalScale, duration)
                .SetEase(Ease.OutBack);
        });
    }
}