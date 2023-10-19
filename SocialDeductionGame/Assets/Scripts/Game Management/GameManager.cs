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
    #region Variables, Refrences and Events
    [Header("State Timers")]
    [SerializeField] private float _introTimerMax;
    [SerializeField] private float _morningTimerMax;
    [SerializeField] private float _middayTimerMax;
    [SerializeField] private float _afternoonTimerMax;
    [SerializeField] private float _eveningTimerMax;
    [SerializeField] private float _nightTimerMax;
    [SerializeField] private NetworkVariable<float> _netIntroTimer = new(writePerm: NetworkVariableWritePermission.Server);
    [SerializeField] private NetworkVariable<float> _netMorningTimer = new(writePerm: NetworkVariableWritePermission.Server);
    [SerializeField] private NetworkVariable<float> _netMiddayTimer = new(writePerm: NetworkVariableWritePermission.Server);
    [SerializeField] private NetworkVariable<float> _netAfternoonTimer = new(writePerm: NetworkVariableWritePermission.Server);
    [SerializeField] private NetworkVariable<float> _netEveningTimer = new(writePerm: NetworkVariableWritePermission.Server);
    [SerializeField] private NetworkVariable<float> _netNightTimer = new(writePerm: NetworkVariableWritePermission.Server);
    [Header("Win Settings")]
    [SerializeField] private int _numDaysTillRescue;
    [Header("Transition Screens")]
    [SerializeField] public GameObject _waitingForPlayersTS;

    private bool _dontTestWin;
    private bool _doCheats;

    private LocationManager _locationManager;

    // ================== Variables ==================
    public enum GameState
    {
        Pregame,    // Wait for players to load, make prefabs
        Intro,      // Assign roles and seats, game intro
        Morning,    // Show new night event
        Midday,   // Pick location, pick cards
        Afternoon,  // Add resources to stash / fire
        Evening,    // Results of night event contribution, take food from fire, vote to exile
        Night       // Summary of night event effects / effects happen, saboteur picks new night event
    }
    [Header("Current State")]
    [SerializeField] private NetworkVariable<GameState> _netCurrentGameState = new(writePerm: NetworkVariableWritePermission.Server);
    [Header("Player Ready Timer Mod Curve")]
    [SerializeField] private AnimationCurve _playerReadyTimerModCurve;
    [Header("Net Variables (For Viewing)")]
    [SerializeField] private NetworkVariable<int> _netDay = new(writePerm: NetworkVariableWritePermission.Server);

    private bool _pregameComplete = false;

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
    #endregion

    // ================== Setup ==================
    #region Setup
    public override void OnNetworkSpawn()
    {
        _locationManager = this.GetComponent<LocationManager>();

        _netCurrentGameState.OnValueChanged += UpdateGameState;

        // CHEATS
        _dontTestWin = LogViewer.Instance.GetTestForWin();
        _doCheats = LogViewer.Instance.GetDoCheats();

        if (IsServer)
        {
            PlayerConnectionManager.OnPlayerSetupComplete += PregameComplete;
            PlayerConnectionManager.OnAllPlayersReady += ProgressState;
        }
    }

    private void OnDisable()
    {
        _netCurrentGameState.OnValueChanged -= UpdateGameState;

        if (IsServer)
        {
            PlayerConnectionManager.OnPlayerSetupComplete -= PregameComplete;
            PlayerConnectionManager.OnAllPlayersReady -= ProgressState;
        }
    }
    #endregion

    // ================== Update ==================
    #region Update
    private void Update()
    {
        if (!IsServer) return;

        // pregame stall
        // This is here so that start methods propperly trigger before any more functions are called
        if (_pregameComplete && _netCurrentGameState.Value == GameState.Pregame)
        {
            Debug.Log("<color=yellow>SERVER: </color> Pregame stall complete, progressing");
            ProgressState();
        }

        // State Timers
        switch (_netCurrentGameState.Value)
        {
            case 0:
                break;
            case GameState.Intro:
                _netIntroTimer.Value -= (Time.deltaTime * CalculateTimerMod());
                if (_netIntroTimer.Value <= 0)
                {
                    Debug.Log($"<color=yellow>SERVER: </color> Intro Timer up, Progressing");
                    ProgressState();
                }
                break;
            case GameState.Morning:
                _netMorningTimer.Value -= (Time.deltaTime * CalculateTimerMod());
                if (_netMorningTimer.Value <= 0)
                {
                    Debug.Log($"<color=yellow>SERVER: </color> Morning Timer up, Progressing");
                    ProgressState();
                }
                break;
            case GameState.Midday:
                _netMiddayTimer.Value -= (Time.deltaTime * CalculateTimerMod());
                if (_netMiddayTimer.Value <= 0)
                {
                    Debug.Log($"<color=yellow>SERVER: </color> Midday Timer up, Progressing");
                    ProgressState();
                }
                break;
            case GameState.Afternoon:
                _netAfternoonTimer.Value -= (Time.deltaTime * CalculateTimerMod());
                if (_netAfternoonTimer.Value <= 0)
                {
                    Debug.Log($"<color=yellow>SERVER: </color> Afternoon Timer up, Progressing");
                    ProgressState();
                }
                break;
            case GameState.Evening:
                _netEveningTimer.Value -= (Time.deltaTime * CalculateTimerMod());
                //Debug.Log($"Percent players ready: {percentReady} Player bonus: {_playerReadyTimerModCurve.Evaluate(percentReady)} current mod val: {modVal}");
                if (_netEveningTimer.Value <= 0)
                {
                    Debug.Log($"<color=yellow>SERVER: </color> Night Timer up, Progressing");
                    ProgressState();
                }
                break;
            case GameState.Night:
                _netNightTimer.Value -= (Time.deltaTime * CalculateTimerMod());
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

    // Calculate timer speed up modifier based on number of players ready
    private float CalculateTimerMod()
    {
        float percentReady = ((float)PlayerConnectionManager.Instance.GetNumReadyPlayers() / (float)PlayerConnectionManager.Instance.GetNumLivingPlayers());
        return (_playerReadyTimerModCurve.Evaluate(percentReady) + 1f);
    }
    #endregion

    // ====================== State Management ======================
    #region State Management
    private void PregameComplete()
    {
        Debug.Log("<color=yellow>SERVER: </color> Pregame setup complete, waiting a sec for start methods");
        _pregameComplete = true;
    }

    private void ProgressState()
    {
        if (!IsServer) return;

        Debug.Log("<color=yellow>SERVER: </color> Progressing State");

        // Unready all players in case state progressed due to time
        PlayerConnectionManager.Instance.UnreadyAllPlayers();

        // Progress to next state, looping back to morning if day over
        _netCurrentGameState.Value++;
        if (((int)_netCurrentGameState.Value) == System.Enum.GetValues(typeof(GameState)).Length)
            _netCurrentGameState.Value = GameState.Morning;
    }

    public void UpdateGameState(GameState prev, GameState current)
    {
        if (current == GameState.Pregame)
            return;

        Debug.Log("<color=yellow>SERVER: </color> Updating Game State to " + current.ToString());
        OnStateChange?.Invoke();

        if (IsServer && current != GameState.Intro)
            CheckSaboteurWin();

        switch (current)
        {
            case GameState.Intro:
                if (IsServer)
                {
                    _netIntroTimer.Value = _introTimerMax;
                    OnSetup?.Invoke();
                }
                OnStateIntro?.Invoke();
                _locationManager.SetInitialLocation();
                HideWaitingForPlayersTS();
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
            case GameState.Midday:
                if (IsServer) _netMiddayTimer.Value = _middayTimerMax;
                OnStateForage?.Invoke();
                break;
            case GameState.Afternoon:
                if(IsServer) _netAfternoonTimer.Value = _afternoonTimerMax;
                _locationManager.ForceLocation(LocationManager.LocationName.Camp);
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

    // ================== State Transitions ==================
    #region State Transitions
    private void HideWaitingForPlayersTS()
    {
        _waitingForPlayersTS.SetActive(false);
    }
    #endregion

    // ====================== Game Win / Loss Conditions ======================
    #region Win Loss

    // Check for game end via survivor win
    private void CheckSurvivorWin()
    {
        if (_dontTestWin)
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
        if (_dontTestWin)
            return;

        Debug.Log("<color=yellow>SERVER: </color> Checking Saboteur Win");

        // If number of Saboteurs >= survivors
        if (PlayerConnectionManager.Instance.GetNumLivingOnTeam(PlayerData.Team.Saboteurs) >= PlayerConnectionManager.Instance.GetNumLivingOnTeam(PlayerData.Team.Survivors))
        {
            Debug.Log("<color=yellow>SERVER: </color> # of Saboteurs >= # of survivors, WIN!");
            SetSaboteurWinClientRpc();
        }

        // If all players are dead
        if (PlayerConnectionManager.Instance.GetNumLivingPlayers() == 0)
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
            case GameState.Intro:
                return 1 - (_netIntroTimer.Value / _introTimerMax);
            case GameState.Morning:
                return 1 - (_netMorningTimer.Value / _morningTimerMax);
            case GameState.Midday:
                return 1 - (_netMiddayTimer.Value / _middayTimerMax);
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
