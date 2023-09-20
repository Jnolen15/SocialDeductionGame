using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class SeatManager : NetworkBehaviour
{
    // ================== Refrences ==================
    [Header("Player Seating Positions")]
    [SerializeField] private List<Transform> _locationSeats = new();

    [SerializeField] private NetworkList<ulong> _netPlayersAtLocation = new(writePerm: NetworkVariableWritePermission.Server);

    // ================== Setup ==================
    /*public override void OnNetworkSpawn()
    {
        if(IsServer)
            GameManager.OnStateIntro += AssignSeatsServerRpc;
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
            GameManager.OnStateIntro -= AssignSeatsServerRpc;
    }*/

    // ================== Seats ==================
    public void AssignSeat(ulong clientID)
    {
        if (!IsServer)
            return;

        // Track player added
        _netPlayersAtLocation.Add(clientID);

        int seatIndex = _netPlayersAtLocation.Count - 1;

        Debug.Log($"<color=yellow>SERVER: </color>Assigning Seat {seatIndex} for player {clientID}");

        // Make sure there are enough seats
        if (seatIndex >= _locationSeats.Count)
        {
            Debug.LogError("Not Enough Seats!", gameObject);
            return;
        }

        // Assign seat for player
        GameObject playerObj = PlayerConnectionManager.Instance.GetPlayerObjectByID(clientID);

        // Asign player transform a seat
        playerObj.transform.position = _locationSeats[seatIndex].position;
        playerObj.transform.rotation = _locationSeats[seatIndex].rotation;
    }

    public void ClearSeat(ulong clientID)
    {
        if (!IsServer)
            return;

        if (_netPlayersAtLocation.Contains(clientID))
            _netPlayersAtLocation.Remove(clientID);
        else
            Debug.LogError("SeatManager did not contain that client ID!", gameObject);
    }

    [ServerRpc]
    public void ClearAllSeatsServerRpc()
    {
        _netPlayersAtLocation.Clear();
    }
}
