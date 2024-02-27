using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class BiometricsScanFunction : NetworkBehaviour
{
    // Used by the Biometrics Scanner Card
    public void StartScan(ulong playerID)
    {
        StartScanServerRpc(playerID);
    }

    [ServerRpc(RequireOwnership = false)]
    private void StartScanServerRpc(ulong playerToScan, ServerRpcParams serverRpcParams = default)
    {
        var clientId = serverRpcParams.Receive.SenderClientId;
        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { clientId }
            }
        };

        PlayerData.Team team = PlayerConnectionManager.Instance.GetPlayerTeamByID(playerToScan);
        string name = PlayerConnectionManager.Instance.GetPlayerNameByID(playerToScan);

        DisplayResultsClientRpc(team.ToString(), name, clientRpcParams);
    }

    [ClientRpc]
    private void DisplayResultsClientRpc(string pTeam, string pName, ClientRpcParams clientRpcParams = default)
    {
        NotificationManager.Instance.SendNotification($"{pName} is on team {pTeam}.", "Scan", true);
    }
}
