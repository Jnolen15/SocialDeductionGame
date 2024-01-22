using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayRandomSound : MonoBehaviour
{
    // ============== Variables / Refrences ==============
    #region Variables / Refrences
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip[] _randomSounds;
    #endregion

    // ============== Function ==============
    public void PlayRandom()
    {
        int rand = Random.Range(0, _randomSounds.Length);

        _audioSource.PlayOneShot(_randomSounds[rand]);
    }
}
