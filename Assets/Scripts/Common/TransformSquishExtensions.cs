using DG.Tweening;
using UnityEngine;

public static class TransformSquishExtensions
{
    public static void Squish(this Transform target, float scaleFactor, float duration)
    {
        Vector3 originalScale = target.localScale;
        
        target.DOScale(originalScale * scaleFactor, duration).OnComplete(() =>
        {
            target.DOScale(originalScale, duration)
                .SetEase(Ease.OutBack);
        });
    }
}