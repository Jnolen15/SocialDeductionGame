using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ForageUI : MonoBehaviour
{
    // ===================== Refrernces =====================
    [SerializeField] private Transform _cardZone;
    [SerializeField] private GameObject _forageMenu;
    [SerializeField] private GameObject _forageButton;
    //[SerializeField] private GameObject _redealButton;
    [SerializeField] private GameObject _hazardCloseButton;
    [SerializeField] private TextMeshProUGUI _dangerText;

    // ===================== Setup =====================
    #region Setup
    private void Start()
    {
        //Forage.OnDangerIncrement += UpdateDangerUI;
    }

    private void OnDestroy()
    {
        //Forage.OnDangerIncrement -= UpdateDangerUI;
    }
    #endregion

    // ===================== Functions =====================
    #region Functions
    public void OpenForageMenu()
    {
        _forageButton.SetActive(false);
        _forageMenu.SetActive(true);
    }

    public void CloseForageMenu()
    {
        _forageButton.SetActive(true);
        _forageMenu.SetActive(false);
    }

    public void DealCardObjects(List<GameObject> cardObjs)
    {
        OpenForageMenu();

        foreach (GameObject cardObj in cardObjs)
        {
            cardObj.transform.SetParent(_cardZone);
            cardObj.transform.localScale = Vector3.one;
        }
    }

    public void ClearCards()
    {
        foreach (Transform child in _cardZone)
        {
            Destroy(child.gameObject);
        }
    }

    public void UpdateDangerUI(float current)
    {
        _dangerText.text = current.ToString("F1");

        // Should not hard code this (should have value refrences)
        _dangerText.color = new Color32(233, 195, 41, 255);
        //_dangerIcon.sprite = _dangerIconStages[2];
        if (40 < current && current <= 80)
        {
            _dangerText.color = new Color32(217, 116, 24, 255);
            //_dangerIcon.sprite = _dangerIconStages[1];
        }
        else if (80 < current)
        {
            _dangerText.color = new Color32(206, 60, 24, 255);
            //_dangerIcon.sprite = _dangerIconStages[0];
        }
    }
    #endregion
}
