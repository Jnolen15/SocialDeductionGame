using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TotemSounds : MonoBehaviour
{
    // ============== Variables / Refrences ==============
    #region Variables / Refrences
    [Header("UI Sounds")]
    [SerializeField] private AudioSource _uiAudioSource;
    [SerializeField] private AudioClip _addCorrect;
    [SerializeField] private AudioClip _addIncorrect;
    [SerializeField] private AudioClip[] _openSounds;
    [SerializeField] private AudioClip[] _closeSounds;
    [Header("In Game Sounds")]
    [SerializeField] private AudioSource _gameAudioSource;
    [SerializeField] private AudioClip _prepSFX;
    [SerializeField] private AudioClip _deactivateSFX;
    #endregion

    // ============== Function ==============
    public void PlayAddCorrect()
    {
        _uiAudioSource.PlayOneShot(_addCorrect);
    }

    public void PlayAddIncorrect()
    {
        _uiAudioSource.PlayOneShot(_addIncorrect);
    }

    public void PlayOpen()
    {
        int rand = Random.Range(0, _openSounds.Length);

        _uiAudioSource.PlayOneShot(_openSounds[rand]);
    }

    public void PlayClose()
    {
        int rand = Random.Range(0, _closeSounds.Length);

        _uiAudioSource.PlayOneShot(_closeSounds[rand]);
    }

    public void PlayPrepped()
    {
        _gameAudioSource.PlayOneShot(_prepSFX);
    }

    public void PlayDeactivated()
    {
        _gameAudioSource.PlayOneShot(_deactivateSFX);
    }
}
