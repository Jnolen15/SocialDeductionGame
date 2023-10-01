using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class NightEventRecapUI : MonoBehaviour
{
    // ================== Refrences ==================
    [SerializeField] private NightEventCardVisual _eventCard;
    [SerializeField] private TextMeshProUGUI _resultText;
    [SerializeField] private TextMeshProUGUI _consequencesText;

    // ================== Setup ==================
    void OnEnable()
    {
        GameManager.OnStateMorning += CloseRecap;
    }

    private void OnDisable()
    {
        GameManager.OnStateMorning -= CloseRecap;
    }

    public void Setup(int eventID, int playerNum, bool passed, bool bonus)
    {
        _eventCard.Setup(eventID, playerNum);

        NightEvent nEvent = CardDatabase.Instance.GetEvent(eventID);
        if (passed)
        {
            if (bonus)
            {
                _resultText.text = "Passed with bonus!";
                _resultText.color = Color.green;
                _consequencesText.text = "Earned " + nEvent.GetEventBonuses();
            }
            else
            {
                _resultText.text = "Passed";
                _resultText.color = Color.green;
                _consequencesText.text = "No consequences";
            }
        } else
        {
            _resultText.text = "Failed";
            _resultText.color = Color.red;
            _consequencesText.text = nEvent.GetEventConsequences();
        }
    }

    public void CloseRecap()
    {
        gameObject.SetActive(false);
    }
}
