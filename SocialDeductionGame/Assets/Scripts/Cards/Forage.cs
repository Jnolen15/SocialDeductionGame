using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Forage : NetworkBehaviour, ICardPicker
{
    // ============== Parameters / Refrences / Variables ==============
    #region P / R / V
    [Header("Useless Parameters")]
    [SerializeField] private int _uselessOddsDefault;
    [SerializeField] private int _uselessOddsDebuffModifier;
    [Header("Danger Parameters")]
    [SerializeField] private int _tierTwoHazardThreshold;
    [SerializeField] private int _tierThreeHazardThreshold;
    [SerializeField] private float _dangerIncrementNum;
    [SerializeField] private float _dangerPerPlayerScaleNum;
    [SerializeField] private int _dangerLevelBase;
    [Header("Location")]
    [SerializeField] private LocationManager.LocationName _locationName;

    [Header("Refrences")]
    [SerializeField]private ForageUI _forageUI;
    [SerializeField] private GameObject _hazardCardPref;
    private ForageDeck _forageDeck;
    private CardManager _cardManager;
    private GameObject _playerObj;
    private HandManager _playerHandMan;

    [Header("Variables")]
    [SerializeField] private NetworkVariable<float> _netCurrentDanger = new(writePerm: NetworkVariableWritePermission.Server);
    [SerializeField] private NetworkVariable<bool> _netEventDebuffed = new(writePerm: NetworkVariableWritePermission.Server);
    [SerializeField] private NetworkVariable<bool> _netEventBuffed = new(writePerm: NetworkVariableWritePermission.Server);
    [SerializeField] private NetworkVariable<bool> _netTotemActive = new(writePerm: NetworkVariableWritePermission.Server);
    private bool _locationActive;

    public delegate void LocationForageAction(LocationManager.LocationName locationName);
    public static event LocationForageAction OnLocationBuffEnabled;
    public static event LocationForageAction OnLocationBuffDisabled;
    public static event LocationForageAction OnLocationDebuffEnabled;
    public static event LocationForageAction OnLocationDebuffDisabled;
    #endregion

    // ============== Setup ==============
    #region Setup
    public override void OnNetworkSpawn()
    {
        _netCurrentDanger.OnValueChanged += UpdateDangerUI;
        _netTotemActive.OnValueChanged += UpdateTotemUI;
        _netEventDebuffed.OnValueChanged += SendDebuffedEvent;
        _netEventBuffed.OnValueChanged += SendBuffedEvent;
        GameManager.OnStateIntro += SetupPlayerConnections;

        if (IsServer)
        {
            GameManager.OnStateMorning += ResetDangerLevel;
            GameManager.OnStateEvening += ClearBuffs;
            Totem.OnLocationTotemEnable += SetLocationTotem;
            Totem.OnLocationTotemDisable += ClearLocationTotem;
            CardManager.OnInjectCards += InjectCards;
        }
    }

    private void Awake()
    {
        _forageDeck = this.GetComponent<ForageDeck>();
        _cardManager = GameObject.FindGameObjectWithTag("CardManager").GetComponent<CardManager>();
    }

    private void OnDisable()
    {
        _netCurrentDanger.OnValueChanged -= UpdateDangerUI;
        _netTotemActive.OnValueChanged -= UpdateTotemUI;
        _netEventDebuffed.OnValueChanged -= SendDebuffedEvent;
        _netEventBuffed.OnValueChanged -= SendBuffedEvent;
        GameManager.OnStateIntro -= SetupPlayerConnections;

        if (IsServer)
        {
            GameManager.OnStateMorning -= ResetDangerLevel;
            GameManager.OnStateEvening -= ClearBuffs;
            Totem.OnLocationTotemEnable -= SetLocationTotem;
            Totem.OnLocationTotemDisable -= ClearLocationTotem;
            CardManager.OnInjectCards -= InjectCards;
        }
    }

    // Direct player refrences proboably isnt the best so maybe change in the future
    // But also it works sooo
    private void SetupPlayerConnections()
    {
        _playerObj = GameObject.FindGameObjectWithTag("Player");
        if (_playerObj != null)
        {
            _playerHandMan = _playerObj.GetComponent<HandManager>();
        }
        else
        {
            Debug.LogError("Player Object not found!");
        }
    }
    #endregion

    // ============== Inject Cards ==============
    #region Inject Cards
    private void InjectCards(LocationManager.LocationName locationName, int cardID, int num)
    {
        if (!IsServer)
            return;

        if (_locationName != locationName)
            return;

        //_injectedCards.Add(cardID, num);

        Debug.Log($"{_locationName} had {num} card(s) with ID: {cardID} injected");
    }
    #endregion

    // ============== Choose and Deal ==============
    #region Choose and Deal
    public void DealCards()
    {
        if (!_playerObj)
            SetupPlayerConnections();

        if (!PlayerConnectionManager.Instance.GetLocalPlayerLiving())
            return;

        Debug.Log(gameObject.name + " Dealing cards");

        // Get Num to draw
        int numToDeal = 3;
        if (_netTotemActive.Value)
            numToDeal--;
        numToDeal += _playerHandMan.CheckForForageGear(_locationName.ToString()); // bonus from gear

        // Calc useless odds
        int uselessOdds = _uselessOddsDefault;
        if (_netEventDebuffed.Value)
            uselessOdds += _uselessOddsDebuffModifier;
        else if (_netEventBuffed.Value)
            uselessOdds -= _uselessOddsDebuffModifier;

        // Get hazard teir
        Hazard.DangerLevel dangerTier = Hazard.DangerLevel.Low;
        if (_tierTwoHazardThreshold < _netCurrentDanger.Value && _netCurrentDanger.Value <= _tierThreeHazardThreshold)
            dangerTier = Hazard.DangerLevel.Medium;
        else if (_tierThreeHazardThreshold < _netCurrentDanger.Value)
            dangerTier = Hazard.DangerLevel.High;

        // Draw Cards
        List<int> cardIDList = _forageDeck.DrawCards(numToDeal, uselessOdds, _netTotemActive.Value, _netCurrentDanger.Value, dangerTier);
        List<GameObject> cardObjList = new();

        foreach (int cardID in cardIDList)
        {
            if(cardID < 1000) // hazard
            {
                cardObjList.Add(CreateHazard(cardID));
            }
            else // Normal Card
            {
                cardObjList.Add(CreateCard(cardID));
            }
        }

        _forageUI.DealCardObjects(cardObjList);

        // Increase danger with each forage action
        IncrementDanger(_dangerIncrementNum);
    }

    private GameObject CreateHazard(int hazardID)
    {
        if (!_playerObj)
            SetupPlayerConnections();

        // Spawn in hazard
        GameObject hazardCard = Instantiate(_hazardCardPref, transform);
        HazardCardVisual hazard = hazardCard.GetComponent<HazardCardVisual>();

        hazard.Setup(hazardID);
        hazard.RunHazard(_playerHandMan);

        return hazardCard;
    }

    private GameObject CreateCard(int cardID)
    {
        // Put card on screen
        GameObject cardObj = Instantiate(CardDatabase.Instance.GetCard(cardID), transform);
        cardObj.GetComponent<Card>().SetupSelectable();

        return cardObj;
    }
    #endregion

    // ============== Interface ==============
    #region ICardPicker
    public void PickCard(Card card)
    {
        // Give cards to Card Manager
        _cardManager.GiveCard(card.GetCardID());

        _forageUI.ClearCards();
        _forageUI.HideCards();
    }
    #endregion

    // ============== Other ==============
    #region Other
    public void TakeNone()
    {
        _forageUI.ClearCards();
        _forageUI.HideCards();
    }

    public void Setup()
    {
        Debug.Log("Forage Setup");
        _locationActive = true;

        _forageUI.Show();
    }

    public void Shutdown()
    {
        _locationActive = false;

        _forageUI.ClearCards();
        _forageUI.HideCards();
        _forageUI.Hide();
    }

    public bool GetLocationActive()
    {
        return _locationActive;
    }

    public LocationManager.LocationName GetForageLocation()
    {
        return _locationName;
    }
    #endregion

    // ================ Danger Level ================
    #region Danger Level
    public float GetDangerLevel()
    {
        return _netCurrentDanger.Value;
    }

    private void UpdateDangerUI(float prev, float current)
    {
        _forageUI.UpdateDangerUI(current, _netTotemActive.Value);
    }

    private void UpdateTotemUI(bool prev, bool current)
    {
        _forageUI.UpdateTotemWarning(_netTotemActive.Value);
    }

    private void UpdateDebuffUI(bool eventDebuffed)
    {
        _forageUI.UpdateDebuffWarning(eventDebuffed);
    }

    private void UpdateBuffUI(bool eventBuffed)
    {
        _forageUI.UpdateBuffWarning(eventBuffed);
    }

    private void ResetDangerLevel()
    {
        if (!IsServer)
            return;

        SetDangerLevelServerRPC(_dangerLevelBase);
    }

    public void IncrementDanger(float dangerInc)
    {
        Debug.Log("Incrementing danger by " + dangerInc);
        ModifyDangerLevelServerRPC(dangerInc);
    }

    [ServerRpc(RequireOwnership = false)]
    private void ModifyDangerLevelServerRPC(float ammount, ServerRpcParams serverRpcParams = default)
    {
        Debug.Log($"<color=yellow>SERVER: </color>{gameObject.name} has its danger level increasing");

        // temp for calculations
        float tempDL = _netCurrentDanger.Value;

        // Calculate danger increment ammount (Scales per living player)
        float incValue = (100f / (_dangerPerPlayerScaleNum * PlayerConnectionManager.Instance.GetNumLivingPlayers()));
        Debug.Log($"<color=yellow>SERVER: </color>incValue: {incValue} based on 100/({PlayerConnectionManager.Instance.GetNumLivingPlayers()}*3)");
        tempDL += (ammount * incValue);
        Debug.Log($"<color=yellow>SERVER: </color>Total value danger changed by = {tempDL} as {incValue} scaled by {ammount}");

        // Clamp HP within bounds
        if (tempDL < 1)
            tempDL = 1;
        else if (tempDL > 100)
            tempDL = 100;

        _netCurrentDanger.Value = tempDL;
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetDangerLevelServerRPC(int ammount, ServerRpcParams serverRpcParams = default)
    {
        Debug.Log($"<color=yellow>SERVER: </color>{gameObject.name} had its danger level set to {ammount}");

        // Clamp within bounds
        if (ammount < 1)
            ammount = 1;
        else if (ammount > 100)
            ammount = 100;

        _netCurrentDanger.Value = ammount;
    }
    #endregion

    // ================ Event / Totem interaction ================
    #region Event/Totem Interaction
    public void SetLocationEventDebuff()
    {
        if (!IsServer)
        {
            Debug.LogWarning("SetLocationEventDebuff invoked by client!");
            return;
        }

        Debug.Log(gameObject.name + "Debuffed by an event!");
        _netEventDebuffed.Value = true;
    }

    private void SendDebuffedEvent(bool prev, bool current)
    {
        UpdateDebuffUI(current);

        if (current)
            OnLocationDebuffEnabled?.Invoke(_locationName);
        else
            OnLocationDebuffDisabled?.Invoke(_locationName);
    }

    public void SetLocationEventBuff()
    {
        if (!IsServer)
        {
            Debug.LogWarning("SetLocationEventBuff invoked by client!");
            return;
        }

        Debug.Log(gameObject.name + "Buffed by an event!");
        _netEventBuffed.Value = true;
    }

    private void SendBuffedEvent(bool prev, bool current)
    {
        UpdateBuffUI(current);

        if (current)
            OnLocationBuffEnabled?.Invoke(_locationName);
        else
            OnLocationBuffDisabled?.Invoke(_locationName);
    }

    private void SetLocationTotem(LocationManager.LocationName locationName)
    {
        if (!IsServer)
        {
            Debug.LogWarning("SetLocationTotem invoked by client!");
            return;
        }

        if (_locationName != locationName)
            return;

        Debug.Log(locationName + "Totem enabled!");
        _netTotemActive.Value = true;
    }

    private void ClearLocationTotem(LocationManager.LocationName locationName)
    {
        if (!IsServer)
        {
            Debug.LogWarning("ClearLocationTotem invoked by client!");
            return;
        }

        if (_locationName != locationName)
            return;

        Debug.Log(locationName + "Totem disabled!");
        _netTotemActive.Value = false;
    }

    private void ClearBuffs()
    {
        Debug.Log(_locationName + " clearing buffs");
        _netEventDebuffed.Value = false;
        _netEventBuffed.Value = false;
    }
    #endregion
}
