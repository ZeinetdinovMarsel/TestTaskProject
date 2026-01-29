using Cysharp.Threading.Tasks;
using PrimeTween;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Pool;
using UnityEngine.UI;

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
    [SerializeField] private int _serverImageCount = 66;
    private ObjectPool<ArtCell> _cellPool;
    [SerializeField] private List<ArtCell> _activeCells = new();
    [SerializeField] private List<ArtCell> _visibleCells = new();

    private readonly string _serverUrl = "https://data.ikppbb.com/test-task-unity-data/pics/";
    private float _prevScrollY;

    [SerializeField] private float _viewportWorldMin;
    [SerializeField] private float _lowestCoord;



    private void Awake()
    {
        InitializeImagePool();

        SetData();
        _prevScrollY = _scrollRect.verticalNormalizedPosition;
        _scrollRect.onValueChanged.AddListener(OnScrollValueChanged);

    }

    private void Start()
    {
        SetInitialArts();
        UpdateVisible();
    }

    private void OnScrollValueChanged(Vector2 pos)
    {
        float delta = pos.y - _prevScrollY;

        if (Mathf.Abs(delta) < 0.0001f) return;


        UpdateVisible();


        _prevScrollY = pos.y;
    }

    private void SetInitialArts()
    {
        UniTask.DelayFrame(1).ContinueWith(() =>
        {
            float viewportHeight = _viewport.rect.height;
            float cellHeight = _gridLayout.cellSize.y + _gridLayout.spacing.y;
            int visibleRows = Mathf.CeilToInt(viewportHeight / cellHeight) + 1;

            for (int i = 0; i < visibleRows * _gridLayout.constraintCount; i++)
            {
                var cell = _cellPool.Get();
                cell.IdText.text = i.ToString();

                LoadImageAndShowAsync(cell).Forget();
            }
            ApplyFilter();
        }).Forget();


    }

    private void InitializeImagePool()
    {
        _cellPool = new ObjectPool<ArtCell>(
            () => Instantiate(_cellPrefab, _content),
            cell =>
            {
                cell.gameObject.SetActive(true);
                cell.CanvasGroup.alpha = 0;

                cell.ArtImage.color = Color.white;
                cell.LoadingImage.enabled = false;
                UniTask.DelayFrame(1).ContinueWith(() =>
                {
                    UpdateVisibleCell(cell);

                }).Forget();
                _activeCells.Add(cell);
                ApplyFilter();
            },
            cell => cell.gameObject.SetActive(false),
            defaultCapacity: 10

        );
    }

    public void UpdateVisibleCell(ArtCell cell)
    {
        if (cell.Rect.position.y >= _viewportWorldMin && cell.CanvasGroup.alpha == 0)
        {
            cell.gameObject.SetActive(true);
            Tween.Alpha(cell.CanvasGroup, 0f, 1f, _fadeDuration, Ease.InQuad);
        }
        else if (cell.Rect.position.y < _viewportWorldMin && cell.CanvasGroup.alpha == 1)
        {
            Tween.Alpha(cell.CanvasGroup, 1f, 0f, _fadeDuration, Ease.InQuad)
                .OnComplete(() => cell.gameObject.SetActive(false));
        }



    }

    public void SetData(FilterMode filterMode = FilterMode.All)
    {
        _filterMode = filterMode;
        ApplyFilter();
        UpdateVisible();
    }

    private void ApplyFilter()
    {
        _visibleCells.Clear();

        for (int i = 0; i < _activeCells.Count; i++)
        {
            var cell = _activeCells[i];
            cell.Id = i;

            bool isVisible = _filterMode switch
            {
                FilterMode.All => true,
                FilterMode.Even => i % 2 == 0,
                FilterMode.Odd => i % 2 != 0,
                _ => true
            };

            cell.gameObject.SetActive(isVisible);

            if (isVisible)
                _visibleCells.Add(cell);
        }
        Canvas.ForceUpdateCanvases();
        if (_visibleCells.Count > 0)
        {
            _lowestCoord = _visibleCells[Mathf.Clamp(_visibleCells.Count - 3, 0, _visibleCells.Count)].Rect.position.y;
        }
    }





    private void UpdateVisible()
    {
        _viewportWorldMin = _viewport.TransformPoint(new Vector3(0, _viewport.rect.yMin, 0)).y;

        var viewportLocalPos = _content.InverseTransformPoint(_viewport.position);
        var viewportRect = new Rect(
            -_viewport.rect.width * 0.5f,
            -viewportLocalPos.y - _viewport.rect.height * 0.5f,
            _viewport.rect.width,
            _viewport.rect.height
        );

        for (int i = 0, j = 0; i < _activeCells.Count; i++)
        {
            _activeCells[i].IdText.text = _activeCells[i].Id.ToString();
            if (_visibleCells.Count > 0 && j < _visibleCells.Count)
            {
                if (_activeCells[i].Id == _visibleCells[j].Id)
                {
                    _activeCells[i].IdText.color = Color.red;
                    j++;
                }
                else
                {
                    _activeCells[i].IdText.color = Color.black;
                }
            }
            else
            {
                _activeCells[i].IdText.color = Color.black;
            }
        }

        for (int i = 0; i < _visibleCells.Count; i++)
        {
            var cell = _visibleCells[i];
            UpdateVisibleCell(cell);
            if (_lowestCoord > _viewportWorldMin && _activeCells.Count < _serverImageCount)
            {
                _cellPool.Get();
            }

            if (cell.LoadState == CellLoadState.None)
            {
                LoadImageAndShowAsync(cell).Forget();
            }


        }
        if (_visibleCells.Count > 0)
        {
            _lowestCoord = _visibleCells[Mathf.Clamp(_visibleCells.Count - 3, 0, _visibleCells.Count)].Rect.position.y;
        }

    }


    private async UniTaskVoid LoadImageAndShowAsync(ArtCell cell)
    {
        if (cell.LoadState != CellLoadState.None)
            return;
        var token = cell.Cts.Token;
        var cg = cell.CanvasGroup;
        try
        {
            cell.LoadingImage.enabled = true;
            StartLoadAnim(cell);
            

            cell.LoadState = CellLoadState.Loading;

            using var req = UnityWebRequestTexture.GetTexture($"{_serverUrl}{cell.Id + 1}.jpg");
            var op = req.SendWebRequest();
            while (!op.isDone)
            {
                if (token.IsCancellationRequested) { req.Abort(); return; }
                await UniTask.Yield();
            }

            if (req.result != UnityWebRequest.Result.Success) return;

            var tex = ((DownloadHandlerTexture)req.downloadHandler).texture;
            if (tex == null) return;

            var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            if (token.IsCancellationRequested) return;

            cell.ArtImage.sprite = sprite;
            cell.ArtImage.color = Color.white;
            await Tween.Alpha(cg, 0f, 1f, _fadeDuration, Ease.InQuad).ToUniTask(cancellationToken: token);
        }
        catch (OperationCanceledException) { }
        catch (Exception) { }
        finally
        {
            StopLoadAnim(cell);
            cell.LoadingImage.enabled = false;
            cell.LoadState = CellLoadState.Loaded;
        }
    }


    private void StartLoadAnim(ArtCell cell)
    {
        cell.LoadingImage.enabled = true;
        Tween.LocalEulerAngles(cell.LoadingImage.transform, Vector3.zero, new Vector3(0, 0, -360), 2f, Ease.Linear, -1);
    }

    private void StopLoadAnim(ArtCell cell)
    {
        cell.LoadingImage.enabled = false;
        Tween.StopAll(cell.transform);
    }

    public void SetFilter(FilterMode mode)
    {
        if (_filterMode == mode) return;
        _filterMode = mode;
        ApplyFilter();
        UpdateVisible();
    }

    public void ShowEvenOnly() => SetFilter(FilterMode.Even);
    public void ShowOddOnly() => SetFilter(FilterMode.Odd);
    public void ShowAll() => SetFilter(FilterMode.All);
}
