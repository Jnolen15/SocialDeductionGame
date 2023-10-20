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
            GameManager.OnSetup += WorldVoiceActive;
        }
        else
        {
            //Destroy(this);
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

        GameManager.OnSetup -= WorldVoiceActive;
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
        VivoxManager.Instance.SetTransmissionAll();
    }

    void Update3DPosition(Transform transform)
    {
        VivoxManager.Instance.UpdateWorldChannelPosition(Camera.main.transform, transform);
    }
    #endregion
}
