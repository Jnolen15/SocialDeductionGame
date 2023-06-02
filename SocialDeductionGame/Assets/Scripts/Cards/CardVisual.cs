using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class CardVisual : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _cardName;
    [SerializeField] private TextMeshProUGUI _cardDescription;

    public void Setup(string name, string description)
    {
        _cardName.text = name;
        _cardDescription.text = description;
    }
}
