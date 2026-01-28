using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ArtCell : MonoBehaviour
{
    [SerializeField] private Image _contentImage;
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private Image _loadingImage;
    [SerializeField] private TMP_Text _idText;
    private CancellationTokenSource _cts;
    [SerializeField] private int _id;

    public Image ArtImage => _contentImage;
    public CanvasGroup CanvasGroup => _canvasGroup;
    public RectTransform Rect => (RectTransform)transform;
    public Image LoadingImage => _loadingImage;
    public TMP_Text IdText => _idText;
    public CancellationTokenSource Cts=>_cts;

    private void Awake()
    {
        SetLoadingToken(CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken));
    }
    public int Id
    {
        get { return _id; }
        set { _id = value; }
    }

    public void CancelLoad()
    {
        try { _cts?.Cancel(); } catch { }
        _cts = null;
    }

    public void SetLoadingToken(CancellationTokenSource cts)
    {
        CancelLoad();
        _cts = cts;
    }

    public void ResetForReuse()
    {
        CancelLoad();
        if (_canvasGroup != null) _canvasGroup.alpha = 0f;
        if (_contentImage != null) { _contentImage.sprite = null; _contentImage.color = Color.clear; }
        if (_loadingImage != null) _loadingImage.enabled = false;
        gameObject.SetActive(true);
    }
}