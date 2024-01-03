using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public abstract class LimitedTimeObject : NetworkBehaviour
{
    [SerializeField] protected int _life;

    // ========== OVERRIDE CLASSES ==========
    public virtual void SetupLTO(int life)
    {
        _life = life;
    }

    public virtual bool CoutdownLife()
    {
        _life--;

        if (_life <= 0)
            return true;
        else
            return false;
    }

    public virtual void DestroySelf()
    {
        Destroy(this.gameObject);
    }
}