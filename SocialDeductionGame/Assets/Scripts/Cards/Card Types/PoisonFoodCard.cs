using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoisonFoodCard : Card
{
    [Header("Poison Food Details")]
    [SerializeField] private int _servings;
    [SerializeField] private int _hpGain;
    [SerializeField] private int _loseAmmount;

    public override void OnPlay(GameObject playLocation)
    {
        PlayerObj player = playLocation.GetComponent<PlayerObj>();

        if (player != null)
        {
            if (this.HasTag("Edible"))
            {
                int rand = Random.Range(0, 101);
                Debug.Log("Ate Posion food, Rolling effect: " + rand);
                // 50% chance that food works like normal
                if(rand <= 50)
                {
                    player.Eat(_servings, _hpGain);
                    Debug.Log("Food works like normal");
                }
                // 50% chance food makes player loose some hunger
                else
                {
                    player.Eat(-_loseAmmount, 0);
                    Debug.Log($"Player is sick, -{_loseAmmount} hunger");
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
