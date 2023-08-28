using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class CardVisual : MonoBehaviour
{
    // ================== Refrences ==================
    [SerializeField] private GameObject _tagIconPref;

    [SerializeField] private TextMeshProUGUI _cardName;
    [SerializeField] private TextMeshProUGUI _cardDescription;
    [SerializeField] private Transform _tagIconSlot;

    // ================== Setup ==================
    public void Setup(string name, string description, List<CardTag> tags)
    {
        _cardName.text = name;
        _cardDescription.text = description;

        foreach(CardTag tag in tags)
        {
            TagIcon icon = Instantiate(_tagIconPref, _tagIconSlot).GetComponent<TagIcon>();
            icon.SetupIcon(tag.visual, tag.Name);
        }
    }
}
