using Cysharp.Threading.Tasks;
using PrimeTween;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CarouselNoScrollRect : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private RectTransform _viewport;
    [SerializeField] private RectTransform _content;
    [SerializeField] private List<Image> _panels = new();

    [SerializeField] private RectTransform _dotsContainer;
    [SerializeField] private GameObject _dotPrefab;

    [SerializeField] private float _snapDuration = 0.45f;
    [SerializeField] private float _velocityThreshold = 400f;
    [SerializeField] private float _autoScrollInterval = 3f;

    private List<DotBehaviour> _dots = new();
    private int _current;
    private Tween _snapTween;
    private CancellationTokenSource _autoScrollCts;

    private Vector2 _pointerStartLocal;
    private Vector2 _contentStartAnch;
    private Vector2 _lastPointerPos;
    private float _lastPointerTime;

    private void Awake()
    {
        if (_viewport == null) _viewport = (RectTransform)transform;

        _dots = Enumerable.Range(0, _panels.Count)
            .Select(_ => Instantiate(_dotPrefab, _dotsContainer).GetComponent<DotBehaviour>())
            .ToList();

        UpdateDots();
    }

    private async UniTask Start()
    {
        await UniTask.DelayFrame(2);
        StartAutoScroll();
        SnapTo(0, 0.001f);
    }

    private void OnDestroy() => StopAutoScroll();
    private void OnDisable() => StopAutoScroll();

    private void UpdateDots()
    {
        for (int i = 0; i < _dots.Count; i++)
            _dots[i].SetActiveDot(i == _current);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        StopAutoScroll();
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_viewport, eventData.position, eventData.pressEventCamera, out _pointerStartLocal);
        _contentStartAnch = _content.anchoredPosition;
        _lastPointerPos = _pointerStartLocal;
        _lastPointerTime = Time.unscaledTime;
        _snapTween.Stop();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_viewport, eventData.position, eventData.pressEventCamera, out Vector2 currentLocal))
            return;

        float deltaX = currentLocal.x - _pointerStartLocal.x;
        float newX = Mathf.Clamp(_contentStartAnch.x + deltaX, -Mathf.Max(0f, _content.rect.width - _viewport.rect.width), 0f);
        _content.anchoredPosition = new Vector2(newX, _content.anchoredPosition.y);

        _lastPointerPos = currentLocal;
        _lastPointerTime = Time.unscaledTime;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_viewport, eventData.position, eventData.pressEventCamera, out Vector2 endLocal))
        {
            SnapToClosest();
            UniTask.Delay(1000).ContinueWith(StartAutoScroll).Forget();
            return;
        }

        float dt = Mathf.Max(0.001f, Time.unscaledTime - _lastPointerTime);
        float velocity = (endLocal.x - _lastPointerPos.x) / dt;
        DecideSnap(velocity);
        UniTask.Delay(1000).ContinueWith(StartAutoScroll).Forget();
    }

    private void DecideSnap(float pointerVelocity)
    {
        if (pointerVelocity > _velocityThreshold) SetIndex(_current - 1);
        else if (pointerVelocity < -_velocityThreshold) SetIndex(_current + 1);
        else SnapToClosest();
    }

    private void SnapToClosest()
    {
        if (_panels.Count == 0) return;

        float viewportWidth = _viewport.rect.width;
        float contentWidth = _content.rect.width;
        float normalized = Mathf.InverseLerp(0, contentWidth - viewportWidth, -_content.anchoredPosition.x);
        int idx = Mathf.RoundToInt(normalized * (_panels.Count - 1));
        SetIndex(idx);
    }

    private void SetIndex(int idx)
    {
        idx = Mathf.Clamp(idx, 0, _panels.Count - 1);
        if (idx == _current) { SnapTo(idx); return; }
        _current = idx;
        SnapTo(idx);
        UpdateDots();
    }

    private void SnapTo(int idx) => SnapTo(idx, _snapDuration);

    private void SnapTo(int idx, float duration)
    {
        if (_panels.Count == 0) return;

        float viewportWidth = _viewport.rect.width;
        float elementWidth = _panels[idx].rectTransform.rect.width;
        float contentWidth = _content.rect.width;

        float targetFromLeft = idx * elementWidth + elementWidth * 0.5f - viewportWidth * 0.5f;
        float clamped = Mathf.Clamp(targetFromLeft, 0f, Mathf.Max(0f, contentWidth - viewportWidth));
        float targetAnchX = -clamped;

        _snapTween.Stop();
        float start = _content.anchoredPosition.x;
        _snapTween = Tween.Custom(start, targetAnchX, Mathf.Max(0.001f, duration),
            v => _content.anchoredPosition = new Vector2(v, _content.anchoredPosition.y),
            Ease.OutExpo);
    }

    private void StartAutoScroll()
    {
        if (_panels.Count <= 1 || _autoScrollInterval <= 0) return;
        StopAutoScroll();
        _autoScrollCts = new CancellationTokenSource();
        AutoScrollLoop(_autoScrollCts.Token).Forget();
    }

    private void StopAutoScroll()
    {
        _autoScrollCts?.Cancel();
        _autoScrollCts?.Dispose();
        _autoScrollCts = null;
    }

    private async UniTaskVoid AutoScrollLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            await UniTask.Delay((int)(_autoScrollInterval * 1000), cancellationToken: token);
            if (!token.IsCancellationRequested) SetIndex(_current + 1);
        }
    }
}