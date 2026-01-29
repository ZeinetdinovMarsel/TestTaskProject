using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class SimpleSoundManager : MonoBehaviour
{
    [SerializeField] private AudioSource _carouselAudioSource;
    [SerializeField] private AudioSource _buttonsAudioSource;

    private void Start()
    {
        FindAnyObjectByType<CarouselController>().Logic.OnIndexChanged.AddListener((id) => PlaySound(_carouselAudioSource));
        FindObjectsByType<Button>(FindObjectsInactive.Include,FindObjectsSortMode.InstanceID).ToList().ForEach((btn) => btn.onClick.AddListener(() => PlaySound(_buttonsAudioSource)));
    }


    public void PlaySound(AudioSource audioSource)
    {
        if (!audioSource.isPlaying)
        {
            audioSource.PlayOneShot(audioSource.clip);
        }
    }
}
