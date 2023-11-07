using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Totem : NetworkBehaviour, ICardPlayable
{
    // ================== Refrences ==================
    [SerializeField] private GameObject _totemEffects;
    [SerializeField] private GameObject _totemButton;
    [SerializeField] private GameObject _totemPannel;

    // ================== Variables ==================
    [SerializeField] private LocationManager.LocationName _locationName;
    [SerializeField] private NetworkVariable<bool> _netIsPrepped = new(writePerm: NetworkVariableWritePermission.Server);
    [SerializeField] private NetworkVariable<bool> _netIsActive = new(writePerm: NetworkVariableWritePermission.Server);
    [SerializeField] private List<TotemSlot> _totemSlots = new();

    public delegate void TotemAction(LocationManager.LocationName locationName);
    public static event TotemAction OnLocationTotemEnable;
    public static event TotemAction OnLocationTotemDisable;

    // ================== Setup ==================
    #region Setup
    public override void OnNetworkSpawn()
    {
        GameManager.OnStateIntro += InitialVisibiltyToggle;
        _netIsActive.OnValueChanged += ToggleVisibility;
        
        if(IsServer)
            GameManager.OnStateNight += ToggleActive;
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
        _netIsActive.OnValueChanged -= ToggleVisibility;

        if (IsServer)
            GameManager.OnStateNight -= ToggleActive;

        // Invoke the base when using networkobject
        base.OnDestroy();
    }
    #endregion

    // ================== Helpers ==================
    public bool GetTotemActive()
    {
        return _netIsActive.Value;
    }

    public bool CanPlayCardHere(Card cardToPlay)
    {
        return true;
    }

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

        if (complete)
        {
            Debug.Log("<color=yellow>SERVER: </color>All cards successfully added, Disabling totem");
            _netIsActive.Value = false;
        }
    }
    #endregion
}
