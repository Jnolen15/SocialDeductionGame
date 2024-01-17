using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GuidebookUI : MonoBehaviour
{
    // ================== Refrences ==================
    [SerializeField] private GameObject _prevButton;
    [SerializeField] private GameObject _nextButton;
    [SerializeField] private List<GameObject> _pageList;
    [SerializeField] private int _currentPage;

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

    public void LastPage()
    {
        _currentPage--;

        if (_currentPage <= 0)
            _currentPage = 0;

        SwitchToPage(_currentPage);
    }

    public void NextPage()
    {
        _currentPage++;

        if (_currentPage >= _pageList.Count-1)
            _currentPage = _pageList.Count-1;

        SwitchToPage(_currentPage);
    }


    public void SwitchToPage(int pageNum)
    {
        if(pageNum >= _pageList.Count)
        {
            Debug.LogError("Given page number larger than page count");
            return;
        }

        _currentPage = pageNum;

        // Update buttons
        _prevButton.SetActive(true);
        _nextButton.SetActive(true);
        if (pageNum == 0)
            _prevButton.SetActive(false);
        else if (pageNum == _pageList.Count-1)
            _nextButton.SetActive(false);

        // Update Pages
        foreach (GameObject page in _pageList)
        {
            page.SetActive(false);
        }

        _pageList[pageNum].SetActive(true);
    }
    #endregion
}
