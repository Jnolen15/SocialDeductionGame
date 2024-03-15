using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestCardSetup : MonoBehaviour
{
    [SerializeField] private List<Card> _cardsUI;
    [SerializeField] private List<Card> _cardsPlayable;
    [SerializeField] private List<HazardCardVisual> _hazards;

    void Start()
    {
        foreach (Card card in _cardsUI)
        {
            card.SetupUI();
        }

        foreach (Card card in _cardsPlayable)
        {
            card.SetupPlayable();
        }

        int hazardID = 1001;
        foreach (HazardCardVisual hazard in _hazards)
        {
            hazard.Setup(hazardID);

            hazardID++;
        }
    }
}
