using UnityEngine;
using UnityEngine.UI;

public class DotBehaviour : MonoBehaviour
{
    [SerializeField] private Sprite _activeSprite; 
    [SerializeField] private Sprite _inActiveSprite;

    private Image _dotImage;

    private void Awake()
    {
        _dotImage = GetComponent<Image>();
    }

    public void SetActiveDot(bool isActive)
    {
        _dotImage.sprite = isActive ? _activeSprite : _inActiveSprite;
    }
}
