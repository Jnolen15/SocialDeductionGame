using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Pedestal : MonoBehaviour
{
    // ================= Refrences =================
    [SerializeField] private GameObject _skull;
    [SerializeField] private TextMeshPro _playerName;
    private ulong _playerID;
    private bool _markedDead;

    // ================= Function =================
    public void SetupPedestal(ulong playerID, string playerName)
    {
        _playerID = playerID;
        _playerName.text = playerName;
    }

    public void Show()
    {
        this.gameObject.SetActive(true);
    }

    public void SetPlayerDead()
    {
        _markedDead = true;
        _skull.SetActive(true);
    }

    public bool GetSkullActive()
    {
        return _markedDead;
    }

    public ulong GetPlayerID()
    {
        return _playerID;
    }
}
