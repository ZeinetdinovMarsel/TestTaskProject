using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using PrimeTween;

public class ArtCell : MonoBehaviour
{
    [SerializeField] private Image _image;

    [SerializeField] private CanvasGroup _cg;
    [SerializeField] private TMP_Text _idText;

    private LoadingCircle _loadingCircle;

    private CancellationTokenSource _cts;
    private bool _visible;

    public int Id { get; private set; }
    public RectTransform Rect => (RectTransform)transform;
    public CellLoadState LoadState { get; private set; }
    public CancellationToken Token => _cts.Token;

    private Transform _contentObj;

    private Sequence _visibleSequence;

    private PopupShowButtonBehaviour _popupBehaviour;

    public PopupShowButtonBehaviour PopupBehaviour => _popupBehaviour;

    private void Awake()
    {
        _popupBehaviour = GetComponentInChildren<PopupShowButtonBehaviour>();
        _popupBehaviour.SetArtCell(this);
        _contentObj = transform.GetChild(0);
        _loadingCircle = GetComponentInChildren<LoadingCircle>();
    }

    public void Setup(int id)
    {
        Id = id;
        _idText.text = id.ToString();
    }

    public void ResetForReuse()
    {
        Cancel();
        _cg.alpha = 0;
        _visible = false;
        ClearSprite();
        gameObject.SetActive(true);
        LoadState = CellLoadState.None;
    }

    public void SetVisible(bool visible, float duration)
    {
        if (_visible == visible) return;
        _visible = visible;
        _visibleSequence.Complete();

        if (visible) { _contentObj.gameObject.SetActive(true); }

        _visibleSequence= Sequence.Create()
            .Group(Tween.Alpha(_cg, _cg.alpha, visible ? 1 : 0, duration, Ease.InQuad))
            .Group(Tween.Scale(_cg.transform, _cg.transform.localScale.x, visible ? 1 : 0, duration, Ease.InQuad))
            .OnComplete(() =>
            {
                if (!_visible)
                {
                    //Cancel();
                    //ClearSprite();
                    //LoadState = CellLoadState.None;
                    _contentObj.gameObject.SetActive(false);
                }
            });
    }


    public void BeginLoad()
    {
        Cancel();
        _cts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
        _loadingCircle.StartLoadAnim();
        LoadState = CellLoadState.Loading;
    }

    public void SetSprite(Texture2D tex, float fade)
    {
        var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
            new Vector2(0.5f, 0.5f), 100);

        _image.sprite = sprite;
        _image.color = Color.white;

        Tween.StopAll(_image);
        Tween.Alpha(_image, 0, 1, fade);
        LoadState = CellLoadState.Loaded;
        _loadingCircle.StopLoadAnim();
    }

    public Sprite GetSprite()
    {
        return _image.sprite;
    }

    private void ClearSprite()
    {
        if (_image.sprite == null) return;

        var tex = _image.sprite.texture;
        Destroy(_image.sprite);
        Destroy(tex);
        _image.sprite = null;
    }

    private void Cancel()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }
}

public enum CellLoadState
{
    None,
    Loading,
    Loaded
}
