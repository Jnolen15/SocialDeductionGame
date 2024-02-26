using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LockboxKeyCard : Card
{
    public override void OnPlay(GameObject playLocation)
    {
        Lockbox lockbox = playLocation.GetComponent<Lockbox>();

        if (lockbox != null)
        {
            lockbox.RemoveLock();
        }
        else
        {
            Debug.LogError("Key card was played on a location it can't do anything with");
        }
    }
}
