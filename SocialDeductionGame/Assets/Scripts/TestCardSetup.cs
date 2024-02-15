using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestCardSetup : MonoBehaviour
{
    [SerializeField] private List<Card> _cards;
    [SerializeField] private List<HazardCardVisual> _hazards;

    void Start()
    {
        foreach (Card card in _cards)
        {
            card.SetupUI();
        }

        int hazardID = 1001;
        foreach (HazardCardVisual hazard in _hazards)
        {
            hazard.Setup(hazardID);

            hazardID++;
        }
    }
}
