using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandFullUI : MonoBehaviour
{
    // ================ Refrences ================
    [SerializeField] private GameObject _menu;
    [SerializeField] private Transform _cardZone;
    private CardManager _cardManager;
    private int _heldCardID;
    private Card _heldCardObj;

    // ================ Setup ================
    private void Start()
    {
        GetCardMan();
    }

    private void GetCardMan()
    {
        _cardManager = GameObject.FindGameObjectWithTag("CardManager").GetComponent<CardManager>();
    }

    // ================ Function ================
    public void Open(int cardID)
    {
        Show();

        if (_heldCardObj)
            Destroy(_heldCardObj.gameObject);

        _heldCardID = cardID;
        _heldCardObj = Instantiate(CardDatabase.Instance.GetCard(cardID), _cardZone).GetComponent<Card>();
        _heldCardObj.SetupUI();
    }

    public void TakeCard()
    {
        Hide();

        if (!_cardManager)
            GetCardMan();

        _cardManager.GiveCard(_heldCardID);
    }

    public void Show()
    {
        _menu.SetActive(true);
    }

    public void Hide()
    {
        _menu.SetActive(false);
    }
}
