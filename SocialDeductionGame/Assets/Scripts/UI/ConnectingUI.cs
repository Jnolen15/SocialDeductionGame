using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConnectingUI : MonoBehaviour
{
    void Start()
    {
        ConnectionManager.OnTryingToJoinGame += Show;
        ConnectionManager.OnFailedToJoinGame += Hide;
        Hide();
    }

    private void OnDestroy()
    {
        ConnectionManager.OnTryingToJoinGame -= Show;
        ConnectionManager.OnFailedToJoinGame -= Hide;
    }

    private void Show()
    {
        gameObject.SetActive(true);
    }

    private void Hide()
    {
        gameObject.SetActive(false);
    }
}
