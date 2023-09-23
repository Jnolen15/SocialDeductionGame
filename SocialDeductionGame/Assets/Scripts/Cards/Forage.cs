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

    [Header("Refrences")]
    private CardManager _cardManager;
    private PlayerData _playerData;
    [SerializeField] private Transform _cardZone;
    [SerializeField] private GameObject _forageMenu;

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
        _cardDropTable.ValidateTable();
    }
    #endregion

    // ============== Functions ==============
    #region Functions
    public bool HazardTest()
    {
        int playerDangerLevel = _playerData.GetDangerLevel();
        float hazardChance = _dangerLevelDrawChances.Evaluate(playerDangerLevel*0.1f);

        Debug.Log($"<color=blue>CLIENT: </color> Player DL: {playerDangerLevel}, hazrd chance: {hazardChance}. Rolling.");

        float rand = (Random.Range(0, 100)*0.01f);

        if (hazardChance >= rand)
        {
            Debug.Log($"<color=blue>CLIENT: </color> Rolled: {rand}, hazard encountered!");
            //TODO: Spawn hazard
            return true;
        }
        else
        {
            Debug.Log($"<color=blue>CLIENT: </color> Rolled: {rand}, no hazard!");
            return false;
        }
    }

    public void DealCards()
    {
        if (_cardManager == null)
            _cardManager = GameObject.FindGameObjectWithTag("CardManager").GetComponent<CardManager>();

        // Test for hazard encounter
        bool encountered = HazardTest();

        if (encountered)
            return;

        // Pick and deal random foraged cards
        Debug.Log(gameObject.name + " Dealing cards");
        for(int i = 0; i < _cardsDelt; i++)
        {
            // Pick card
            int cardID = _cardDropTable.PickCardDrop();

            // Put card on screen
            Card newCard = Instantiate(CardDatabase.GetCard(cardID), _cardZone).GetComponent<Card>();
            newCard.SetupSelectable();
        }

        // Increase danger with each forage action
        IncrementDanger(1);
    }

    public void RedealCards()
    {
        Debug.Log(gameObject.name + " Redealing cards");
        ClearCards();
        DealCards();
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
    #endregion
}
