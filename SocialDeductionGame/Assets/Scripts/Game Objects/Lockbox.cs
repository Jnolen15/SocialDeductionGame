using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Lockbox : LimitedTimeObject
{
    // Start is called before the first frame update
    void Start()
    {
        if (IsServer)
        {
            //Forage.OnInjectCards(LocationManager.LocationName.Beach, 1001, 2)?.Invoke();
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
