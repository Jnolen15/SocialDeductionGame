using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FoodCard : Card
{
    [Header("Food Details")]
    [SerializeField] private bool _edible;
    [SerializeField] private float _servings;
    [SerializeField] private int _hpGain;

    public override void OnPlay(GameObject playLocation)
    {
        Campfire campfire = playLocation.GetComponent<Campfire>();
        PlayerObj player = playLocation.GetComponent<PlayerObj>();

        if (campfire != null)
        {
            Debug.Log("Playng food card. Servings: " + _servings);
            campfire.AddFood(_servings);
        }
        else if (player != null)
        {
            if (!_edible)
            {
                Debug.Log("This food is inedible, cannot be eaten");
                return;
            }

            Debug.Log($"Player eating {_servings} servings, healed for {_hpGain}");
            player.Eat(_servings, _hpGain);
        }
        else
        {
            Debug.LogError("Card was played on a location it can't do anything with");
        }

        Destroy(gameObject);
    }
}
