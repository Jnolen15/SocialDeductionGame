using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening;

public class CraftingUI : MonoBehaviour
{
    // =============== Refrences ===============
    [SerializeField] private Transform _blueprintZone;
    [SerializeField] private Transform _cardZone;
    [SerializeField] private Transform _recourcesZone;
    [SerializeField] private GameObject _blueprintEntryPref;
    [SerializeField] private GameObject _resourceRequirementPref;
    [SerializeField] private TextMeshProUGUI _description;
    [SerializeField] private GameObject _cantCraftMessage;
    [SerializeField] private List<BlueprintSO> _blueprints;
    [SerializeField] private List<BlueprintEntry> _blueprintEntries;
    private HandManager _handMan;
    private CardManager _cardManager;

    // =============== Variables ===============
    private BlueprintSO _currentBlueprint;

    // =============== Setup ===============
    #region Setup
    private void Start()
    {
        _handMan = GetComponentInParent<HandManager>();
        _cardManager = GameObject.FindGameObjectWithTag("CardManager").GetComponent<CardManager>();

        Setup();
    }

    public void Setup()
    {
        if(_blueprints.Count == 0)
        {
            Debug.LogError("NO BLUEPRINTS!");
            return;
        }

        foreach (BlueprintSO blueprint in _blueprints)
        {
            BlueprintEntry blueprintEntry = Instantiate(_blueprintEntryPref, _blueprintZone).GetComponent<BlueprintEntry>();
            blueprintEntry.Setup(blueprint, this);
            _blueprintEntries.Add(blueprintEntry);
        }

        // Select first blueprint by default
        _blueprintZone.transform.GetChild(0).gameObject.GetComponent<BlueprintEntry>().Select();
    }
    #endregion

    // =============== UI ===============
    #region UI
    private void OnEnable()
    {
        MarkBlueprintsCraftable();
    }

    private void MarkBlueprintsCraftable()
    {
        foreach (BlueprintEntry entry in _blueprintEntries)
        {
            bool craftable = _handMan.CheckLocalCanCraft(entry.GetBlueprint().GetCardComponents());
            entry.Highlight(craftable);
        }
    }

    #endregion

    // =============== Functions ===============
    #region Functions
    public void SelectBlueprint(BlueprintSO blueprint)
    {
        if(_cardZone.childCount > 0)
            Destroy(_cardZone.GetChild(0).gameObject);

        foreach (Transform child in _recourcesZone)
        {
            Destroy(child.gameObject);
        }

        _cantCraftMessage.transform.DOKill();
        _cantCraftMessage.gameObject.SetActive(false);
        _description.gameObject.SetActive(true);

        _currentBlueprint = blueprint;

        _description.text = blueprint.GetCardDescription();

        Card newCard = Instantiate(CardDatabase.Instance.GetCard(_currentBlueprint.GetCardID()), _cardZone).GetComponent<Card>();
        newCard.SetupUI();

        foreach (CardTag tag in blueprint.GetCardComponents())
        {
            ResourceRequirement resourceRequirement = Instantiate(_resourceRequirementPref, _recourcesZone).GetComponent<ResourceRequirement>();
            resourceRequirement.Setup(tag);
        }
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

            // Track Analytics
            int curDay = GameManager.Instance.GetCurrentDay();
            AnalyticsTracker.Instance.TrackItemsCrafted(curDay, _currentBlueprint.GetCardID());

            // Give crafted card
            _cardManager.GiveCard(_currentBlueprint.GetCardID());
        }
        // Player does not have all needed componenets
        else
        {
            Debug.Log("Crafting fail! Do not have required resources");
            _cantCraftMessage.transform.DOKill();
            _cantCraftMessage.gameObject.SetActive(true);
            _cantCraftMessage.transform.DOShakePosition(1f).OnComplete( () => _cantCraftMessage.gameObject.SetActive(false));
        }

        MarkBlueprintsCraftable();
    }
    #endregion
}
