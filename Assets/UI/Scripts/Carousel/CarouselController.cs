using Cysharp.Threading.Tasks;
using PrimeTween;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CarouselController : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
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
    private CarouselLogic _logic;
    private CarouselAnimator _animator;
    private Vector2 _pointerStartLocal;
    private Vector2 _contentStartAnch;
    private Vector2 _lastPointerPos;
    private float _lastPointerTime;
    private List<RectTransform> _panelRects;

    public CarouselLogic Logic=>_logic;

    private void Awake()
    {
        if (_viewport == null) _viewport = (RectTransform)transform;
        _panelRects = _panels.Select(p => p.rectTransform).ToList();
        _dots = Enumerable.Range(0, _panels.Count)
            .Select(_ => Instantiate(_dotPrefab, _dotsContainer).GetComponent<DotBehaviour>())
            .ToList();
        _logic = new CarouselLogic(_panels.Count, _autoScrollInterval, _velocityThreshold);
        _animator = new CarouselAnimator(_content, _panelRects, _viewport, _snapDuration);
        _logic.OnIndexChanged.AddListener(UpdateDots);
        _logic.OnSnapRequested.AddListener(_animator.SnapTo);
        UpdateDots(0);
    }

    private async UniTask Start()
    {
        await UniTask.DelayFrame(2);
        _logic.StartAutoScroll();
        _logic.SetIndex(0, 0.001f);
    }

    private void OnDestroy() => _logic?.StopAutoScroll();

    private void OnDisable() => _logic?.StopAutoScroll();

    private void UpdateDots(int idx)
    {
        for (int i = 0; i < _dots.Count; i++)
            _dots[i].SetActiveDot(i == idx);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        _logic.StopAutoScroll();
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_viewport, eventData.position, eventData.pressEventCamera, out _pointerStartLocal);
        _contentStartAnch = _content.anchoredPosition;
        _lastPointerPos = _pointerStartLocal;
        _lastPointerTime = Time.unscaledTime;
        _animator.StopSnap();
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
        float velocity = 0f;
        bool hasEndLocal = RectTransformUtility.ScreenPointToLocalPointInRectangle(_viewport, eventData.position, eventData.pressEventCamera, out Vector2 endLocal);
        if (hasEndLocal)
        {
            float dt = Mathf.Max(0.001f, Time.unscaledTime - _lastPointerTime);
            velocity = (endLocal.x - _lastPointerPos.x) / dt;
        }
        int closest = ComputeClosest();
        _logic.DecideSnap(velocity, closest);
        UniTask.Delay(1000).ContinueWith(_logic.StartAutoScroll).Forget();
    }

    private int ComputeClosest()
    {
        if (_panels.Count == 0) return 0;
        float viewportWidth = _viewport.rect.width;
        float contentWidth = _content.rect.width;
        float normalized = Mathf.InverseLerp(0, contentWidth - viewportWidth, -_content.anchoredPosition.x);
        int idx = Mathf.RoundToInt(normalized * (_panels.Count - 1));
        return idx;
    }
}