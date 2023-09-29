using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Forage : MonoBehaviour
{
    // ============== Parameters / Refrences / Variables ==============
    #region P / R / V
    [Header("Parameters")]
    [SerializeField] private CardDropTable _cardDropTable = new CardDropTable();
    [SerializeField] private int _cardsDelt;
    [SerializeField] private AnimationCurve _dangerLevelDrawChances;
    [SerializeField] private int _tierTwoHazardThreshold;
    [SerializeField] private int _tierThreeHazardThreshold;

    [Header("Refrences")]
    private CardManager _cardManager;
    private PlayerData _playerData;
    private HandManager _playerHandMan;
    [SerializeField] private Transform _cardZone;
    [SerializeField] private GameObject _forageMenu;
    //[SerializeField] private GameObject _redealButton;
    [SerializeField] private GameObject _hazardCloseButton;
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

    private void Start()
    {
        _cardManager = GameObject.FindGameObjectWithTag("CardManager").GetComponent<CardManager>();
        _playerData = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerData>();
        _playerHandMan = GameObject.FindGameObjectWithTag("Player").GetComponent<HandManager>();
        _cardDropTable.ValidateTable();
    }
    #endregion

    // ============== Functions ==============
    #region Functions
    public void TestHazardThenDeal()
    {
        if (!HazardTest())
            DealCards();
    }

    public bool HazardTest()
    {
        // Increase danger with each forage action
        IncrementDanger(1);

        // Test for hazard
        int playerDangerLevel = _playerData.GetDangerLevel();

        // Get hazard teir
        Hazard.DangerLevel dangerLevel = Hazard.DangerLevel.Low;
        if (_tierTwoHazardThreshold < playerDangerLevel && playerDangerLevel <= _tierThreeHazardThreshold)
            dangerLevel = Hazard.DangerLevel.Medium;
        else if (_tierThreeHazardThreshold < playerDangerLevel)
            dangerLevel = Hazard.DangerLevel.High;

        // Roll Hazard chances
        float hazardChance = _dangerLevelDrawChances.Evaluate(playerDangerLevel*0.1f);
        Debug.Log($"<color=blue>CLIENT: </color> Player DL: {playerDangerLevel}, hazard chance: {hazardChance}, hazard level {dangerLevel}. Rolling.");
        float rand = (Random.Range(0, 100)*0.01f);

        // Hazard
        if (hazardChance >= rand)
        {
            Debug.Log($"<color=blue>CLIENT: </color> Rolled: {rand}, hazard encountered!");
            SpawnHazard(dangerLevel);
            return true;
        }
        // No Hazard
        else
        {
            Debug.Log($"<color=blue>CLIENT: </color> Rolled: {rand}, no hazard!");
            return false;
        }
    }

    private void SpawnHazard(Hazard.DangerLevel dangerLevel)
    {
        // Spawn in random hazard
        int randHazardID = CardDatabase.Instance.GetRandHazard(dangerLevel);
        HazardCardVisual hazard = Instantiate(_hazardCardPref, _cardZone).GetComponent<HazardCardVisual>();
        hazard.Setup(randHazardID);

        hazard.RunHazard(_playerHandMan);

        OpenHazardUI();
    }

    public void DealCards()
    {
        if (_cardManager == null)
            _cardManager = GameObject.FindGameObjectWithTag("CardManager").GetComponent<CardManager>();

        // Pick and deal random foraged cards
        Debug.Log(gameObject.name + " Dealing cards");
        for(int i = 0; i < _cardsDelt; i++)
        {
            // Pick card
            int cardID = _cardDropTable.PickCardDrop();

            // Put card on screen
            Card newCard = Instantiate(CardDatabase.Instance.GetCard(cardID), _cardZone).GetComponent<Card>();
            newCard.SetupSelectable();
        }
    }

    public void RedealCards()
    {
        Debug.Log(gameObject.name + " Redealing cards");
        ClearCards();
        TestHazardThenDeal();
    }

    public void SelectCard(Card card)
    {
        // Give cards to Card Manager
        _cardManager.GiveCard(card.GetCardID());

        ClearCards();
        CloseForageMenu();
    }

    private void ClearCards()
    {
        // Clear lists
        foreach (Transform child in _cardZone)
        {
            Destroy(child.gameObject);
        }
    }

    private void CloseForageMenu()
    {
        _forageMenu.SetActive(false);
    }

    private void IncrementDanger(int dangerInc)
    {
        Debug.Log("Sending Increment Danger Event " + dangerInc);
        OnDangerIncrement?.Invoke(dangerInc);
    }

    private void OpenHazardUI()
    {
        _hazardCloseButton.SetActive(true);
        //_redealButton.SetActive(false);
    }

    public void CloseHazardAndDeal()
    {
        _hazardCloseButton.SetActive(false);
        //_redealButton.SetActive(true);
        ClearCards();
        DealCards();
    }

    public void Shutdown()
    {
        ClearCards();
        CloseForageMenu();
    }
    #endregion
}
