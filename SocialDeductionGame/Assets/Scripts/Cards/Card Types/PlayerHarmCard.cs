using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHarmCard : Card
{
    [Header("Harm Details")]
    [SerializeField] private int _hpDamage;

    public override void OnPlay(GameObject playLocation)
    {
        PlayerHealth playerHealth = playLocation.GetComponentInParent<PlayerHealth>();

        if (playerHealth != null)
        {
            playerHealth.ModifyHealth(-2, "Gunshot");
        }
        else
        {
            Debug.LogError("Card was played on a location it can't do anything with");
        }
    }
}
