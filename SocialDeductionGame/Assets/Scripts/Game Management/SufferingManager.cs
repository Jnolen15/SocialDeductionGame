using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class SufferingManager : NetworkBehaviour
{
    // ============== Singleton pattern ==============
    #region Singleton
    public static SufferingManager Instance { get; private set; }
    private void InitializeSingleton()
    {
        if (Instance != null && Instance != this)
            Destroy(gameObject);
        else
            Instance = this;
    }
    #endregion

    // ================== Refrences / Variables ==================
    [Header("Suffering")]
    [SerializeField] private NetworkVariable<int> _netShrineLevel = new(writePerm: NetworkVariableWritePermission.Server);
    [SerializeField] private NetworkVariable<int> _netShrineMaxLevel = new(writePerm: NetworkVariableWritePermission.Server);
    [SerializeField] private NetworkVariable<int> _netSufferning = new(writePerm: NetworkVariableWritePermission.Server);
    [SerializeField] private ShrineLevels _selectedShrineLevels = new();

    private bool _isSabo;

    public delegate void SufferingValueModified(int ModifiedAmmount, int newTotal, int reasonCode);
    public static event SufferingValueModified OnSufferingModified;

    [System.Serializable]
    public class ShrineLevels
    {
        public int TeamDifference = new();
        public List<int> LevelSuffering = new();
    }
    [SerializeField] private List<ShrineLevels> _teamDifferenceLevelList = new();


    // ================== Setup ==================
    #region Setup
    public override void OnNetworkSpawn()
    {
        GameManager.OnStateIntro += Setup;

        if (IsServer)
        {
            _netShrineLevel.Value = 1;

            GameManager.OnStateMorning += DailySuffering;
            GameManager.OnStateMidnight += LevelUpShrine;
            PlayerConnectionManager.OnPlayerDied += ResetShrineLevel;
        }

        InitializeSingleton();
    }

    private void OnDisable()
    {
        GameManager.OnStateIntro -= Setup;

        if (IsServer)
        {
            GameManager.OnStateMorning -= DailySuffering;
            GameManager.OnStateMidnight -= LevelUpShrine;
            PlayerConnectionManager.OnPlayerDied -= ResetShrineLevel;
        }
    }

    private void Setup()
    {
        if (PlayerConnectionManager.Instance.GetLocalPlayerTeam() == PlayerData.Team.Saboteurs)
        {
            _isSabo = true;
        }

        if (IsServer)
            CalculateMaxShrineLevel();
    }
    #endregion

    // FOR TESTING
    private void Update()
    {
        if (!LogViewer.Instance.GetDoCheats())
            return;

        if (Input.GetKeyDown(KeyCode.Comma))
        {
            ModifySuffering(1, Random.Range(101, 105), true);
        }

        if (Input.GetKeyDown(KeyCode.Period))
        {
            ModifySuffering(-1, Random.Range(201, 205), true);
        }
    }

    // ================== Helpers ==================
    #region Helpers
    public int GetCurrentSufffering()
    {
        if (!_isSabo)
            return -1;

        return _netSufferning.Value;
    }
    #endregion

    // ================== Shrine ==================
    #region Shrine
    private void CalculateMaxShrineLevel()
    {
        int numSurvivors = PlayerConnectionManager.Instance.GetNumLivingOnTeam(PlayerData.Team.Survivors);
        int numSaboteurs = PlayerConnectionManager.Instance.GetNumLivingOnTeam(PlayerData.Team.Saboteurs);
        int teamDiff = numSurvivors - numSaboteurs;

        ShrineLevels selectedLevel = _teamDifferenceLevelList[0];
        foreach (ShrineLevels shrinelevels in _teamDifferenceLevelList)
        {
            if (teamDiff >= shrinelevels.TeamDifference)
                selectedLevel = shrinelevels;
        }

        _selectedShrineLevels = selectedLevel;
        _netShrineMaxLevel.Value = selectedLevel.LevelSuffering.Count;

        Debug.Log($"<color=yellow>SERVER: </color> Survivors: {numSurvivors}, Sabos: {numSaboteurs}, Difference: {teamDiff}. Max shrine level: {_netShrineMaxLevel.Value}");
    }

    private void LevelUpShrine()
    {
        if (!IsServer)
            return;

        _netShrineLevel.Value += 1;

        if (_netShrineLevel.Value >= _netShrineMaxLevel.Value)
            _netShrineLevel.Value = _netShrineMaxLevel.Value;

        Debug.Log("<color=yellow>SERVER: </color>Shrine level up, now " + _netShrineLevel.Value);
    }

    private void ResetShrineLevel()
    {
        if (!IsServer)
            return;

        _netShrineLevel.Value = 1;

        Debug.Log("<color=yellow>SERVER: </color>Player death, Shrine level reset! " + _netShrineLevel.Value);
    }
    #endregion

    // ================== Suffering ==================
    #region Suffering Increment / Decrement
    private void DailySuffering()
    {
        if (!IsServer)
            return;

        int dailySuffering = _selectedShrineLevels.LevelSuffering[_netShrineLevel.Value - 1];

        ModifySufferingServerRPC(dailySuffering, 101, false);
    }

    public void ModifySuffering(int ammount, int reasonCode, bool ServerOverride)
    {
        if (ServerOverride && IsServer)
            Debug.Log("<color=yellow>SERVER: </color> Modify Suffering Server Overide");
        else if (!_isSabo)
            return;

        ModifySufferingServerRPC(ammount, reasonCode, true);
    }

    [ServerRpc(RequireOwnership = false)]
    private void ModifySufferingServerRPC(int ammount, int reasonCode, bool add, ServerRpcParams serverRpcParams = default)
    {
        Debug.Log($"Suffering incremented by {ammount}");

        // temp for calculations
        int tempSuffering = _netSufferning.Value;

        if (add)
            tempSuffering += ammount;
        else
            tempSuffering = ammount;

        // Clamp Suffering within bounds
        if (tempSuffering < 0)
            tempSuffering = 0;
        else if (tempSuffering > 9)
            tempSuffering = 9;

        _netSufferning.Value = tempSuffering;

        UpdateSufferingUIClientRpc(ammount, _netSufferning.Value, reasonCode);
    }

    [ClientRpc]
    private void UpdateSufferingUIClientRpc(int changedVal, int newTotal, int reasonCode)
    {
        if (!_isSabo)
            return;

        OnSufferingModified?.Invoke(changedVal, newTotal, reasonCode);
    }
    #endregion
}
