using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Notebook : MonoBehaviour
{
    // ================= Refrences =================
    [SerializeField] private GameObject _notebook;
    [SerializeField] private Transform _nameEntryZone;
    [SerializeField] private GameObject _nameEntryPref;

    // ================= Setup =================
    private void Start()
    {
        GameManager.OnStateIntro += Setup;
        TabButtonUI.OnNotebookPressed += Show;
    }

    private void OnDestroy()
    {
        GameManager.OnStateIntro -= Setup;
        TabButtonUI.OnNotebookPressed -= Show;
    }

    private void Setup()
    {
        foreach (ulong pID in PlayerConnectionManager.Instance.GetPlayerIDs())
        {
            NotebookNameEntry newEntry = Instantiate(_nameEntryPref, _nameEntryZone).GetComponent<NotebookNameEntry>();
            newEntry.Setup(PlayerConnectionManager.Instance.GetPlayerNameByID(pID));
        }
    }

    // ================= Function =================
    public void Show()
    {
        _notebook.SetActive(true);
    }

    public void Hide()
    {
        _notebook.SetActive(false);
    }
}
