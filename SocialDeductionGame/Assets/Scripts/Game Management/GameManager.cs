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
    [Header("State Timers")]
    [SerializeField] private float _morningTimerMax;
    [SerializeField] private float _afternoonTimerMax;
    [SerializeField] private float _eveningTimerMax;
    [SerializeField] private NetworkVariable<float> _netMorningTimer = new(writePerm: NetworkVariableWritePermission.Server);
    [SerializeField] private NetworkVariable<float> _netAfternoonTimer = new(writePerm: NetworkVariableWritePermission.Server);
    [SerializeField] private NetworkVariable<float> _netEveningTimer = new(writePerm: NetworkVariableWritePermission.Server);
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
    [SerializeField] private AnimationCurve _playerReadyTimerModCurve;
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
    public static event Action<bool> OnGameEnd;

    // ================== Setup ==================
    #region Setup
    public override void OnNetworkSpawn()
    {
        Instance._netCurrentGameState.OnValueChanged += UpdateGameState;
    }

    private void OnDisable()
    {
        Instance._netCurrentGameState.OnValueChanged -= UpdateGameState;
    }

    private void Start()
    {
        UpdateGameState(GameState.Morning, Instance._netCurrentGameState.Value);
    }
    #endregion

    // ================== Update ==================
    #region Update
    private void Update()
    {
        if (!IsServer) return;

        // Calculate timer speed up modifier based on number of players ready
        float percentReady = ((float)Instance._netPlayersReadied.Value / (float)PlayerConnectionManager.CheckNumLivingPlayers());
        float modVal = (_playerReadyTimerModCurve.Evaluate(percentReady) + 1f);

        // State Timers
        switch (Instance._netCurrentGameState.Value)
        {
            case 0:
                break;
            case GameState.Morning:
                Instance._netMorningTimer.Value -= (Time.deltaTime * modVal);
                if (Instance._netMorningTimer.Value <= 0)
                {
                    Debug.Log($"<color=yellow>SERVER: </color> Morning Timer up, Progressing");
                    ProgressState();
                }
                break;
            case GameState.Afternoon:
                Instance._netAfternoonTimer.Value -= (Time.deltaTime * modVal);
                if (Instance._netAfternoonTimer.Value <= 0)
                {
                    Debug.Log($"<color=yellow>SERVER: </color> Afternoon Timer up, Progressing");
                    ProgressState();
                }
                break;
            case GameState.Evening:
                Instance._netEveningTimer.Value -= (Time.deltaTime * modVal);
                //Debug.Log($"Percent players ready: {percentReady} Player bonus: {_playerReadyTimerModCurve.Evaluate(percentReady)} current mod val: {modVal}");
                if (Instance._netEveningTimer.Value <= 0)
                {
                    Debug.Log($"<color=yellow>SERVER: </color> Night Timer up, Progressing");
                    ProgressState();
                }
                break;
        }

        // FOR TESTING Skip to next state
        if (_doCheats && Input.GetKeyDown(KeyCode.S))
        {
            Instance._netCurrentGameState.Value++;

            if (((int)Instance._netCurrentGameState.Value) == System.Enum.GetValues(typeof(GameState)).Length)
                Instance._netCurrentGameState.Value = 0;
        }
    }
    #endregion

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
            ProgressState();
        }
    }

    private void ProgressState()
    {
        if (!IsServer) return;

        Debug.Log($"<color=yellow>SERVER: </color> All players ready");

        // Unready
        Instance._netPlayersReadied.Value = 0;
        foreach (ulong key in Instance._playerReadyDictionary.Keys.ToList())
        {
            Instance._playerReadyDictionary[key] = false;
        }
        PlayerUnreadyClientRpc();

        // Progress to next state, looping back to morning if day over
        Instance._netCurrentGameState.Value++;
        if (((int)Instance._netCurrentGameState.Value) == System.Enum.GetValues(typeof(GameState)).Length)
            Instance._netCurrentGameState.Value = GameState.Morning;
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
                    Instance._netMorningTimer.Value = Instance._morningTimerMax;
                    IncrementDay();
                    CheckSurvivorWin();
                }
                OnStateMorning?.Invoke();
                break;
            case GameState.M_Forage:
                OnStateForage?.Invoke();
                break;
            case GameState.Afternoon:
                if(IsServer) Instance._netAfternoonTimer.Value = Instance._afternoonTimerMax;
                this.GetComponent<LocationManager>().ForceLocation(LocationManager.Location.Camp);
                OnStateAfternoon?.Invoke();
                break;
            case GameState.Evening:
                if (IsServer) Instance._netEveningTimer.Value = Instance._eveningTimerMax;
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
        OnGameEnd?.Invoke(true);
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
        OnGameEnd?.Invoke(false);
    }

    #endregion

    // ====================== Helpers ======================
    #region Helpers
    public static GameManager.GameState GetCurrentGameState()
    {
        return Instance._netCurrentGameState.Value;
    }

    public static int GetCurrentDay()
    {
        return Instance._netDay.Value;
    }

    public static float GetStateTimer()
    {
        switch (Instance._netCurrentGameState.Value)
        {
            case 0:
                return 1;
            case GameState.Morning:
                return 1 - (Instance._netMorningTimer.Value / Instance._morningTimerMax);
            case GameState.Afternoon:
                return 1 - (Instance._netAfternoonTimer.Value / Instance._afternoonTimerMax);
            case GameState.Evening:
                return 1 - (Instance._netEveningTimer.Value / Instance._eveningTimerMax);
        }

        return 1;
    }
    #endregion
}
