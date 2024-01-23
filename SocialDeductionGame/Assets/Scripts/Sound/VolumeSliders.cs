using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class VolumeSliders : MonoBehaviour
{
    // ============== Variables / Refrences ==============
    [SerializeField] private Slider _ambienceSlider;
    [SerializeField] private Slider _sfxSlider;
    [SerializeField] private AudioMixer _audioMixer;

    private void OnEnable()
    {
        float savedAmbVol = PlayerPrefs.GetFloat("AmbienceVol");
        float savedSFXVol = PlayerPrefs.GetFloat("SFXVol");

        if (savedAmbVol > 0.001 && savedAmbVol <= 1)
        {
            _audioMixer.SetFloat("AmbienceVol", Mathf.Log10(savedAmbVol) * 20);
            _ambienceSlider.value = savedAmbVol;
        }

        if (savedSFXVol > 0.001 && savedSFXVol <= 1)
        {
            _audioMixer.SetFloat("SFXVol", Mathf.Log10(savedSFXVol) * 20);
            _audioMixer.SetFloat("UIVol", Mathf.Log10(savedSFXVol) * 20);
            _sfxSlider.value = savedSFXVol;
        }
    }

    // ============== Functions ==============
    public void SetAmbienceVolume(float vol)
    {
        PlayerPrefs.SetFloat("AmbienceVol", vol);

        _audioMixer.SetFloat("AmbienceVol", Mathf.Log10(vol) * 20);
    }

    public void SetSFXVolume(float vol)
    {
        PlayerPrefs.SetFloat("SFXVol", vol);

        _audioMixer.SetFloat("SFXVol", Mathf.Log10(vol) * 20);
        _audioMixer.SetFloat("UIVol", Mathf.Log10(vol) * 20);
    }
}
