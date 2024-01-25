using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TextChatManager : MonoBehaviour
{
    // =============== Setup ===============
    [SerializeField] private TMP_InputField _messageInputField;
    [SerializeField] private Transform _messageContent;
    [SerializeField] private GameObject _textMessagePref;

    // just for testing
    [SerializeField] private bool _saboChat;
    [SerializeField] private bool _deadChat;

    // =============== Function ===============
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            SendChatMessage(_messageInputField.text);
            _messageInputField.text = "";
        }
    }

    private void SendChatMessage(string msg)
    {
        if (string.IsNullOrWhiteSpace(msg)) return;

        TextChatMessage chatMsg = Instantiate(_textMessagePref, _messageContent).GetComponent<TextChatMessage>();

        if(_saboChat)
            chatMsg.Setup(msg, "sabo");
        else if (_deadChat)
            chatMsg.Setup(msg, "dead");
        else
            chatMsg.Setup(msg);

        chatMsg.transform.SetAsFirstSibling();
    }
}
