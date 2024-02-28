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
    [SerializeField] private NetworkVariable<int> _netSufferning = new(writePerm: NetworkVariableWritePermission.Server);

    private bool _isSabo;

    public delegate void SufferingValueModified(int ModifiedAmmount, int newTotal, int reasonCode);
    public static event SufferingValueModified OnSufferingModified;

    // ================== Setup ==================
    #region Setup
    public override void OnNetworkSpawn()
    {
        GameManager.OnStateIntro += Setup;

        //if(IsServer)
        //    GameManager.OnStateMorning += DailySuffering;

        InitializeSingleton();
    }

    private void OnDisable()
    {
        GameManager.OnStateIntro -= Setup;

        //if(IsServer)
        //    GameManager.OnStateMorning -= DailySuffering;
    }

    private void Setup()
    {
        if (PlayerConnectionManager.Instance.GetLocalPlayerTeam() == PlayerData.Team.Saboteurs)
        {
            _isSabo = true;
        }
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

    // ================== Suffering ==================
    // Suffering Increment / Decrement
    #region Suffering
    public int GetCurrentSufffering()
    {
        if (!_isSabo)
            return -1;

        return _netSufferning.Value;
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

    // Misc
    #region Misc
    // Earn daily suffering per sabo
    private void DailySuffering()
    {
        if (!IsServer)
            return;

        int daily;
        int teamDiff = PlayerConnectionManager.Instance.GetNumLivingOnTeam(PlayerData.Team.Survivors) - PlayerConnectionManager.Instance.GetNumLivingOnTeam(PlayerData.Team.Saboteurs);

        if (teamDiff <= 1)
            daily = 1;
        else if (teamDiff > 1 && teamDiff <= 3)
            daily = 2;
        else
            daily = 3;

        ModifySuffering(daily, 101, true);
    }
    #endregion
}
