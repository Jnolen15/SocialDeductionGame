using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LocationInfoUI : MonoBehaviour
{
    // ============== Variables / Refrences ==============
    [SerializeField] private LocationManager.LocationName _location;
    [SerializeField] private GameObject _travelButton;
    [SerializeField] private GameObject _currentLocationMsg;
    [SerializeField] private GameObject _buffIcon;
    [SerializeField] private GameObject _debuffIcon;
    [SerializeField] private GameObject _totemIcon;

    // ============== Setup ==============
    private void Awake()
    {
        LocationManager.OnLocationChanged += UpdateMoveButton;
        Forage.OnLocationBuffEnabled += ShowBuffIcon;
        Forage.OnLocationBuffDisabled += HideBuffIcon;
        Forage.OnLocationDebuffEnabled += ShowDebuffIcon;
        Forage.OnLocationDebuffDisabled += HideDebuffIcon;
        Totem.OnLocationTotemEnable += ShowTotemIcon;
        Totem.OnLocationTotemDisable += HideTotemIcon;
    }

    private void OnDestroy()
    {
        LocationManager.OnLocationChanged -= UpdateMoveButton;
        Forage.OnLocationBuffEnabled -= ShowBuffIcon;
        Forage.OnLocationBuffDisabled -= HideBuffIcon;
        Forage.OnLocationDebuffEnabled -= ShowDebuffIcon;
        Forage.OnLocationDebuffDisabled -= HideDebuffIcon;
        Totem.OnLocationTotemEnable -= ShowTotemIcon;
        Totem.OnLocationTotemDisable -= HideTotemIcon;
    }

    // ============== UI Functions ==============
    private void UpdateMoveButton(LocationManager.LocationName location)
    {
        if (_location == location)
        {
            _travelButton.SetActive(false);
            _currentLocationMsg.SetActive(true);
        }
        else
        {
            _travelButton.SetActive(true);
            _currentLocationMsg.SetActive(false);
        }
    }

    private void ShowBuffIcon(LocationManager.LocationName location)
    {
        if(_location == location && _buffIcon != null)
            _buffIcon.SetActive(true);
    }

    private void HideBuffIcon(LocationManager.LocationName location)
    {
        if (_location == location && _buffIcon != null)
            _buffIcon.SetActive(false);
    }

    private void ShowDebuffIcon(LocationManager.LocationName location)
    {
        if (_location == location && _debuffIcon != null)
            _debuffIcon.SetActive(true);
    }

    private void HideDebuffIcon(LocationManager.LocationName location)
    {
        if (_location == location && _debuffIcon != null)
            _debuffIcon.SetActive(false);
    }

    private void ShowTotemIcon(LocationManager.LocationName location)
    {
        if (_location == location && _totemIcon != null)
            _totemIcon.SetActive(true);
    }

    private void HideTotemIcon(LocationManager.LocationName location)
    {
        if (_location == location && _totemIcon != null)
            _totemIcon.SetActive(false);
    }
}
