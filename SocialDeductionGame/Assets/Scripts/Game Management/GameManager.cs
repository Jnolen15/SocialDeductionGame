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
    [SerializeField] private float _afternoonTimerMax;
    [SerializeField] private float _eveningTimerMax;
    [SerializeField] private float _nightTimerMax;
    [SerializeField] private float _transitionTimerMax;
    [SerializeField] private NetworkVariable<float> _netIntroTimer = new(writePerm: NetworkVariableWritePermission.Server);
    [SerializeField] private NetworkVariable<float> _netMorningTimer = new(writePerm: NetworkVariableWritePermission.Server);
    [SerializeField] private NetworkVariable<float> _netAfternoonTimer = new(writePerm: NetworkVariableWritePermission.Server);
    [SerializeField] private NetworkVariable<float> _netEveningTimer = new(writePerm: NetworkVariableWritePermission.Server);
    [SerializeField] private NetworkVariable<float> _netNightTimer = new(writePerm: NetworkVariableWritePermission.Server);
    [SerializeField] private NetworkVariable<float> _netTransitionTimer = new(writePerm: NetworkVariableWritePermission.Server);
    [Header("Win Settings")]
    [SerializeField] private int _numDaysTillRescue;

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
        MorningTransition
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
            PlayerConnectionManager.OnPlayerDied += CheckSaboteurWin;
        }
    }

    private void OnDisable()
    {
        _netCurrentGameState.OnValueChanged -= UpdateGameState;

        if (IsServer)
        {
            PlayerConnectionManager.OnPlayerSetupComplete -= PregameComplete;
            PlayerConnectionManager.OnAllPlayersReady -= ProgressState;
            PlayerConnectionManager.OnPlayerDied -= CheckSaboteurWin;
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
            _netCurrentGameState.Value++;

            if (((int)_netCurrentGameState.Value) == System.Enum.GetValues(typeof(GameState)).Length)
                _netCurrentGameState.Value = 0;
        }
    }

    private void RunTimer(NetworkVariable<float> timer)
    {
        timer.Value -= (Time.deltaTime * CalculateTimerMod());
        if (timer.Value <= 0)
        {
            Debug.Log($"<color=yellow>SERVER: </color> {timer} Timer up, Progressing");
            ProgressState();
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
        OnStateChange?.Invoke(prev, current);

        // This shouldn't be needed. But since the game can be started with euqal sabos and survivors ill leave it
        // But in the future when the lobby update comes it should not be possible
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
            case GameState.AfternoonTransition:
                if (IsServer) _netTransitionTimer.Value = _transitionTimerMax;
                break;
            case GameState.Afternoon:
                if(IsServer) _netAfternoonTimer.Value = _afternoonTimerMax;
                _locationManager.ForceLocation(LocationManager.LocationName.Camp);
                OnStateAfternoon?.Invoke();
                break;
            case GameState.EveningTransition:
                if (IsServer) _netTransitionTimer.Value = _transitionTimerMax;
                break;
            case GameState.Evening:
                if (IsServer) _netEveningTimer.Value = _eveningTimerMax;
                OnStateEvening?.Invoke();
                break;
            case GameState.NightTransition:
                if (IsServer) _netTransitionTimer.Value = _transitionTimerMax;
                break;
            case GameState.Night:
                if (IsServer) _netNightTimer.Value = _nightTimerMax;
                OnStateNight?.Invoke();
                break;
            case GameState.MorningTransition:
                if (IsServer) _netTransitionTimer.Value = _transitionTimerMax;
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
