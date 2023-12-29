using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FruitClusterCard : Card
{
    [Header("Cluster Details")]
    [SerializeField] private int _servings;
    [SerializeField] private int _givenCardID;
    private CardManager _cardManager;

    // Fruit cluster acts like a 0.5 serving food. But each time its used it gives the player the next fruit card
    // Fruit cluster (3) => Fruit cluster (2) => Fruit
    public override void OnPlay(GameObject playLocation)
    {
        _cardManager = GameObject.FindGameObjectWithTag("CardManager").GetComponent<CardManager>();

        PlayerObj player = playLocation.GetComponent<PlayerObj>();
        Campfire campfire = playLocation.GetComponent<Campfire>();

        if (campfire != null)
        {
            Debug.Log("Cooking food card. Adding " + _servings + " Servings");
            campfire.AddFood(_servings);

            _cardManager.GiveCard(_givenCardID);
        }
        else if (player != null)
        {
            Debug.Log($"Player eating {_servings} servings");
            player.Eat(_servings, 0);

            _cardManager.GiveCard(_givenCardID);
        }
        else
        {
            Debug.LogError("Card was played on a location it can't do anything with");
        }
    }
}
