using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class KeywordPopup : MonoBehaviour
{
    // =============== Refrences ===============
    [SerializeField] private TextMeshProUGUI _kName;
    [SerializeField] private TextMeshProUGUI _kDescription;

    // =============== Setup ===============
    public void Setup(string keywordName, string keywordDescription)
    {
        _kName.text = keywordName;
        _kDescription.text = keywordDescription;
    }
}
