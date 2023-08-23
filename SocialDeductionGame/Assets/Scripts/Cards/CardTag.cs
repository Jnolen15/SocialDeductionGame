using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Card Tag", menuName = "Card Tag/New Card Tag")]
public class CardTag : ScriptableObject
{
    public string Name => name;

    public Sprite visual;
}
