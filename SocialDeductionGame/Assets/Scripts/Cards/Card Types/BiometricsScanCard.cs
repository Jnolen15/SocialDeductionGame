using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BiometricsScanCard : Card
{
    public override void OnPlay(GameObject playLocation)
    {
        BiometricsScanFunction bsf = GameObject.FindGameObjectWithTag("Tools").GetComponent<BiometricsScanFunction>();
        PlayerData playerData = playLocation.GetComponentInParent<PlayerData>();

        if (playerData != null && bsf != null)
        {
            ulong pId = playerData.GetPlayerID();

            Debug.Log("Biometrics scan used on player " + pId);

            bsf.StartScan(pId);
        }
        else
        {
            Debug.LogError("Card was played on a location it can't do anything with");
        }
    }
}
