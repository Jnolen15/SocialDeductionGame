using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class VivoxClient : NetworkBehaviour
{
    // ================== Refrences ==================
    [SerializeField] private Transform _playerSpeaker;
    [SerializeField] private Vector3 _cachedPos;

    // ================== Setup ==================
    #region Setup
    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            // THIS IS TEMP! FIND A BETTER PLACE FOR IT
            //VivoxManager.Instance.LeaveLobbyChannel();

            WorldVoiceActive();
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

        // ?
    }

    // ================== Function ==================
    private void Update()
    {
        if (_cachedPos != this.transform.position)
        {
            _cachedPos = this.transform.position;
            Update3DPosition(this.transform);
        }
    }

    private void WorldVoiceActive()
    {
        Debug.Log("Call to Vivox manager to set transmission to all", gameObject);
        VivoxManager.Instance.SetTransmissionAll();
    }

    void Update3DPosition(Transform transform)
    {
        VivoxManager.Instance.UpdateWorldChannelPosition(Camera.main.transform, transform);
    }
    #endregion
}
