using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Forage : NetworkBehaviour
{
    // ============== Parameters / Refrences / Variables ==============
    #region P / R / V
    [Header("Parameters")]
    [SerializeField] private CardDropTable _cardDropTable = new CardDropTable();
    [SerializeField] private int _uselessCardID;
    [SerializeField] private int _uselessOddsDefault;
    [SerializeField] private int _uselessOddsDebuffModifier;
    [SerializeField] private AnimationCurve _dangerLevelDrawChances;
    [SerializeField] private int _tierTwoHazardThreshold;
    [SerializeField] private int _tierThreeHazardThreshold;
    [SerializeField] private LocationManager.LocationName _locationName;

    [Header("Refrences")]
    [SerializeField]private ForageUI _forageUI;
    private CardManager _cardManager;
    private HandManager _playerHandMan;
    [SerializeField] private GameObject _forageCanvas;
    [SerializeField] private GameObject _hazardCardPref;

    [Header("Variables")]
    [SerializeField] private NetworkVariable<float> _netCurrentDanger = new(writePerm: NetworkVariableWritePermission.Server);
    [SerializeField] private NetworkVariable<bool> _eventDebuffed = new(writePerm: NetworkVariableWritePermission.Server);
    [SerializeField] private NetworkVariable<bool> _eventBuffed = new(writePerm: NetworkVariableWritePermission.Server);
    [SerializeField] private NetworkVariable<bool> _totemActive = new(writePerm: NetworkVariableWritePermission.Server);

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
        _netCurrentDanger.OnValueChanged += SendDangerChangedEvent;

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
        //_playerHandMan = GameObject.FindGameObjectWithTag("Player").GetComponent<HandManager>();
        _cardDropTable.ValidateTable();
    }

    private void OnDisable()
    {
        _netCurrentDanger.OnValueChanged -= SendDangerChangedEvent;

        if (IsServer)
        {
            GameManager.OnStateMorning -= ResetDangerLevel;
            GameManager.OnStateEvening -= ClearBuffs;
            Totem.OnLocationTotemEnable -= SetLocationTotem;
            Totem.OnLocationTotemDisable -= ClearLocationTotem;
        }
    }
    #endregion

    // ============== Choose and Deal ==============
    #region Choose and Deal
    public void DealCards()
    {
        if (!_playerHandMan)
            _playerHandMan = GameObject.FindGameObjectWithTag("Player").GetComponent<HandManager>();

        Debug.Log(gameObject.name + " Dealing cards");

        // Increase danger with each forage action
        IncrementDanger(1);

        int numToDeal = 3;
        if (_playerHandMan.CheckForForageGear(_locationName.ToString()))
            numToDeal++;

        List<GameObject> cardObjList = new();

        GameObject hazardCard = HazardTest();
        if (!hazardCard)// If no hazard drawn
        {
            for(int i = 0; i < numToDeal; i++)
                cardObjList.Add(ChooseCard());
        }
        else// If hazard drawn
        {
            cardObjList.Add(hazardCard);
            for (int i = 0; i < (numToDeal-1); i++)
                cardObjList.Add(ChooseCard());
        }

        _forageUI.DealCardObjects(cardObjList);
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
        if(!_playerHandMan)
            _playerHandMan = GameObject.FindGameObjectWithTag("Player").GetComponent<HandManager>();

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
        if (_eventDebuffed.Value)
            uselessOdds += _uselessOddsDebuffModifier;
        else if (_eventBuffed.Value)
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

    // ============== Other ==============
    #region Other
    public void SelectCard(Card card)
    {
        // Give cards to Card Manager
        _cardManager.GiveCard(card.GetCardID());

        _forageUI.ClearCards();
        _forageUI.CloseForageMenu();
    }

    public void Setup()
    {
        Debug.Log("Forage Setup");
        _forageCanvas.SetActive(true);
    }

    public void Shutdown()
    {
        _forageUI.ClearCards();
        _forageUI.CloseForageMenu();
        _forageCanvas.SetActive(false);
    }
    #endregion

    // ================ Danger Level ================
    #region Danger Level
    public float GetDangerLevel()
    {
        return _netCurrentDanger.Value;
    }

    private void SendDangerChangedEvent(float prev, float current)
    {
        _forageUI.UpdateDangerUI(current);
    }

    private void ResetDangerLevel()
    {
        if (!IsServer)
            return;

        SetDangerLevelServerRPC(0);
    }

    public void IncrementDanger(float dangerInc)
    {
        if (_totemActive.Value)
            dangerInc = (dangerInc * 1.5f);

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
        float incValue = (100f / (3f * PlayerConnectionManager.Instance.GetNumLivingPlayers()));
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

        // Clamp HP within bounds
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
        _eventDebuffed.Value = true;
        OnLocationDebuffEnabled?.Invoke(_locationName);
    }
    
    public void SetLocationEventBuff()
    {
        if (!IsServer)
        {
            Debug.LogWarning("SetLocationEventBuff invoked by client!");
            return;
        }

        Debug.Log(gameObject.name + "Buffed by an event!");
        _eventBuffed.Value = true;
        OnLocationBuffEnabled?.Invoke(_locationName);
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
        _totemActive.Value = true;
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
        _totemActive.Value = false;
    }

    private void ClearBuffs()
    {
        Debug.Log(gameObject.name + " clearing buffs");
        _eventDebuffed.Value = false;
        _eventBuffed.Value = false;
        OnLocationDebuffDisabled?.Invoke(_locationName);
        OnLocationBuffDisabled?.Invoke(_locationName);
    }
    #endregion
}
