using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FoodCard : Card
{
    [SerializeField] private float _servings;

    public override void OnPlay(GameObject playLocation)
    {
        Campfire campfire = playLocation.GetComponent<Campfire>();

        if (campfire != null)
        {
            Debug.Log("Playng food card. Servings: " + _servings);
            campfire.AddFood(_servings);
        }
        else
        {
            Debug.LogError("Card was played on a location it can't do anything with");
        }

        Destroy(gameObject);
    }
}
