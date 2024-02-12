using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TrialVoteUI : MonoBehaviour
{
    // ================== Refrences / Variables ==================
    [SerializeField] private GameObject _pannel;
    [SerializeField] private TextMeshProUGUI _playerName;
    [SerializeField] private TextMeshProUGUI _subtitle;
    [SerializeField] private TextMeshProUGUI _exileVoteText;
    [SerializeField] private TextMeshProUGUI _spareVoteText;
    [SerializeField] private GameObject _voteButtons;
    [SerializeField] private GameObject _voteText;
    [SerializeField] private Image _voteTimerFill;
    [SerializeField] private GameObject _closeButton;
    private ExileManager _exileManager;

    // ================== Setup ==================
    #region Setup
    private void Start()
    {
        GameManager.OnStateNight += Hide;

        _exileManager = gameObject.GetComponentInParent<ExileManager>();
    }

    private void OnDestroy()
    {
        GameManager.OnStateNight -= Hide;
    }
    #endregion

    // ================== Update ==================
    private void Update()
    {
        if (_exileManager.TrialStarted())
            _voteTimerFill.fillAmount = _exileManager.CalculateTrialTimerFill();
    }

    // ================== UI Function ==================
    #region UI
    public void Show()
    {
        _pannel.SetActive(true);
    }

    public void Hide()
    {
        _pannel.SetActive(false);
    }

    public void Setup(ulong playerID, bool canVote)
    {
        Show();
        _closeButton.SetActive(false);
        _playerName.text = PlayerConnectionManager.Instance.GetPlayerNameByID(playerID);
        _exileVoteText.text = "0";
        _spareVoteText.text = "0";

        if (playerID == PlayerConnectionManager.Instance.GetLocalPlayersID())
        {
            canVote = false;
            _subtitle.text = "You are on trial. Make your defense.";
        }
        else
            _subtitle.text = "Is on trial. Exile them?";

        if (canVote)
        {
            _voteButtons.SetActive(true);
            _voteText.SetActive(false);
        }
        else
        {
            _voteText.SetActive(true);
            _voteButtons.SetActive(false);
        }
    }

    public void UpdateTrialResults(int exileVotes, int SpareVotes)
    {
        _exileVoteText.text = exileVotes.ToString();
        _spareVoteText.text = SpareVotes.ToString();
    }

    public void VoteEnded(bool wasExiled)
    {
        _voteText.SetActive(true);
        _voteButtons.SetActive(false);

        if(!wasExiled)
            _subtitle.text = "Was Spared.";
        else
            _subtitle.text = "Was Exiled.";

        _closeButton.SetActive(true);
    }
    #endregion
}
