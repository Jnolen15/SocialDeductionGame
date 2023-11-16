using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class TabButtonUI : MonoBehaviour
{
    // ================== Variables / Refrences ==================
    #region Variables / Refrences
    [Header("Tab Positions")]
    [SerializeField] private float _hiddenPos;
    [SerializeField] private float _minimizedPos;
    [SerializeField] private float _shownPos;
    [SerializeField] private float _extendedPos;
    [Header("Hover Timer")]
    [SerializeField] private float _bufferTimerMax;
    private float _bufferTimer;
    private bool _hovering;
    private bool _minimized;
    [Header("Tabs")]
    [SerializeField] private GameObject _exileTab;
    [SerializeField] private GameObject _mapTab;
    [SerializeField] private GameObject _craftingTab;
    [SerializeField] private GameObject _helpTab;
    private bool _exileTabHidden;
    private bool _mapTabHidden;
    private bool _craftingTabHidden;
    private bool _helpTabHidden;

    public delegate void TabPressedAction();
    public static event TabPressedAction OnExilePressed;
    public static event TabPressedAction OnMapPressed;
    public static event TabPressedAction OnCraftingPressed;
    public static event TabPressedAction OnHelpPressed;
    #endregion

    // ================== Setup ==================
    #region Setup
    public void OnEnable()
    {
        GameManager.OnStateEvening += ShowExileButton;
        GameManager.OnStateNight += HideExileButton;

        GameManager.OnStateMorning += ShowMapButton;
        GameManager.OnStateAfternoon += HideMapButton;

        GameManager.OnStateMorning += ShowCraftingButton;
        GameManager.OnStateEvening += HideCraftingButton;

        GameManager.OnStateMorning += ShowHelpButton;
        GameManager.OnStateNight += HideHelpButton;
    }

    private void Start()
    {
        HideExileButton();
        HideMapButton();
        HideCraftingButton();
        HideHelpButton();
    }

    public void OnDisable()
    {
        GameManager.OnStateEvening -= ShowExileButton;
        GameManager.OnStateNight -= HideExileButton;

        GameManager.OnStateMorning -= ShowMapButton;
        GameManager.OnStateAfternoon -= HideMapButton;

        GameManager.OnStateMorning -= ShowCraftingButton;
        GameManager.OnStateEvening -= HideCraftingButton;

        GameManager.OnStateMorning -= ShowHelpButton;
        GameManager.OnStateNight -= HideHelpButton;
    }
    #endregion

    // ================== Interaction ==================
    #region Interaction
    private void MouseEnter(Transform tab)
    {
        _hovering = true;

        MaximizeActiveTabs(tab);
    }

    public void MouseExit()
    {
        if (_minimized)
            return;

        _hovering = false;
        _bufferTimer = _bufferTimerMax;
    }

    private void Update()
    {
        if (_hovering)
            return;

        if (_bufferTimer > 0)
            _bufferTimer -= Time.deltaTime;
        else if (!_minimized)
            MinimizeActiveTabs();
    }
    #endregion

    // ================== Tab Positions ==================
    #region Tab Positions
    private void MaximizeActiveTabs(Transform tab)
    {
        _minimized = false;

        if (!_exileTabHidden)
        {
            if(_exileTab.transform == tab)
                ExtendTab(_exileTab.transform);
            else
                ShowTab(_exileTab.transform);
        }
        if (!_mapTabHidden)
        {
            if (_mapTab.transform == tab)
                ExtendTab(_mapTab.transform);
            else
                ShowTab(_mapTab.transform);
        }
        if (!_craftingTabHidden)
        {
            if (_craftingTab.transform == tab)
                ExtendTab(_craftingTab.transform);
            else
                ShowTab(_craftingTab.transform);
        }
        if (!_helpTabHidden)
        {
            if (_helpTab.transform == tab)
                ExtendTab(_helpTab.transform);
            else
                ShowTab(_helpTab.transform);
        }
    }

    private void MinimizeActiveTabs()
    {
        _minimized = true;

        if (!_exileTabHidden)
            MinimizeTab(_exileTab.transform);
        if (!_mapTabHidden)
            MinimizeTab(_mapTab.transform);
        if (!_craftingTabHidden)
            MinimizeTab(_craftingTab.transform);
        if (!_helpTabHidden)
            MinimizeTab(_helpTab.transform);
    }

    private void HideTab(Transform tab)
    {
        tab.DOKill();
        tab.DOLocalMoveX(_hiddenPos, 0.4f).SetEase(Ease.OutSine);
    }

    private void MinimizeTab(Transform tab)
    {
        tab.DOKill();
        tab.DOLocalMoveX(_minimizedPos, 0.4f).SetEase(Ease.OutSine);
    }

    private void ShowTab(Transform tab)
    {
        tab.DOKill();
        tab.DOLocalMoveX(_shownPos, 0.1f).SetEase(Ease.OutSine);
    }

    private void ExtendTab(Transform tab)
    {
        tab.DOKill();
        tab.DOLocalMoveX(_extendedPos, 0.1f).SetEase(Ease.OutSine);
    }
    #endregion

    // ================== Exile ==================
    #region Exile
    private void ShowExileButton()
    {
        MinimizeTab(_exileTab.transform);
        _exileTabHidden = false;
    }

    private void HideExileButton()
    {
        HideTab(_exileTab.transform);
        _exileTabHidden = true;
    }

    public void ExileHovered()
    {
        MouseEnter(_exileTab.transform);
    }

    public void ExilePressed()
    {
        if (_exileTabHidden)
            return;

        Debug.Log("Exile button pressed");
        OnExilePressed?.Invoke();
    }
    #endregion

    // ================== Map ==================
    #region Map
    private void ShowMapButton()
    {
        MinimizeTab(_mapTab.transform);
        _mapTabHidden = false;
    }

    private void HideMapButton()
    {
        HideTab(_mapTab.transform);
        _mapTabHidden = true;
    }

    public void MapHovered()
    {
        MouseEnter(_mapTab.transform);
    }

    public void MapPressed()
    {
        if (_mapTabHidden)
            return;

        Debug.Log("Map button pressed");
        OnMapPressed?.Invoke();
    }
    #endregion

    // ================== Crafting ==================
    #region Crafting
    private void ShowCraftingButton()
    {
        MinimizeTab(_craftingTab.transform);
        _craftingTabHidden = false;
    }

    private void HideCraftingButton()
    {
        HideTab(_craftingTab.transform);
        _craftingTabHidden = true;
    }

    public void CraftingHovered()
    {
        MouseEnter(_craftingTab.transform);
    }

    public void CraftingPressed()
    {
        if (_craftingTabHidden)
            return;

        Debug.Log("Crafting button pressed");
        OnCraftingPressed?.Invoke();
    }
    #endregion

    // ================== Help ==================
    #region Help
    private void ShowHelpButton()
    {
        MinimizeTab(_helpTab.transform);
        _helpTabHidden = false;
    }

    private void HideHelpButton()
    {
        HideTab(_helpTab.transform);
        _helpTabHidden = true;
    }

    public void HelpHovered()
    {
        MouseEnter(_helpTab.transform);
    }

    public void HelpPressed()
    {
        if (_helpTabHidden)
            return;

        Debug.Log("Map button pressed");
        OnHelpPressed?.Invoke();
    }
    #endregion
}
