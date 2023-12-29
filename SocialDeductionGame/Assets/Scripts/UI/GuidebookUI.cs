using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GuidebookUI : MonoBehaviour
{
    // ================== Refrences ==================
    [SerializeField] private List<GameObject> _pageList;

    // ================== Setup ==================
    private void Start()
    {
        Debug.Log("Guidebook setup");

        TabButtonUI.OnHelpPressed += ToggleGuidebook;

        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        TabButtonUI.OnHelpPressed -= ToggleGuidebook;
    }

    // ================== UI ==================
    #region UI
    private void ToggleGuidebook()
    {
        Debug.Log("Toggle Guidebook");

        if (!gameObject.activeSelf)
            Show();
        else
            Hide();
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void SwitchToPage(int pageNum)
    {
        if(pageNum >= _pageList.Count)
        {
            Debug.LogError("Given page number larger than page count");
            return;
        }

        foreach(GameObject page in _pageList)
        {
            page.SetActive(false);
        }

        _pageList[pageNum].SetActive(true);
    }
    #endregion
}
