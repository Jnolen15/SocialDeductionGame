using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NotebookNameEntry : MonoBehaviour
{
    // ================= Refrences =================
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private Image _nameImage;
    [SerializeField] private List<Sprite> _sprites;
    private int _spriteIndex;

    // ================= Setup =================
    public void Setup(string playerName)
    {
        _nameText.text = playerName;
    }

    // ================= Function =================
    public void OnClick()
    {
        _spriteIndex++;

        if (_spriteIndex >= _sprites.Count)
            _spriteIndex = 0;

        _nameImage.sprite = _sprites[_spriteIndex];
    }
}
