using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PlayerHealth : NetworkBehaviour
{
    // Refrences
    private PlayerData _playerData;

    // Data
    [SerializeField] private int _maxHP;
    [SerializeField] private NetworkVariable<int> _netCurrentHP = new(writePerm: NetworkVariableWritePermission.Server);
    [SerializeField] private int _maxHunger;
    [SerializeField] private NetworkVariable<int> _netCurrentHunger = new(writePerm: NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> _netIsLiving = new(writePerm: NetworkVariableWritePermission.Server);

    private bool _doCheats;

    // Events
    public delegate void ValueModified(int ModifiedAmmount, int newTotal);
    public static event ValueModified OnHealthModified;
    public static event ValueModified OnHungerModified;

    public delegate void HealthEvent();
    public static event HealthEvent OnDeath;
    public static event HealthEvent OnHungerDrain;
    public static event HealthEvent OnStarvation;

    // The following events are for the HealthHungerViewer debug UI
    public delegate void DetailHealthHungerEvent(int ModifiedAmmount, string cause);
    public static event DetailHealthHungerEvent OnHungerDecrease;
    public static event DetailHealthHungerEvent OnHungerIncrease;
    public static event DetailHealthHungerEvent OnHealthDecrease;
    public static event DetailHealthHungerEvent OnHealthIncrease;

    // ===================== SETUP =====================
    #region Setup
    public override void OnNetworkSpawn()
    {
        if (!IsOwner && !IsServer)
            enabled = false;

        if (IsOwner)
        {
            GameManager.OnStateNight += HungerDrain;
            _netCurrentHP.OnValueChanged += HealthChanged;
            _netCurrentHunger.OnValueChanged += HungerChanged;
            _netIsLiving.OnValueChanged += Die;

            _doCheats = LogViewer.Instance.GetDoCheats();
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
            ModifyHungerServerRPC(6, false);
        }
    }

    private void OnDisable()
    {
        if (!IsOwner) return;

        GameManager.OnStateNight -= HungerDrain;
        _netCurrentHP.OnValueChanged -= HealthChanged;
        _netCurrentHunger.OnValueChanged -= HungerChanged;
        _netIsLiving.OnValueChanged -= Die;
    }
    #endregion

    private void Update()
    {
        if (!_doCheats)
            return;

        if (Input.GetKeyDown(KeyCode.T))
            ModifyHealth(1, "Cheat");

        if (Input.GetKeyDown(KeyCode.Y))
            ModifyHunger(1, "Cheat");

        if (Input.GetKeyDown(KeyCode.G))
            ModifyHealth(-1, "Cheat");

        if (Input.GetKeyDown(KeyCode.H))
            ModifyHunger(-1, "Cheat");
    }

    // ===================== Helpers =====================
    #region Helpers
    public int GetMaxHP()
    {
        return _maxHP;
    }

    public int GetMaxHunger()
    {
        return _maxHunger;
    }
    #endregion

    // ==================== Health ====================
    #region Health
    // Calls server to increase or decrease player health
    public void ModifyHealth(int ammount, string mesage)
    {
        if (!IsLiving())
            return;

        if (ammount > 0)
            OnHealthIncrease?.Invoke(ammount, mesage);
        else
            OnHealthDecrease?.Invoke(ammount, mesage);

        // Track Analytics
        AnalyticsTracker.Instance.TrackPlayerTakeDamage(ammount, mesage);

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
        {
            _netIsLiving.Value = false;

            // Track Analytics
            int day = GameManager.Instance.GetCurrentDay();
            AnalyticsTracker.Instance.TrackPlayerDeath(day);
        }
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
    public void ModifyHunger(int ammount, string mesage)
    {
        if (!IsLiving())
            return;

        if (ammount > 0)
            OnHungerIncrease?.Invoke(ammount, mesage);
        else
            OnHungerDecrease?.Invoke(ammount, mesage);

        ModifyHungerServerRPC(ammount, true);
    }

    [ServerRpc(RequireOwnership = false)]
    private void ModifyHungerServerRPC(int ammount, bool add, ServerRpcParams serverRpcParams = default)
    {
        Debug.Log($"{NetworkManager.Singleton.LocalClientId} had its hunger incremented by {ammount}");

        // temp for calculations
        int tempHunger = _netCurrentHunger.Value;

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
        // loose HP if hunger is less than 2
        if (_netCurrentHunger.Value < 2)
        {
            ModifyHealth(-1, "Starvation");
            OnStarvation?.Invoke();
        }

        ModifyHunger(-2, "Hunger Drain");
        OnHungerDrain?.Invoke();
    }
    #endregion

    // ==================== Send Events ====================
    private void HealthChanged(int prev, int next)
    {
        int modifiedAmmount = next-prev;
        OnHealthModified?.Invoke(modifiedAmmount, next);
    }

    private void HungerChanged(int prev, int next)
    {
        int modifiedAmmount = next - prev;
        OnHungerModified?.Invoke(modifiedAmmount, next);
    }
}
