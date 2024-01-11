using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VivoxLobbyClient : MonoBehaviour
{
    // ================== Setup ==================
    #region Setup
    private void Start()
    {
        JoinLobbyVivoxChannel();
    }

    public void JoinLobbyVivoxChannel()
    {
        // Positional first (documention says always positional first)
        VivoxManager.Instance.JoinWorldChannel(LobbyManager.Instance.GetLobbyId());

        // Lobby channel
        VivoxManager.Instance.JoinLobbyChannel(LobbyManager.Instance.GetLobbyId());
    }
    #endregion

    // ============== Function =============
    #region Function
    private void Update()
    {
        PushToTalk(VivoxManager.ChannelSeshName.Lobby);
    }

    private void PushToTalk(VivoxManager.ChannelSeshName channel)
    {
        if (Input.GetButtonDown("PTT"))
        {
            VivoxManager.Instance.SetTransmissionChannel(channel);
            //OnBeginSpeaking?.Invoke(channel);
        }
        else if (Input.GetButtonUp("PTT"))
        {
            VivoxManager.Instance.SetTransmissionNone();
            //OnEndSpeaking?.Invoke(channel);
        }
    }
    #endregion
}
