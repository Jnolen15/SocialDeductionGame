using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using TMPro;

public class ConnectionFailUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _reasonText;

    void Start()
    {
        ConnectionManager.OnFailedToJoinGame += OnFailToJoinGame;
        Hide();
    }

    private void OnDestroy()
    {
        ConnectionManager.OnFailedToJoinGame -= OnFailToJoinGame;
    }

    private void OnFailToJoinGame()
    {
        _reasonText.text = NetworkManager.Singleton.DisconnectReason;

        if (_reasonText.text == "")
            _reasonText.text = "Failed to connect";

        Show();
    }

    private void Show()
    {
        gameObject.SetActive(true);
    }

    private void Hide()
    {
        gameObject.SetActive(false);
    }
}
