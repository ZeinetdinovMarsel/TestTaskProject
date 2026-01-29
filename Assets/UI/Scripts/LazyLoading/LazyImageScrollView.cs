using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.Pool;
using UnityEngine.UI;
using Zenject;

public enum FilterMode { All, Even, Odd }

public class LazyImageScrollView : MonoBehaviour
{
    [SerializeField] private ScrollRect _scrollRect;
    [SerializeField] private RectTransform _viewport;
    [SerializeField] private RectTransform _content;
    [SerializeField] private GridLayoutGroup _gridLayout;
    [SerializeField] private ArtCell _cellPrefab;
    [SerializeField] private float _fadeDuration = 0.25f;
    [SerializeField] private FilterMode _filterMode = FilterMode.All;
    private LoadingCircle _loadingCircle;
    [Inject] private DiContainer _container;

    private ObjectPool<ArtCell> _cellPool;
    private readonly List<ArtCell> _activeCells = new();
    private readonly List<ArtCell> _visibleCells = new();

    private readonly string _serverUrl = "https://data.ikppbb.com/test-task-unity-data/pics/";
    private static readonly Vector3[] _corners = new Vector3[4];
    private float _viewportWorldMin => _viewport.TransformPoint(new Vector3(0, _viewport.rect.yMin, 0)).y;
    private float _viewportWorldMax => _viewport.TransformPoint(new Vector3(0, _viewport.rect.yMax, 0)).y;
    private UniTask _updateTask;
    private bool _updateRunning => !_updateTask.Status.IsCompleted();
    private CancellationTokenSource _lifetimeCts;
    private CancellationTokenSource _updateCts;


    private int _bonusLoadCells = 2;

    public UnityEvent OnCellInstatiated = new();
    private void Awake()
    {
        _lifetimeCts = new CancellationTokenSource();
        _updateCts = CancellationTokenSource.CreateLinkedTokenSource(_lifetimeCts.Token);
        InitializeImagePool();
        _loadingCircle = GetComponentInChildren<LoadingCircle>();
    }
    private void OnDisable()
    {
        CancelTasks();
    }

    private void OnDestroy()
    {
        CancelTasks();
    }

    private void CancelTasks()
    {
        if (_lifetimeCts == null) return;

        _lifetimeCts.Cancel();
        _lifetimeCts.Dispose();
        _lifetimeCts = null;

        _updateCts?.Cancel();
        _updateCts?.Dispose();
        _updateCts = null;
    }

    private void Start()
    {
        SetInitialArts();
        _scrollRect.onValueChanged.AddListener(OnScrollValueChanged);
    }

    private void OnScrollValueChanged(Vector2 pos)
    {
        TryUpdateVisible();
        UpdateVisibleCells();
    }

    private void SetInitialArts()
    {
        UniTask.DelayFrame(1).ContinueWith(() =>
        {
            TryUpdateVisible();
        }).Forget();
    }

    private void InitializeImagePool()
    {
        _cellPool = new ObjectPool<ArtCell>(
            () => _container.InstantiatePrefabForComponent<ArtCell>(_cellPrefab, _content),
            cell => cell.ResetForReuse(),
            cell => cell.gameObject.SetActive(false)
        );

    }

    private ArtCell GetArtCellFromPool(int id)
    {
        id++;
        var cell = _cellPool.Get();
        cell.Setup(id);

        _activeCells.Add(cell);


        if (FilterCondition(_filterMode, id))
        {
            if (!_visibleCells.Contains(cell))
                _visibleCells.Add(cell);
            else
                _visibleCells[id].gameObject.SetActive(true);

            

            int visibleIndex = _visibleCells.Count - 1;
            if (cell.PopupBehaviour != null)
            {
                cell.PopupBehaviour.ArtCellType = ((visibleIndex + 1) % 4 == 0)
                    ? ArtCellType.Premium
                    : ArtCellType.Default;
            }
        }
        else
        {
            cell.gameObject.SetActive(false);
        }

        return cell;
    }

    private void ApplyFilter(FilterMode filter)
    {
        _filterMode = filter;
        _visibleCells.Clear();

        foreach (var cell in _activeCells)
        {
            bool visible = FilterCondition(filter, cell.Id);
            cell.gameObject.SetActive(visible);
            if (visible) _visibleCells.Add(cell);
        }
        for (int i = 0; i < _visibleCells.Count; i++)
        {
            var artCell = _visibleCells[i];
            if (artCell.PopupBehaviour != null)
            {
                artCell.PopupBehaviour.ArtCellType = ((i + 1) % 4 == 0)
                    ? ArtCellType.Premium
                    : ArtCellType.Default;
            }
        }
    }

    private bool FilterCondition(FilterMode filter, int id) =>
                (filter == FilterMode.All ||
                (filter == FilterMode.Even && id % 2 == 0) ||
                (filter == FilterMode.Odd && id % 2 != 0));

    public void UpdateVisibleCell(ArtCell cell)
    {
        if (!FilterCondition(_filterMode, cell.Id))
    {
        cell.SetVisible(false, _fadeDuration);
        return;
    }


        cell.Rect.GetWorldCorners(_corners);

        float minY = _corners[0].y;
        float maxY = _corners[1].y;

        bool shouldBeVisible =
            maxY > _viewportWorldMin &&
            minY < _viewportWorldMax;

        cell.SetVisible(shouldBeVisible, _fadeDuration);
        if (shouldBeVisible) { LoadImage(cell).Forget(); OnCellInstatiated?.Invoke(); }
    }

    private void TryUpdateVisible()
    {
        if (_updateRunning || _updateCts.IsCancellationRequested) return;
        _updateTask = UpdateVisible(_updateCts.Token);
    }

    private async UniTask UpdateVisible(CancellationToken token)
    {
        await UniTask.WaitForEndOfFrame(this, token);

        if (token.IsCancellationRequested) return;

        if (!token.IsCancellationRequested &&
               NeedMoreCells())
            await EnsureEnoughCells(token);
        else
            UpdateVisibleCells();
    }
    private void UpdateVisibleCells()
    {
        foreach (var cell in _visibleCells)
        {
            UpdateVisibleCell(cell);
        }
    }
    private bool NeedMoreCells()
    {
        if (_visibleCells.Count == 0 && _activeCells.Count == 0)
            return true;

        ArtCell lastCell =
            _visibleCells.Count > 0
                ? _visibleCells[^1]
                : _activeCells[^1];

        lastCell.Rect.GetWorldCorners(_corners);

        float lastCellWorldMinY = _corners[0].y;

        float preloadOffset =
            _gridLayout.cellSize.y * _bonusLoadCells;

        return lastCellWorldMinY + preloadOffset > _viewportWorldMin;
    }

    private async UniTask EnsureEnoughCells(CancellationToken token)
    {
        while (!token.IsCancellationRequested &&
               NeedMoreCells())
        {
            int nextId = _activeCells.Count;

            if (!await CheckImageExists(nextId + 1, token))
                break;

            var cell = GetArtCellFromPool(nextId);

            await UniTask.DelayFrame(1, cancellationToken: token);
            UpdateVisibleCell(cell);
        }
    }

    private async UniTask<bool> CheckImageExists(int id, CancellationToken token)
    {
        using var headReq = UnityWebRequest.Head($"{_serverUrl}{id}.jpg");
        try
        {
            _loadingCircle.StartLoadAnim();
            await headReq.SendWebRequest().WithCancellation(token);
            _loadingCircle.StopLoadAnim();
            return headReq.result == UnityWebRequest.Result.Success;
        }
        catch
        {
            return false;
        }
    }

    private async UniTaskVoid LoadImage(ArtCell cell)
    {
        if (cell.LoadState != CellLoadState.None)
            return;

        cell.BeginLoad();
        int expectedId = cell.Id;
        using var req = UnityWebRequestTexture.GetTexture($"{_serverUrl}{expectedId}.jpg");
        await req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success || cell.Token.IsCancellationRequested)
            return;

        if (cell.Id != expectedId)
            return;
        var tex = ((DownloadHandlerTexture)req.downloadHandler).texture;
        cell.SetSprite(tex, _fadeDuration);
    }

    public void SetFilter(FilterMode mode = FilterMode.All)
    {
        _filterMode = mode;
        ApplyFilter(mode);
        UniTask.DelayFrame(1).ContinueWith(async () =>
        {
            await RestartUpdateTask();
        }).Forget();
    }
    private async UniTask RestartUpdateTask()
    {
        _updateCts?.Cancel();
        var isCanceled = await _updateTask.SuppressCancellationThrow();
        _updateCts?.Dispose();
        _updateCts = CancellationTokenSource.CreateLinkedTokenSource(_lifetimeCts.Token);
        _updateTask = UpdateVisible(_updateCts.Token);
    }


    public void ShowEvenOnly() => SetFilter(FilterMode.Even);
    public void ShowOddOnly() => SetFilter(FilterMode.Odd);
    public void ShowAll() => SetFilter(FilterMode.All);
}
