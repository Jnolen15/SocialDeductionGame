using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using TMPro;

public class PlayerConnectionManager : NetworkBehaviour
{
    private NetworkVariable<int> _netNumPlayers = new(writePerm: NetworkVariableWritePermission.Server);

    [SerializeField] private TextMeshProUGUI _playersConnected;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        NetworkManager.Singleton.OnClientConnectedCallback += ClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += ClientDisconnected;
    }

    private void Awake()
    {
        _netNumPlayers.OnValueChanged += UpdatePlayerList;
    }

    private void OnDisable()
    {
        NetworkManager.Singleton.OnClientConnectedCallback -= ClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback -= ClientDisconnected;
    }

    // Functions
    private void UpdatePlayerList(int prev, int next)
    {
        _playersConnected.text = "Connected Players: " + next;
    }

    private void ClientConnected(ulong clientID)
    {
        Debug.Log($"SERVER: Client {clientID} connected");
        _netNumPlayers.Value++;
    }

    private void ClientDisconnected(ulong clientID)
    {
        Debug.Log($"SERVER: Client {clientID} disconnected");
        _netNumPlayers.Value--;
    }

    public int GetNumConnectedPlayers()
    {
        return _netNumPlayers.Value;
    }
}
