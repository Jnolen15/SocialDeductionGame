using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using TMPro;

public class ExileVote : MonoBehaviour
{
    // ================== Refrences / Variables ==================
    [SerializeField] private GameObject _voteStage;
    [SerializeField] private GameObject _resultsStage;
    [SerializeField] private GameObject _confirmationButton;
    [SerializeField] private TextMeshProUGUI _buttonName;
    [SerializeField] private TextMeshProUGUI _textName;
    [SerializeField] private TextMeshProUGUI _resultsNum;
    private PlayerUI _playerUI;
    private ulong _playerID;
    private ExileManager _exileManager;

    // Event
    public delegate void HitNameButton();
    public static event HitNameButton OnHitNameButton;

    // ================== Setup ==================
    private void OnEnable()
    {
        OnHitNameButton += CloseConfirmation;
    }

    private void OnDisable()
    {
        OnHitNameButton -= CloseConfirmation;
    }

    public void Setup(ulong playerID, string pName, ExileManager eManager)
    {
        _playerUI = gameObject.GetComponentInParent<PlayerUI>();

        _playerID = playerID;
        _exileManager = eManager;

        _buttonName.text = PlayerConnectionManager.Instance.GetPlayerNameByID(playerID) ?? pName;
        _textName.text = PlayerConnectionManager.Instance.GetPlayerNameByID(playerID) ?? pName;

        _voteStage.SetActive(true);
        _resultsStage.SetActive(false);
    }

    public void DisplayResults(int numVotes)
    {
        _resultsNum.text = numVotes.ToString();

        _voteStage.SetActive(false);
        _resultsStage.SetActive(true);
    }

    public void SelectNameButton()
    {
        OnHitNameButton();

        if (!_playerUI.HasVoted())
            _confirmationButton.SetActive(true);
    }

    public void CloseConfirmation()
    {
        _confirmationButton.SetActive(false);
    }

    // ================== Exile ==================
    public void SubmitVote()
    {
        // Don't submit vote if already submitted
        if (_playerUI.HasVoted())
            return;

        _exileManager.SubmitPlayerVoteServerRpc(_playerID);
        _playerUI.Vote();
    }
}
