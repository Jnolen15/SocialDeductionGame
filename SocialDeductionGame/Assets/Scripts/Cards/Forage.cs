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
    [SerializeField] private AnimationCurve _dangerLevelDrawChances;
    [SerializeField] private int _tierTwoHazardThreshold;
    [SerializeField] private int _tierThreeHazardThreshold;

    [SerializeField] private int _maxDanger;
    [SerializeField] private NetworkVariable<int> _netCurrentDanger = new(writePerm: NetworkVariableWritePermission.Server);

    [Header("Refrences")]
    [SerializeField]private ForageUI _forageUI;
    private CardManager _cardManager;
    private HandManager _playerHandMan;
    [SerializeField] private GameObject _forageCanvas;
    [SerializeField] private GameObject _hazardCardPref;

    public delegate void ForageAction(int dangerLevel);
    public static event ForageAction OnDangerIncrement;
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

        if (!IsServer)
            GameManager.OnStateMorning += ResetDangerLevel;
    }

    private void Start()
    {
        _cardManager = GameObject.FindGameObjectWithTag("CardManager").GetComponent<CardManager>();
        _cardDropTable.ValidateTable();
    }

    private void OnDisable()
    {
        _netCurrentDanger.OnValueChanged -= SendDangerChangedEvent;

        if (!IsServer)
            GameManager.OnStateMorning -= ResetDangerLevel;
    }
    #endregion

    // ============== Choose and Deal ==============
    #region Choose and Deal
    public void DealCards()
    {
        Debug.Log(gameObject.name + " Dealing cards");

        // Increase danger with each forage action
        IncrementDanger(1);

        List<GameObject> cardObjList = new();

        GameObject hazardCard = HazardTest();
        if (!hazardCard)// If no hazard drawn
        {
            for(int i = 0; i < 3; i++)
                cardObjList.Add(ChooseCard());
        }
        else// If hazard drawn
        {
            cardObjList.Add(hazardCard);
            for (int i = 0; i < 2; i++)
                cardObjList.Add(ChooseCard());
        }

        _forageUI.DealCardObjects(cardObjList);
    }

    private GameObject HazardTest()
    {
        // Test for hazard
        int dangerLevel = _netCurrentDanger.Value;

        // Get hazard teir
        Hazard.DangerLevel dangerTier = Hazard.DangerLevel.Low;
        if (_tierTwoHazardThreshold < dangerLevel && dangerLevel <= _tierThreeHazardThreshold)
            dangerTier = Hazard.DangerLevel.Medium;
        else if (_tierThreeHazardThreshold < dangerLevel)
            dangerTier = Hazard.DangerLevel.High;

        // Roll Hazard chances
        float hazardChance = _dangerLevelDrawChances.Evaluate(dangerLevel*0.1f);
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
        // Pick and deal random foraged card
        int cardID = _cardDropTable.PickCardDrop();
        Debug.Log("Picked Card " + cardID);

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
    public int GetDangerLevel()
    {
        return _netCurrentDanger.Value;
    }

    private void SendDangerChangedEvent(int prev, int current)
    {
        OnDangerIncrement?.Invoke(current);
    }

    private void ResetDangerLevel()
    {
        SetDangerLevel(0);
    }

    private void IncrementDanger(int dangerInc)
    {
        Debug.Log("Sending Increment Danger Event " + dangerInc);
        ModifyDangerLevelServerRPC(dangerInc, true);
    }

    public void SetDangerLevel(int dangerInc)
    {
        if (!IsServer)
            return;

        Debug.Log("Sending Increment Danger Event " + dangerInc);
        ModifyDangerLevelServerRPC(dangerInc, false);
    }

    [ServerRpc(RequireOwnership = false)]
    private void ModifyDangerLevelServerRPC(int ammount, bool add, ServerRpcParams serverRpcParams = default)
    {
        Debug.Log($"{gameObject.name} had its danger level incremented by {ammount}");

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
}
