using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using TMPro;

public class ExileVote : NetworkBehaviour
{
    // ================== Refrences / Variables ==================
    [SerializeField] private GameObject _voteStage;
    [SerializeField] private GameObject _resultsStage;
    [SerializeField] private TextMeshProUGUI _buttonName;
    [SerializeField] private TextMeshProUGUI _textName;
    [SerializeField] private TextMeshProUGUI _resultsNum;

    private ulong _playerID;
    private ExileManager _exileManager;

    // ================== Setup ==================
    public void Setup(ulong playerID, string pName, ExileManager eManager)
    {
        _playerID = playerID;
        _exileManager = eManager;

        _buttonName.text = pName;
        _textName.text = pName;

        _voteStage.SetActive(true);
        _resultsStage.SetActive(false);
    }

    public void DisplayResults(int numVotes)
    {
        _resultsNum.text = numVotes.ToString();

        _voteStage.SetActive(false);
        _resultsStage.SetActive(true);
    }

    // ================== Exile ==================
    public void SubmitVote()
    {
        _exileManager.SubmitPlayerVoteServerRpc(_playerID);

        // Lock player out of voting more
    }
}
