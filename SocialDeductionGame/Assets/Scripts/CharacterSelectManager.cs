using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class CharacterSelectManager : NetworkBehaviour
{
    // ============== Refrences ==============

    // ============== Setup ==============
    private void Awake()
    {
        PlayerConnectionManager.OnAllPlayersReady += ProgressState;
    }

    private void OnDisable()
    {
        PlayerConnectionManager.OnAllPlayersReady -= ProgressState;
    }

    // ============== Scene Management ==============
    private void ProgressState()
    {
        if (!IsServer)
            return;

        SceneLoader.LoadNetwork(SceneLoader.Scene.SampleScene);
    }
}
