using PrimeTween;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

public abstract class PopupBehaviour : MonoBehaviour
{
    protected CanvasGroup _targetPopupCG;
    [SerializeField] private Button _closeButton;
    private Sequence _appearingSequence;
    [Inject]
    protected virtual void InitPopup()
    {
        _targetPopupCG = GetComponent<CanvasGroup>();
        _closeButton.onClick.AddListener(() =>
        {
            AnimateAppearing(_targetPopupCG, true);
        });

    }
    private void AnimateAppearing(CanvasGroup targetPopupCG, bool hide)
    {
        _appearingSequence.Complete();
        var startValue = hide ? 1 : 0;
        var endValue = hide ? 0 : 1;

        var duration = 1f;
        _appearingSequence = Sequence.Create()
            .Group(Tween.Scale(transform, startValue, endValue, duration, Ease.OutBack))
            .Group(Tween.Alpha(targetPopupCG, startValue, endValue, duration))
            .OnComplete(() =>
            {
                if (hide) targetPopupCG.gameObject.SetActive(false);
            });

    }

    public virtual void ShowPopup()
    {
        AnimateAppearing(_targetPopupCG, false);
    }

    public virtual void HidePopup()
    {
        AnimateAppearing(_targetPopupCG, true);
    }
}
