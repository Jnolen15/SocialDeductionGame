using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using TMPro;

public class GameManager : NetworkBehaviour
{
    [Header("Basics")]
    [SerializeField] private TextMeshProUGUI _gameStateText;
    [SerializeField] private GameObject _readyButton;

    [Header("Pick Location")]
    [SerializeField] private GameObject _locationChoiceMenu;

    [Header("Forage")]
    [SerializeField] private GameObject _forage;

    public enum GameState
    {
        M_PickLocation,
        M_Forage,
        Afternoon,
        Night
    }
    private NetworkVariable<GameState> _netCurrentGameState = new(writePerm: NetworkVariableWritePermission.Server);
    private NetworkVariable<int> _netPlayersReadied = new(writePerm: NetworkVariableWritePermission.Server);
    private bool playerReady;
    private PlayerConnectionManager _pcMan;

    private void Awake()
    {
        _netCurrentGameState.OnValueChanged += UpdateGameState;
    }

    private void OnDisable()
    {
        _netCurrentGameState.OnValueChanged -= UpdateGameState;
    }

    private void Start()
    {
        _pcMan = this.GetComponent<PlayerConnectionManager>();

        UpdateGameState(GameState.M_Forage, _netCurrentGameState.Value);

        _readyButton.SetActive(false);
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
    #region Player Readying
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
        Debug.Log("Unready!");
    }
    #endregion

    // ====================== State Management ======================
    #region State Management
    public void UpdateGameState(GameState prev, GameState next)
    {
        if (_gameStateText != null)
            _gameStateText.text = next.ToString();

        CloseAllStateMenus();

        switch (next)
        {
            case GameState.M_PickLocation:
                _locationChoiceMenu.SetActive(true);
                _readyButton.SetActive(false);
                break;
            case GameState.M_Forage:
                _forage.SetActive(true);
                _readyButton.SetActive(false);
                break;
            case GameState.Afternoon:
                this.GetComponent<LocationManager>().SetLocation("Camp");
                _readyButton.SetActive(true);
                break;
            case GameState.Night:
                _readyButton.SetActive(true);
                break;
        }
    }

    private void CloseAllStateMenus()
    {
        _locationChoiceMenu.SetActive(false);
        _forage.SetActive(false);
    }
    #endregion
}
