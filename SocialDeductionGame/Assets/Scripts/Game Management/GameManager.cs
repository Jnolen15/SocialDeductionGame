using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using TMPro;

public class GameManager : NetworkBehaviour
{
    // ================== Refrences ==================
    [Header("UI Refrences")]
    [SerializeField] private TextMeshProUGUI _gameStateText;
    [SerializeField] private TextMeshProUGUI _dayText;
    [SerializeField] private GameObject _endScreen;
    [SerializeField] private TextMeshProUGUI _endScreenText;
    [Header("Player Seating Positions")]
    [SerializeField] private List<Transform> playerPositions = new();
    [Header("Win Settings")]
    [SerializeField] private int _numDaysTillRescue;
    [SerializeField] private bool _testForWin;

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
    [Header("Net Variables (For Viewing)")]
    [SerializeField] private NetworkVariable<int> _netDay = new(writePerm: NetworkVariableWritePermission.Server);
    [SerializeField] private NetworkVariable<int> _netPlayersReadied = new(writePerm: NetworkVariableWritePermission.Server);

    // State Events
    public delegate void ChangeStateAction();
    public static event ChangeStateAction OnStateChange;
    public static event ChangeStateAction OnSetup;
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
        _netDay.OnValueChanged += UpdateDayText;
    }

    private void OnDisable()
    {
        _netCurrentGameState.OnValueChanged -= UpdateGameState;
        _netDay.OnValueChanged -= UpdateDayText;
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
        if (Input.GetKeyDown(KeyCode.S))
        {
            _netCurrentGameState.Value++;

            if (((int)_netCurrentGameState.Value) == System.Enum.GetValues(typeof(GameState)).Length)
                _netCurrentGameState.Value = 0;
        }
    }

    // ================== Player Positions ==================
    #region Player Positions
    public void GetSeat(Transform playerTrans, ulong playerID)
    {
        Debug.Log("Getting Seat for player " + playerID);

        playerTrans.position = playerPositions[(int)playerID].position;
    }
    #endregion

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

        if (IsServer && next != GameState.Pregame && next != GameState.Intro)
            CheckSaboteurWin();

        switch (next)
        {
            case GameState.Intro:
                if (IsServer)
                    OnSetup();
                OnStateIntro();
                break;
            case GameState.Morning:
                if (IsServer)
                {
                    IncrementDay();
                    CheckSurvivorWin();
                }
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

    private void IncrementDay()
    {
        if (!IsServer)
            return;

        Debug.Log("<color=yellow>SERVER: </color> Incrementing Day");
        _netDay.Value++;
    }

    private void UpdateDayText(int prev, int next)
    {
        if (_dayText != null)
            _dayText.text = "Day: " + next.ToString();
    }
    #endregion

    // ====================== Game Win / Loss Conditions ======================
    #region Win Loss

    // Check for game end via survivor win
    private void CheckSurvivorWin()
    {
        if (!_testForWin)
            return;

        Debug.Log("<color=yellow>SERVER: </color> Checking Survivor Win");

        if (_netDay.Value >= _numDaysTillRescue)
        {
            if (PlayerConnectionManager.GetNumLivingOnTeam(PlayerData.Team.Survivors) > PlayerConnectionManager.GetNumLivingOnTeam(PlayerData.Team.Saboteurs))
                SetSurvivorWinClientRpc();
        }
    }

    [ClientRpc]
    private void SetSurvivorWinClientRpc()
    {
        Debug.Log("<color=blue>CLIENT: </color> Survivors Win!");

        // Show end screens
        _endScreen.SetActive(true);
        _endScreenText.text = "Survivors Win";
        _endScreenText.color = Color.green;
    }

    // Check for game end via Saboteur win
    private void CheckSaboteurWin()
    {
        if (!_testForWin)
            return;

        Debug.Log("<color=yellow>SERVER: </color> Checking Saboteur Win");

        if (PlayerConnectionManager.GetNumLivingOnTeam(PlayerData.Team.Saboteurs) >= PlayerConnectionManager.GetNumLivingOnTeam(PlayerData.Team.Survivors))
        {
            SetSaboteurWinClientRpc();
        }
    }

    [ClientRpc]
    private void SetSaboteurWinClientRpc()
    {
        Debug.Log("<color=blue>CLIENT: </color> Saboteur Wins!");

        // Show end screens
        _endScreen.SetActive(true);
        _endScreenText.text = "Saboteur Wins";
        _endScreenText.color = Color.red;
    }

    #endregion
}
