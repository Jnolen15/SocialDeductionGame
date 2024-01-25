using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TextChatMessage : MonoBehaviour
{
    // =============== Refrences ===============
    [SerializeField] private TextMeshProUGUI _messageText;
    [SerializeField] private Color _saboColor;
    [SerializeField] private Color _deadColor;

    // =============== Setup ===============
    public void Setup(string message, string chat = null)
    {
        _messageText.text = message;

        if(chat == "sabo")
        {
            _messageText.color = _saboColor;
        }
        else if (chat == "dead")
        {
            _messageText.color = _deadColor;
        }
    }
}
