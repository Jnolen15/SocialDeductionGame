using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSeat : MonoBehaviour
{
    // ================== Refrences ==================
    private GameManager _gameManager;
    private PlayerData _playerData;

    // ================== Setup ==================
    void Start()
    {
        Debug.Log("Player Seat Start");

        GameManager.OnStateIntro += AssignSeat;

        _playerData = gameObject.GetComponent<PlayerData>();
        _gameManager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();
    }

    private void OnDisable()
    {
        GameManager.OnStateIntro -= AssignSeat;
    }

    // ================== Seats ==================
    public void AssignSeat()
    {
        Debug.Log("Assigning Seat");

        _gameManager.GetSeat(transform, _playerData.GetPlayerID());
    }
}
