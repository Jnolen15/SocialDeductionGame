using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class EventBonus : ScriptableObject
{
    // ========== OVERRIDE CLASSES ==========
    public abstract void InvokeBonus(GameObject player = null);
}
