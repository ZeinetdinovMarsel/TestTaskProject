using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MenuButtonsManager : MonoBehaviour
{
    [SerializeField] private List<ButtonBehaviour> _buttons;

    private void Awake()
    {
        _buttons = GetComponentsInChildren<ButtonBehaviour>().ToList();

        foreach (var button in _buttons)
        {
            button.OnButtonClicked.AddListener(() => DeselectAllExcept(button));
        }
    }

    private void DeselectAllExcept(ButtonBehaviour selected)
    {
        foreach (var btn in _buttons)
        {
            btn.ButtonSelected = (btn == selected);
        }
    }
}