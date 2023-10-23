using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class VivoxClient : NetworkBehaviour
{
    // ================== Refrences ==================
    [SerializeField] private Transform _playerSpeaker;
    [SerializeField] private Vector3 _cachedPos;
    [SerializeField] private bool _deathMuted;

    public delegate void SpeakingAction();
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

            PlayerHealth.OnDeath += DeathMute;
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

        PlayerHealth.OnDeath -= DeathMute;
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

        if (_deathMuted)
            return;

        if (Input.GetButtonDown("PTT"))
        {
            VivoxManager.Instance.SetTransmissionAll();
            OnBeginSpeaking?.Invoke();
        }
        else if (Input.GetButtonUp("PTT"))
        {
            VivoxManager.Instance.SetTransmissionNone();
            OnEndSpeaking?.Invoke();
        }
    }

    private void Update3DPosition(Transform transform)
    {
        VivoxManager.Instance.UpdateWorldChannelPosition(Camera.main.transform, transform);
    }

    private void DeathMute()
    {
        _deathMuted = true;
        VivoxManager.Instance.SetTransmissionNone();
        OnEndSpeaking?.Invoke();
    }
    #endregion
}
