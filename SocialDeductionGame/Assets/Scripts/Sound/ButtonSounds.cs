using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ButtonSounds : MonoBehaviour
{
    // ============== Variables / Refrences ==============
    #region Variables / Refrences
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip[] _hoverSounds;
    [SerializeField] private AudioClip[] _clickSounds;
    #endregion

    // ============== Function ==============
    public void PlayRandomHover()
    {
        int rand = Random.Range(0, _hoverSounds.Length);

        _audioSource.PlayOneShot(_hoverSounds[rand]);
    }

    public void PlayRandomClick()
    {
        int rand = Random.Range(0, _clickSounds.Length);

        _audioSource.PlayOneShot(_clickSounds[rand]);
    }
}
