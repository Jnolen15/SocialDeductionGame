using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestCardSetup : MonoBehaviour
{
    [SerializeField] private List<Card> _cards;
    void Start()
    {
        foreach (Card card in _cards)
        {
            card.SetupUI();
        }
    }
}
