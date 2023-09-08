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
    [SerializeField] private float _nightTimerMax;
    [SerializeField] private NetworkVariable<float> _netMorningTimer = new(writePerm: NetworkVariableWritePermission.Server);
    [SerializeField] private NetworkVariable<float> _netAfternoonTimer = new(writePerm: NetworkVariableWritePermission.Server);
    [SerializeField] private NetworkVariable<float> _netEveningTimer = new(writePerm: NetworkVariableWritePermission.Server);
    [SerializeField] private NetworkVariable<float> _netNightTimer = new(writePerm: NetworkVariableWritePermission.Server);
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

    public static event Action<bool> OnGameEnd;

    // ================== Setup ==================
    #region Setup
    public override void OnNetworkSpawn()
    {
        _netCurrentGameState.OnValueChanged += UpdateGameState;
        PlayerConnectionManager.OnAllPlayersReady += ProgressState;
    }

    private void OnDisable()
    {
        _netCurrentGameState.OnValueChanged -= UpdateGameState;
        PlayerConnectionManager.OnAllPlayersReady -= ProgressState;
    }

    private void Start()
    {
        UpdateGameState(GameState.Morning, _netCurrentGameState.Value);
    }
    #endregion

    // ================== Update ==================
    #region Update
    private void Update()
    {
        if (!IsServer) return;

        // Calculate timer speed up modifier based on number of players ready
        float percentReady = ((float)PlayerConnectionManager.Instance.GetNumReadyPlayers() / (float)PlayerConnectionManager.Instance.CheckNumLivingPlayers());
        float modVal = (_playerReadyTimerModCurve.Evaluate(percentReady) + 1f);

        // State Timers
        switch (_netCurrentGameState.Value)
        {
            case 0:
                break;
            case GameState.Morning:
                _netMorningTimer.Value -= (Time.deltaTime * modVal);
                if (_netMorningTimer.Value <= 0)
                {
                    Debug.Log($"<color=yellow>SERVER: </color> Morning Timer up, Progressing");
                    ProgressState();
                }
                break;
            case GameState.Afternoon:
                _netAfternoonTimer.Value -= (Time.deltaTime * modVal);
                if (_netAfternoonTimer.Value <= 0)
                {
                    Debug.Log($"<color=yellow>SERVER: </color> Afternoon Timer up, Progressing");
                    ProgressState();
                }
                break;
            case GameState.Evening:
                _netEveningTimer.Value -= (Time.deltaTime * modVal);
                //Debug.Log($"Percent players ready: {percentReady} Player bonus: {_playerReadyTimerModCurve.Evaluate(percentReady)} current mod val: {modVal}");
                if (_netEveningTimer.Value <= 0)
                {
                    Debug.Log($"<color=yellow>SERVER: </color> Night Timer up, Progressing");
                    ProgressState();
                }
                break;
            case GameState.Night:
                _netNightTimer.Value -= (Time.deltaTime * modVal);
                //Debug.Log($"Percent players ready: {percentReady} Player bonus: {_playerReadyTimerModCurve.Evaluate(percentReady)} current mod val: {modVal}");
                if (_netNightTimer.Value <= 0)
                {
                    Debug.Log($"<color=yellow>SERVER: </color> Night Timer up, Progressing");
                    ProgressState();
                }
                break;
        }

        // FOR TESTING Skip to next state
        if (_doCheats && Input.GetKeyDown(KeyCode.S))
        {
            _netCurrentGameState.Value++;

            if (((int)_netCurrentGameState.Value) == System.Enum.GetValues(typeof(GameState)).Length)
                _netCurrentGameState.Value = 0;
        }
    }
    #endregion

    // ================== Player Positions ==================
    #region Player Positions
    public void GetSeat(Transform playerTrans, ulong playerID)
    {
        Debug.Log("Getting Seat for player " + playerID);

        if((int)playerID > playerPositions.Count - 1)
        {
            Debug.LogError("Not Enough Seats!");
            return;
        }

        playerTrans.position = playerPositions[(int)playerID].position;
        playerTrans.rotation = playerPositions[(int)playerID].rotation;
    }
    #endregion

    // ====================== State Management ======================
    #region State Management
    private void ProgressState()
    {
        if (!IsServer) return;

        Debug.Log("<color=yellow>SERVER: </color> Progressing State");

        // Progress to next state, looping back to morning if day over
        _netCurrentGameState.Value++;
        if (((int)_netCurrentGameState.Value) == System.Enum.GetValues(typeof(GameState)).Length)
            _netCurrentGameState.Value = GameState.Morning;
    }

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
                    _netMorningTimer.Value = _morningTimerMax;
                    IncrementDay();
                    CheckSurvivorWin();
                }
                OnStateMorning?.Invoke();
                break;
            case GameState.M_Forage:
                OnStateForage?.Invoke();
                break;
            case GameState.Afternoon:
                if(IsServer) _netAfternoonTimer.Value = _afternoonTimerMax;
                this.GetComponent<LocationManager>().ForceLocation(LocationManager.Location.Camp);
                OnStateAfternoon?.Invoke();
                break;
            case GameState.Evening:
                if (IsServer) _netEveningTimer.Value = _eveningTimerMax;
                OnStateEvening?.Invoke();
                break;
            case GameState.Night:
                if (IsServer) _netNightTimer.Value = _nightTimerMax;
                OnStateNight?.Invoke();
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
            if (PlayerConnectionManager.Instance.GetNumLivingOnTeam(PlayerData.Team.Survivors) > PlayerConnectionManager.Instance.GetNumLivingOnTeam(PlayerData.Team.Saboteurs))
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
        if (!_testForWin)
            return;

        Debug.Log("<color=yellow>SERVER: </color> Checking Saboteur Win");

        // If number of Saboteurs >= survivors
        if (PlayerConnectionManager.Instance.GetNumLivingOnTeam(PlayerData.Team.Saboteurs) >= PlayerConnectionManager.Instance.GetNumLivingOnTeam(PlayerData.Team.Survivors))
        {
            Debug.Log("<color=yellow>SERVER: </color> # of Saboteurs >= # of survivors, WIN!");
            SetSaboteurWinClientRpc();
        }

        // If all players are dead
        if (PlayerConnectionManager.Instance.CheckNumLivingPlayers() == 0)
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
    public GameManager.GameState GetCurrentGameState()
    {
        return _netCurrentGameState.Value;
    }

    public int GetCurrentDay()
    {
        return _netDay.Value;
    }

    public float GetStateTimer()
    {
        switch (_netCurrentGameState.Value)
        {
            case 0:
                return 1;
            case GameState.Morning:
                return 1 - (_netMorningTimer.Value / _morningTimerMax);
            case GameState.Afternoon:
                return 1 - (_netAfternoonTimer.Value / _afternoonTimerMax);
            case GameState.Evening:
                return 1 - (_netEveningTimer.Value / _eveningTimerMax);
            case GameState.Night:
                return 1 - (_netNightTimer.Value / _nightTimerMax);
        }

        return 1;
    }
    #endregion
}
