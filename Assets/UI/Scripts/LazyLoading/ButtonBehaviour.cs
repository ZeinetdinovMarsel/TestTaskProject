using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ButtonBehaviour : MonoBehaviour
{
    [SerializeField] private Color _selectedColor;
    [SerializeField] private Color _defaultColor = Color.black;

    [SerializeField] private TMP_Text _buttonText;
    [SerializeField] private Image _outline;
    private Button _button;
    private bool _buttonSelected = false;

    public UnityEvent OnButtonClicked = new();
    public bool ButtonSelected
    {
        get { return _buttonSelected; }
        set
        {
            _buttonSelected = value;
            ChangeButtonSelect();
        }
    }

    private void Awake()
    {
        _buttonText = GetComponentInChildren<TMP_Text>();
        _button = GetComponent<Button>();
        _button.onClick.AddListener(OnButtonSelectChanged);
    }

    private void OnButtonSelectChanged()
    {
        OnButtonClicked?.Invoke();
        _buttonSelected = true;
        ChangeButtonSelect();
    }

    private void ChangeButtonSelect()
    {
        float duration = 0.4f;
        Tween.CompleteAll(_outline.transform);
        Tween.CompleteAll(_buttonText);
        if (_buttonSelected)
        {
            _outline.enabled = true;
            Tween.ScaleX(_outline.transform, 0, 1, duration, Ease.InOutExpo);
        }
        else
        {
            Tween.ScaleX(_outline.transform, 1, 0f, duration, Ease.InOutExpo)
                 .OnComplete(() => _outline.enabled = false);
        }

        Color currentColor = _buttonText.color;
        Color targetColor = _buttonSelected ? _selectedColor : _defaultColor;

        Tween.Color(_buttonText, currentColor, targetColor, duration, Ease.InOutExpo);
    }
}
