using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GearDurabilityVisual : MonoBehaviour
{
    // ================== Refrences ==================
    [SerializeField] private GameObject _durability;
    [SerializeField] private TextMeshProUGUI _durabilityText;

    // ================== Setup ==================
    public void Setup(bool hasDurability, int durability)
    {
        if (hasDurability)
        {
            _durability.SetActive(true);
            _durabilityText.text = durability.ToString();
        }
    }

    public void UpdateDurability(int durability)
    {
        _durabilityText.text = durability.ToString();
    }
}
