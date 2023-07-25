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
    [SerializeField] private NetworkVariable<int> _netPlayersReadied = new(writePerm: NetworkVariableWritePermission.Server);

    // State Events
    public delegate void ChangeStateAction();
    public static event ChangeStateAction OnStateChange;
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

        // Check if all players are ready
        if (_netPlayersReadied.Value >= PlayerConnectionManager.GetNumLivingPlayers())
        {
            _netPlayersReadied.Value = 0;

            // Progress to next state, looping back to morning if day over
            Debug.Log("All Players ready, progressing state");
            _netCurrentGameState.Value++;
            if (((int)_netCurrentGameState.Value) == System.Enum.GetValues(typeof(GameState)).Length)
                _netCurrentGameState.Value = GameState.Morning;
        }
    }
    #endregion

    // ====================== State Management ======================
    #region State Management
    public void UpdateGameState(GameState prev, GameState next)
    {
        if (_gameStateText != null)
            _gameStateText.text = next.ToString();

        if(next != GameState.Pregame)
            OnStateChange();

        switch (next)
        {
            case GameState.Intro:
                if (IsServer)
                    OnStateIntro();
                break;
            case GameState.Morning:
                OnStateMorning();
                break;
            case GameState.M_Forage:
                OnStateForage();
                break;
            case GameState.Afternoon:
                this.GetComponent<LocationManager>().ForceLocation(LocationManager.Location.Camp);
                OnStateAfternoon();
                break;
            case GameState.Evening:
                OnStateEvening();
                break;
            case GameState.Night:
                OnStateNight();
                break;
        }
    }
    #endregion
}
