using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Blueprints/New Blueprint")]
public class BlueprintSO : ScriptableObject
{
    // ============== Blueprint Data ==============
    [Header("Data")]
    [SerializeField] private int _craftedCardID;
    [SerializeField] private string _craftedCardName;
    [Header("Components")]
    [SerializeField] private List<CardTag> _componentTags;

    // ============== Blueprint Getters ==============
    public int GetCardID()
    {
        return _craftedCardID;
    }

    public string GetCardName()
    {
        return _craftedCardName;
    }

    public List<CardTag> GetCardComponents()
    {
        List<CardTag> returnList = new(_componentTags);
        return returnList;
    }
}
