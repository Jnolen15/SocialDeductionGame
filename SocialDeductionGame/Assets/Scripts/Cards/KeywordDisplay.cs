using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class KeywordDisplay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private RectTransform _displayZone;
    [SerializeField] private GameObject _keywordPopup;
    private List<KeywordSO> _keywords = new();
    private List<GameObject> _popups = new();
    IKeywordKeeper _keywordKeeper;
    private bool _initialized;

    // ============= Setup =============
    private void Start()
    {
        _keywordKeeper = GetComponentInParent<IKeywordKeeper>();
    }

    private void Update()
    {
        if (!_initialized)
            Initialize();
    }

    private void Initialize()
    {
        if (_keywordKeeper != null)
        {
            if (_keywordKeeper.GetKeywords() != null)
                _keywords = _keywordKeeper.GetKeywords();
            else
                return;
        }
        else
        {
            Debug.LogError("Keyword Keeper not found!");
            return;
        }

        foreach (KeywordSO keyword in _keywords)
        {
            var popup = Instantiate(_keywordPopup, _displayZone);
            popup.GetComponent<KeywordPopup>().Setup(keyword.KeywordName, keyword.KeywordDescription);
            _popups.Add(popup);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(_displayZone);

        HidePopups();

        _initialized = true;
    }

    // ============= Interfaces =============
    public void OnPointerEnter(PointerEventData eventData)
    {
        ShowPopups();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        HidePopups();
    }

    // ============= Functions =============
    private void HidePopups()
    {
        foreach (GameObject popup in _popups)
        {
            popup.SetActive(false);
        }
    }

    private void ShowPopups()
    {
        foreach (GameObject popup in _popups)
        {
            popup.SetActive(true);
        }
    }
}
