using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using TMPro;
using Unity.Collections;

public class ExileVote : MonoBehaviour
{
    // ================== Refrences / Variables ==================
    [SerializeField] private GameObject _voteStage;
    [SerializeField] private GameObject _resultsStage;
    [SerializeField] private GameObject _confirmationButton;
    [SerializeField] private GameObject _voteSubmittedText;
    [SerializeField] private TextMeshProUGUI _buttonName;
    [SerializeField] private TextMeshProUGUI _textName;
    [SerializeField] private TextMeshProUGUI _resultsNum;

    private ulong _votePlayerID;
    private string _playerName;
    private ExileManager _exileManager;

    // Event
    public delegate void OnVoteAction();
    public static event OnVoteAction OnHitNameButton;
    public static event OnVoteAction OnVoteSubmitted;

    // ================== Setup ==================
    #region Setup
    private void OnEnable()
    {
        OnHitNameButton += CloseConfirmation;
        OnVoteSubmitted += DisplaySubmitted;
    }

    private void OnDisable()
    {
        OnHitNameButton -= CloseConfirmation;
        OnVoteSubmitted -= DisplaySubmitted;
    }

    public void Setup(ulong pID, string pName)
    {
        _votePlayerID = pID;

        if(pID == 999)
        {
            _playerName = "Nobody";
            _buttonName.text = "Nobody";
            _textName.text = "Nobody";
        } else
        {
            _playerName = pName;
            _buttonName.text = _playerName;
            _textName.text = _playerName;
        }

        _voteStage.SetActive(true);
        _resultsStage.SetActive(false);

        _exileManager = this.GetComponentInParent<ExileManager>();
    }
    #endregion

    // ================== Function ==================
    public void ResetVote()
    {
        _voteStage.SetActive(true);
        _resultsStage.SetActive(false);
        _voteSubmittedText.SetActive(false);
    }

    public void DisplayResults(int numVotes)
    {
        _resultsNum.text = numVotes.ToString();

        _voteStage.SetActive(false);
        _resultsStage.SetActive(true);
    }

    public void DisplaySubmitted()
    {
        _resultsNum.text = "?";

        _voteStage.SetActive(false);
        _resultsStage.SetActive(true);
    }

    public void SelectNameButton()
    {
        // Call to close confirmation on all other votes
        OnHitNameButton?.Invoke();

        _confirmationButton.SetActive(true);
    }

    public void CloseConfirmation()
    {
        _confirmationButton.SetActive(false);
    }

    public void SubmitVote()
    {
        OnVoteSubmitted?.Invoke();
        _exileManager.SubmitVote(PlayerConnectionManager.Instance.GetLocalPlayersID(), _votePlayerID);
    }
}
