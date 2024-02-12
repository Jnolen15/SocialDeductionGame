using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class VolcanoLocation : NetworkBehaviour
{
    // ================== Refrences ==================
    [Header("Location Refrences")]
    [SerializeField] private GameObject _locationDecor;
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private Transform _locationCamPos;
    [SerializeField] private Transform _exileCamPos;
    private Camera _mainCam;

    private bool _wasSurvivor;

    [Header("Player Seating Positions")]
    [SerializeField] private Transform _trialSeat;
    [SerializeField] private List<Transform> _councilSeats = new();
    private ulong _trialSeatPlayer;
    private Dictionary<Transform, ulong> _seatDictionary = new();

    // ================== Setup ==================
    #region Setup
    private void Start()
    {
        foreach (Transform seat in _councilSeats)
        {
            _seatDictionary.Add(seat, (ulong)999);
        }

        _mainCam = Camera.main;
    }
    #endregion

    // ================== Location ==================
    #region Location
    public void EnableLocation()
    {
        Debug.Log("Enabling location " + gameObject.name);

        _locationDecor.SetActive(true);

        if (_audioSource)
            _audioSource.Play();

        _mainCam.transform.position = _locationCamPos.localToWorldMatrix.GetPosition();
        _mainCam.transform.rotation = _locationCamPos.localToWorldMatrix.rotation;
    }

    public void SwapToExileCam()
    {
        _mainCam.transform.position = _exileCamPos.localToWorldMatrix.GetPosition();
        _mainCam.transform.rotation = _exileCamPos.localToWorldMatrix.rotation;
    }

    public void DisableLocation()
    {
        _locationDecor.SetActive(false);

        if (_audioSource)
            _audioSource.Pause();
    }
    #endregion

    // ================== Seats ==================
    #region Seats
    public void AssignCouncilSeat(ulong clientID)
    {
        if (!IsServer)
            return;

        // Look for free seat
        Transform chosenSeat = null;
        foreach (Transform seat in _seatDictionary.Keys)
        {
            if (_seatDictionary[seat] == (ulong)999)
            {
                Debug.Log("<color=yellow>SERVER: </color>Found free seat");
                chosenSeat = seat;
                break;
            }
        }

        // Assign seat
        if (chosenSeat)
        {
            // Track player added
            Debug.Log($"<color=yellow>SERVER: </color>Assigning Seat for player {clientID}");

            // Assign seat for player
            _seatDictionary[chosenSeat] = clientID;

            // Asign player transform a seat
            GameObject playerObj = PlayerConnectionManager.Instance.GetPlayerObjectByID(clientID);
            playerObj.transform.position = chosenSeat.position;
            playerObj.transform.rotation = chosenSeat.rotation;
        }
        else
        {
            Debug.LogError("Seats are all full or there are not enough!", gameObject);
        }
    }

    public void ClearSeats()
    {
        if (!IsServer)
            return;

        Transform[] keys = new Transform[_seatDictionary.Keys.Count];
        _seatDictionary.Keys.CopyTo(keys, 0);
        foreach (Transform seat in keys)
        {
            _seatDictionary[seat] = 999;
        }
    }

    public void AssignTrialSeat(ulong clientID)
    {
        if (!IsServer)
            return;

        // Track player added
        Debug.Log($"<color=yellow>SERVER: </color>Assigning Seat for player {clientID}");

        // Assign seat for player
        _trialSeatPlayer = clientID;

        // Asign player transform a seat
        GameObject playerObj = PlayerConnectionManager.Instance.GetPlayerObjectByID(clientID);
        playerObj.transform.position = _trialSeat.position;
        playerObj.transform.rotation = _trialSeat.rotation;
    }

    public void ClearTrialSeat()
    {
        if (!IsServer)
            return;

        _trialSeatPlayer = 999;
    }

    public Transform GetTrialSeat()
    {
        return _trialSeat;
    }
    #endregion

    // ================== Other ==================
    #region Other

    public void SetExileTeam(bool wasSurvivor)
    {
        _wasSurvivor = wasSurvivor;
    }

    public bool GetWasSurvivor()
    {
        return _wasSurvivor;
    }

    #endregion
}
