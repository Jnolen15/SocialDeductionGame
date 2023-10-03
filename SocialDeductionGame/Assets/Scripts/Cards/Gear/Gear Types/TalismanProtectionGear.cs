using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TalismanProtectionGear : Gear
{
    // ========== Talisman Details ==========

    // ========== Override Functions ==========
    public override void OnUse()
    {
        Debug.Log("Talisman of protection used!");
    }
}
