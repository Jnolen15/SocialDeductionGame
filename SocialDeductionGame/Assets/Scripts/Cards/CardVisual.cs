using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardVisual : MonoBehaviour
{
    // ================== Refrences ==================
    [SerializeField] private GameObject _tagIconPref;
    [SerializeField] private Outline _cardOutline;
    [SerializeField] private TextMeshProUGUI _cardName;
    [SerializeField] private TextMeshProUGUI _cardDescription;
    [SerializeField] private Image _cardSprite;
    [SerializeField] private Transform _tagIconSlot;

    // ================== Setup ==================
    public void Setup(string name, string description, Sprite art, List<CardTag> tags)
    {
        _cardName.text = name;
        _cardDescription.text = description;

        if(art != null)
        {
            _cardSprite.sprite = art;
            //if(artDimensions.x != 0 && artDimensions.y != 0)
            //    _cardSprite.rectTransform.sizeDelta = artDimensions;
            //_cardSprite.transform.localScale = new Vector3(artAdjust.x, artAdjust.x, artAdjust.x);
            //_cardSprite.transform.localPosition = new Vector3(0, artAdjust.y, 0);
            //_cardSprite.transform.localRotation = Quaternion.Euler(new Vector3(0, 0, artAdjust.z));
        }

        foreach (CardTag tag in tags)
        {
            TagIcon icon = Instantiate(_tagIconPref, _tagIconSlot).GetComponent<TagIcon>();
            icon.SetupIcon(tag.visual, tag.Name);
        }
    }

    public void ShowOutline(Color outlineColor)
    {
        _cardOutline.gameObject.SetActive(true);
        _cardOutline.effectColor = outlineColor;
    }
}
