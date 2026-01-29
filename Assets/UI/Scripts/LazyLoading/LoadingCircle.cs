using PrimeTween;
using UnityEngine;
using UnityEngine.UI;

public class LoadingCircle : MonoBehaviour
{
    [SerializeField] private Image _loader;
    [SerializeField] private float _speed = 2;

    private void Awake()
    {
        _loader = GetComponent<Image>();
    }
    public void StartLoadAnim()
    {
        _loader.enabled = true;
        Tween.LocalEulerAngles(_loader.transform, Vector3.zero, new Vector3(0, 0, -360), _speed, Ease.Linear, -1);
    }

    public void StopLoadAnim()
    {
        _loader.enabled = false;
        Tween.StopAll(_loader.transform);
    }
}
