using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using TMPro;
using UnityEngine.UI;
using DG.Tweening;

public class TextChatManager : NetworkBehaviour
{
    // =============== Refrences / Variables ===============
    [SerializeField] private Transform _chatArea;
    [SerializeField] private Transform _outPos;
    [SerializeField] private Transform _inPos;
    [SerializeField] private RectTransform _messageOpenBubble;
    [SerializeField] private GameObject _messageNotif;
    [SerializeField] private TMP_InputField _messageInputField;
    [SerializeField] private Transform _messageContent;
    [SerializeField] private GameObject _saboChatButton;
    [SerializeField] private Image _saboChatButtonImage;
    [SerializeField] private Image _inputAreaImage;
    [SerializeField] private Sprite _paperNormal;
    [SerializeField] private Sprite _paperSabo;
    [SerializeField] private Sprite _paperDead;
    [SerializeField] private GameObject _msgNote;
    [SerializeField] private GameObject _msgDigital;
    [SerializeField] private GameObject _msgBloodied;
    [SerializeField] private GameObject _msgGhostly;
    [SerializeField] private PlayRandomSound _randSound;
    private bool _chatOpen = false;
    private bool _inSaboChat;
    private bool _saboChatActive;
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
        Dead,
        Notification
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
    #region Setup
    public override void OnNetworkSpawn()
    {
        _localPlayerID = PlayerConnectionManager.Instance.GetLocalPlayersID();
        _localPlayerName = PlayerConnectionManager.Instance.GetPlayerNameByID(_localPlayerID);

        LocationManager.OnLocationChanged += UpdateLocation;
        PlayerHealth.OnDeath += EnterDeathChat;
        GameManager.OnStateIntro += EnterSaboChat;
    }

    private void OnDisable()
    {
        LocationManager.OnLocationChanged -= UpdateLocation;
        PlayerHealth.OnDeath -= EnterDeathChat;
        GameManager.OnStateIntro -= EnterSaboChat;
    }
    #endregion

    // =============== Channels ===============
    #region Channels
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

    public void EnterSaboChat()
    {
        Debug.Log("entered saboteur chat");

        if (PlayerConnectionManager.Instance.GetLocalPlayerTeam() == PlayerData.Team.Saboteurs)
        {
            _saboChatButton.SetActive(true);
            _inSaboChat = true;
        }
    }

    public void ToggleSaboChat()
    {
        if (_inDeadChat)
            return;

        _saboChatActive = !_saboChatActive;

        if (_saboChatActive)
        {
            _inputAreaImage.sprite = _paperSabo;
            _saboChatButtonImage.color = Color.grey;
        }
        else
        {
            _inputAreaImage.sprite = _paperNormal;
            _saboChatButtonImage.color = Color.white;
        }
    }

    public void EnterDeathChat()
    {
        _inDeadChat = true;
        _inputAreaImage.sprite = _paperDead;

        _saboChatButton.SetActive(false);
    }
    #endregion

    // =============== UI ===============
    #region UI
    public void ToggleChatOpen()
    {
        _chatOpen = !_chatOpen;

        if (!_chatOpen)
            _chatArea.position = _inPos.position;
        else
        {
            _messageNotif.SetActive(false);
            _chatArea.position = _outPos.position;
        }
    }

    public void InstantiateMessage(ChatMessage msg, int style = 0)
    {
        GameObject pref = _msgNote; // deafult 0
        if (style == 1)
            pref = _msgDigital; // Digital 1
        else if (style == 2)
            pref = _msgBloodied; // Bloodied 2
        if (style == 3)
            pref = _msgGhostly; // Ghostly 3

        TextChatMessage chatMsg = Instantiate(pref, _messageContent).GetComponent<TextChatMessage>();

        chatMsg.Setup(msg.MSG, msg.SenderName, msg.Channel);

        chatMsg.transform.SetAsFirstSibling();

        _randSound.PlayRandom();

        // Show notif if chat minimized
        if (!_chatOpen)
            ShowNotification();
    }

    private void ShowNotification()
    {
        _messageNotif.SetActive(true);

        Vector2 punchPos = new Vector2(0, 80);

        _messageOpenBubble.DOPunchAnchorPos(punchPos, 1f, 4, 0.4f);
    }
    #endregion

    // =============== Send / Recive ===============
    #region Send Recive
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
        else if(_saboChatActive)
            channel = ChatChannel.Saboteur;
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
        //Debug.Log($"Message recieved from {msg.SenderName}, at {msg.Channel}: {msg.MSG}");

        // Determine if chat should be rejected
        int msgStyle = 0;
        if (msg.Channel == ChatChannel.Dead)
        {
            msgStyle = 3;
            if (!_inDeadChat)
            {
                Debug.Log("Message rejected. Not dead");
                return;
            }
        }
        else if (msg.Channel == ChatChannel.Saboteur)
        {
            msgStyle = 2;
            if (!_inSaboChat)
            {
                Debug.Log("Message rejected. Not Saboteur");
                return;
            }
        }
        else if(msg.Channel != _currentLocationChannel)
        {
            Debug.Log("Message rejected. Not at same location");
            return;
        }

        InstantiateMessage(msg, msgStyle);
    }
    #endregion
}
