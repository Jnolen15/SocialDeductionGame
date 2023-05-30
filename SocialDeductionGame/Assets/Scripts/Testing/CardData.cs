using UnityEngine;

[CreateAssetMenu(fileName ="NewCard", menuName ="Card")]
public class CardData : ScriptableObject
{
    // The card ID is whats sent across the network
    public int CardID;

    public string CardName;
}
