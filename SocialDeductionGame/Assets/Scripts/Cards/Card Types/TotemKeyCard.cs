using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TotemKeyCard : Card
{
    public override void OnPlay(GameObject playLocation)
    {
        Totem totem = playLocation.GetComponent<Totem>();

        if (totem != null)
        {
            totem.DeactivateTotem();
        }
        else
        {
            Debug.LogError("Totem Key card was played on a location it can't do anything with");
        }
    }
}
