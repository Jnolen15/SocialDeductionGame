using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class VivoxClient : NetworkBehaviour
{
    // ================== Refrences ==================
    [SerializeField] private Transform _playerSpeaker;
    [SerializeField] private Vector3 _cachedPos;
    [SerializeField] private bool _deathChannel;

    public delegate void SpeakingAction(VivoxManager.ChannelSeshName channelName);
    public static event SpeakingAction OnBeginSpeaking;
    public static event SpeakingAction OnEndSpeaking;

    // ================== Setup ==================
    #region Setup
    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            // THIS IS TEMP! FIND A BETTER PLACE FOR IT
            //VivoxManager.Instance.LeaveLobbyChannel();

            PlayerHealth.OnDeath += TransitionDeathSpeak;
            VivoxManager.OnDeathChannelJoined += JoinedDeathChannel;
        }
        else
        {
            enabled = false;
        }
    }

    private void Start()
    {
        _cachedPos = this.transform.position;
    }

    private void OnDisable()
    {
        if (!IsOwner) return;

        PlayerHealth.OnDeath -= TransitionDeathSpeak;
        VivoxManager.OnDeathChannelJoined -= JoinedDeathChannel;
    }
    #endregion

    // ================== Function ==================
    #region Function
    private void Update()
    {
        if (_cachedPos != this.transform.position)
        {
            _cachedPos = this.transform.position;
            Update3DPosition(this.transform);
        }

        if (_deathChannel)
        {
            PushToTalk(VivoxManager.ChannelSeshName.Death);
        }
        else
            PushToTalk(VivoxManager.ChannelSeshName.World);
    }

    private void PushToTalk(VivoxManager.ChannelSeshName channel)
    {
        if (Input.GetButtonDown("PTT"))
        {
            VivoxManager.Instance.SetTransmissionChannel(channel);
            OnBeginSpeaking?.Invoke(channel);
        }
        else if (Input.GetButtonUp("PTT"))
        {
            VivoxManager.Instance.SetTransmissionNone();
            OnEndSpeaking?.Invoke(channel);
        }
    }

    private void Update3DPosition(Transform transform)
    {
        VivoxManager.Instance.UpdateWorldChannelPosition(Camera.main.transform, transform);
    }

    private void TransitionDeathSpeak()
    {
        VivoxManager.Instance.SetTransmissionNone();
        OnEndSpeaking?.Invoke(VivoxManager.ChannelSeshName.World);
        VivoxManager.Instance.JoinDeathChannel();
    }

    private void JoinedDeathChannel()
    {
        _deathChannel = true;
        Debug.Log("<color=green>VIVOX: </color>Now speaking in death channel");
    }
    #endregion
}
