using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Pedestal : MonoBehaviour
{
    // ================= Refrences =================
    [SerializeField] private TextMeshPro _playerName;
    [SerializeField] private GameObject _placedSkull;
    [SerializeField] private GameObject _hoveredSkull;
    [SerializeField] private Transform _upPos;
    [SerializeField] private Transform _downPos;
    private ulong _playerID;
    private bool _markedDead;
    private bool _interactable;
    private bool _hasVote;
    private bool _isLocalSabo;
    private ShrineLocation _shrineLocation;

    // ================= Setup =================
    #region Setup and Helpers
    public void SetupPedestal(ulong playerID, string playerName)
    {
        _playerID = playerID;
        _playerName.text = playerName;

        _shrineLocation = this.GetComponentInParent<ShrineLocation>();

        _isLocalSabo = (PlayerConnectionManager.Instance.GetLocalPlayerTeam() == PlayerData.Team.Saboteurs);
    }

    public void Show()
    {
        this.gameObject.SetActive(true);
    }

    public bool GetMarkedDead()
    {
        return _markedDead;
    }

    public ulong GetPlayerID()
    {
        return _playerID;
    }
    #endregion

    // ================= Death Skull Display =================
    #region Death Skull Display
    public void SetPlayerDead()
    {
        _markedDead = true;
        _placedSkull.SetActive(true);
        _hoveredSkull.SetActive(false);
    }
    #endregion

    // ================= Vote Skull Display =================
    #region Vote Skull Display
    public void SetInteractable(bool setTo)
    {
        if (!_isLocalSabo)
            return;

        _interactable = setTo;
    }

    private void ChooseSacrifice()
    {
        if (!_isLocalSabo)
            return;

        _shrineLocation.ChooseSacrifice(_playerID);
    }

    public void SetSacrificeVote()
    {
        if (!_isLocalSabo)
            return;

        _hasVote = true;
        _hoveredSkull.SetActive(true);
        _hoveredSkull.transform.position = _downPos.position;
    }

    public void ClearSacrificeVote()
    {
        if (!_isLocalSabo)
            return;

        _hasVote = false;
        _hoveredSkull.SetActive(false);
    }

    // ~~~~~~~~~~~~ Mouse interaction ~~~~~~~~~~~~
    private void OnMouseDown()
    {
        if (!_interactable || _markedDead)
            return;

        ChooseSacrifice();
    }

    private void OnMouseEnter()
    {
        if (!_interactable || _markedDead)
            return;

        _hoveredSkull.SetActive(true);
        _hoveredSkull.transform.position = _upPos.position;
    }

    private void OnMouseExit()
    {
        if (!_interactable || _markedDead)
            return;

        if (_hasVote)
            _hoveredSkull.transform.position = _downPos.position;
        else
            _hoveredSkull.SetActive(false);
    }
    #endregion
}
