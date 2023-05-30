using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PlayerController : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        if (!IsOwner) Destroy(this);
    }

    void Update()
    {
        
    }
}
