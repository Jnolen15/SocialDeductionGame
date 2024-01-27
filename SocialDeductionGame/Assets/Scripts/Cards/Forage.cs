using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Forage : NetworkBehaviour, ICardPicker
{
    // ============== Parameters / Refrences / Variables ==============
    #region P / R / V
    [Header("Card Parameters")]
    [SerializeField] private CardDropTable _cardDropTable = new CardDropTable();
    [SerializeField] private int _uselessCardID;
    [SerializeField] private int _uselessOddsDefault;
    [SerializeField] private int _uselessOddsDebuffModifier;
    [Header("Danger Parameters")]
    [SerializeField] private AnimationCurve _dangerLevelDrawChances;
    [SerializeField] private int _tierTwoHazardThreshold;
    [SerializeField] private int _tierThreeHazardThreshold;
    [SerializeField] private float _dangerIncrementNum;
    [SerializeField] private float _dangerPerPlayerScaleNum;
    [SerializeField] private int _dangerLevelBase;
    [Header("Location")]
    [SerializeField] private LocationManager.LocationName _locationName;

    [Header("Refrences")]
    [SerializeField]private ForageUI _forageUI;
    private CardManager _cardManager;
    private GameObject _playerObj;
    private HandManager _playerHandMan;
    [SerializeField] private GameObject _hazardCardPref;

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
    void OnValidate()
    {
        _cardDropTable.ValidateTable();
    }

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
        }
    }

    private void Awake()
    {
        _cardManager = GameObject.FindGameObjectWithTag("CardManager").GetComponent<CardManager>();
        _cardDropTable.ValidateTable();
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

    // ============== Choose and Deal ==============
    #region Choose and Deal
    public void DealCards()
    {
        if (!_playerObj)
            SetupPlayerConnections();

        if (!PlayerConnectionManager.Instance.GetLocalPlayerLiving())
            return;

        Debug.Log(gameObject.name + " Dealing cards");

        int numToDeal = 3;
        if (_netTotemActive.Value)
            numToDeal--;
        // Add card draw bonus gear
        numToDeal += _playerHandMan.CheckForForageGear(_locationName.ToString());

        List<GameObject> cardObjList = new();

        GameObject hazardCard = HazardTest();
        if (!hazardCard)// If no hazard drawn
        {
            for(int i = 0; i < numToDeal; i++)
                cardObjList.Add(ChooseCard());
        }
        else// If hazard drawn
        {
            for (int i = 0; i < (numToDeal-1); i++)
                cardObjList.Add(ChooseCard());
            cardObjList.Insert(Random.Range(0, cardObjList.Count), hazardCard);
        }

        _forageUI.DealCardObjects(cardObjList);

        // Increase danger with each forage action
        IncrementDanger(_dangerIncrementNum);
    }

    private GameObject HazardTest()
    {
        // Test for hazard
        float dangerLevel = _netCurrentDanger.Value;

        // Get hazard teir
        Hazard.DangerLevel dangerTier = Hazard.DangerLevel.Low;
        if (_tierTwoHazardThreshold < dangerLevel && dangerLevel <= _tierThreeHazardThreshold)
            dangerTier = Hazard.DangerLevel.Medium;
        else if (_tierThreeHazardThreshold < dangerLevel)
            dangerTier = Hazard.DangerLevel.High;

        // Roll Hazard chances
        float hazardChance = _dangerLevelDrawChances.Evaluate(dangerLevel*0.01f);
        Debug.Log($"<color=blue>CLIENT: </color> Player DL: {dangerLevel}, hazard chance: {hazardChance}, hazard level {dangerTier}. Rolling.");
        float rand = (Random.Range(0, 100)*0.01f);

        // Hazard
        if (hazardChance >= rand)
        {
            Debug.Log($"<color=blue>CLIENT: </color> Rolled: {rand}, hazard encountered!");
            return SpawnHazard(dangerTier);
        }
        // No Hazard
        else
        {
            Debug.Log($"<color=blue>CLIENT: </color> Rolled: {rand}, no hazard!");
            return null;
        }
    }

    private GameObject SpawnHazard(Hazard.DangerLevel dangerLevel)
    {
        if (!_playerObj)
            SetupPlayerConnections();

        // Spawn in random hazard
        int randHazardID = CardDatabase.Instance.GetRandHazard(dangerLevel);
        GameObject hazardCard = Instantiate(_hazardCardPref, transform);
        HazardCardVisual hazard = hazardCard.GetComponent<HazardCardVisual>();
        
        hazard.Setup(randHazardID);
        hazard.RunHazard(_playerHandMan);

        return hazardCard;
    }

    private GameObject ChooseCard()
    {
        int cardID;

        // Test for useless card
        int uselessOdds = _uselessOddsDefault;
        if (_netEventDebuffed.Value)
            uselessOdds += _uselessOddsDebuffModifier;
        else if (_netEventBuffed.Value)
            uselessOdds -= _uselessOddsDebuffModifier;
        int rand = (Random.Range(0, 100));
        Debug.Log($"Useless Odds are {uselessOdds}, rolled a {rand}");
        if (uselessOdds >= rand)
        {
            cardID = _uselessCardID;
            Debug.Log("Picked Useless Card " + cardID);
        }
        else
        {
            // Pick and deal random foraged card
            cardID = _cardDropTable.PickCardDrop();
            Debug.Log("Picked Card " + cardID);
        }

        if (!CardDatabase.Instance.VerifyCard(cardID))
        {
            Debug.Log("Card ID could not be verified. Picking new from drop table");
            cardID = _cardDropTable.PickCardDrop();
        }

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
