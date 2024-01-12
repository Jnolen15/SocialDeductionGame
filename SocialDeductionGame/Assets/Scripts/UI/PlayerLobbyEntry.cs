using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Unity.Services.Lobbies.Models;

public class PlayerLobbyEntry : MonoBehaviour
{
    // ============== Refrences ==============
    [SerializeField] private TextMeshProUGUI _playerName;
    [SerializeField] private GameObject _kickButton;
    private Player _player;

    public delegate void PlayerLobbyEntryaction(string playerID);
    public static event PlayerLobbyEntryaction OnKickPlayer;

    // ============== Setup ==============
    public void Setup(Player player, bool playerIsHost)
    {
        _player = player;

        _playerName.text = _player.Data[LobbyManager.KEY_PLAYER_NAME].Value;
        
        if(playerIsHost)
            _kickButton.SetActive(true);
        
        //this.GetComponent<Image>().color = Random.ColorHSV();
    }

    public void KickPlayer()
    {
        OnKickPlayer?.Invoke(_player.Id);
    }
}
