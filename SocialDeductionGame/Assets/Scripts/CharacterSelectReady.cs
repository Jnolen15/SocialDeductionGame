using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CharacterSelectReady : MonoBehaviour
{
    // ============== Refrences ==============
    [SerializeField] private Image _buttonSprite;
    [SerializeField] private bool _localPlayerReady;

    // ============== Setup ==============
    private void Awake()
    {
        PlayerConnectionManager.OnPlayerReady += Ready;
        PlayerConnectionManager.OnPlayerUnready += Unready;
    }

    private void OnDestroy()
    {
        PlayerConnectionManager.OnPlayerReady -= Ready;
        PlayerConnectionManager.OnPlayerUnready -= Unready;
    }

    // ============== Functions ==============
    public void ToggleReadyPlayer()
    {
        if(!_localPlayerReady)
            PlayerConnectionManager.Instance.ReadyPlayer();
        else
            PlayerConnectionManager.Instance.UnreadyPlayer();
    }

    private void Ready()
    {
        _buttonSprite.color = Color.green;
        _localPlayerReady = true;
    }

    private void Unready()
    {
        _buttonSprite.color = Color.red;
        _localPlayerReady = false;
    }
}
