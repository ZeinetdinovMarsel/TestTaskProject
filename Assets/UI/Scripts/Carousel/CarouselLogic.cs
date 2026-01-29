using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;

public class CarouselLogic
{
    public UnityEvent<int> OnIndexChanged = new();
    public UnityEvent<int, float> OnSnapRequested = new();

    private int _current;
    private int _panelCount;
    private float _autoScrollInterval;
    private float _velocityThreshold;
    private CancellationTokenSource _autoScrollCts;

    public CarouselLogic(int panelCount, float autoScrollInterval, float velocityThreshold)
    {
        _panelCount = panelCount;
        _autoScrollInterval = autoScrollInterval;
        _velocityThreshold = velocityThreshold;
    }

    public void SetIndex(int idx, float duration = -1f)
    {
        idx = Mathf.Clamp(idx, 0, _panelCount - 1);
        if (idx == _current)
        {
            OnSnapRequested.Invoke(idx, duration);
            return;
        }
        _current = idx;
        OnIndexChanged.Invoke(idx);
        OnSnapRequested.Invoke(idx, duration);
    }

    public void DecideSnap(float velocity, int closest)
    {
        int target = closest;
        if (velocity > _velocityThreshold)
            target = closest - 1;
        else if (velocity < -_velocityThreshold)
            target = closest + 1;
        SetIndex(target);
    }

    public void StartAutoScroll()
    {
        if (_panelCount <= 1 || _autoScrollInterval <= 0) return;
        StopAutoScroll();
        _autoScrollCts = new CancellationTokenSource();
        AutoScrollLoop(_autoScrollCts.Token).Forget();
    }

    public void StopAutoScroll()
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