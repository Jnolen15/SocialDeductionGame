using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class NightEventRecapUI : MonoBehaviour
{
    // ================== Refrences ==================
    [Header("Survivor Reacp")]
    [SerializeField] private GameObject _survivorRecap;
    [SerializeField] private NightEventCardVisual _eventCard;
    [SerializeField] private TextMeshProUGUI _resultText;
    [SerializeField] private TextMeshProUGUI _consequencesText;

    [Header("Sabotuer Reacp")]
    [SerializeField] private GameObject _saborRecap;
    [SerializeField] private Transform _RecapZone;

    [Header("Night Recap Objs")]
    [SerializeField] private GameObject _hungerDrain;
    [SerializeField] private GameObject _starvation;
    [SerializeField] private GameObject _death;
    [SerializeField] private GameObject _talismanNourishment;

    private GameObject _recapObject;

    // ================== Setup ==================
    #region Setup
    void OnEnable()
    {
        GameManager.OnStateMorning += CloseRecap;
        PlayerHealth.OnHungerDrain += ShowHungerDrain;
        PlayerHealth.OnStarvation += ShowStarvation;
        PlayerHealth.OnDeath += ShowDeath;
        TalismanNourishmentGear.OnGiveMeal += ShowTalismanNourishment;
    }

    private void Start()
    {
        // By deafult
        _recapObject = _survivorRecap;
    }

    private void OnDisable()
    {
        GameManager.OnStateMorning -= CloseRecap;
        PlayerHealth.OnHungerDrain -= ShowHungerDrain;
        PlayerHealth.OnStarvation -= ShowStarvation;
        PlayerHealth.OnDeath -= ShowDeath;
        TalismanNourishmentGear.OnGiveMeal -= ShowTalismanNourishment;
    }

    public void Setup(PlayerData.Team prev, PlayerData.Team current)
    {
        Debug.Log("Setting up night event recap UI for team " + current);

        if (current == PlayerData.Team.Survivors)
            _recapObject = _survivorRecap;
        else
        {
            _recapObject = _saborRecap;

            // Move recap objects to survivor recap
            _hungerDrain.transform.SetParent(_RecapZone);
            _starvation.transform.SetParent(_RecapZone);
            _death.transform.SetParent(_RecapZone);
            _talismanNourishment.transform.SetParent(_RecapZone);
        }
    }
    #endregion

    // ================== Function ==================
    #region Function
    public void OpenRecap()
    {
        _recapObject.SetActive(true);
    }

    public void CloseRecap()
    {
        _hungerDrain.SetActive(false);
        _starvation.SetActive(false);
        _death.SetActive(false);
        _talismanNourishment.SetActive(false);

        _recapObject.SetActive(false);
    }

    public void UpdateNightEvent(int eventID, int playerNum, bool passed, bool bonus)
    {
        _eventCard.Setup(eventID, playerNum);

        NightEvent nEvent = CardDatabase.Instance.GetEvent(eventID);
        if (passed)
        {
            if (bonus)
            {
                _resultText.text = "Passed with Bonus!";
                _resultText.color = Color.green;
                _consequencesText.text = "Earned " + nEvent.GetEventBonuses();
            }
            else
            {
                _resultText.text = "Passed.";
                _resultText.color = Color.green;
                _consequencesText.text = "No consequences";
            }
        } else
        {
            _resultText.text = "Failed.";
            _resultText.color = Color.red;
            _consequencesText.text = nEvent.GetEventConsequences();
        }
    }

    private void ShowHungerDrain()
    {
        _hungerDrain.SetActive(true);
    }

    private void ShowStarvation()
    {
        _starvation.SetActive(true);
    }

    private void ShowDeath()
    {
        if (GameManager.Instance.GetCurrentGameState() != GameManager.GameState.Night)
            return;

        _death.SetActive(true);
    }

    private void ShowTalismanNourishment()
    {
        _talismanNourishment.SetActive(true);
    }
    #endregion
}
