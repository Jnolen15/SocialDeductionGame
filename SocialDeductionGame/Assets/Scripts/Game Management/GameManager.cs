using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using TMPro;

public class GameManager : NetworkBehaviour
{
    [SerializeField] private TextMeshProUGUI _gameStateText;
    [SerializeField] private GameObject _readyButton;

    public enum GameState
    {
        Morning,
        Afternoon,
        Night
    }
    private NetworkVariable<GameState> _netCurrentGameState = new(writePerm: NetworkVariableWritePermission.Server);

    [SerializeField] private NetworkVariable<int> _netPlayersReadied = new(writePerm: NetworkVariableWritePermission.Server);
    [SerializeField] private bool playerReady;
    private PlayerConnectionManager _pcMan;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        // Something here?
    }

    private void Awake()
    {
        _netCurrentGameState.OnValueChanged += UpdateGameState;
    }

    private void Start()
    {
        _pcMan = this.GetComponent<PlayerConnectionManager>();
    }

    public void UpdateGameState(GameState prev, GameState next)
    {
        if(_gameStateText != null)
            _gameStateText.text = next.ToString();
    }

    private void Update()
    {
        if (!IsServer) return;

        // For Testing
        if (Input.GetKeyDown(KeyCode.T))
        {
            _netCurrentGameState.Value++;

            if (((int)_netCurrentGameState.Value) == System.Enum.GetValues(typeof(GameState)).Length)
                _netCurrentGameState.Value = 0;
        }
    }

    // ====================== Player Readying ======================
    public void ReadyPlayer()
    {
        if(!playerReady)
            PlayerReadyServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    public void PlayerReadyServerRpc(ServerRpcParams serverRpcParams = default)
    {
        // Get client data
        var clientId = serverRpcParams.Receive.SenderClientId;
        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { clientId }
            }
        };

        // Record ready player on server
        _netPlayersReadied.Value++;
        
        // Record ready player on client
        PlayerReadyClientRpc(clientRpcParams);

        // Check if all players are ready
        if (_netPlayersReadied.Value >= _pcMan.GetNumConnectedPlayers())
        {
            Debug.Log("All Players ready, progressing state");

            _netCurrentGameState.Value++;
            if (((int)_netCurrentGameState.Value) == System.Enum.GetValues(typeof(GameState)).Length)
                _netCurrentGameState.Value = 0;

            _netPlayersReadied.Value = 0;

            UnReadyPlayerClientRpc();
        }
    }

    [ClientRpc]
    public void PlayerReadyClientRpc(ClientRpcParams clientRpcParams = default)
    {
        playerReady = true;
        _readyButton.SetActive(false);
        Debug.Log("Ready!");
    }

    [ClientRpc]
    public void UnReadyPlayerClientRpc()
    {
        playerReady = false;
        _readyButton.SetActive(true);
        Debug.Log("Unready!");
    }
}
