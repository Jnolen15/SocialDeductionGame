using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReturnToMain : MonoBehaviour
{
    public void ReturnToMainMenu()
    {
        VivoxManager.Instance.LeaveAll();
        LobbyManager.Instance.DisconnectFromLobby();

        SceneLoader.Load(SceneLoader.Scene.MainMenu);
    }
}
