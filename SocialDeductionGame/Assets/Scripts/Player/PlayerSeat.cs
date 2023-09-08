using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSeat : MonoBehaviour
{
    // ================== Refrences ==================
    private PlayerData _playerData;

    // ================== Setup ==================
    void Start()
    {
        Debug.Log("Player Seat Start");

        GameManager.OnStateIntro += AssignSeat;

        _playerData = gameObject.GetComponent<PlayerData>();
    }

    private void OnDisable()
    {
        GameManager.OnStateIntro -= AssignSeat;
    }

    // ================== Seats ==================
    public void AssignSeat()
    {
        Debug.Log("Assigning Seat");

        GameManager.Instance.GetSeat(transform, _playerData.GetPlayerID());
    }
}
