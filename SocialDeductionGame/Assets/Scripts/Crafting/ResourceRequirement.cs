using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ResourceRequirement : MonoBehaviour
{
    // =============== Refrences ===============
    [SerializeField] private TextMeshProUGUI _cardNameText;
    [SerializeField] private TagIcon _tagObj;

    // =============== Setup ===============
    public void Setup(CardTag tag)
    {
        _cardNameText.text = tag.Name;

        _tagObj.SetupIcon(tag.visual, tag.Name);
    }
}
