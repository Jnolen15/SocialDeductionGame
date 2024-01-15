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
    void OnEnable()
    {
        GameManager.OnStateNight += Hide;
        GameManager.OnStateGameEnd += Hide;
    }

    private void OnDisable()
    {
        GameManager.OnStateNight -= Hide;
        GameManager.OnStateGameEnd -= Hide;
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    // ================== Function ==================
    public void DisplayResults(int[] goodCardIDs, int[] badCardIDs, ulong[] contributorIDS, int eventID, int playerNum, 
                                bool passed, bool bonus, Vector2 objectivePoints, Vector3 bonusPoints)
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
        foreach (int cardID in goodCardIDs)
        {
            Card cardObj = Instantiate(CardDatabase.Instance.GetCard(cardID), _cardArea).GetComponent<Card>();
            cardObj.SetupUI();
            cardObj.GetComponentInChildren<CardVisual>().ShowOutline(Color.green);
        }

        foreach (int cardID in badCardIDs)
        {
            Card cardObj = Instantiate(CardDatabase.Instance.GetCard(cardID), _cardArea).GetComponent<Card>();
            cardObj.SetupUI();
            cardObj.GetComponentInChildren<CardVisual>().ShowOutline(Color.red);
        }

        // Breakdown
        _primeTitle.text = eventDetails.GetPrimaryResource().Name;
        if(objectivePoints.x < eventDetails.GetRequirements(playerNum).x) // Failed prime resource
        {
            _primeScore.text = objectivePoints.x.ToString();
            _primeScore.color = Color.red;
        }
        else // Passed prime resource
        {
            _primeScore.text = eventDetails.GetRequirements(playerNum).x.ToString();
            _primeScore.color = Color.green;
        }

        _secondTitle.text = eventDetails.GetSecondaryResource().Name;
        if (objectivePoints.y < eventDetails.GetRequirements(playerNum).y) // Failed secondary resource
        {
            _secondScore.text = objectivePoints.y.ToString();
            _secondScore.color = Color.red;
        }
        else // Passed secondary resource
        {
            _secondScore.text = eventDetails.GetRequirements(playerNum).y.ToString();
            _secondScore.color = Color.green;
        }

        _bonusScore.text = bonusPoints.x.ToString();
        if(bonusPoints.x < 0) // Sabotaged
            _bonusScore.color = Color.red;
        else if (bonusPoints.x < 2) // Extra, not enough for bonus
            _bonusScore.color = Color.white;
        else // Bonus
            _bonusScore.color = Color.green;

        _bonusCorrect.text = "+" + bonusPoints.y.ToString() + " Correct";
        _bonusIncorrect.text = "-" + bonusPoints.z.ToString() + " Incorrect";
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
