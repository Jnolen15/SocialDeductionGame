using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TextChatMessage : MonoBehaviour
{
    // =============== Refrences ===============
    [SerializeField] private TextMeshProUGUI _messageText;
    [SerializeField] private TextMeshProUGUI _channleNameText;

    // =============== Setup ===============
    public void Setup(string message, string senderName, TextChatManager.ChatChannel channel)
    {
        _messageText.text = "<b>" + senderName + ":</b> " + message;
        _channleNameText.text = channel.ToString();
    }
}
