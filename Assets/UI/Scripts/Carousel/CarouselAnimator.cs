using PrimeTween;
using System.Collections.Generic;
using UnityEngine;

public class CarouselAnimator
{
    private RectTransform _content;
    private List<RectTransform> _panels;
    private RectTransform _viewport;
    private float _snapDuration;
    private Tween _snapTween;

    public CarouselAnimator(RectTransform content, List<RectTransform> panels, RectTransform viewport, float snapDuration)
    {
        _content = content;
        _panels = panels;
        _viewport = viewport;
        _snapDuration = snapDuration;
    }

    public void SnapTo(int idx, float duration)
    {
        if (_panels.Count == 0) return;
        if (duration < 0) duration = _snapDuration;
        float viewportWidth = _viewport.rect.width;
        float targetFromLeft = 0f;
        for (int i = 0; i < idx; i++)
        {
            targetFromLeft += _panels[i].rect.width;
        }
        targetFromLeft += _panels[idx].rect.width * 0.5f - viewportWidth * 0.5f;
        float contentWidth = _content.rect.width;
        float clamped = Mathf.Clamp(targetFromLeft, 0f, Mathf.Max(0f, contentWidth - viewportWidth));
        float targetAnchX = -clamped;
        _snapTween.Stop();
        float start = _content.anchoredPosition.x;
        _snapTween = Tween.Custom(start, targetAnchX, Mathf.Max(0.001f, duration),
            v => _content.anchoredPosition = new Vector2(v, _content.anchoredPosition.y),
            Ease.OutExpo);
    }

    public void StopSnap()
    {
        _snapTween.Stop();
    }
}