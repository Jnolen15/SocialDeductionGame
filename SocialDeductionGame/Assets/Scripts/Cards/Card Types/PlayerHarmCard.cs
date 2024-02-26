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
        LocationManager locMan = GameObject.FindGameObjectWithTag("GameManager").GetComponent<LocationManager>();

        if (playerHealth != null)
        {
            playerHealth.ModifyHealth(-2, "Gunshot");

            if(locMan == null)
                NotificationManager.Instance.SendNotification("A gunshot was heard in the distance.", "Bang!");
            else
                NotificationManager.Instance.SendNotification($"A gunshot was heard at the {locMan.GetCurrentLocalLocation()}.", "Bang!");
        }
        else
        {
            Debug.LogError("Card was played on a location it can't do anything with");
        }
    }
}
