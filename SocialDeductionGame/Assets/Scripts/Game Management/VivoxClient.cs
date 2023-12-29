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
    [SerializeField] private bool _saboChannel;
    private PlayerData _playerData;

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
            GameManager.OnStateIntro += OnIntro;
        }
        else
        {
            enabled = false;
        }
    }

    private void Start()
    {
        _playerData = this.GetComponent<PlayerData>();

        _cachedPos = this.transform.position;
    }

    private void OnDisable()
    {
        if (!IsOwner) return;

        PlayerHealth.OnDeath -= TransitionDeathSpeak;
        VivoxManager.OnDeathChannelJoined -= JoinedDeathChannel;
        GameManager.OnStateIntro -= OnIntro;
    }

    // Sometimes players do not disconnect from lobby VC in time before game starts
    // This makes sure that the transmission is shut off so they dont keep talking
    // without PTT
    // Also join sabo chanel if sabo
    private void OnIntro()
    {
        VivoxManager.Instance.SetTransmissionNone();

        if(_playerData == null)
            _playerData = this.GetComponent<PlayerData>();
        
        if (_playerData.GetPlayerTeam() == PlayerData.Team.Saboteurs)
            JoinSaboChannel();
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
        {
            // May need to change this so both cant be pressed at the same time

            PushToTalk(VivoxManager.ChannelSeshName.World);

            if (_saboChannel)
                SaboPushToTalk();
        }
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

    private void SaboPushToTalk()
    {
        if (Input.GetButtonDown("SPTT"))
        {
            VivoxManager.Instance.SetTransmissionChannel(VivoxManager.ChannelSeshName.Sabo);
            OnBeginSpeaking?.Invoke(VivoxManager.ChannelSeshName.Sabo);
        }
        else if (Input.GetButtonUp("SPTT"))
        {
            VivoxManager.Instance.SetTransmissionNone();
            OnEndSpeaking?.Invoke(VivoxManager.ChannelSeshName.Sabo);
        }
    }

    private void Update3DPosition(Transform transform)
    {
        VivoxManager.Instance.UpdateWorldChannelPosition(Camera.main.transform, transform);
    }

    private void JoinSaboChannel()
    {
        _saboChannel = true;
        VivoxManager.Instance.JoinSaboChannel();
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
