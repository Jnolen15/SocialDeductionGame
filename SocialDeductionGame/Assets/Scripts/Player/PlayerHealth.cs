using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PlayerHealth : NetworkBehaviour
{
    // Refrences

    // Data
    [SerializeField] private int _maxHP = 6;
    [SerializeField] private NetworkVariable<int> _netCurrentHP = new();
    [SerializeField] private int _maxHunger = 3;
    [SerializeField] private NetworkVariable<float> _netCurrentHunger = new();

    // Events
    public delegate void ValueModified(float ModifiedAmmount, float newTotal);
    public static event ValueModified OnHealthModified;
    public static event ValueModified OnHungerModified;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner && !IsServer)
            enabled = false;

        if (IsOwner)
        {
            GameManager.OnStateMorning += HungerDrain;
            _netCurrentHP.OnValueChanged += HealthChanged;
            _netCurrentHunger.OnValueChanged += HungerChanged;
        }
    }

    void Start()
    {
        _netCurrentHP.Value = 3;
        _netCurrentHunger.Value = 1.5f;
    }

    private void OnDisable()
    {
        if (!IsOwner) return;

        GameManager.OnStateMorning -= HungerDrain;
        _netCurrentHP.OnValueChanged -= HealthChanged;
        _netCurrentHunger.OnValueChanged -= HungerChanged;
    }

    // ==================== Health ====================
    #region Health
    // Calls server then client to increase or decrease player health
    public void ModifyHealth(int ammount)
    {
        ModifyHealthServerRPC(ammount);
    }

    [ServerRpc]
    private void ModifyHealthServerRPC(int ammount, ServerRpcParams serverRpcParams = default)
    {
        // Get client data
        var clientId = serverRpcParams.Receive.SenderClientId;
        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { clientId }
            }
        };

        // TODO - Check if health augment is viable currently
        // Note: If no checks take place, no RPCs are needed just adjust the _netCurrentHP in the base function

        ModifyHealthClientRpc(ammount, clientRpcParams);
    }

    [ClientRpc]
    private void ModifyHealthClientRpc(int ammount, ClientRpcParams clientRpcParams = default)
    {
        Debug.Log($"{NetworkManager.Singleton.LocalClientId} had its health incremented by {ammount}");

        _netCurrentHP.Value += ammount;

        if (_netCurrentHP.Value < 0)
            _netCurrentHP.Value = 0;
        else if (_netCurrentHP.Value > _maxHP)
            _netCurrentHP.Value = _maxHP;
    }
    #endregion

    // ==================== Hunger ====================
    #region Health
    // Calls server then client to increase or decrease player health
    public void ModifyHunger(float ammount)
    {
        _netCurrentHunger.Value += ammount;

        if (_netCurrentHunger.Value < 0)
            _netCurrentHunger.Value = 0;
        else if (_netCurrentHunger.Value > _maxHunger)
            _netCurrentHunger.Value = _maxHunger;
    }

    // Loose hunger each day
    private void HungerDrain()
    {
        // loose HP if less than 1 hunger
        if (_netCurrentHunger.Value < 1)
            ModifyHealth(-1);

        ModifyHunger(-1);
    }
    #endregion

    // ==================== Send Events ====================
    private void HealthChanged(int prev, int next)
    {
        int modifiedAmmount = next-prev;
        OnHealthModified(modifiedAmmount, next);
    }

    private void HungerChanged(float prev, float next)
    {
        float modifiedAmmount = next - prev;
        OnHungerModified(modifiedAmmount, next);
    }
}
