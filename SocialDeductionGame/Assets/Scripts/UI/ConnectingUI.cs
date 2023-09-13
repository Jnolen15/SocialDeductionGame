using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConnectingUI : MonoBehaviour
{
    void Start()
    {
        RelayTest.OnTryingToJoinGame += Show;
        RelayTest.OnFailedToJoinGame += Hide;
        Hide();
    }

    private void OnDestroy()
    {
        RelayTest.OnTryingToJoinGame -= Show;
        RelayTest.OnFailedToJoinGame -= Hide;
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
