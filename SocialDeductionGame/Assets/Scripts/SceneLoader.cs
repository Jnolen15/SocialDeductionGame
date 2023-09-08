using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneLoader
{
    // Code inspired from CodeMonkey https://www.youtube.com/watch?v=7glCsF9fv3s&t=22017s

    public enum Scene
    {
        LobbyScene,
        CharacterSelectScene,
        SampleScene
    }

    public static void Load(Scene targetScene)
    {
        SceneManager.LoadScene(targetScene.ToString());
    }

    // when loading scenes during a live networked multiplayer game, must use this method
    public static void LoadNetwork(Scene targetScene)
    {
        NetworkManager.Singleton.SceneManager.LoadScene(targetScene.ToString(), LoadSceneMode.Single);
    }
}
