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
    private ulong _playerID;
    private bool _markedDead;
    private bool _interactable;
    private ShrineLocation _shrineLocation;

    // ================= Function =================
    public void SetupPedestal(ulong playerID, string playerName)
    {
        _playerID = playerID;
        _playerName.text = playerName;

        _shrineLocation = this.GetComponentInParent<ShrineLocation>();
    }

    public void Show()
    {
        this.gameObject.SetActive(true);
    }

    public void SetPlayerDead()
    {
        _markedDead = true;
        _placedSkull.SetActive(true);
    }

    public bool GetSkullActive()
    {
        return _markedDead;
    }

    public ulong GetPlayerID()
    {
        return _playerID;
    }

    public void SetInteractable(bool setTo)
    {
        if (setTo && PlayerConnectionManager.Instance.GetLocalPlayerTeam() != PlayerData.Team.Saboteurs)
            return;

        _interactable = setTo;
    }

    private void ChooseSacrifice()
    {
        SetPlayerDead();
        _shrineLocation.ChooseSacrifice(_playerID);
    }

    private void OnMouseDown()
    {
        if (!_interactable || _markedDead)
            return;

        _hoveredSkull.SetActive(false);

        ChooseSacrifice();
    }

    private void OnMouseEnter()
    {
        if (_interactable && !_markedDead)
            return;

        _hoveredSkull.SetActive(true);
    }

    private void OnMouseExit()
    {
        _hoveredSkull.SetActive(false);
    }
}
