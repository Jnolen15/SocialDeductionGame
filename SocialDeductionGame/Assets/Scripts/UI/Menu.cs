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

    public void ReturnToMenu()
    {
        ConnectionManager.Instance.Shutdown();

        SceneLoader.Load(SceneLoader.Scene.MainMenu);
    }
}
