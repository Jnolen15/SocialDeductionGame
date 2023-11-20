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
    [Header("Breakdown")]
    [SerializeField] private TextMeshProUGUI _primeScore;
    [SerializeField] private TextMeshProUGUI _primeTitle;
    [SerializeField] private TextMeshProUGUI _secondScore;
    [SerializeField] private TextMeshProUGUI _secondTitle;
    [SerializeField] private TextMeshProUGUI _bonusScore;
    [SerializeField] private TextMeshProUGUI _bonusCorrect;
    [SerializeField] private TextMeshProUGUI _bonusIncorrect;

    [Header("Prefabs")]
    [SerializeField] private GameObject _playerNamePref;

    // ================== Setup ==================
    public void DisplayResults(int[] cardIDs, ulong[] contributorIDS, int eventID, int playerNum, bool passed, bool bonus, Vector3 scores)
    {
        ClearBoard();

        NightEvent eventDetails = CardDatabase.Instance.GetEvent(eventID);

        // Set up event card
        _nightEventCard.Setup(eventID, playerNum);

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
        //SortCards(int[] cardIDs)
        foreach (int cardID in cardIDs)
        {
            Card cardObj = Instantiate(CardDatabase.Instance.GetCard(cardID), _cardArea).GetComponent<Card>();
            cardObj.SetupUI();
        }

        int extraPrimary = 0;
        int extraSecondary = 0;

        // Breakdown
        _primeTitle.text = eventDetails.GetPrimaryResource().Name;
        if(scores.x < eventDetails.GetRequirements(playerNum).x) // Failed prime resource
        {
            _primeScore.text = scores.x.ToString();
            _primeScore.color = Color.red;
        }
        else // Passed prime resource
        {
            extraPrimary = (int)(scores.x - eventDetails.GetRequirements(playerNum).x);
            _primeScore.text = eventDetails.GetRequirements(playerNum).x.ToString();
            _primeScore.color = Color.green;
        }

        _secondTitle.text = eventDetails.GetSecondaryResource().Name;
        if (scores.y < eventDetails.GetRequirements(playerNum).y) // Failed prime resource
        {
            _secondScore.text = scores.y.ToString();
            _secondScore.color = Color.red;
        }
        else // Passed prime resource
        {
            extraSecondary = (int)(scores.y - eventDetails.GetRequirements(playerNum).y);
            _secondScore.text = eventDetails.GetRequirements(playerNum).y.ToString();
            _secondScore.color = Color.green;
        }

        _bonusScore.text = scores.z.ToString();
        if(scores.z < 0) // Sabotaged
            _bonusScore.color = Color.red;
        else if (scores.z < 2) // Extra, not enough for bonus
            _bonusScore.color = Color.white;
        else // Bonus
            _bonusScore.color = Color.green;

        int bonusCorrectNum = (extraPrimary + extraSecondary);
        int bonusIncorrectNum = (int)Mathf.Abs(scores.z - bonusCorrectNum);
        _bonusCorrect.text = "+" + bonusCorrectNum.ToString() + " Correct";
        _bonusIncorrect.text = "-" + bonusIncorrectNum.ToString() + " Incorrect";
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
