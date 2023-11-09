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
    [SerializeField] private TextMeshProUGUI _exileVoteText;
    [SerializeField] private TextMeshProUGUI _spareVoteText;
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

    public void Setup(ulong playerID)
    {
        Show();
        _closeButton.SetActive(false);
        _playerName.text = PlayerConnectionManager.Instance.GetPlayerNameByID(playerID);
        _exileVoteText.text = "0";
        _spareVoteText.text = "0";
    }

    public void UpdateTrialResults(int exileVotes, int SpareVotes)
    {
        _exileVoteText.text = exileVotes.ToString();
        _spareVoteText.text = SpareVotes.ToString();
    }

    public void VoteEnded()
    {
        _closeButton.SetActive(true);
    }
    #endregion
}
