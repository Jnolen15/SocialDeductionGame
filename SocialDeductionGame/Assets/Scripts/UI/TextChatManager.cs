using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using TMPro;

public class TextChatManager : NetworkBehaviour
{
    // =============== Setup ===============
    [SerializeField] private TMP_InputField _messageInputField;
    [SerializeField] private Transform _messageContent;
    [SerializeField] private GameObject _saboChatIcon;
    [SerializeField] private GameObject _deathChatIcon;
    [SerializeField] private GameObject _textMessagePref;

    // just for testing
    [SerializeField] private bool _saboChat;
    [SerializeField] private bool _deadChat;

    public enum ChatChannel 
    {
        Camp,
        Beach,
        Forest,
        Plateau,
        Saboteur,
        Dead
    }


    public class ChatMessage : INetworkSerializable
    {
        public string MSG;
        public string SenderName;
        public ChatChannel Channel;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            if (serializer.IsReader)
            {
                var reader = serializer.GetFastBufferReader();
                reader.ReadValueSafe(out MSG);
                reader.ReadValueSafe(out SenderName);
                reader.ReadValueSafe(out Channel);
            }
            else
            {
                var writer = serializer.GetFastBufferWriter();
                writer.WriteValueSafe(MSG);
                writer.WriteValueSafe(SenderName);
                writer.WriteValueSafe(Channel);
            }
        }

        public ChatMessage()
        {
            MSG = "Message";
            SenderName = "Sender";
            Channel = ChatChannel.Camp;
        }

        public ChatMessage(string msg, string sender, ChatChannel channel)
        {
            MSG = msg;
            SenderName = sender;
            Channel = channel;
        }
    }


    // =============== Function ===============
    public void ToggleSaboChat()
    {
        _saboChat = !_saboChat;

        _saboChatIcon.SetActive(_saboChat);
    }

    public void ToggleDeathChat()
    {
        _deadChat = !_deadChat;

        _deathChatIcon.SetActive(_deadChat);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            if (!string.IsNullOrWhiteSpace(_messageInputField.text))
            {
                ChatMessage newMessage = new ChatMessage(_messageInputField.text, "Player", ChatChannel.Camp);
                SendChatMessageServerRpc(newMessage);
            }

            _messageInputField.text = "";
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SendChatMessageServerRpc(ChatMessage msg)
    {
        RecieveChatMessageClientRpc(msg);
    }

    [ClientRpc]
    private void RecieveChatMessageClientRpc(ChatMessage msg)
    {
        Debug.Log("Message recieved: " + msg.MSG);

        TextChatMessage chatMsg = Instantiate(_textMessagePref, _messageContent).GetComponent<TextChatMessage>();

        //chatMsg.Setup(msg, "player", ChatChannel.Camp);
        chatMsg.Setup(msg.MSG, msg.SenderName, msg.Channel);

        chatMsg.transform.SetAsFirstSibling();
    }
}
