using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LobbyUI : MonoBehaviour
{
    // ============== Refrences ==============
    [SerializeField] private TMP_InputField _joinCode;

    // ============== Functions ==============
    public void QuickJoin()
    {
        LobbyManager.Instance.QuickJoin();
    }

    public void JoinWithCode()
    {
        LobbyManager.Instance.JoinWithCode(_joinCode.text);
    }
}
