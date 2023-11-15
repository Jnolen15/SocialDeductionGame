using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PauseMenuUI : MonoBehaviour
{
    // =================== Refrences ===================
    [Header("Quit Menu")]
    [SerializeField] private GameObject _quitMenu;

    // =================== Update ===================
    #region Update
    void Update()
    {
        // Quit Menu
        if (Input.GetKeyDown(KeyCode.Escape))
            _quitMenu.SetActive(true);
    }
    #endregion

    // =================== UI Functions ===================
    public void QuitGame()
    {
        Debug.Log("Quitting Game");
        Application.Quit();
    }

    public void ReturnToMainMenu()
    {
        ConnectionManager.Instance.Shutdown();
        VivoxManager.Instance.LeaveAll();

        SceneLoader.Load(SceneLoader.Scene.MainMenu);
    }
}
