using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PlayerUI : MonoBehaviour
{
    [SerializeField] private GameObject _islandMap;
    [SerializeField] private TextMeshProUGUI _locationText;

    public void OnEnable()
    {
        GameManager.OnStateForage += ToggleMap;
    }

    private void OnDisable()
    {
        GameManager.OnStateForage -= ToggleMap;
    }

    private void ToggleMap()
    {
        _islandMap.SetActive(!_islandMap.activeSelf);
    }

    public void UpdateLocationText(string location)
    {
        _locationText.text = location;
    }
}
