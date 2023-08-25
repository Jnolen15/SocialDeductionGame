using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using TMPro;
using System.Linq;
using System;

public class GameManager : NetworkBehaviour
{
    // ============== Singleton pattern ==============
    #region Singleton
    public static GameManager Instance { get; private set; }
    private void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(this);
        else
            Instance = this;
    }
    #endregion

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
    [Header("Cheats")]
    [SerializeField] private bool _testForWin;
    [SerializeField] private bool _doCheats;

    // ================== Variables ==================
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
    private Dictionary<ulong, bool> _playerReadyDictionary = new();

    // ================== Events ==================
    public delegate void ChangeStateAction();
    public static event ChangeStateAction OnStateChange;
    public static event ChangeStateAction OnSetup;
    public static event ChangeStateAction OnStateIntro;
    public static event ChangeStateAction OnStateMorning;
    public static event ChangeStateAction OnStateForage;
    public static event ChangeStateAction OnStateAfternoon;
    public static event ChangeStateAction OnStateEvening;
    public static event ChangeStateAction OnStateNight;

    public static event Action<bool> OnPlayerReadyToggled;

    // ================== Setup ==================
    #region Setup
    public override void OnNetworkSpawn()
    {
        Instance._netCurrentGameState.OnValueChanged += UpdateGameState;
        Instance._netDay.OnValueChanged += UpdateDayText;
    }

    private void OnDisable()
    {
        Instance._netCurrentGameState.OnValueChanged -= UpdateGameState;
        Instance._netDay.OnValueChanged -= UpdateDayText;
    }

    private void Start()
    {
        UpdateGameState(GameState.Morning, Instance._netCurrentGameState.Value);
    }
    #endregion

    // FOR TESTING
    private void Update()
    {
        if (!IsServer) return;

        if (!_doCheats) return;

        // Skip to next state
        if (Input.GetKeyDown(KeyCode.S))
        {
            Instance._netCurrentGameState.Value++;

            if (((int)Instance._netCurrentGameState.Value) == System.Enum.GetValues(typeof(GameState)).Length)
                Instance._netCurrentGameState.Value = 0;
        }
    }

    // ================== Player Positions ==================
    #region Player Positions
    public static void GetSeat(Transform playerTrans, ulong playerID)
    {
        Debug.Log("Getting Seat for player " + playerID);

        if((int)playerID > Instance.playerPositions.Count - 1)
        {
            Debug.LogError("Not Enough Seats!");
            return;
        }

        playerTrans.position = Instance.playerPositions[(int)playerID].position;
        playerTrans.rotation = Instance.playerPositions[(int)playerID].rotation;
    }
    #endregion

    // ====================== Player Readying ======================
    #region Player Readying
    public static void ReadyPlayer()
    {
        Instance.PlayerReadyServerRpc();
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void PlayerReadyServerRpc(ServerRpcParams serverRpcParams = default)
    {
        ulong clientID = serverRpcParams.Receive.SenderClientId;

        // Check if player is already Readied
        if (Instance._playerReadyDictionary.ContainsKey(clientID) && Instance._playerReadyDictionary[clientID] == true)
            return;

        // Get client data
        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { clientID }
            }
        };

        // Record ready player on server
        Instance._netPlayersReadied.Value++;
        Instance._playerReadyDictionary[clientID] = true;
        PlayerReadyClientRpc(clientRpcParams);

        // Check if all players ready
        if (Instance._netPlayersReadied.Value >= PlayerConnectionManager.CheckNumLivingPlayers())
        {
            Debug.Log($"<color=yellow>SERVER: </color> All players ready");

            // Unready
            Instance._netPlayersReadied.Value = 0;
            foreach(ulong key in Instance._playerReadyDictionary.Keys.ToList())
            {
                Instance._playerReadyDictionary[key] = false;
            }
            PlayerUnreadyClientRpc();

            // Progress to next state, looping back to morning if day over
            Instance._netCurrentGameState.Value++;
            if (((int)Instance._netCurrentGameState.Value) == System.Enum.GetValues(typeof(GameState)).Length)
                Instance._netCurrentGameState.Value = GameState.Morning;
        }
    }

    [ClientRpc]
    public void PlayerReadyClientRpc(ClientRpcParams clientRpcParams = default)
    {
        OnPlayerReadyToggled?.Invoke(true);
    }

    [ClientRpc]
    public void PlayerUnreadyClientRpc()
    {
        OnPlayerReadyToggled?.Invoke(false);
    }
    #endregion

    // ====================== State Management ======================
    #region State Management
    public void UpdateGameState(GameState prev, GameState next)
    {
        if (Instance._gameStateText != null)
            Instance._gameStateText.text = next.ToString();

        if (next != GameState.Pregame)
            OnStateChange?.Invoke();

        if (IsServer && next != GameState.Pregame && next != GameState.Intro)
            CheckSaboteurWin();

        switch (next)
        {
            case GameState.Intro:
                if (IsServer)
                    OnSetup?.Invoke();
                OnStateIntro?.Invoke();
                break;
            case GameState.Morning:
                if (IsServer)
                {
                    IncrementDay();
                    CheckSurvivorWin();
                }
                OnStateMorning?.Invoke();
                break;
            case GameState.M_Forage:
                OnStateForage?.Invoke();
                break;
            case GameState.Afternoon:
                this.GetComponent<LocationManager>().ForceLocation(LocationManager.Location.Camp);
                OnStateAfternoon?.Invoke();
                break;
            case GameState.Evening:
                OnStateEvening?.Invoke();
                break;
            case GameState.Night:
                OnStateNight?.Invoke();
                break;
        }
    }

    private void IncrementDay()
    {
        if (!IsServer)
            return;

        Debug.Log("<color=yellow>SERVER: </color> Incrementing Day");
        Instance._netDay.Value++;
    }

    private void UpdateDayText(int prev, int next)
    {
        if (Instance._dayText != null)
            Instance._dayText.text = "Day: " + next.ToString();
    }
    #endregion

    // ====================== Game Win / Loss Conditions ======================
    #region Win Loss

    // Check for game end via survivor win
    private void CheckSurvivorWin()
    {
        if (!Instance._testForWin)
            return;

        Debug.Log("<color=yellow>SERVER: </color> Checking Survivor Win");

        if (Instance._netDay.Value >= Instance._numDaysTillRescue)
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
        Instance._endScreen.SetActive(true);
        Instance._endScreenText.text = "Survivors Win";
        Instance._endScreenText.color = Color.green;
    }

    // Check for game end via Saboteur win
    private void CheckSaboteurWin()
    {
        if (!Instance._testForWin)
            return;

        Debug.Log("<color=yellow>SERVER: </color> Checking Saboteur Win");

        // If number of Saboteurs >= survivors
        if (PlayerConnectionManager.GetNumLivingOnTeam(PlayerData.Team.Saboteurs) >= PlayerConnectionManager.GetNumLivingOnTeam(PlayerData.Team.Survivors))
        {
            Debug.Log("<color=yellow>SERVER: </color> # of Saboteurs >= # of survivors, WIN!");
            SetSaboteurWinClientRpc();
        }

        // If all players are dead
        if (PlayerConnectionManager.CheckNumLivingPlayers() == 0)
        {
            Debug.Log("<color=yellow>SERVER: </color> All players have died, WIN!");
            SetSaboteurWinClientRpc();
        }
    }

    [ClientRpc]
    private void SetSaboteurWinClientRpc()
    {
        Debug.Log("<color=blue>CLIENT: </color> Saboteur Wins!");

        // Show end screens
        Instance._endScreen.SetActive(true);
        Instance._endScreenText.text = "Saboteur Wins";
        Instance._endScreenText.color = Color.red;
    }

    #endregion
}
