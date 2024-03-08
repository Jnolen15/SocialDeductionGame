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
    [SerializeField] private GameObject _activateTotemButton;
    [SerializeField] private GameObject _totemStatus;
    [SerializeField] private TextMeshProUGUI _totemStatusText;
    private TotemSounds _totemSounds;

    // ================== Variables ==================
    [SerializeField] private LocationManager.LocationName _locationName;
    [SerializeField] private bool _isDormant = true;
    [SerializeField] private NetworkVariable<bool> _netIsPrepped = new(writePerm: NetworkVariableWritePermission.Server);
    [SerializeField] private NetworkVariable<bool> _netIsActive = new(writePerm: NetworkVariableWritePermission.Server);
    [SerializeField] private NetworkVariable<int> _netCooldown = new(writePerm: NetworkVariableWritePermission.Server);
    [SerializeField] private PlayerData.Team _localTeam = PlayerData.Team.Unassigned;

    public delegate void TotemAction(LocationManager.LocationName locationName);
    public static event TotemAction OnLocationTotemEnable;
    public static event TotemAction OnLocationTotemDisable;

    // ================== Setup ==================
    #region Setup
    public override void OnNetworkSpawn()
    {
        GameManager.OnStateIntro += InitialButtonSetup;
        _netIsActive.OnValueChanged += ToggleVisibility;
        _netCooldown.OnValueChanged += UpdateCooldownStatus;

        if (IsServer)
        {
            GameManager.OnStateMorning += CheckCooldown;
            GameManager.OnStateNight += ToggleActive;
        }

    }

    private void Start()
    {
        _totemSounds = this.GetComponent<TotemSounds>();
    }

    public override void OnDestroy()
    {
        GameManager.OnStateIntro -= InitialButtonSetup;
        _netIsActive.OnValueChanged -= ToggleVisibility;
        _netCooldown.OnValueChanged -= UpdateCooldownStatus;

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
    private void SetStatusText(string status)
    {
        if (_localTeam == PlayerData.Team.Saboteurs)
        {
            _totemStatus.SetActive(true);
            _totemStatusText.text = status;
        }
    }

    private void HideStatus()
    {
        _totemStatus.SetActive(false);
    }

    private void UpdateCooldownStatus(int prev, int current)
    {
        if (current > 0)
            SetStatusText($"Totem is on cooldown for {current} days.");
    }

    private void TryEnableActiavteButton()
    {
        // Only sabos, can see totem button
        if (_localTeam == PlayerData.Team.Saboteurs)
            _activateTotemButton.SetActive(true);
        else
            _activateTotemButton.SetActive(false);
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

        TryEnableActiavteButton();
    }

    private void SetTotemDormant()
    {
        if (!IsServer)
            return;

        _isDormant = true;

        SetTotemDormantClientRpc();
    }

    [ClientRpc]
    private void SetTotemDormantClientRpc()
    {
        HideStatus();
        TryEnableActiavteButton();
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
            _activateTotemButton.SetActive(true);
            OnLocationTotemEnable?.Invoke(_locationName);

            HideActivateButton();
            HideStatus();

            if (IsServer)
            {
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

            if (IsServer)
            {
                // Track analytics
                int curDay = GameManager.Instance.GetCurrentDay();
                AnalyticsTracker.Instance.TrackTotemDeactivated(curDay);
            }

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

    // Call from server, sets active at night if prepped
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

    // ================== Player interaction ==================
    public void AttemptActivateTotem()
    {
        if (!PlayerConnectionManager.Instance.GetLocalPlayerLiving())
            return;

        if (_netIsPrepped.Value)
        {
            Debug.LogWarning("Totem is already prepped!");
            return;
        }

        if (SufferingManager.Instance.GetCurrentSufffering() < 1)
            return;

        SufferingManager.Instance.ModifySuffering(-1, 201, false);

        ActivateTotemServerRpc();
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

    public bool CanPlayCardHere(Card cardToPlay)
    {
        if (cardToPlay.HasTag("Totem") && _netIsActive.Value)
            return true;

        return false;
    }

    public void DeactivateTotem()
    {
        if (_totemSounds)
            _totemSounds.PlayClose();

        DeactivateTotemServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    public void DeactivateTotemServerRpc()
    {
        Debug.Log("<color=yellow>SERVER: </color>Key successfully added, Disabling totem");
        _netCooldown.Value = 2;
        _netIsActive.Value = false;
    }
    #endregion
}
