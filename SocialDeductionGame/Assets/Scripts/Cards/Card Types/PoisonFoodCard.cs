using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoisonFoodCard : Card
{
    [Header("Poison Food Details")]
    [SerializeField] private int _servings;
    [SerializeField] private int _hpGain;

    public override void OnPlay(GameObject playLocation)
    {
        PlayerObj player = playLocation.GetComponent<PlayerObj>();

        if (player != null)
        {
            if (this.HasTag("Edible"))
            {
                int rand = Random.Range(0, 101);
                Debug.Log("Ate Posion food, Rolling effect: " + rand);
                // 60% chance that food works like normal
                if(rand <= 60)
                {
                    player.Eat(_servings, _hpGain);
                    Debug.Log("Food works like normal");
                }
                // 25% chance food makes player loose 2 hunger
                else if (rand > 60 && rand <= 85)
                {
                    player.Eat(-2, 0);
                    Debug.Log("Player is sick, -2 hunger");
                }
                // 15% chance food makes player take 1 damage
                else
                {
                    player.Eat(0, -1);
                    Debug.Log("Player is really sick -1 hp");
                }
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
