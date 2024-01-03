using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using TMPro;

public class Totem : NetworkBehaviour, ICardPlayable
{
    // ================== Refrences ==================
    [SerializeField] private GameObject _totemEffects;
    [SerializeField] private GameObject _dormantTotemButton;
    [SerializeField] private GameObject _enabledTotemButton;
    [SerializeField] private GameObject _totemPannel;
    [SerializeField] private GameObject _totemStatus;
    [SerializeField] private TextMeshProUGUI _totemStatusText;

    // ================== Variables ==================
    [SerializeField] private int _sufferingCost;
    [SerializeField] private LocationManager.LocationName _locationName;
    [SerializeField] private NetworkVariable<bool> _netIsEnabled = new(writePerm: NetworkVariableWritePermission.Server);
    [SerializeField] private NetworkVariable<bool> _netIsPrepped = new(writePerm: NetworkVariableWritePermission.Server);
    [SerializeField] private NetworkVariable<bool> _netIsActive = new(writePerm: NetworkVariableWritePermission.Server);
    [SerializeField] private NetworkVariable<int> _netCooldown = new(writePerm: NetworkVariableWritePermission.Server);
    [SerializeField] private List<TotemSlot> _totemSlots = new();
    private PlayerData.Team _localTeam = PlayerData.Team.Unassigned;

    public delegate void TotemAction(LocationManager.LocationName locationName);
    public static event TotemAction OnLocationTotemEnable;
    public static event TotemAction OnLocationTotemDisable;
    public static event TotemAction OnTotemMenuOpened;
    public static event TotemAction OnTotemMenuClosed;

    // ================== Setup ==================
    #region Setup
    public override void OnNetworkSpawn()
    {
        GameManager.OnStateIntro += InitialButtonSetup;
        GameManager.OnStateAfternoon += CloseUIOnStateChange;
        _netIsActive.OnValueChanged += ToggleVisibility;
        _netCooldown.OnValueChanged += UpdateStatus;

        if (IsServer)
        {
            GameManager.OnStateMorning += CheckCooldown;
            GameManager.OnStateNight += ToggleActive;
        }

    }

    private void Start()
    {
        foreach(TotemSlot slot in _totemSlots)
        {
            slot.Setup(this);
        }

        _totemPannel.SetActive(false);
    }

    public override void OnDestroy()
    {
        GameManager.OnStateIntro -= InitialButtonSetup;
        GameManager.OnStateAfternoon -= CloseUIOnStateChange;
        _netIsActive.OnValueChanged -= ToggleVisibility;
        _netCooldown.OnValueChanged -= UpdateStatus;

        if (IsServer)
        {
            GameManager.OnStateMorning -= CheckCooldown;
            GameManager.OnStateNight -= ToggleActive;
        }

        // Invoke the base when using networkobject
        base.OnDestroy();
    }
    #endregion

    // ================== UI ==================
    #region UI
    public void Show()
    {
        _totemPannel.SetActive(true);
        ForageUI.HideForageUI?.Invoke();
    }

    public void Hide()
    {
        _totemPannel.SetActive(false);
        ForageUI.ShowForageUI?.Invoke();
    }

    public void CloseUIOnStateChange()
    {
        _totemPannel.SetActive(false);
    }

    private void SetStatusText(string status)
    {
        _totemStatus.SetActive(true);
        _totemStatusText.text = status;
    }

    private void HideStatus()
    {
        _totemStatus.SetActive(false);
    }

    private void UpdateStatus(int prev, int current)
    {
        if (current > 0)
            SetStatusText($"Totem is on cooldown for {current} days.");
        else
            SetStatusText("Add cards to prepare totem for activation.");
    }
    #endregion

    // ================== Helpers ==================
    #region Helpers
    public bool GetTotemActive()
    {
        return _netIsActive.Value;
    }

    public bool GetTotemOnCooldown()
    {
        return _netCooldown.Value > 0;
    }

    public bool CanPlayCardHere(Card cardToPlay)
    {
        return true;
    }

    private void GetLocalTeam()
    {
        _localTeam = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerData>().GetPlayerTeam();
    }
    #endregion

    // ================== Dormant Totem ==================
    #region Dormant Totem
    private void InitialButtonSetup()
    {
        if (_localTeam == PlayerData.Team.Unassigned)
            GetLocalTeam();

        if (_localTeam == PlayerData.Team.Saboteurs)
            _dormantTotemButton.SetActive(true);
    }

    private void SetTotemDormant()
    {
        if (!IsServer)
            return;

        _netIsEnabled.Value = false;

        foreach (TotemSlot slot in _totemSlots)
            slot.TotemDeactivatedServerRpc();

        SetTotemDormantClientRpc();
    }

    [ClientRpc]
    private void SetTotemDormantClientRpc()
    {
        _enabledTotemButton.SetActive(false);

        // If player is sabo, can see dormant button while dormant
        if (_localTeam == PlayerData.Team.Saboteurs)
            _dormantTotemButton.SetActive(true);
    }

    // Called by pressing the dormant totem button
    public void DormantButtonPress()
    {
        if (_netIsEnabled.Value)
            return;

        Debug.Log("Totem Dormant, Checking if enough suffering to enable");

        if (SufferingManager.Instance.GetCurrentSufffering() >= _sufferingCost)
        {
            SufferingManager.Instance.ModifySuffering(-_sufferingCost, 201, false);
            EnableTotemServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void EnableTotemServerRpc()
    {
        _netIsEnabled.Value = true;
        EnableTotemClientRpc();
    }

    [ClientRpc]
    private void EnableTotemClientRpc()
    {
        if (_localTeam != PlayerData.Team.Saboteurs)
            return;

        _enabledTotemButton.SetActive(true);
        _dormantTotemButton.SetActive(false);
    }
    #endregion

    // ================== Functions ==================
    #region Function
    private void ToggleVisibility(bool prev, bool current)
    {
        Debug.Log("Toggling totem visibility " + current);

        // Set totem active
        if (current)
        {
            _totemEffects.SetActive(true);
            _enabledTotemButton.SetActive(true);
            OnLocationTotemEnable?.Invoke(_locationName);

            HideStatus();

            if (IsServer)
            {
                foreach (TotemSlot slot in _totemSlots)
                    slot.TotemActivatedServerRpc();
            }
        }
        // Set totem deactive
        else
        {
            _totemEffects.SetActive(false);
            OnLocationTotemDisable?.Invoke(_locationName);

            SetStatusText($"Totem is on cooldown for {_netCooldown.Value} days.");
        }
    }

    // Call from server
    private void CheckCooldown()
    {
        if (!IsServer)
            return;

        if (_netIsActive.Value)
            return;

        if (_netCooldown.Value > 0)
            _netCooldown.Value--;
        
        if(_netCooldown.Value <= 0)
            SetTotemDormant();
    }

    // Call from server
    private void ToggleActive()
    {
        if (!IsServer)
            return;

        if (_netIsPrepped.Value)
        {
            _netIsPrepped.Value = false;
            _netIsActive.Value = true;
        }
    }

    // Called by Server
    public void CardAddedToInactiveTotem()
    {
        if (!IsServer)
            return;

        _netIsPrepped.Value = true;
    }

    // Called by Server
    public void CardAddedToActiveTotem()
    {
        if (!IsServer)
            return;

        // Test if all totem slots are full
        bool complete = true;
        foreach (TotemSlot slot in _totemSlots)
        {
            if (!slot.GetCardSatesfied())
            {
                complete = false;
                break;
            }
        }

        // Deactivate totem and go on cooldown
        if (complete)
        {
            Debug.Log("<color=yellow>SERVER: </color>All cards successfully added, Disabling totem");
            _netCooldown.Value = 2;
            _netIsActive.Value = false;
        }
    }
    #endregion
}
