using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using TMPro;
using Unity.Services.Analytics;

public class Totem : NetworkBehaviour, ICardPlayable
{
    // ================== Refrences ==================
    [SerializeField] private GameObject _preppedTotemEffects;
    [SerializeField] private GameObject _activeTotemEffects;
    [SerializeField] private GameObject _openTotemButton;
    [SerializeField] private GameObject _totemPannel;
    [SerializeField] private GameObject _totemStatus;
    [SerializeField] private TextMeshProUGUI _totemStatusText;
    [SerializeField] private GameObject _activateTotemButton;
    [SerializeField] private TextMeshProUGUI _activateCostText;
    private TotemSounds _totemSounds;

    // ================== Variables ==================
    [SerializeField] private LocationManager.LocationName _locationName;
    [SerializeField] private bool _isDormant = true;
    [SerializeField] private NetworkVariable<bool> _netIsPrepped = new(writePerm: NetworkVariableWritePermission.Server);
    [SerializeField] private NetworkVariable<bool> _netIsActive = new(writePerm: NetworkVariableWritePermission.Server);
    [SerializeField] private NetworkVariable<int> _netCooldown = new(writePerm: NetworkVariableWritePermission.Server);
    [SerializeField] private NetworkVariable<int> _currentCost = new(writePerm: NetworkVariableWritePermission.Server);
    [SerializeField] private List<TotemSlot> _totemSlots = new();
    [SerializeField] private PlayerData.Team _localTeam = PlayerData.Team.Unassigned;

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
        _totemSounds = this.GetComponent<TotemSounds>();

        foreach (TotemSlot slot in _totemSlots)
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
        if(_totemSounds)
            _totemSounds.PlayOpen();

        _totemPannel.SetActive(true);
        ForageUI.HideForageUI?.Invoke();
    }

    public void Hide()
    {
        if (_totemSounds)
            _totemSounds.PlayClose();

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

    private void ShowActivateButton(int cost)
    {
        _activateTotemButton.SetActive(true);
        _activateCostText.text = cost.ToString();
    }

    private void HideActivateButton()
    {
        _activateTotemButton.SetActive(false);
    }
    #endregion

    // ================== Helpers ==================
    #region Helpers
    public bool GetTotemPrepped()
    {
        return _netIsPrepped.Value;
    }
    
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
        _localTeam = PlayerConnectionManager.Instance.GetLocalPlayerTeam();
    }
    #endregion

    // ================== Dormant Totem ==================
    #region Dormant Totem
    private void InitialButtonSetup()
    {
        if (_localTeam == PlayerData.Team.Unassigned)
            GetLocalTeam();

        if (_localTeam == PlayerData.Team.Saboteurs)
            _openTotemButton.SetActive(true);
        else
            _openTotemButton.SetActive(false);
    }

    private void SetTotemDormant()
    {
        if (!IsServer)
            return;

        _isDormant = true;

        foreach (TotemSlot slot in _totemSlots)
            slot.TotemDeactivatedServerRpc();

        SetTotemDormantClientRpc();
    }

    [ClientRpc]
    private void SetTotemDormantClientRpc()
    {
        // If player is sabo, can see dormant button while dormant
        if (_localTeam == PlayerData.Team.Saboteurs)
            _openTotemButton.SetActive(true);
        else
            _openTotemButton.SetActive(false);
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
            _preppedTotemEffects.SetActive(false);
            _activeTotemEffects.SetActive(true);
            _openTotemButton.SetActive(true);
            OnLocationTotemEnable?.Invoke(_locationName);

            SetStatusText("Add cards with the matching tags to disable the totem.");

            HideActivateButton();

            if (IsServer)
            {
                foreach (TotemSlot slot in _totemSlots)
                    slot.TotemActivatedServerRpc();

                // Track analytics
                int curDay = GameManager.Instance.GetCurrentDay();
                AnalyticsTracker.Instance.TrackTotemActivated(curDay);
            }
        }
        // Set totem deactive
        else
        {
            if (_totemSounds)
                _totemSounds.PlayDeactivated();

            _activeTotemEffects.SetActive(false);
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
        
        if(_netCooldown.Value <= 0 && !_isDormant)
            SetTotemDormant();
    }

    // Call from server
    private void ToggleActive()
    {
        if (!IsServer)
            return;

        if (_netIsPrepped.Value)
        {
            _isDormant = false;
            _netIsPrepped.Value = false;
            _netIsActive.Value = true;
        }
    }

    // Called by Server
    public void CardAddedToInactiveTotem()
    {
        if (!IsServer)
            return;

        if (_netIsPrepped.Value)
        {
            Debug.LogWarning("Totem is already prepped! Should not accept card");
            return;
        }

        // CALCULATE COST BASED ON NUMBER OF CARDS
        int numCards = 0;
        foreach (TotemSlot slot in _totemSlots)
        {
            if (slot.HasSaboCard())
                numCards++;
        }

        if(numCards <= 1)
            _currentCost.Value = 2;
        else if(numCards == 2)
            _currentCost.Value = 3;
        else
            _currentCost.Value = 4;

        ShowActivateTotemButtonClientRpc(_currentCost.Value);
    }

    [ClientRpc]
    private void ShowActivateTotemButtonClientRpc(int cost)
    {
        if (_localTeam != PlayerData.Team.Saboteurs)
            return;

        HideStatus();
        ShowActivateButton(cost);
    }

    // Player pressed activate button
    public void ActivateTotem()
    {
        if (SufferingManager.Instance.GetCurrentSufffering() >= _currentCost.Value)
        {
            SufferingManager.Instance.ModifySuffering(-_currentCost.Value, 201, false);
            ActivateTotemServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void ActivateTotemServerRpc()
    {
        _netIsPrepped.Value = true;
        ActivateTotemClientRpc();
    }

    [ClientRpc]
    private void ActivateTotemClientRpc()
    {
        if (_totemSounds)
            _totemSounds.PlayPrepped();

        _preppedTotemEffects.SetActive(true);
        HideActivateButton();
        SetStatusText("Totem will activate in the night");
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
