using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using TMPro;

public class Totem : NetworkBehaviour, ICardPlayable
{
    // ================== Refrences ==================
    [SerializeField] private GameObject _totemEffects;
    [SerializeField] private GameObject _totemButton;
    [SerializeField] private GameObject _totemPannel;
    [SerializeField] private GameObject _totemStatus;
    [SerializeField] private TextMeshProUGUI _totemStatusText;

    // ================== Variables ==================
    [SerializeField] private LocationManager.LocationName _locationName;
    [SerializeField] private NetworkVariable<bool> _netIsPrepped = new(writePerm: NetworkVariableWritePermission.Server);
    [SerializeField] private NetworkVariable<bool> _netIsActive = new(writePerm: NetworkVariableWritePermission.Server);
    [SerializeField] private NetworkVariable<int> _netCooldown = new(writePerm: NetworkVariableWritePermission.Server);
    [SerializeField] private List<TotemSlot> _totemSlots = new();

    public delegate void TotemAction(LocationManager.LocationName locationName);
    public static event TotemAction OnLocationTotemEnable;
    public static event TotemAction OnLocationTotemDisable;
    public static event TotemAction OnTotemMenuOpened;
    public static event TotemAction OnTotemMenuClosed;

    // ================== Setup ==================
    #region Setup
    public override void OnNetworkSpawn()
    {
        GameManager.OnStateIntro += InitialVisibiltyToggle;
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
        GameManager.OnStateIntro -= InitialVisibiltyToggle;
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
        OnTotemMenuOpened?.Invoke(_locationName);
    }

    public void Hide()
    {
        _totemPannel.SetActive(false);
        OnTotemMenuClosed?.Invoke(_locationName);
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
    #endregion

    // ================== Functions ==================
    #region Function
    private void InitialVisibiltyToggle()
    {
        ToggleVisibility(false, false);
    }

    private void ToggleVisibility(bool prev, bool current)
    {
        Debug.Log("Toggling totem visibility " + current);

        // Set totem active
        if (current)
        {
            _totemEffects.SetActive(true);
            _totemButton.SetActive(true);
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
            _totemButton.SetActive(false);
            OnLocationTotemDisable?.Invoke(_locationName);

            if (_netCooldown.Value > 0)
                SetStatusText($"Totem is on cooldown for {_netCooldown.Value} days.");
            else
                SetStatusText("Add cards to prepare totem for activation.");

            if (IsServer)
            {
                foreach (TotemSlot slot in _totemSlots)
                    slot.TotemDeactivatedServerRpc();
            }
        }

        // If player is sabo, can see button while deactive
        if (GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerData>().GetPlayerTeam() == PlayerData.Team.Saboteurs)
            _totemButton.SetActive(true);
    }

    // Call from server
    private void CheckCooldown()
    {
        if (!IsServer)
            return;

        if (_netCooldown.Value > 0)
            _netCooldown.Value--;
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
