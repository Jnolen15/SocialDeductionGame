using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TextChatMessage : MonoBehaviour
{
    // =============== Refrences ===============
    [SerializeField] private TextMeshProUGUI _messageText;

    // =============== Setup ===============
    public void Setup(string message, string senderName, TextChatManager.ChatChannel channel)
    {
        _messageText.text = channel.ToString() + ", <b>" + senderName + ":</b> " + message;
    }
}
