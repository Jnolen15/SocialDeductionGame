using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PlayerHealth : NetworkBehaviour
{
    // Data
    [SerializeField] private int _maxHP = 6;
    [SerializeField] private NetworkVariable<int> _netCurrentHP = new(writePerm: NetworkVariableWritePermission.Owner);
    [SerializeField] private int _maxHunger = 3;
    [SerializeField] private NetworkVariable<float> _netCurrentHunger = new(writePerm: NetworkVariableWritePermission.Owner);
    [SerializeField] private NetworkVariable<bool> _netIsLiving = new(writePerm: NetworkVariableWritePermission.Owner);

    // Events
    public delegate void ValueModified(float ModifiedAmmount, float newTotal);
    public static event ValueModified OnHealthModified;
    public static event ValueModified OnHungerModified;
    public delegate void Death();
    public static event Death OnDeath;

    // ===================== SETUP =====================
    #region Setup
    public override void OnNetworkSpawn()
    {
        if (!IsOwner && !IsServer)
            enabled = false;

        if (IsOwner)
        {
            _netIsLiving.Value = true;

            GameManager.OnStateMorning += HungerDrain;
            _netCurrentHP.OnValueChanged += HealthChanged;
            _netCurrentHunger.OnValueChanged += HungerChanged;
        }
    }

    void Start()
    {
        if (IsOwner)
        {
            _netCurrentHP.Value = 3;
            _netCurrentHunger.Value = 1.5f;
        }
    }

    private void OnDisable()
    {
        if (!IsOwner) return;

        GameManager.OnStateMorning -= HungerDrain;
        _netCurrentHP.OnValueChanged -= HealthChanged;
        _netCurrentHunger.OnValueChanged -= HungerChanged;
    }
    #endregion

    // FOR TESTING
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
            ModifyHealth(-1);
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

        // Clamp HP within bounds
        if (_netCurrentHP.Value < 0)
            _netCurrentHP.Value = 0;
        else if (_netCurrentHP.Value > _maxHP)
            _netCurrentHP.Value = _maxHP;

        // Death Check
        if (_netCurrentHP.Value == 0)
            Die();
    }

    // sets networked bool to false and triggers OnDeath event
    private void Die()
    {
        Debug.Log($"<color=#FF0000>Player {NetworkManager.Singleton.LocalClientId} has died!</color>");
        _netIsLiving.Value = false;
        OnDeath();
    }

    public bool IsLiving()
    {
        return _netIsLiving.Value;
    }
    #endregion

    // ==================== Hunger ====================
    #region Hunger
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
