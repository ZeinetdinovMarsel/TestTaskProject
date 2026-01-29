using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class DefaultPopupBehaviour : PopupBehaviour
{
    [SerializeField]private Image _contentImage;

    public void SetContent(Sprite content)
    {
        _contentImage.sprite = content;
    }
}
