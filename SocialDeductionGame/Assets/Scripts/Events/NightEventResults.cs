using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class NightEventResults : MonoBehaviour
{
    // ================== Refrences ==================
    [Header("UI Refrences")]
    [SerializeField] private NightEventCardVisual _nightEventCard;
    [SerializeField] private GameObject _eventFailText;
    [SerializeField] private GameObject _eventPassText;
    [SerializeField] private GameObject _eventBonusText;
    [SerializeField] private Transform _contributorsArea;
    [SerializeField] private Transform _cardArea;

    [Header("Prefabs")]
    [SerializeField] private GameObject _playerNamePref;

    // ================== Setup ==================
    public void DisplayResults(int[] cardIDs, ulong[] contributorIDS, int eventID, bool passed, bool bonus)
    {
        ClearBoard();

        // Set up event card
        _nightEventCard.Setup(eventID);

        // Pass / Fail Text
        _eventPassText.SetActive(false);
        _eventFailText.SetActive(false);
        _eventBonusText.SetActive(false);
        if (passed)
            _eventPassText.SetActive(true);
        else
            _eventFailText.SetActive(true);
        if(bonus)
            _eventBonusText.SetActive(true);

        // Contributors list
        foreach (ulong id in contributorIDS)
        {
            TextMeshProUGUI namePlate = Instantiate(_playerNamePref, _contributorsArea).GetComponent<TextMeshProUGUI>();
            namePlate.text = PlayerConnectionManager.Instance.GetPlayerNameByID(id);
        }

        // Cards
        foreach (int cardID in cardIDs)
        {
            Card cardObj = Instantiate(CardDatabase.GetCard(cardID), _cardArea).GetComponent<Card>();
            cardObj.SetupUI();
        }
    }

    private void ClearBoard()
    {
        foreach (Transform child in _cardArea)
        {
            Destroy(child.gameObject);
        }

        foreach (Transform child in _contributorsArea)
        {
            Destroy(child.gameObject);
        }
    }
}
