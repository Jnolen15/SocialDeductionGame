using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerNamer : MonoBehaviour
{
    // ============== Refrences ==============
    [SerializeField] private List<string> _randNameList = new List<string>();

    private const string KEY_PLAYERNAME = "KeyPlayerName";

    // ============== Setup ==============
    void Awake()
    {
        string playerName = PlayerPrefs.GetString(KEY_PLAYERNAME, "defaultName");

        if(playerName == "defaultName")
        {
            PlayerPrefs.SetString(KEY_PLAYERNAME, GetRandomName());
        }
    }

    // ============== Function ==============
    private string GetRandomName()
    {
        if (_randNameList.Count <= 1)
            return "John Locke";

        int rand = Random.Range(0, _randNameList.Count);
        return _randNameList[rand];
    }

    public void SetPlayerName(string newName)
    {
        PlayerPrefs.SetString(KEY_PLAYERNAME, newName);
    }

    public string GetPlayerName()
    {
        return PlayerPrefs.GetString(KEY_PLAYERNAME);
    }
}
