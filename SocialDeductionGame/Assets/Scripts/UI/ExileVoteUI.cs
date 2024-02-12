using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ExileVoteUI : MonoBehaviour
{
    // ================== Refrences / Variables ==================
    [SerializeField] private GameObject _book;
    [SerializeField] private TextMeshProUGUI _calledByNameText;
    [SerializeField] private Transform _voteArea;
    [SerializeField] private GameObject _exileVotePrefab;
    [SerializeField] private Image _voteTimerFill;
    private ExileManager _exileManager;

    // ================== Setup ==================
    #region Setup
    private void Start()
    {
        GameManager.OnStateNight += Hide;
        ExileManager.OnTrialVoteEnded += Hide;

        _exileManager = gameObject.GetComponentInParent<ExileManager>();
    }

    private void OnDestroy()
    {
        GameManager.OnStateNight -= Hide;
        ExileManager.OnTrialVoteEnded -= Hide;
    }
    #endregion

    // ================== Update ==================
    private void Update()
    {
        if (_exileManager.ExileStarted())
            _voteTimerFill.fillAmount = _exileManager.CalculateExileTimerFill();
    }

    // ================== UI Function ==================
    #region UI
    public void Show()
    {
        _book.SetActive(true);
    }

    public void Hide()
    {
        _book.SetActive(false);
    }

    public void InitializeVotePrefabs(ulong[] playerIDs)
    {
        // Initialize vote prefabs
        for (int i = 0; i < playerIDs.Length; i++)
        {
            GameObject vote = Instantiate(_exileVotePrefab, _voteArea);
            vote.GetComponent<ExileVote>().Setup(playerIDs[i], PlayerConnectionManager.Instance.GetPlayerNameByID(playerIDs[i]));
            Debug.Log("<color=blue>CLIENT: </color>Spawned an exile vote", vote);
        }
    }

    public void UpdateExileUI(string calledByName)
    {
        Show();

        _calledByNameText.text = calledByName;

        // Reset vote objects
        foreach (Transform exilevote in _voteArea)
        {
            ExileVote vote = exilevote.GetComponent<ExileVote>();
            // Reset player votes, show if the player is dead or living (or dead if diconnected)
            if (vote.GetVotePlayerID() != 999)
            {
                vote.ResetVote(PlayerConnectionManager.Instance.GetPlayerLivingByID(vote.GetVotePlayerID()));
            }
            else // Nobody vote
                vote.ResetVote(true);
        }
    }

    public void ShowResults(int[] results)
    {
        Show();

        int i = 0;
        foreach (Transform child in _voteArea)
        {
            child.GetComponent<ExileVote>().DisplayResults(results[i]);
            i++;
        }
    }
    #endregion
}
