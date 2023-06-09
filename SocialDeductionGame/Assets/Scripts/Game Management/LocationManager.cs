using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LocationManager : MonoBehaviour
{
    // Location gameobject refrences
    [Header("Locations")]
    [SerializeField] private GameObject _beachForage;
    [SerializeField] private GameObject _forestForage;
    [SerializeField] private GameObject _plateauForage;

    // Events
    public delegate void ChangeLocationAction(Location newLocation);
    public static event ChangeLocationAction OnForceLocationChange;

    // Location
    public enum Location
    {
        Camp,
        Beach,
        Forest,
        Plateau
    }
    [SerializeField] private Location location;

    // ====================== Location Setting ======================
    public void SetLocation(Location newLocation)
    {
        location = newLocation;

        MoveToLocation();
    }

    public void ForceLocation(Location newLocation)
    {
        location = newLocation;
        OnForceLocationChange(newLocation);

        MoveToLocation();
    }

    private void MoveToLocation()
    {
        DisableAllLocations();

        switch (location)
        {
            case Location.Camp:
                break;
            case Location.Beach:
                _beachForage.SetActive(true);
                break;
            case Location.Forest:
                _forestForage.SetActive(true);
                break;
            case Location.Plateau:
                _plateauForage.SetActive(true);
                break;
            default:
                Debug.LogError("MoveToLocation picked default case");
                break;
        }
    }

    private void DisableAllLocations()
    {
        _beachForage.SetActive(false);
        _forestForage.SetActive(false);
        _plateauForage.SetActive(false);
    }
}
