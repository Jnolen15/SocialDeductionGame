using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Gear : Card
{
    // ========== Gear Functions ==========
    public virtual void OnEquip()
    {
        Debug.Log($"CARD {_cardName} EQUIPPED");
    }

    public virtual void OnUnequip()
    {
        Debug.Log($"CARD {_cardName} UNEQUIPPED");
    }

    public virtual void OnUse()
    {
        Debug.Log($"CARD {_cardName} USED");
    }

    // ========== Override Functions ==========
    public override void OnPlay(GameObject playLocation)
    {
        Debug.Log("Played a gear card. Nothing happened");
    }
}
