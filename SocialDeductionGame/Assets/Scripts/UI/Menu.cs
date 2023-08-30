using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;

public class Menu : NetworkBehaviour
{
    public void QuitGame()
    {
        Debug.Log("Quitting Game");
        Application.Quit();
    }

    public void RestartGame()
    {
        if(IsServer)
            NetworkManager.Singleton.Shutdown();

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
