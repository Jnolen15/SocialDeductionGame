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
    public void SendNotification(string message, string from, bool onlyLocal)
    {
        TextChatManager.ChatMessage notification = new TextChatManager.ChatMessage(message, from, TextChatManager.ChatChannel.Notification);
        SendNotificationServerRpc(notification, onlyLocal);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SendNotificationServerRpc(TextChatManager.ChatMessage notif, bool onlyLocal, ServerRpcParams serverRpcParams = default)
    {
        var clientID = serverRpcParams.Receive.SenderClientId;
        ClientRpcParams clientRpcParams = new ClientRpcParams 
        { 
            Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { clientID } }
        };


        if (onlyLocal)
            RecieveNotificationClientRpc(notif, clientRpcParams);
        else
            RecieveNotificationClientRpc(notif);
    }

    [ClientRpc]
    private void RecieveNotificationClientRpc(TextChatManager.ChatMessage notif, ClientRpcParams clientRpcParams = default)
    {
        _textChat.InstantiateMessage(notif, 1);
    }
}
