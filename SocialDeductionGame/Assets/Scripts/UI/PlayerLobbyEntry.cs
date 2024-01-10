using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class PlayerLobbyEntry : MonoBehaviour
{
    // ============== Refrences ==============
    [SerializeField] private TextMeshProUGUI _playerName;
    
    // ============== Setup ==============
    public void Setup(string playerName)
    {
        _playerName.text = playerName;
        this.GetComponent<Image>().color = Random.ColorHSV();
    }
}
