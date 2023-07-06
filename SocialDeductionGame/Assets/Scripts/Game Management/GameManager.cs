using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using TMPro;

public class GameManager : NetworkBehaviour
{
    // ================== Refrences ==================
    [Header("Basics")]
    [SerializeField] private TextMeshProUGUI _gameStateText;
    [SerializeField] private GameObject _readyButton;
    
    private bool playerReady;

    // ================== State ==================
    public enum GameState
    {
        Pregame,
        Intro,
        Morning,
        M_Forage,
        Afternoon,
        Evening,
        Night
    }
    private NetworkVariable<GameState> _netCurrentGameState = new(writePerm: NetworkVariableWritePermission.Server);
    private NetworkVariable<int> _netPlayersReadied = new(writePerm: NetworkVariableWritePermission.Server);

    // State Events
    public delegate void ChangeStateAction();
    public static event ChangeStateAction OnStateIntro;
    public static event ChangeStateAction OnStateMorning;
    public static event ChangeStateAction OnStateForage;
    public static event ChangeStateAction OnStateAfternoon;
    public static event ChangeStateAction OnStateEvening;
    public static event ChangeStateAction OnStateNight;

    // ================== Setup ==================
    #region Setup
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
        UpdateGameState(GameState.Morning, _netCurrentGameState.Value);
    }
    #endregion

    // FOR TESSTING
    private void Update()
    {
        if (!IsServer) return;

        // Skip to next state
        if (Input.GetKeyDown(KeyCode.T))
        {
            _netCurrentGameState.Value++;

            if (((int)_netCurrentGameState.Value) == System.Enum.GetValues(typeof(GameState)).Length)
                _netCurrentGameState.Value = 0;
        }
    }

    // ====================== Player Readying ======================
    #region Player Readying
    private void EnableReadyButton()
    {
        _readyButton.SetActive(true);
    }

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
        if (_netPlayersReadied.Value >= PlayerConnectionManager.GetNumConnectedPlayers())
        {
            Debug.Log("All Players ready, progressing state");

            // Progress to next state, looping back to morning if day over
            _netCurrentGameState.Value++;
            if (((int)_netCurrentGameState.Value) == System.Enum.GetValues(typeof(GameState)).Length)
                _netCurrentGameState.Value = GameState.Morning;

            _netPlayersReadied.Value = 0;

            UnReadyPlayerClientRpc();
        }
    }

    [ClientRpc]
    public void PlayerReadyClientRpc(ClientRpcParams clientRpcParams = default)
    {
        playerReady = true;
        _readyButton.SetActive(false);
    }

    [ClientRpc]
    public void UnReadyPlayerClientRpc()
    {
        playerReady = false;
    }
    #endregion

    // ====================== State Management ======================
    #region State Management
    public void UpdateGameState(GameState prev, GameState next)
    {
        if (_gameStateText != null)
            _gameStateText.text = next.ToString();

        switch (next)
        {
            case GameState.Intro:
                if (IsServer)
                    OnStateIntro();
                EnableReadyButton();
                break;
            case GameState.Morning:
                OnStateMorning();
                StartCoroutine(MorningTransition());
                break;
            case GameState.M_Forage:
                OnStateForage();
                break;
            case GameState.Afternoon:
                this.GetComponent<LocationManager>().ForceLocation(LocationManager.Location.Camp);
                OnStateAfternoon();
                EnableReadyButton();
                break;
            case GameState.Evening:
                OnStateEvening();
                EnableReadyButton();
                break;
            case GameState.Night:
                OnStateNight();
                EnableReadyButton();
                break;
        }
    }

    // Morning Transition
    private IEnumerator MorningTransition()
    {
        yield return new WaitForSeconds(0.2f);
        EnableReadyButton();
    }
    #endregion
}
