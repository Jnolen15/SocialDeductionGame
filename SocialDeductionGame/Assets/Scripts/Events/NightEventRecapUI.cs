using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class NightEventRecapUI : MonoBehaviour
{
    // ================== Refrences ==================
    [Header("Text Colors")]
    [SerializeField] private Color _neutralColor;
    [SerializeField] private Color _goodColor;
    [SerializeField] private Color _badColor;

    [Header("Survivor Reacp")]
    [SerializeField] private GameObject _survivorRecap;
    [SerializeField] private Transform _survivorRecapZone;
    [SerializeField] private NightEventCardVisual _eventCard;
    [SerializeField] private TextMeshProUGUI _resultText;
    [SerializeField] private TextMeshProUGUI _consequencesText;

    [Header("Sabotuer Reacp")]
    [SerializeField] private GameObject _saborRecap;
    [SerializeField] private Transform _saboRecapZone;

    [Header("Night Recap Objs")]
    [SerializeField] private GameObject _genericRecapMessage;
    [SerializeField] private GameObject _hungerDrain;
    [SerializeField] private GameObject _starvation;
    [SerializeField] private GameObject _death;

    private GameObject _recapObject;
    private PlayerData.Team _localTeam;
    private List<GameObject> _extraRecapObjects = new();

    // ================== Setup ==================
    #region Setup
    void OnEnable()
    {
        GameManager.OnStateMorning += CloseRecap;
        PlayerHealth.OnHungerDrain += ShowHungerDrain;
        PlayerHealth.OnStarvation += ShowStarvation;
        PlayerHealth.OnDeath += ShowDeath;
        TalismanGear.OnGiveCard += ShowTalisman;
        Totem.OnLocationTotemEnable += ShowTotem;
        LTOSpawner.OnLTOSpawned += ShowLTOSpawn;
        LTOSpawner.OnLTODespawned += ShowLTODespawn;
    }

    private void OnDisable()
    {
        GameManager.OnStateMorning -= CloseRecap;
        PlayerHealth.OnHungerDrain -= ShowHungerDrain;
        PlayerHealth.OnStarvation -= ShowStarvation;
        PlayerHealth.OnDeath -= ShowDeath;
        TalismanGear.OnGiveCard -= ShowTalisman;
        Totem.OnLocationTotemEnable -= ShowTotem;
        LTOSpawner.OnLTOSpawned -= ShowLTOSpawn;
        LTOSpawner.OnLTODespawned -= ShowLTODespawn;
    }

    public void Setup(PlayerData.Team prev, PlayerData.Team current)
    {
        Debug.Log("Setting up night event recap UI for team " + current);

        if (current == PlayerData.Team.Survivors)
        {
            _localTeam = PlayerData.Team.Survivors;
            _recapObject = _survivorRecap;
        }
        else
        {
            _localTeam = PlayerData.Team.Saboteurs;
            _recapObject = _saborRecap;

            // Move recap objects to survivor recap
            _hungerDrain.transform.SetParent(_saboRecapZone);
            _starvation.transform.SetParent(_saboRecapZone);
            _death.transform.SetParent(_saboRecapZone);
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
        
        for(int i = _extraRecapObjects.Count-1; i >= 0; i--)
        {
            Destroy(_extraRecapObjects[i]);
        }
        _extraRecapObjects.Clear();

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
                _resultText.color = _goodColor;
                _consequencesText.text = "Earned " + nEvent.GetEventBonuses();
            }
            else
            {
                _resultText.text = "Passed.";
                _resultText.color = _goodColor;
                _consequencesText.text = "No consequences";
            }
        } else
        {
            _resultText.text = "Failed.";
            _resultText.color = _badColor;
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

    private void ShowTalisman(string message)
    {
        Debug.Log("Showing Talisman message: " + message);

        ShowCustomEventMessage(message, _goodColor);
    }
    
    private void ShowTotem(LocationManager.LocationName location)
    {
        string message = "A Saboteur activated the totem at the " + location.ToString();

        ShowCustomEventMessage(message, _badColor);
    }

    private void ShowLTOSpawn(LocationManager.LocationName location)
    {
        string message = "Something has appeared at the " + location.ToString();

        ShowCustomEventMessage(message, _neutralColor);
    }

    private void ShowLTODespawn(LocationManager.LocationName location)
    {
        string message = $"The mysterious object at the {location} has disappeared";

        ShowCustomEventMessage(message, _neutralColor);
    }

    private void ShowCustomEventMessage(string msg, Color color)
    {
        GameObject recapMessage = Instantiate(_genericRecapMessage, _survivorRecapZone);
        recapMessage.GetComponentInChildren<TextMeshProUGUI>().text = msg;
        recapMessage.GetComponentInChildren<TextMeshProUGUI>().color = color;

        _extraRecapObjects.Add(recapMessage);

        if (_localTeam == PlayerData.Team.Saboteurs)
            recapMessage.transform.SetParent(_saboRecapZone);
    }
    #endregion
}
