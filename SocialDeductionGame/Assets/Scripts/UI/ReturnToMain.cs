using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReturnToMain : MonoBehaviour
{
    public void ReturnToMainMenu()
    {
        LobbyManager.Instance.DisconnectFromLobby();
        VivoxManager.Instance.LeaveAll();

        SceneLoader.Load(SceneLoader.Scene.MainMenu);
    }
}
