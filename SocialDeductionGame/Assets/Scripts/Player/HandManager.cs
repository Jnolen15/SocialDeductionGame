using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using TMPro;

public class HandManager : NetworkBehaviour
{
    private ServerSidePlayerData _pData;
    private CardDatabase _cardDB;

    private Transform _cardSlot;
    [SerializeField] private GameObject _cardText;

    [SerializeField] private List<CardData> _playerDeck = new List<CardData>();

    private void Start()
    {
        _pData = gameObject.GetComponent<ServerSidePlayerData>();
        _cardDB = GameObject.FindGameObjectWithTag("cardDB").GetComponent<CardDatabase>();
        _cardSlot = GameObject.FindGameObjectWithTag("cardSlot").transform;
    }

    private void Update()
    {
        if (!IsOwner) return;

        // TEST Draw a card
        if (Input.GetKeyDown(KeyCode.D))
        {
            _pData.DrawCard();
        }

        // TEST Play top card
        if (Input.GetKeyDown(KeyCode.P) && _playerDeck.Count > 0)
        {
            _pData.PlayCard(_playerDeck[0].CardID);
        }
    }

    // ================ Deck Management ================
    #region Deck Management

    public void AddCard(int cardID)
    {
        Debug.Log($"Adding a card {_cardDB.GetCard(cardID).CardName} to client {NetworkManager.Singleton.LocalClientId}");

        CardData newCard = _cardDB.GetCard(cardID);

        _playerDeck.Add(newCard);

        var cardTxt = Instantiate(_cardText, _cardSlot);
        cardTxt.GetComponent<TextMeshProUGUI>().text = newCard.CardName;
    }

    public void RemoveCard(int cardID)
    {
        Debug.Log($"Removing card {_cardDB.GetCard(cardID).CardName} from client {NetworkManager.Singleton.LocalClientId}");

        if (_playerDeck.Contains(_cardDB.GetCard(cardID)))
        {
            _playerDeck.Remove(_cardDB.GetCard(cardID));

            Destroy(_cardSlot.GetChild(0).gameObject);
        }
        else
            Debug.LogError($"{cardID} not found in player's local hand!");
    }

    #endregion
}
