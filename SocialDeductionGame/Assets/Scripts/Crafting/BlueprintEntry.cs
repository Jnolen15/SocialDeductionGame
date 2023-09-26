using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class BlueprintEntry : MonoBehaviour
{
    // =============== Refrences ===============
    [SerializeField] private Image _background;
    [SerializeField] private TextMeshProUGUI _cardNameText;
    [SerializeField] private Transform _componentTagZone;
    [SerializeField] private GameObject _tagIconPref;
    private CraftingUI _craftingUI;
    // =============== Variables ===============
    private BlueprintSO _blueprint;

    public delegate void BlueprintAction();
    public static event BlueprintAction OnSelect;

    // =============== Setup ===============
    private void Start()
    {
        OnSelect += Deselect;

        _craftingUI = GetComponentInParent<CraftingUI>();
    }

    private void OnDestroy()
    {
        OnSelect -= Deselect;
    }

    public void Setup(BlueprintSO blueprint)
    {
        _blueprint = blueprint;

        _cardNameText.text = _blueprint.GetCardName();

        foreach (CardTag tag in _blueprint.GetCardComponents())
        {
            TagIcon icon = Instantiate(_tagIconPref, _componentTagZone).GetComponent<TagIcon>();
            icon.SetupIcon(tag.visual, tag.Name);
        }
    }

    // =============== Functions ===============
    public void Select()
    {
        OnSelect?.Invoke();

        transform.localScale = new Vector3(1.05f, 1.05f, 1.05f);
        _cardNameText.color = Color.blue;

        _craftingUI.SelectBlueprint(_blueprint);
    }

    public void Deselect()
    {
        transform.localScale = Vector3.one;
        _cardNameText.color = Color.white;
    }
}
