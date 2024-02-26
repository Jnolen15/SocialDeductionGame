using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class NotificationManager : NetworkBehaviour
{
    // ============== Singleton pattern ==============
    #region Singleton
    public static NotificationManager Instance { get; private set; }
    private void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(this);
        else
            Instance = this;
    }
    #endregion

    // ============== Refrences / Variables ==============
    private TextChatManager _textChat;

    // ============== Setup ==============
    private void Start()
    {
        _textChat = this.GetComponent<TextChatManager>();
    }

    // ============== Function ==============
    public void SendNotification(string message, string from)
    {
        TextChatManager.ChatMessage notification = new TextChatManager.ChatMessage(message, from, TextChatManager.ChatChannel.Notification);
        SendNotificationServerRpc(notification);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SendNotificationServerRpc(TextChatManager.ChatMessage notif)
    {
        RecieveNotificationClientRpc(notif);
    }

    [ClientRpc]
    private void RecieveNotificationClientRpc(TextChatManager.ChatMessage notif)
    {
        _textChat.InstantiateMessage(notif);
    }
}
