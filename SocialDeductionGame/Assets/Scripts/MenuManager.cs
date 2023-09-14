using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class MenuManager : MonoBehaviour
{
    // ============== Refrences ==============


    // ============== Setup ==============
    private void Awake()
    {
        // Cleanup
        if (ConnectionManager.Instance != null)
            Destroy(ConnectionManager.Instance.gameObject);
        if (LobbyManager.Instance != null)
            Destroy(LobbyManager.Instance.gameObject);
        if (PlayerConnectionManager.Instance != null)
            Destroy(PlayerConnectionManager.Instance.gameObject);
    }

    // ============== Functions ==============
    public void Play()
    {
        Debug.Log("Loading Lobby Scene");
        SceneLoader.Load(SceneLoader.Scene.LobbyScene);
    }

    public void Quit()
    {
        Application.Quit();
    }
}
