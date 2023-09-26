using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class CraftingUI : MonoBehaviour
{
    // =============== Refrences ===============
    [SerializeField] private Transform _blueprintZone;
    [SerializeField] private Transform _cardZone;
    [SerializeField] private GameObject _blueprintEntryPref;
    [SerializeField] private GameObject _cantCraftMessage;
    [SerializeField] private List<BlueprintSO> _blueprints;
    private HandManager _handMan;
    private CardManager _cardManager;

    // =============== Variables ===============
    private BlueprintSO _currentBlueprint;

    // =============== Setup ===============
    private void Start()
    {
        _handMan = GetComponentInParent<HandManager>();
        _cardManager = GameObject.FindGameObjectWithTag("CardManager").GetComponent<CardManager>();

        Setup();
    }

    public void Setup()
    {
        foreach (BlueprintSO blueprint in _blueprints)
        {
            BlueprintEntry blueprintEntry = Instantiate(_blueprintEntryPref, _blueprintZone).GetComponent<BlueprintEntry>();
            blueprintEntry.Setup(blueprint);
        }
    }

    // =============== Functions ===============
    public void SelectBlueprint(BlueprintSO blueprint)
    {
        if(_cardZone.childCount > 0)
         Destroy(_cardZone.GetChild(0).gameObject);

        _cantCraftMessage.SetActive(false);

        _currentBlueprint = blueprint;

        Card newCard = Instantiate(CardDatabase.Instance.GetCard(_currentBlueprint.GetCardID()), _cardZone).GetComponent<Card>();
        newCard.SetupUI();
    }

    public void AttemptCraft()
    {
        if (_currentBlueprint == null)
            return;

        _handMan.TryCraft(_currentBlueprint.GetCardComponents());
    }

    public void Craft(bool crafted)
    {
        if (_currentBlueprint == null)
            return;

        // Player has all componenets
        if (crafted)
        {
            Debug.Log("Crafting Success!");

            // Give crafted card
            _cardManager.GiveCard(_currentBlueprint.GetCardID());
        }
        // Player does not have all needed componenets
        else
        {
            Debug.Log("Crafting fail! Do not have required resources");
            _cantCraftMessage.SetActive(true);
        }
    }
}
