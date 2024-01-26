using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GuidebookUI : MonoBehaviour
{
    // ================== Refrences ==================
    [SerializeField] private GameObject _uiElements;
    [SerializeField] private GameObject _prevButton;
    [SerializeField] private GameObject _nextButton;
    [SerializeField] private List<GameObject> _pageList;
    [SerializeField] private int _currentPage;
    [SerializeField] private PlayRandomSound _randomBookSound;
    [SerializeField] private PlayRandomSound _randomPageSound;

    // ================== Setup ==================
    private void Start()
    {
        Debug.Log("Guidebook setup");

        TabButtonUI.OnHelpPressed += ToggleGuidebook;
        GameManager.OnStateChange += StateClose;

        _uiElements.SetActive(false);
    }

    private void OnDestroy()
    {
        TabButtonUI.OnHelpPressed -= ToggleGuidebook;
        GameManager.OnStateChange -= StateClose;
    }

    // ================== UI ==================
    #region UI
    private void ToggleGuidebook()
    {
        Debug.Log("Toggle Guidebook");

        if (!_uiElements.activeSelf)
            Show();
        else
            Hide();
    }

    private void StateClose(GameManager.GameState prev, GameManager.GameState cur)
    {
        Hide();
    }

    public void Show()
    {
        _uiElements.SetActive(true);

        _randomBookSound.PlayRandom();
    }

    public void Hide()
    {
        _randomBookSound.PlayRandom();

        _uiElements.SetActive(false);
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

        _randomPageSound.PlayRandom();
    }
    #endregion
}
