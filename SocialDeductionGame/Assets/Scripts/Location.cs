using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Location : MonoBehaviour
{
    // ============== Variables / Refrences ==============
    #region Variables / Refrences
    [SerializeField] private GameObject _locationContent;
    [SerializeField] private GameObject _locationDecor;
    [SerializeField] private Transform _locationCamPos;

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
        _locationContent.SetActive(true);
        _locationDecor.SetActive(true);

        _mainCam.transform.position = _locationCamPos.localToWorldMatrix.GetPosition();
        _mainCam.transform.rotation = _locationCamPos.localToWorldMatrix.rotation;
    }

    public void DisableLocation()
    {
        _locationContent.SetActive(false);
        _locationDecor.SetActive(false);
    }

    public SeatManager GetLocationSeatManager()
    {
        return _seatManager;
    }
    #endregion
}