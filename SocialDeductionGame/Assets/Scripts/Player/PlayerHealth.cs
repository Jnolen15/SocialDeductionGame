using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PlayerHealth : NetworkBehaviour
{
    // Refrences
    private PlayerData _playerData;

    // Data
    [SerializeField] private int _maxHP = 6;
    [SerializeField] private NetworkVariable<int> _netCurrentHP = new(writePerm: NetworkVariableWritePermission.Server);
    [SerializeField] private int _maxHunger = 3;
    [SerializeField] private NetworkVariable<float> _netCurrentHunger = new(writePerm: NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> _netIsLiving = new(writePerm: NetworkVariableWritePermission.Server);

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
            GameManager.OnStateMorning += HungerDrain;
            _netCurrentHP.OnValueChanged += HealthChanged;
            _netCurrentHunger.OnValueChanged += HungerChanged;
            _netIsLiving.OnValueChanged += Die;
        }

        if (IsServer)
        {
            _netIsLiving.Value = true;
        }
    }

    void Start()
    {
        _playerData = gameObject.GetComponent<PlayerData>();

        if (IsOwner)
        {
            ModifyHealthServerRPC(4, false);
            ModifyHungerServerRPC(3f, false);
        }
    }

    private void OnDisable()
    {
        if (!IsOwner) return;

        GameManager.OnStateMorning -= HungerDrain;
        _netCurrentHP.OnValueChanged -= HealthChanged;
        _netCurrentHunger.OnValueChanged -= HungerChanged;
        _netIsLiving.OnValueChanged -= Die;
    }
    #endregion

    // FOR TESTING
    /*private void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
            ModifyHealth(1);

        if (Input.GetKeyDown(KeyCode.Y))
            ModifyHunger(1);

        if (Input.GetKeyDown(KeyCode.G))
            ModifyHealth(-1);

        if (Input.GetKeyDown(KeyCode.H))
            ModifyHunger(-1);
    }*/

    // ==================== Health ====================
    #region Health
    // Calls server to increase or decrease player health
    public void ModifyHealth(int ammount)
    {
        if (!IsLiving())
            return;

        ModifyHealthServerRPC(ammount, true);
    }

    [ServerRpc(RequireOwnership = false)]
    private void ModifyHealthServerRPC(int ammount, bool add, ServerRpcParams serverRpcParams = default)
    {
        Debug.Log($"{NetworkManager.Singleton.LocalClientId} had its health incremented by {ammount}");

        // temp for calculations
        int tempHP = _netCurrentHP.Value;

        if (add)
            tempHP += ammount;
        else
            tempHP = ammount;

        // Clamp HP within bounds
        if (tempHP < 0)
            tempHP = 0;
        else if (tempHP > _maxHP)
            tempHP = _maxHP;

        _netCurrentHP.Value = tempHP;

        // Death Check
        if (_netCurrentHP.Value == 0)
            _netIsLiving.Value = false;
    }

    // called when _netIsLiving changes, triggers OnDeath event
    private void Die(bool prev, bool next)
    {
        if (next == true)
            return;

        Debug.Log($"<color=#FF0000>Player {NetworkManager.Singleton.LocalClientId} has died!</color>");
        _playerData.OnPlayerDeath();
        OnDeath?.Invoke();
    }

    public bool IsLiving()
    {
        return _netIsLiving.Value;
    }
    #endregion

    // ==================== Hunger ====================
    #region Hunger
    // Calls server to increase or decrease player hunger
    public void ModifyHunger(float ammount)
    {
        if (!IsLiving())
            return;

        ModifyHungerServerRPC(ammount, true);
    }

    [ServerRpc(RequireOwnership = false)]
    private void ModifyHungerServerRPC(float ammount, bool add, ServerRpcParams serverRpcParams = default)
    {
        Debug.Log($"{NetworkManager.Singleton.LocalClientId} had its hunger incremented by {ammount}");

        // temp for calculations
        float tempHunger = _netCurrentHunger.Value;

        if (add)
            tempHunger += ammount;
        else
            tempHunger = ammount;

        // Clamp Hunger within bounds
        if (tempHunger < 0)
            tempHunger = 0;
        else if (tempHunger > _maxHunger)
            tempHunger = _maxHunger;

        _netCurrentHunger.Value = tempHunger;
    }

    // Loose hunger each day
    private void HungerDrain()
    {
        // loose HP if hunger is less than 1
        if (_netCurrentHunger.Value < 1)
            ModifyHealth(-1);

        ModifyHunger(-1);
    }
    #endregion

    // ==================== Send Events ====================
    private void HealthChanged(int prev, int next)
    {
        int modifiedAmmount = next-prev;
        OnHealthModified?.Invoke(modifiedAmmount, next);
    }

    private void HungerChanged(float prev, float next)
    {
        float modifiedAmmount = next - prev;
        OnHungerModified?.Invoke(modifiedAmmount, next);
    }
}
