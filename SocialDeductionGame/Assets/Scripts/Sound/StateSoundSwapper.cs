using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StateSoundSwapper : MonoBehaviour
{
    // ============== Variables / Refrences ==============
    #region Variables / Refrences
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _morningClip;
    [SerializeField] private AudioClip _eveningClip;
    #endregion

    // ============== Setup ==============
    #region Setup
    private void Awake()
    {
        GameManager.OnStateChange += UpdateAudio;
        ExileManager.OnTrialVoteStarted += PauseAudio;
        ExileManager.OnTrialVoteEnded += PlayAudio;
    }

    private void OnDestroy()
    {
        GameManager.OnStateChange -= UpdateAudio;
        ExileManager.OnTrialVoteStarted -= PauseAudio;
        ExileManager.OnTrialVoteEnded -= PlayAudio;
    }
    #endregion

    // ============== Function ==============
    private void UpdateAudio(GameManager.GameState prev, GameManager.GameState cur)
    {
        if(cur == GameManager.GameState.Morning && _morningClip)
        {
            _audioSource.clip = _morningClip;
            _audioSource.Play();
        }
        else if (cur == GameManager.GameState.Evening && _eveningClip)
        {
            _audioSource.clip = _eveningClip;
            _audioSource.Play();
        }
    }

    private void PauseAudio()
    {
        _audioSource.Pause();
    }

    private void PlayAudio()
    {
        _audioSource.Play();
    }
}
