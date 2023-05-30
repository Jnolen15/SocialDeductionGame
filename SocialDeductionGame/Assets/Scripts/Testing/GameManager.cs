using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using TMPro;

public class GameManager : NetworkBehaviour
{
    private NetworkVariable<int> _numPlayers = new(writePerm: NetworkVariableWritePermission.Server);

    [SerializeField] private TextMeshProUGUI playersConnected;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        Debug.Log("SERVER: Game Manger Server Spawn");

        NetworkManager.Singleton.OnClientConnectedCallback += ClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += ClientDisconnected;
    }

    private void Awake()
    {
        _numPlayers.OnValueChanged += UpdatePlayerList;
    }

    private void UpdatePlayerList(int prev, int next)
    {
        playersConnected.text = "Connected Players: " + next;
    }

    private void ClientConnected(ulong clientID)
    {
        Debug.Log($"SERVER: Client {clientID} connected");
        _numPlayers.Value++;

        //playersConnected.text = "Connected Players: " + _numPlayers.Value;
    }

    private void ClientDisconnected(ulong clientID)
    {
        Debug.Log($"SERVER: Client {clientID} disconnected");
        _numPlayers.Value--;

        //playersConnected.text = "Connected Players: " + _numPlayers.Value;
    }
}
