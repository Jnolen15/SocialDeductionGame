using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using TMPro;

public class PlayerConnectionManager : NetworkBehaviour
{
    // ============== Singleton pattern ==============
    #region Singleton
    public static PlayerConnectionManager Instance { get; private set; }
    private void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(this);
        else
            Instance = this;
    }
    #endregion

    // ============== Variables ==============
    [SerializeField] private NetworkVariable<int> _netNumPlayers = new(writePerm: NetworkVariableWritePermission.Server);

    // ============== Refrences ==============
    [SerializeField] private TextMeshProUGUI _playersConnected;

    // ============== Setup ==============
    public override void OnNetworkSpawn()
    {
        Instance._netNumPlayers.OnValueChanged += UpdatePlayerList;

        if (!IsServer) return;

        NetworkManager.Singleton.OnClientConnectedCallback += ClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += ClientDisconnected;
    }

    public override void OnNetworkDespawn()
    {
        Instance._netNumPlayers.OnValueChanged -= UpdatePlayerList;

        if (!IsServer) return;

        NetworkManager.Singleton.OnClientConnectedCallback -= ClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback -= ClientDisconnected;
    }

    // ============== Functions ==============
    private void UpdatePlayerList(int prev, int next)
    {
        Instance._playersConnected.text = "Connected Players: " + next;
    }

    private void ClientConnected(ulong clientID)
    {
        Debug.Log($"SERVER: Client {clientID} connected");
        Instance._netNumPlayers.Value++;
    }

    private void ClientDisconnected(ulong clientID)
    {
        Debug.Log($"SERVER: Client {clientID} disconnected");
        Instance._netNumPlayers.Value--;
    }

    public static int GetNumConnectedPlayers()
    {
        Debug.Log("GetNumConnectedPlayers " + Instance._netNumPlayers.Value);
        return Instance._netNumPlayers.Value;
    }
}
