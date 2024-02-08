using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class LocationManager : NetworkBehaviour
{
    // ============== Variables / Refrences ==============
    #region Variables / Refrences
    [Header("Locations")]
    [SerializeField] private Location _campLocation;
    [SerializeField] private Location _beachLocation;
    [SerializeField] private Location _forestLocation;
    [SerializeField] private Location _plateauLocation;

    // Events
    public delegate void ChangeLocationAction(LocationName newLocation);
    public static event ChangeLocationAction OnLocationChanged;

    // Location
    public enum LocationName
    {
        Camp,
        Beach,
        Forest,
        Plateau
    }
    [SerializeField] private LocationName _curLocalLocation;
    #endregion

    // ============== Location Management ==============
    #region Location Management
    // Sets player to camp location.
    // Called only at the start of the game
    public void SetInitialLocation()
    {
        Debug.Log("<color=blue>CLIENT: </color>Setting player to camp");
        SetClientServerRpc(NetworkManager.Singleton.LocalClientId, LocationName.Camp);
    }

    public void SetLocation(LocationName newLocation)
    {
        MoveClientServerRpc(NetworkManager.Singleton.LocalClientId, _curLocalLocation, newLocation, false);
    }

    public void ForceLocation(LocationName newLocation)
    {
        MoveClientServerRpc(NetworkManager.Singleton.LocalClientId, _curLocalLocation, newLocation, true);
    }

    [ClientRpc]
    private void MoveToLocationClientRpc(LocationName newLocation, ClientRpcParams clientRpcParams = default)
    {
        _curLocalLocation = newLocation;

        OnLocationChanged?.Invoke(newLocation);

        DisableAllLocations();

        switch (_curLocalLocation)
        {
            case LocationName.Camp:
                _campLocation.EnableLocation();
                break;
            case LocationName.Beach:
                _beachLocation.EnableLocation();
                break;
            case LocationName.Forest:
                _forestLocation.EnableLocation();
                break;
            case LocationName.Plateau:
                _plateauLocation.EnableLocation();
                break;
            default:
                Debug.LogError("MoveToLocation picked default case");
                _campLocation.EnableLocation();
                break;
        }
    }

    private void DisableAllLocations()
    {
        _campLocation.DisableLocation();
        _beachLocation.DisableLocation();
        _forestLocation.DisableLocation();
        _plateauLocation.DisableLocation();
    }

    // Removes player from previous seat location and sets them to the new seat location
    [ServerRpc(RequireOwnership = false)]
    private void MoveClientServerRpc(ulong clientID, LocationName oldLocationName, LocationName newLocationName, bool ignoreSame)
    {
        if(!ignoreSame && oldLocationName == newLocationName)
        {
            Debug.Log("<color=yellow>SERVER: </color>Old and new locations are the same. Not updating seats");
            return;
        }

        // Get Client
        ClientRpcParams clientRpcParams = default;
        clientRpcParams.Send.TargetClientIds = new ulong[] { clientID };

        // Get location refrences
        Location oldLocation = GetLocationFromName(oldLocationName);
        Location newLocation = GetLocationFromName(newLocationName);

        // Remove client from previous location
        SeatManager seatMan = oldLocation.GetLocationSeatManager();
        seatMan.ClearSeat(clientID);

        // Assign client to new location
        seatMan = newLocation.GetLocationSeatManager();
        seatMan.AssignSeat(clientID);

        MoveToLocationClientRpc(newLocationName, clientRpcParams);
    }

    // Sets player to new seat location
    // Used at game setup
    [ServerRpc(RequireOwnership = false)]
    private void SetClientServerRpc(ulong clientID, LocationName newLocationName)
    {
        // Get Client
        ClientRpcParams clientRpcParams = default;
        clientRpcParams.Send.TargetClientIds = new ulong[] { clientID };

        // Get location refrence
        Location newLocation = GetLocationFromName(newLocationName);

        // Assign client to new location
        SeatManager seatMan = newLocation.GetLocationSeatManager();
        seatMan.AssignSeat(clientID);

        MoveToLocationClientRpc(newLocationName, clientRpcParams);
    }

    private Location GetLocationFromName(LocationName locationName)
    {
        switch (locationName)
        {
            case LocationName.Camp:
                return _campLocation;
            case LocationName.Beach:
                return _beachLocation;
            case LocationName.Forest:
                return _forestLocation;
            case LocationName.Plateau:
                return _plateauLocation;
            default:
                Debug.LogError("MoveToLocation picked default case");
                return null;
        }
    }
    #endregion

    // ============== Helpers ==============
    public LocationName GetCurrentLocalLocation()
    {
        return _curLocalLocation;
    }

    // ============== Location Interaction ==============
    #region Location Interaction
    public void SetLocationDebuff(LocationName loctaion)
    {
        if (!IsServer)
        {
            Debug.LogWarning("SetLocationDebuff invoked by client!");
            return;
        }

        switch (loctaion)
        {
            case LocationName.Camp:
                Debug.LogWarning("Can't debuff camp.");
                break;
            case LocationName.Beach:
                _beachLocation.SetLocationEventDebuff();
                break;
            case LocationName.Forest:
                _forestLocation.SetLocationEventDebuff();
                break;
            case LocationName.Plateau:
                _plateauLocation.SetLocationEventDebuff();
                break;
            default:
                Debug.LogError("SetLocationDebuff picked default case");
                break;
        }
    }

    public void SetLocationBuff(LocationName loctaion)
    {
        if (!IsServer)
        {
            Debug.LogWarning("SetLocationBuff invoked by client!");
            return;
        }

        switch (loctaion)
        {
            case LocationName.Camp:
                Debug.LogWarning("Can't Buff camp.");
                break;
            case LocationName.Beach:
                _beachLocation.SetLocationEventBuff();
                break;
            case LocationName.Forest:
                _forestLocation.SetLocationEventBuff();
                break;
            case LocationName.Plateau:
                _plateauLocation.SetLocationEventBuff();
                break;
            default:
                Debug.LogError("SetLocationBuff picked default case");
                break;
        }
    }
    #endregion
}
