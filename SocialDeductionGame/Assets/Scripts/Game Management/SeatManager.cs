using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class SeatManager : NetworkBehaviour
{
    // ================== Refrences ==================
    [Header("Player Seating Positions")]
    [SerializeField] private List<Transform> _locationSeats = new();
    [SerializeField] private Dictionary<Transform, ulong> _seatDictionary = new();

    [SerializeField] private NetworkList<ulong> _netPlayersAtLocation;

    // ================== Setup ==================
    private void Awake()
    {
        _netPlayersAtLocation = new(writePerm: NetworkVariableWritePermission.Server);
    }

    private void Start()
    {
        foreach(Transform seat in _locationSeats)
        {
            _seatDictionary.Add(seat, (ulong)999);
        }
    }

    // ================== Seats ==================
    public void AssignSeat(ulong clientID)
    {
        if (!IsServer)
            return;

        // Look for free seat
        Transform chosenSeat = null;
        foreach(Transform seat in _seatDictionary.Keys)
        {
            if(_seatDictionary[seat] == (ulong)999)
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
            _netPlayersAtLocation.Add(clientID);
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

    public void ClearSeat(ulong clientID)
    {
        if (!IsServer)
            return;

        if (_netPlayersAtLocation.Contains(clientID))
        {
            _netPlayersAtLocation.Remove(clientID);

            _seatDictionary[GetSeatOfId(clientID)] = 999;
        }
        else
            Debug.LogError("SeatManager did not contain that client ID in _netPlayersAtLocation!", gameObject);
    }

    public Transform GetSeatOfId(ulong clientID)
    {
        foreach(Transform seat in _seatDictionary.Keys)
        {
            if (_seatDictionary[seat] == clientID)
                return seat;
        }

        Debug.LogError("SeatManager dictionary did not contain that player!", gameObject);
        return null;
    }

    [ServerRpc]
    public void ClearAllSeatsServerRpc()
    {
        _netPlayersAtLocation.Clear();
    }
}
