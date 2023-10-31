using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Location : MonoBehaviour
{
    // ============== Variables / Refrences ==============
    #region Variables / Refrences
    [SerializeField] private GameObject _locationDecor;
    [SerializeField] private Transform _locationCamPos;
    [SerializeField] private Forage _forage;

    private SeatManager _seatManager;
    private Camera _mainCam;
    #endregion

    // ============== Setup ==============
    #region Setup
    private void Start()
    {
        _seatManager = this.GetComponent<SeatManager>();

        _mainCam = Camera.main;
    }
    #endregion

    // ============== Location Functions ==============
    #region Location Functions
    public void EnableLocation()
    {
        Debug.Log("Enabling location " + gameObject.name);

        _locationDecor.SetActive(true);

        _mainCam.transform.position = _locationCamPos.localToWorldMatrix.GetPosition();
        _mainCam.transform.rotation = _locationCamPos.localToWorldMatrix.rotation;

        if (_forage)
            _forage.Setup();
    }

    public void DisableLocation()
    {
        LocationShutdown();

        _locationDecor.SetActive(false);
    }

    public void LocationShutdown()
    {
        if (!_forage)
            return;

        Debug.Log("Shutting down location " + gameObject.name);
        _forage.Shutdown();
    }

    public SeatManager GetLocationSeatManager()
    {
        return _seatManager;
    }
    #endregion
}
