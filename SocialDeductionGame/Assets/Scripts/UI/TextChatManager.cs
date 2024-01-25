using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using TMPro;

public class TextChatManager : NetworkBehaviour
{
    // =============== Refrences / Variables ===============
    [SerializeField] private TMP_InputField _messageInputField;
    [SerializeField] private Transform _messageContent;
    [SerializeField] private GameObject _saboChatIcon;
    [SerializeField] private GameObject _deathChatIcon;
    [SerializeField] private GameObject _textMessagePref;
    private bool _inSaboChat;
    private bool _inDeadChat;

    private ulong _localPlayerID;
    private string _localPlayerName;

    public enum ChatChannel 
    {
        Camp,
        Beach,
        Forest,
        Plateau,
        Saboteur,
        Dead
    }
    private ChatChannel _currentLocationChannel;

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

    // =============== Setup ===============
    public override void OnNetworkSpawn()
    {
        _localPlayerID = PlayerConnectionManager.Instance.GetLocalPlayersID();
        _localPlayerName = PlayerConnectionManager.Instance.GetPlayerNameByID(_localPlayerID);

        LocationManager.OnLocationChanged += UpdateLocation;
        PlayerHealth.OnDeath += EnterDeathChat;
    }

    private void OnDisable()
    {
        LocationManager.OnLocationChanged -= UpdateLocation;
        PlayerHealth.OnDeath -= EnterDeathChat;
    }

    // =============== Function ===============
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            if (!string.IsNullOrWhiteSpace(_messageInputField.text))
                SendChatMessage(_messageInputField.text);

            _messageInputField.text = "";
        }
    }

    private void SendChatMessage(string message)
    {
        ChatChannel channel;

        if (_inDeadChat)
            channel = ChatChannel.Dead;
        else
            channel = _currentLocationChannel;

        ChatMessage newMessage = new ChatMessage(message, _localPlayerName, channel);
        SendChatMessageServerRpc(newMessage);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SendChatMessageServerRpc(ChatMessage msg)
    {
        RecieveChatMessageClientRpc(msg);
    }

    [ClientRpc]
    private void RecieveChatMessageClientRpc(ChatMessage msg)
    {
        Debug.Log($"Message recieved from {msg.SenderName}, at {msg.Channel}: {msg.MSG}");

        if (msg.Channel == ChatChannel.Dead)
        {
            if (!_inDeadChat)
            {
                Debug.Log("Message rejected. Not dead");
                return;
            }
        }
        else if(msg.Channel != _currentLocationChannel)
        {
            Debug.Log("Message rejected. Not at same location");
            return;
        }

        TextChatMessage chatMsg = Instantiate(_textMessagePref, _messageContent).GetComponent<TextChatMessage>();

        //chatMsg.Setup(msg, "player", ChatChannel.Camp);
        chatMsg.Setup(msg.MSG, msg.SenderName, msg.Channel);

        chatMsg.transform.SetAsFirstSibling();
    }

    private void UpdateLocation(LocationManager.LocationName location)
    {
        switch (location)
        {
            case LocationManager.LocationName.Camp:
                _currentLocationChannel = ChatChannel.Camp;
                break;
            case LocationManager.LocationName.Beach:
                _currentLocationChannel = ChatChannel.Beach;
                break;
            case LocationManager.LocationName.Forest:
                _currentLocationChannel = ChatChannel.Forest;
                break;
            case LocationManager.LocationName.Plateau:
                _currentLocationChannel = ChatChannel.Plateau;
                break;
            default:
                _currentLocationChannel = ChatChannel.Camp;
                break;
        }
    }

    public void ToggleSaboChat()
    {
        _inSaboChat = !_inSaboChat;

        _saboChatIcon.SetActive(_inSaboChat);
    }

    public void EnterDeathChat()
    {
        _inDeadChat = true;
        _deathChatIcon.SetActive(true);
    }
}
