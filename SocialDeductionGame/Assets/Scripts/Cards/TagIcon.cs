using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TagIcon : MonoBehaviour
{
    [SerializeField] private Image _iconVisual;
    [SerializeField] private TextMeshProUGUI _iconName;
    
    public void SetupIcon(Sprite vis, string name)
    {
        _iconVisual.sprite = vis;
        _iconName.text = name;
    }

    public string GetIconName()
    {
        return _iconName.text;
    }
}
