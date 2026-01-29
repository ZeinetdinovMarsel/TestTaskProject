using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class SimpleSoundManager : MonoBehaviour
{
    [SerializeField] private AudioSource _carouselAudioSource;
    [SerializeField] private AudioSource _buttonsAudioSource;
    [SerializeField] private AudioSource _otherAudioSource;

    private void Start()
    {
        FindAnyObjectByType<CarouselController>().Logic.OnIndexChanged.AddListener((id) => _carouselAudioSource.Play());
        FindObjectsByType<Button>(FindObjectsInactive.Include,FindObjectsSortMode.InstanceID).ToList().ForEach((btn) => btn.onClick.AddListener(() => _buttonsAudioSource.Play()));
        FindAnyObjectByType<LazyImageScrollView>().OnCellInstatiated.AddListener(() => _otherAudioSource.Play());

    }
}
