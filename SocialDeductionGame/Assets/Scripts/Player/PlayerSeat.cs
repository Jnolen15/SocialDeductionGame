using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PlayerSeat : NetworkBehaviour
{
    // ================== Refrences ==================
    [Header("Player Seating Positions")]
    [SerializeField] private List<Transform> _playerPositions = new();

    // ================== Setup ==================
    public override void OnNetworkSpawn()
    {
        if(IsServer)
            GameManager.OnStateIntro += AssignSeatsServerRpc;
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
            GameManager.OnStateIntro -= AssignSeatsServerRpc;
    }

    // ================== Seats ==================
    [ServerRpc]
    private void AssignSeatsServerRpc()
    {
        if (!IsServer)
            return;

        // Assign Seat for each player
        int i = 0;
        foreach (ulong clientID in NetworkManager.Singleton.ConnectedClientsIds)
        {
            Debug.Log("<color=yellow>SERVER: </color>Getting Seat for player " + clientID);

            if (i > _playerPositions.Count - 1)
            {
                Debug.LogError("Not Enough Seats!");
                return;
            }

            GameObject playerObj = PlayerConnectionManager.Instance.GetPlayerObjectByID(clientID);

            // Asign player transform a seat
            playerObj.transform.position = _playerPositions[i].position;
            playerObj.transform.rotation = _playerPositions[i].rotation;

            i++;
        }
    }
}
