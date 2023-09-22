using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardPicker : MonoBehaviour
{
    // ============== Parameters / Refrences / Variables ==============
    #region P / R / V
    [Header("Parameters")]
    [SerializeField] private CardDropTable _cardDropTable = new CardDropTable();
    [SerializeField] private int _cardsDelt;

    [Header("Refrences")]
    private CardManager _cardManager;
    [SerializeField] private Transform _cardZone;
    [SerializeField] private GameObject _forageMenu;
    #endregion

    // ============== Setup ==============
    #region Setup
    void OnValidate()
    {
        _cardDropTable.ValidateTable();
    }

    private void OnEnable()
    {
        _cardManager = GameObject.FindGameObjectWithTag("CardManager").GetComponent<CardManager>();
        _cardDropTable.ValidateTable();
    }
    #endregion

    // ============== Functions ==============
    #region Functions
    public void DealCards()
    {
        if(_cardManager == null)
            _cardManager = GameObject.FindGameObjectWithTag("CardManager").GetComponent<CardManager>();

        Debug.Log(gameObject.name + " Dealing cards");
        for(int i = 0; i < _cardsDelt; i++)
        {
            // Pick card
            int cardID = _cardDropTable.PickCardDrop();

            // Put card on screen
            Card newCard = Instantiate(CardDatabase.GetCard(cardID), _cardZone).GetComponent<Card>();
            newCard.SetupSelectable();
        }
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
    #endregion
}
