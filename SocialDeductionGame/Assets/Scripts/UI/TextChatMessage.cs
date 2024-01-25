using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TextChatMessage : MonoBehaviour
{
    // =============== Refrences ===============
    [SerializeField] private TextMeshProUGUI _messageText;
    [SerializeField] private TextMeshProUGUI _channleNameText;
    [SerializeField] private Color _saboColor;
    [SerializeField] private Color _deadColor;

    // =============== Setup ===============
    public void Setup(string message, string senderName, TextChatManager.ChatChannel channel)
    {
        _messageText.text = "<b>" + senderName + ":</b> " + message;
        _channleNameText.text = channel.ToString();

        if (channel == TextChatManager.ChatChannel.Saboteur)
        {
            _messageText.color = _saboColor;
        }
        else if (channel == TextChatManager.ChatChannel.Dead)
        {
            _messageText.color = _deadColor;
        }
    }
}
