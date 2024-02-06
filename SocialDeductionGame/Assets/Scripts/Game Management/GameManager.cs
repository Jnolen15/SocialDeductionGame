using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using TMPro;
using System.Linq;
using System;
using Unity.Services.Analytics;

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
    private GameRules _gameRules;    // Server
    private NetworkVariable<float> _introTimerMax = new(writePerm: NetworkVariableWritePermission.Server);
    private NetworkVariable<float> _morningTimerMax = new(writePerm: NetworkVariableWritePermission.Server);
    private NetworkVariable<float> _afternoonTimerMax = new(writePerm: NetworkVariableWritePermission.Server);
    private NetworkVariable<float> _eveningTimerMax = new(writePerm: NetworkVariableWritePermission.Server);
    private NetworkVariable<float> _nightTimerMax = new(writePerm: NetworkVariableWritePermission.Server);
    private NetworkVariable<float> _transitionTimerMax = new(writePerm: NetworkVariableWritePermission.Server);
    [SerializeField] private NetworkVariable<float> _netIntroTimer = new(writePerm: NetworkVariableWritePermission.Server);
    [SerializeField] private NetworkVariable<float> _netMorningTimer = new(writePerm: NetworkVariableWritePermission.Server);
    [SerializeField] private NetworkVariable<float> _netAfternoonTimer = new(writePerm: NetworkVariableWritePermission.Server);
    [SerializeField] private NetworkVariable<float> _netEveningTimer = new(writePerm: NetworkVariableWritePermission.Server);
    [SerializeField] private NetworkVariable<float> _netNightTimer = new(writePerm: NetworkVariableWritePermission.Server);
    [SerializeField] private NetworkVariable<float> _netTransitionTimer = new(writePerm: NetworkVariableWritePermission.Server);
    private float _pauseTimer;
    private bool _rescueEarly;
    private bool _gameOver;

    private bool _dontTestWin;
    private bool _doCheats;

    private LocationManager _locationManager;

    // ================== Variables ==================
    public enum GameState
    {
        Pregame,    // Wait for players to load, make prefabs
        Intro,      // Assign roles and seats, game intro
        Morning,    // Show new night event
        AfternoonTransition,
        Afternoon,  // Add resources to stash / fire
        EveningTransition,
        Evening,    // Results of night event contribution, take food from fire, vote to exile
        NightTransition,
        Night,      // Summary of night event effects / effects happen, saboteur picks new night event
        MorningTransition,
        GameOver    // Game over state
    }
    [Header("Current State")]
    [SerializeField] private NetworkVariable<GameState> _netCurrentGameState = new(writePerm: NetworkVariableWritePermission.Server);
    [Header("Player Ready Timer Mod Curve")]
    [SerializeField] private AnimationCurve _playerReadyTimerModCurve;
    [Header("Net Variables (For Viewing)")]
    [SerializeField] private NetworkVariable<int> _netDay = new(writePerm: NetworkVariableWritePermission.Server);

    private bool _pregameComplete = false;

    // ================== Events ==================
    public delegate void ChangeStateAction(GameState prev, GameState current);
    public static event ChangeStateAction OnStateChange;
    public delegate void ChangeStateToAction();
    public static event ChangeStateToAction OnSetup;
    public static event ChangeStateToAction OnStateIntro;
    public static event ChangeStateToAction OnStateMorning;
    public static event ChangeStateToAction OnStateAfternoon;
    public static event ChangeStateToAction OnStateEvening;
    public static event ChangeStateToAction OnStateNight;
    public static event ChangeStateToAction OnStateGameEnd;

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
            PlayerConnectionManager.OnPlayerDied += OnPlayerDied;

            _gameRules = PlayerConnectionManager.Instance.GetGameRules();
            SetupGameRules();
        }
    }

    private void SetupGameRules()
    {
        _introTimerMax.Value = _gameRules.IntroTimerMax;
        _morningTimerMax.Value = _gameRules.MorningTimerMax;
        _afternoonTimerMax.Value = _gameRules.AfternoonTimerMax;
        _eveningTimerMax.Value = _gameRules.EveningTimerMax;
        _nightTimerMax.Value = _gameRules.NightTimerMax;
        _transitionTimerMax.Value = _gameRules.TransitionTimerMax;

        if(_gameRules.TimerLength == GameRules.TimerLengths.Shorter)
        {
            Debug.Log("<color=yellow>SERVER: </color>Setting timer length to shorter");
            _morningTimerMax.Value = _morningTimerMax.Value - 30;
            _afternoonTimerMax.Value = _afternoonTimerMax.Value - 20;
            _eveningTimerMax.Value = _eveningTimerMax.Value - 20;
            _nightTimerMax.Value = _nightTimerMax.Value - 5;
        }
        else if (_gameRules.TimerLength == GameRules.TimerLengths.Longer)
        {
            Debug.Log("<color=yellow>SERVER: </color>Setting timer length to longer");
            _morningTimerMax.Value = _morningTimerMax.Value + 30;
            _afternoonTimerMax.Value = _afternoonTimerMax.Value + 20;
            _eveningTimerMax.Value = _eveningTimerMax.Value + 20;
            _nightTimerMax.Value = _nightTimerMax.Value + 10;
        }

        // Track Analytics
        AnalyticsTracker.Instance.TrackGameSettings(_gameRules.NumSaboteurs, _gameRules.NumDaysToWin, _gameRules.TimerLength.ToString());
    }

    private void OnDisable()
    {
        _netCurrentGameState.OnValueChanged -= UpdateGameState;

        if (IsServer)
        {
            PlayerConnectionManager.OnPlayerSetupComplete -= PregameComplete;
            PlayerConnectionManager.OnAllPlayersReady -= ProgressState;
            PlayerConnectionManager.OnPlayerDied -= OnPlayerDied;
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
                RunTimer(_netIntroTimer);
                break;
            case GameState.Morning:
                RunTimer(_netMorningTimer);
                break;
            case GameState.AfternoonTransition:
                RunTimer(_netTransitionTimer);
                break;
            case GameState.Afternoon:
                RunTimer(_netAfternoonTimer);
                break;
            case GameState.EveningTransition:
                RunTimer(_netTransitionTimer);
                break;
            case GameState.Evening:
                RunTimer(_netEveningTimer);
                break;
            case GameState.NightTransition:
                RunTimer(_netTransitionTimer);
                break;
            case GameState.Night:
                RunTimer(_netNightTimer);
                break;
            case GameState.MorningTransition:
                RunTimer(_netTransitionTimer);
                break;
        }

        // FOR TESTING Skip to next state
        if (_doCheats && Input.GetKeyDown(KeyCode.S))
        {
            if (_netCurrentGameState.Value == GameState.MorningTransition)
                _netCurrentGameState.Value = GameState.Morning;
            else
                _netCurrentGameState.Value++;
        }
    }

    private void RunTimer(NetworkVariable<float> timer)
    {
        // Pause Timer
        if(_pauseTimer >= 0)
        {
            _pauseTimer -= Time.deltaTime;
        }
        // Normal state timer
        else
        {
            timer.Value -= (Time.deltaTime * CalculateTimerMod());
            if (timer.Value <= 0)
            {
                Debug.Log($"<color=yellow>SERVER: </color> {timer} Timer up, Progressing");
                ProgressState();
            }
        }
    }

    // Calculate timer speed up modifier based on number of players ready
    private float CalculateTimerMod()
    {
        float percentReady = ((float)PlayerConnectionManager.Instance.GetNumReadyPlayers() / (float)PlayerConnectionManager.Instance.GetNumLivingPlayers());
        return (_playerReadyTimerModCurve.Evaluate(percentReady) + 1f);
    }

    public void PauseCurrentTimer(float time)
    {
        if (!IsServer) return;

        Debug.Log($"<color=yellow>SERVER: </color> Pausing {_netCurrentGameState.Value} timer for {time} seconds.");

        _pauseTimer += time;
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

        // If a timer was paused, reset pause duration
        _pauseTimer = 0;

        // Win checking
        if (_netCurrentGameState.Value == GameState.MorningTransition)
        {
            // If players died in the night
            if (CheckSaboteurWin())
            {
                EndGame(false);
                return;
            }
            // If survivors made it to last day
            else if (CheckSurvivorWin())
            {
                EndGame(true);
                return;
            }
        }

        // Progress to next state, looping back to morning if day over
        if (_netCurrentGameState.Value == GameState.MorningTransition)
            _netCurrentGameState.Value = GameState.Morning;
        else
            _netCurrentGameState.Value++;
    }

    public void UpdateGameState(GameState prev, GameState current)
    {
        if (current == GameState.Pregame)
            return;

        Debug.Log("<color=yellow>SERVER: </color> Updating Game State to " + current.ToString());
        OnStateChange?.Invoke(prev, current);

        switch (current)
        {
            case GameState.Intro:
                if (IsServer)
                {
                    _netIntroTimer.Value = _introTimerMax.Value;
                    OnSetup?.Invoke();
                }
                OnStateIntro?.Invoke();
                _locationManager.SetInitialLocation();
                break;
            case GameState.Morning:
                if (IsServer)
                {
                    _netMorningTimer.Value = _morningTimerMax.Value;
                    IncrementDay();
                }
                OnStateMorning?.Invoke();
                break;
            case GameState.AfternoonTransition:
                if (IsServer) _netTransitionTimer.Value = _transitionTimerMax.Value;
                break;
            case GameState.Afternoon:
                if(IsServer) _netAfternoonTimer.Value = _afternoonTimerMax.Value;
                _locationManager.ForceLocation(LocationManager.LocationName.Camp);
                OnStateAfternoon?.Invoke();
                break;
            case GameState.EveningTransition:
                if (IsServer) _netTransitionTimer.Value = _transitionTimerMax.Value;
                break;
            case GameState.Evening:
                if (IsServer) _netEveningTimer.Value = _eveningTimerMax.Value;
                OnStateEvening?.Invoke();
                break;
            case GameState.NightTransition:
                if (IsServer) _netTransitionTimer.Value = _transitionTimerMax.Value;
                break;
            case GameState.Night:
                if (IsServer) _netNightTimer.Value = _nightTimerMax.Value;
                OnStateNight?.Invoke();
                break;
            case GameState.MorningTransition:
                if (IsServer) _netTransitionTimer.Value = _transitionTimerMax.Value;
                break;
            case GameState.GameOver:
                _locationManager.ForceLocation(LocationManager.LocationName.Camp);
                OnStateGameEnd?.Invoke();
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
    private bool CheckSurvivorWin()
    {
        if (_dontTestWin || _gameOver)
            return false;

        int numLivingSurvivors = PlayerConnectionManager.Instance.GetNumLivingOnTeam(PlayerData.Team.Survivors);
        int numLivingSaboteurs = PlayerConnectionManager.Instance.GetNumLivingOnTeam(PlayerData.Team.Saboteurs);

        Debug.Log($"<color=yellow>SERVER: </color>Testing Survivor Win. Survivors living: {numLivingSurvivors} Saboteurs living: {numLivingSaboteurs}");

        // Check if its days to win -1 because it checks before updating day
        if (_netDay.Value >= _gameRules.NumDaysToWin-1)
        {
            Debug.Log("<color=yellow>SERVER: </color>Rescue has arrived. Survivor Win!");
            return true; // WIN
        }
        else if (_rescueEarly)
        {
            Debug.Log("<color=yellow>SERVER: </color>Rescue has arrived early. Survivor Win!");
            _rescueEarly = false;
            return true; // WIN
        }
        else if (numLivingSaboteurs <= 0)
        {
            Debug.Log("<color=yellow>SERVER: </color>All Saboteurs are dead. Rescue arriving tomorrow.");
            _rescueEarly = true;
        }

        return false;
    }

    private void OnPlayerDied()
    {
        // Dont end game during night
        if (_netCurrentGameState.Value == GameState.Night)
            return;

        if (CheckSaboteurWin())
            EndGame(false);
    }

    // Check for game end via Saboteur win
    private bool CheckSaboteurWin()
    {
        if (_dontTestWin || _gameOver)
            return false;

        int numLivingSurvivors = PlayerConnectionManager.Instance.GetNumLivingOnTeam(PlayerData.Team.Survivors);
        int numLivingSaboteurs = PlayerConnectionManager.Instance.GetNumLivingOnTeam(PlayerData.Team.Saboteurs);

        Debug.Log($"<color=yellow>SERVER: </color>Testing Saboteur Win. Survivors living: {numLivingSurvivors} Saboteurs living: {numLivingSaboteurs}");

        // If all players are dead
        if (PlayerConnectionManager.Instance.GetNumLivingPlayers() == 0)
        {
            Debug.Log("<color=yellow>SERVER: </color> All players have died, WIN!");
            return true;
        }

        // If number of Saboteurs >= survivors
        if (numLivingSaboteurs >= numLivingSurvivors)
        {
            Debug.Log("<color=yellow>SERVER: </color> # of Saboteurs >= # of survivors, WIN!");
            return true;
        }

        return false;
    }

    private void EndGame(bool survivorWin)
    {
        if (!IsServer)
            return;

        _gameOver = true;
        _netCurrentGameState.Value = GameState.GameOver;

        // Record analyitic data
        int numPlayers = PlayerConnectionManager.Instance.GetNumConnectedPlayers();
        int numSabos = PlayerConnectionManager.Instance.GetNumSaboteurs();
        AnalyticsTracker.Instance.TrackGameEnd(survivorWin, GetCurrentDay(), numPlayers, numSabos);

        SetGameOverClientRpc(survivorWin);
    }

    [ClientRpc]
    private void SetGameOverClientRpc(bool survivorWin)
    {
        Debug.Log("<color=blue>CLIENT: </color> Game over. Survivor win " + survivorWin);

        // Show end screens
        OnGameEnd?.Invoke(survivorWin);
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
                return 1 - (_netIntroTimer.Value / _introTimerMax.Value);
            case GameState.Morning:
                return 1 - (_netMorningTimer.Value / _morningTimerMax.Value);
            case GameState.Afternoon:
                return 1 - (_netAfternoonTimer.Value / _afternoonTimerMax.Value);
            case GameState.Evening:
                return 1 - (_netEveningTimer.Value / _eveningTimerMax.Value);
            case GameState.Night:
                return 1 - (_netNightTimer.Value / _nightTimerMax.Value);
        }

        return 1;
    }

    public bool InTransition()
    {
        switch (_netCurrentGameState.Value)
        {
            case GameState.Intro:
                return false;
            case GameState.Morning:
                return false;
            case GameState.AfternoonTransition:
                return true;
            case GameState.Afternoon:
                return false;
            case GameState.EveningTransition:
                return true;
            case GameState.Evening:
                return false;
            case GameState.NightTransition:
                return true;
            case GameState.Night:
                return false;
            case GameState.MorningTransition:
                return true;
            case GameState.GameOver:
                return false;
            default:
                return true;
        }
    }
    #endregion
}
