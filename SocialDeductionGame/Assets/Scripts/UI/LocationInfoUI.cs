using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LocationInfoUI : MonoBehaviour
{
    // ============== Variables / Refrences ==============
    [SerializeField] private LocationManager.LocationName _location;
    [SerializeField] private GameObject _buffIcon;
    [SerializeField] private GameObject _debuffIcon;
    [SerializeField] private GameObject _totemIcon;

    // ============== Setup ==============
    private void Awake()
    {
        Forage.OnLocationBuffEnabled += ShowBuffIcon;
        Forage.OnLocationBuffDisabled += HideBuffIcon;
        Forage.OnLocationDebuffEnabled += ShowDebuffIcon;
        Forage.OnLocationDebuffDisabled += HideDebuffIcon;
    }

    private void OnDestroy()
    {
        Forage.OnLocationBuffEnabled -= ShowBuffIcon;
        Forage.OnLocationBuffDisabled -= HideBuffIcon;
        Forage.OnLocationDebuffEnabled -= ShowDebuffIcon;
        Forage.OnLocationDebuffDisabled -= HideDebuffIcon;
    }

    // ============== UI Functions ==============
    private void ShowBuffIcon(LocationManager.LocationName location)
    {
        if(_location == location)
            _buffIcon.SetActive(true);
    }

    private void HideBuffIcon(LocationManager.LocationName location)
    {
        if (_location == location)
            _buffIcon.SetActive(false);
    }

    private void ShowDebuffIcon(LocationManager.LocationName location)
    {
        if (_location == location)
            _debuffIcon.SetActive(true);
    }

    private void HideDebuffIcon(LocationManager.LocationName location)
    {
        if (_location == location)
            _debuffIcon.SetActive(false);
    }

    private void ShowTotemIcon(LocationManager.LocationName location)
    {
        if (_location == location)
            _totemIcon.SetActive(true);
    }

    private void HideTotemIcon(LocationManager.LocationName location)
    {
        if (_location == location)
            _totemIcon.SetActive(false);
    }
}
