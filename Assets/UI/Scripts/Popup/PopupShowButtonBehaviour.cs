using PrimeTween;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

public enum ArtCellType
{
    Default,
    Premium
}
public class PopupShowButtonBehaviour : MonoBehaviour
{
    [Inject(Id = "DefaultPopup")] private PopupBehaviour _defaultPopup;
    [Inject(Id = "PremiumPopup")] private PopupBehaviour _premiumPopup;

    private Button _popupButton;
    [SerializeField] private Image _premiumImage;

    private ArtCellType _artCellType = ArtCellType.Default;
    public ArtCellType ArtCellType
    {
        get => _artCellType;
        set
        {
            _artCellType = value;
            bool isPremium = value == ArtCellType.Premium;
            _premiumImage.gameObject.SetActive(isPremium);
            if (isPremium)
                Tween.Scale(_premiumImage.transform, 0, 1, 1, Ease.InOutBounce);
        }
    }
    private ArtCell _artCell;
    public void SetArtCell(ArtCell artCell)
    {
        _artCell = artCell;
    }
    private void Awake()
    {
        _popupButton = GetComponent<Button>();
        _popupButton.onClick.AddListener(() =>
        {
            switch (ArtCellType)
            {
                case ArtCellType.Default:
                    _defaultPopup.gameObject.SetActive(true);
                    _premiumPopup.HidePopup();
                    _defaultPopup.ShowPopup();

                    if(_defaultPopup is DefaultPopupBehaviour defaultPopupBehaviour)
                    {
                        defaultPopupBehaviour.SetContent(_artCell.GetSprite());
                    }
                    break;
                case ArtCellType.Premium:
                    _premiumPopup.gameObject.SetActive(true);
                    _defaultPopup.HidePopup();
                    _premiumPopup.ShowPopup();
                    break;
            }
        });

    }
}
