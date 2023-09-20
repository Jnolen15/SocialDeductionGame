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
    public static event ChangeLocationAction OnForceLocationChange;

    // Location
    public enum LocationName
    {
        Camp,
        Beach,
        Forest,
        Plateau
    }
    [SerializeField] private LocationName curLocalLocation;
    #endregion

    // ============== Location Management ==============
    #region Location Management
    // Sets player to camp location.
    // Called only at the start of the game
    public void SetInitialLocation()
    {
        Debug.Log("<color=blue>CLIENT: </color>Setting player to camp");
        OnForceLocationChange?.Invoke(LocationName.Camp);
        SetClientServerRpc(NetworkManager.Singleton.LocalClientId, LocationName.Camp);
        MoveToLocation(LocationName.Camp);
    }

    public void SetLocation(LocationName newLocation)
    {
        MoveClientServerRpc(NetworkManager.Singleton.LocalClientId, curLocalLocation, newLocation);
        MoveToLocation(newLocation);
    }

    public void ForceLocation(LocationName newLocation)
    {
        OnForceLocationChange?.Invoke(newLocation);

        MoveClientServerRpc(NetworkManager.Singleton.LocalClientId, curLocalLocation, newLocation);
        MoveToLocation(newLocation);
    }

    private void MoveToLocation(LocationName newLocation)
    {
        curLocalLocation = newLocation;

        DisableAllLocations();

        switch (curLocalLocation)
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
    private void MoveClientServerRpc(ulong clientID, LocationName oldLocationName, LocationName newLocationName)
    {
        if(oldLocationName == newLocationName)
        {
            Debug.Log("<color=yellow>SERVER: </color>Old and new locations are the same. Not updating seats");
            return;
        }

        // Get location refrences
        Location oldLocation = GetLocationFromName(oldLocationName);
        Location newLocation = GetLocationFromName(newLocationName);

        // Remove client from previous location
        SeatManager seatMan = oldLocation.GetLocationSeatManager();
        seatMan.ClearSeat(clientID);

        // Assign client to new location
        seatMan = newLocation.GetLocationSeatManager();
        seatMan.AssignSeat(clientID);
    }

    // Sets player to new seat location
    // Used at game setup
    [ServerRpc(RequireOwnership = false)]
    private void SetClientServerRpc(ulong clientID, LocationName newLocationName)
    {
        // Get location refrence
        Location newLocation = GetLocationFromName(newLocationName);

        // Assign client to new location
        SeatManager seatMan = newLocation.GetLocationSeatManager();
        seatMan.AssignSeat(clientID);
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
}
