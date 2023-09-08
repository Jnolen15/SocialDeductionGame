using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class PlayerUI : MonoBehaviour
{
    private PlayerData _playerData;
    private PlayerHealth _playerHealth;

    [Header("UI Refrences")]
    [SerializeField] private GameObject _readyButton;
    [SerializeField] private GameObject _readyIndicator;
    [SerializeField] private GameObject _islandMap;
    [SerializeField] private TextMeshProUGUI _locationText;
    [SerializeField] private TextMeshProUGUI _healthText;
    [SerializeField] private TextMeshProUGUI _hungerText;
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private GameObject _deathMessage;
    [SerializeField] private GameObject _playerNameSubmission;
    [SerializeField] private Image _healthFlashSprite;
    [SerializeField] private Image _hungerFlashSprite;

    [Header("Exile Vote UI Refrences")]
    [SerializeField] private GameObject _votePrefab;
    [SerializeField] private Transform _voteArea;
    [SerializeField] private GameObject _exileUI;
    [SerializeField] private GameObject _closeUIButton;

    private bool hasVoted;

    // ================== Setup ==================
    #region Setup
    public void OnEnable()
    {
        _playerData = this.GetComponentInParent<PlayerData>();
        _playerHealth = this.GetComponentInParent<PlayerHealth>();

        GameManager.OnStateChange += EnableReadyButton;
        PlayerConnectionManager.OnPlayerReady += Ready;
        PlayerConnectionManager.OnPlayerUnready += Unready;
        GameManager.OnStateIntro += EnablePlayerNaming;
        GameManager.OnStateMorning += DisablePlayerNaming;
        GameManager.OnStateForage += ToggleMap;
        GameManager.OnStateNight += CloseExileVote;
        PlayerHealth.OnHealthModified += UpdateHealth;
        PlayerHealth.OnHungerModified += UpdateHunger;
        PlayerHealth.OnDeath += DisplayDeathMessage;
    }

    private void OnDisable()
    {
        GameManager.OnStateChange -= EnableReadyButton;
        PlayerConnectionManager.OnPlayerReady -= Ready;
        PlayerConnectionManager.OnPlayerUnready -= Unready;
        GameManager.OnStateIntro -= EnablePlayerNaming;
        GameManager.OnStateMorning -= DisablePlayerNaming;
        GameManager.OnStateForage -= ToggleMap;
        GameManager.OnStateNight -= CloseExileVote;
        PlayerHealth.OnHealthModified -= UpdateHealth;
        PlayerHealth.OnHungerModified -= UpdateHunger;
        PlayerHealth.OnDeath -= DisplayDeathMessage;
    }
    #endregion

    // ================== Misc UI ==================
    #region Misc UI
    private void EnablePlayerNaming()
    {
        _playerNameSubmission.SetActive(true);
    }

    private void DisablePlayerNaming()
    {
        _playerNameSubmission.SetActive(false);
    }

    public void SetPlayerName(string name)
    {
        _playerData.SetPlayerName(name);
    }

    public void UpdatePlayerNameText(string name)
    {
        _nameText.text = name;
    }

    private void EnableReadyButton()
    {
        if (!_playerHealth.IsLiving())
            return;

        _readyButton.SetActive(true);
    }

    public void DisableReadyButton()
    {
        _readyButton.SetActive(false);
    }

    public void Ready()
    {
        _readyIndicator.SetActive(true);

        _readyButton.GetComponent<Image>().color = Color.green;
    }

    public void Unready()
    {
        _readyIndicator.SetActive(false);

        _readyButton.GetComponent<Image>().color = Color.red;
    }

    private void ToggleMap()
    {
        if (!_playerHealth.IsLiving())
            return;

        _islandMap.SetActive(!_islandMap.activeSelf);
    }

    public void UpdateLocationText(string location)
    {
        _locationText.text = location;
    }

    private void UpdateHealth(float ModifiedAmmount, float newTotal)
    {
        if (ModifiedAmmount == 0)
            return;

        if (ModifiedAmmount > 0)
            _healthFlashSprite.color = Color.green;
        else if (ModifiedAmmount < 0)
            _healthFlashSprite.color = Color.red;

        _healthFlashSprite.DOFade(0.8f, 0.25f).SetEase(Ease.Flash).OnComplete(() => { _healthFlashSprite.DOFade(0, 0.25f).SetEase(Ease.Flash); });

        _healthText.text = newTotal.ToString();
    }

    private void UpdateHunger(float ModifiedAmmount, float newTotal)
    {
        if (ModifiedAmmount == 0)
            return;

        if (ModifiedAmmount > 0)
            _hungerFlashSprite.color = Color.green;
        else if (ModifiedAmmount < 0)
            _hungerFlashSprite.color = Color.red;

        _hungerFlashSprite.DOFade(0.8f, 0.25f).OnComplete(() => { _hungerFlashSprite.DOFade(0, 0.25f); });

        _hungerText.text = newTotal.ToString();
    }

    private void DisplayDeathMessage()
    {
        _deathMessage.SetActive(true);
    }
    #endregion

    // ================== Exile UI ==================
    #region Exile UI
    public void Vote()
    {
        hasVoted = true;
    }

    public bool HasVoted()
    {
        return hasVoted;
    }

    public void StartExile(ulong[] playerIDList, ExileManager exileManager)
    {
        // Dont let dead players vote
        if (!_playerHealth.IsLiving())
            return;

        // Clear old stuff
        foreach (Transform child in _voteArea)
            Destroy(child.gameObject);

        _closeUIButton.SetActive(false);
        _exileUI.SetActive(true);
        hasVoted = false;

        // Add nobody vote
        ExileVote nobodyVote = Instantiate(_votePrefab, _voteArea).GetComponent<ExileVote>();
        nobodyVote.Setup(999, "Nobody", exileManager);

        // Add entry for each player
        foreach (ulong id in playerIDList)
        {
            if (id != 999)
            {
                // Instantiate a vote box
                var curVote = Instantiate(_votePrefab, _voteArea).GetComponent<ExileVote>();

                // Setup Vote
                ulong pID = id;
                string pName = "Player " + pID.ToString();
                curVote.Setup(pID, pName, exileManager);
            }
        }
    }

    public void ShowResults(int[] results)
    {
        // Dont show dead players (Unless they just died)
        if (!_exileUI.activeInHierarchy)
            return;

        _closeUIButton.SetActive(true);

        int i = 0;

        foreach (Transform child in _voteArea)
        {
            child.GetComponent<ExileVote>().DisplayResults(results[i]);
            i++;
        }
    }

    // Called when the state transitions.
    // This matters if the timer ends but players are not done voting.
    private void CloseExileVote()
    {
        _exileUI.SetActive(false);
    }
    #endregion
}
