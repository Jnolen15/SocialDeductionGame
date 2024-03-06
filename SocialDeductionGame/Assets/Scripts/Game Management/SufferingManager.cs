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
    [SerializeField] private NetworkVariable<bool> _netDeathReset = new(writePerm: NetworkVariableWritePermission.Server);
    [SerializeField] private NetworkVariable<bool> _netSacrificeAvailable = new(writePerm: NetworkVariableWritePermission.Server);
    [SerializeField] private ShrineLevels _selectedShrineLevels = new();
    private bool _isSabo;

    public delegate void SufferingValueModified(int ModifiedAmmount, int newTotal, int reasonCode);
    public static event SufferingValueModified OnSufferingModified;

    public delegate void ShrineSetup(int maxLevel, int[] numSuffering);
    public static event ShrineSetup OnShrineSetup;
    public delegate void ShrineLevelUp(int newLevel, int numSuffering, bool deathReset);
    public static event ShrineLevelUp OnShrineLevelUp;

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
            PlayerConnectionManager.OnPlayerDied += OnPlayerDeath;
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
            PlayerConnectionManager.OnPlayerDied -= OnPlayerDeath;
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


        ShrineSetupClientRpc(selectedLevel.LevelSuffering.Count, selectedLevel.LevelSuffering.ToArray());

        Debug.Log($"<color=yellow>SERVER: </color> Survivors: {numSurvivors}, Sabos: {numSaboteurs}, Difference: {teamDiff}. Max shrine level: {_netShrineMaxLevel.Value}");
    }

    [ClientRpc]
    private void ShrineSetupClientRpc(int maxLevel, int[] numSuffering)
    {
        OnShrineSetup?.Invoke(maxLevel, numSuffering);
    }

    private void LevelUpShrine()
    {
        if (!IsServer)
            return;

        bool deathReset = _netDeathReset.Value;
        // Reset level if player died during the day
        if (_netDeathReset.Value)
        {
            ResetShrineLevel();
        }
        // Do sacrifice
        else if (_netSacrificeAvailable.Value)
        {
            DoSacrifice();
            deathReset = true;
        }
        // Level up
        else
        {
            _netShrineLevel.Value += 1;

            if (_netShrineLevel.Value >= _netShrineMaxLevel.Value)
            {
                _netShrineLevel.Value = _netShrineMaxLevel.Value;
                _netSacrificeAvailable.Value = true;
            }
        }

        LevelUpShrineClientRpc(_netShrineLevel.Value, _selectedShrineLevels.LevelSuffering[_netShrineLevel.Value - 1], deathReset);

        Debug.Log("<color=yellow>SERVER: </color>Shrine level up, now " + _netShrineLevel.Value);
    }

    private void ResetShrineLevel()
    {
        _netDeathReset.Value = false;
        _netSacrificeAvailable.Value = false;
        _netShrineLevel.Value = 1;

        Debug.Log("<color=yellow>SERVER: </color>Player death, Shrine level reset! " + _netShrineLevel.Value);
    }

    private void DoSacrifice()
    {
        _netSacrificeAvailable.Value = false;

        // Get random player to execute
        List<ulong> livingSurvivors = new();
        foreach (ulong pID in PlayerConnectionManager.Instance.GetLivingPlayerIDs())
        {
            if (PlayerConnectionManager.Instance.GetPlayerTeamByID(pID) == PlayerData.Team.Survivors)
                livingSurvivors.Add(pID);
        }

        if (livingSurvivors.Count <= 0)
            return;

        int rand = Random.Range(0, livingSurvivors.Count);
        GameObject playerToExecute = PlayerConnectionManager.Instance.GetPlayerObjectByID(livingSurvivors[rand]);
        playerToExecute.GetComponent<PlayerHealth>().ModifyHealth(-99, "Exile");

        ResetShrineLevel();
    }

    [ClientRpc]
    private void LevelUpShrineClientRpc(int newLevel, int numSuffering, bool deathReset)
    {
        OnShrineLevelUp?.Invoke(newLevel, numSuffering, deathReset);
    }

    private void OnPlayerDeath()
    {
        if (!IsServer)
            return;

        _netDeathReset.Value = true;
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

        // Set death reset to false each morning
        _netDeathReset.Value = false;
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
