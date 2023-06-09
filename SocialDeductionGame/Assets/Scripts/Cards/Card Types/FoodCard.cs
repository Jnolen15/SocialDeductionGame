using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FoodCard : Card
{
    [Header("Food Details")]
    [SerializeField] private float _servings;
    [SerializeField] private int _hpGain;

    public override void OnPlay(GameObject playLocation)
    {
        Campfire campfire = playLocation.GetComponent<Campfire>();
        PlayerObj player = playLocation.GetComponent<PlayerObj>();

        if (campfire != null)
        {
            Debug.Log("Cooking food card. Adding " + _servings + " Servings");
            campfire.AddFood(_servings);
        }
        else if (player != null)
        {
            if (this.HasTag("Edible"))
            {
                Debug.Log($"Player eating {_servings} servings, healed for {_hpGain}");
                player.Eat(_servings, _hpGain);
            }
            else
                Debug.Log("This food is inedible, nothing happens");
        }
        else
        {
            Debug.LogError("Card was played on a location it can't do anything with");
        }
    }
}
