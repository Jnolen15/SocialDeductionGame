using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using TMPro;

public class PlayerData : NetworkBehaviour
{
    // ================== Refrences ==================
    #region Refrences
    private HandManager _handManager;
    private PlayerCardManager _playerCardManager;
    private PlayerHealth _playerHealth;
    [SerializeField] private PlayerUI _playerUI;

    private LocationManager _locationManager;
    private EventManager _nightEventManger;
    #endregion

    // ================== Variables ==================
    #region Variables
    public NetworkVariable<FixedString32Bytes> _netPlayerName = new(writePerm: NetworkVariableWritePermission.Server);
    [SerializeField] private NetworkVariable<ulong> _netPlayerID = new();
    [SerializeField] private NetworkVariable<LocationManager.LocationName> _netCurrentLocation = new(writePerm: NetworkVariableWritePermission.Owner);
    public enum Team
    {
        Survivors,
        Saboteurs
    }
    private NetworkVariable<Team> _netTeam = new(writePerm: NetworkVariableWritePermission.Server);
    [SerializeField] private int _defaultMP = 2;
    [SerializeField] private NetworkVariable<int> _netMaxMP = new(writePerm: NetworkVariableWritePermission.Server);
    [SerializeField] private NetworkVariable<int> _netCurrentMP = new(writePerm: NetworkVariableWritePermission.Server);
    [SerializeField] private int _maxDanger = 10;
    [SerializeField] private int _morningDangerReductionVal = 3;
    [SerializeField] private NetworkVariable<int> _netCurrentDanger = new(writePerm: NetworkVariableWritePermission.Server);
    #endregion

    // ================== Setup ==================
    #region Setup
    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            LocationManager.OnForceLocationChange += UpdateLocation;
            _netTeam.OnValueChanged += UpdateTeamText;
            _netCurrentMP.OnValueChanged += UpdateMovementPointUI;
            _netCurrentDanger.OnValueChanged += UpdateDangerLevelUI;
            GameManager.OnStateNight += ShowEventChoices;
            GameManager.OnStateMorning += ResetMovementPoints;
            GameManager.OnStateMorning += MorningDangerReduction;
            Forage.OnDangerIncrement += ModifyDangerLevel;

            gameObject.tag = "Player";

            SetPlayerIDServerRpc();
            SetDangerLevel(1);
        } else
        {
            Destroy(_playerUI.gameObject);
            _playerUI = null;
        }

        if (!IsOwner && !IsServer)
            enabled = false;
    }

    private void OnDisable()
    {
        if (!IsOwner) return;

        LocationManager.OnForceLocationChange -= UpdateLocation;
        _netTeam.OnValueChanged -= UpdateTeamText;
        _netCurrentMP.OnValueChanged -= UpdateMovementPointUI;
        _netCurrentDanger.OnValueChanged -= UpdateDangerLevelUI;
        GameManager.OnStateNight -= ShowEventChoices;
        GameManager.OnStateMorning -= ResetMovementPoints;
        GameManager.OnStateMorning -= MorningDangerReduction;
        Forage.OnDangerIncrement -= ModifyDangerLevel;
    }

    private void Start()
    {
        _handManager = gameObject.GetComponent<HandManager>();
        _playerCardManager = gameObject.GetComponent<PlayerCardManager>();
        _playerHealth = gameObject.GetComponent<PlayerHealth>();

        ResetMovementPoints();

        // TODO: NOT HAVE DIRECT REFRENCES, Use singleton or some other method ?
        GameObject gameMan = GameObject.FindGameObjectWithTag("GameManager");
        _locationManager = gameMan.GetComponent<LocationManager>();
        _nightEventManger = gameMan.GetComponent<EventManager>();

        UpdateTeamText(Team.Survivors, _netTeam.Value);
    }
    #endregion

    // ================ Player Name / ID ================
    #region Player Name / ID
    [ServerRpc]
    private void SetPlayerIDServerRpc(ServerRpcParams serverRpcParams = default)
    {
        _netPlayerID.Value = serverRpcParams.Receive.SenderClientId;
    }

    public ulong GetPlayerID()
    {
        return _netPlayerID.Value;
    }

    [ServerRpc(RequireOwnership = false)]
    public void UpdatePlayerNameServerRPC(string pName)
    {
        Debug.Log("<color=yellow>SERVER: </color>Updating player name " + _netPlayerID.Value + " to " + pName);
        _netPlayerName.Value = pName;
    }
    #endregion

    // ================ Teams ================
    #region Teams
    public void SetTeam(Team team)
    {
        _netTeam.Value = team;
    }

    private void UpdateTeamText(Team prev, Team current)
    {
        if(_playerUI != null)
            _playerUI.UpdateTeamText(current);
    }

    // Show night event choices if Saboteur, else show Recap
    private void ShowEventChoices()
    {
        if (_netTeam.Value == Team.Saboteurs)
            _nightEventManger.OpenNightEventPicker();
        else if (_playerHealth.IsLiving())
            _nightEventManger.ShowRecap();
    }

    public Team GetPlayerTeam()
    {
        return _netTeam.Value;
    }
    #endregion

    // ====================== Player Readying ======================
    #region Player Readying
    public void ReadyPlayer()
    {
        PlayerConnectionManager.Instance.ReadyPlayer();
    }
    #endregion

    // ================ Location / Movement ================
    #region Location
    // Called when the player chooses a location on their map
    // Or when location change is forced by game manager / location manager
    // Called by button
    public void ChangeLocation(string locationName)
    {
        // Dont move if already at the same location
        if (_netCurrentLocation.Value.ToString() == locationName)
            return;

        if (GetMovementPoints() > 0)
            SpendMovementPoint();
        else
        {
            Debug.Log("<color=blue>CLIENT: </color>Cannot move, no points!");
            return;
        }

        LocationManager.LocationName newLocation;

        switch (locationName)
        {
            case "Camp":
                newLocation = LocationManager.LocationName.Camp;
                break;
            case "Beach":
                newLocation = LocationManager.LocationName.Beach;
                break;
            case "Forest":
                newLocation = LocationManager.LocationName.Forest;
                break;
            case "Plateau":
                newLocation = LocationManager.LocationName.Plateau;
                break;
            default:
                Debug.LogError("MoveToLocation picked default case, setting camp");
                newLocation = LocationManager.LocationName.Camp;
                break;
        }

        ChangeLocation(newLocation);
    }

    private void UpdateLocation(LocationManager.LocationName location)
    {
        Debug.Log("Updating player location to " + location.ToString());

        _netCurrentLocation.Value = location;
    }

    private void ChangeLocation(LocationManager.LocationName newLocation)
    {
        _locationManager.SetLocation(newLocation);

        UpdateLocation(newLocation);
    }

    // ==== MOVEMENT POINTS ====
    public void ResetMovementPoints()
    {
        if(_netMaxMP.Value == 0)
        {
            Debug.LogError("_netMaxMP == 0!");
            ModifyMovementPointsServerRPC(_defaultMP, false);
        }

        ModifyMovementPointsServerRPC(_netMaxMP.Value, false);
    }

    public void SpendMovementPoint()
    {
        ModifyMovementPointsServerRPC(-1, true);
    }

    public int GetMovementPoints()
    {
        return _netCurrentMP.Value;
    }

    private void UpdateMovementPointUI(int prev, int cur)
    {
        _playerUI.UpdateMovement(prev, cur);
    }

    [ServerRpc(RequireOwnership = false)]
    private void ModifyMovementPointsServerRPC(int ammount, bool add)
    {
        Debug.Log($"{NetworkManager.Singleton.LocalClientId} had its MP modified by {ammount}");

        // temp for calculations
        int tempMP = _netCurrentMP.Value;

        if (add)
            tempMP += ammount;
        else
            tempMP = ammount;

        // Clamp MP within bounds
        if (tempMP < 0)
            tempMP = 0;
        else if (tempMP > _netMaxMP.Value)
            tempMP = _netMaxMP.Value;

        _netCurrentMP.Value = tempMP;
    }

    public void IncrementPlayerMaxMovement(int num)
    {
        IncrementPlayerMaxMovementServerRpc(num);
    }

    [ServerRpc]
    private void IncrementPlayerMaxMovementServerRpc(int num, ServerRpcParams serverRpcParams = default)
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

        _netMaxMP.Value += num;
        ModifyMovementPointsServerRPC(num, true);
        Debug.Log($"<color=yellow>SERVER: </color> Incremented player {clientId}'s move speed by {num}");
    }
    #endregion

    // ================ Danger Level ================
    #region Danger Level
    private void UpdateDangerLevelUI(int prev, int cur)
    {
        _playerUI.UpdateDanger(prev, cur);
    }

    public int GetDangerLevel()
    {
        return _netCurrentDanger.Value;
    }

    public void MorningDangerReduction()
    {
        ModifyDangerLevelServerRPC(-_morningDangerReductionVal, true);
    }

    public void ModifyDangerLevel(int ammount)
    {
        ModifyDangerLevelServerRPC(ammount, true);
    }

    public void SetDangerLevel(int ammount)
    {
        ModifyDangerLevelServerRPC(ammount, false);
    }

    [ServerRpc(RequireOwnership = false)]
    private void ModifyDangerLevelServerRPC(int ammount, bool add, ServerRpcParams serverRpcParams = default)
    {
        Debug.Log($"{NetworkManager.Singleton.LocalClientId} had its danger level incremented by {ammount}");

        // temp for calculations
        int tempDL = _netCurrentDanger.Value;

        if (add)
            tempDL += ammount;
        else
            tempDL = ammount;

        // Clamp HP within bounds
        if (tempDL < 1)
            tempDL = 1;
        else if (tempDL > _maxDanger)
            tempDL = _maxDanger;

        _netCurrentDanger.Value = tempDL;
    }
    #endregion

    // ================ Player Death ================
    #region OnPlayerDeath
    public void OnPlayerDeath()
    {
        PlayerConnectionManager.Instance.RecordPlayerDeath(GetPlayerID());

        _playerCardManager.DiscardHandServerRPC();

        // Deal with ready for this round
        //ReadyPlayer();
        _playerUI.DisableReadyButton();
    }
    #endregion
}
