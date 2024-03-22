using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class PlayerUI : MonoBehaviour
{
    // ================== Variables / Refrences ==================
    #region Variables / Refrences
    private PlayerData _playerData;
    private PlayerHealth _playerHealth;

    [Header("Other")]
    [SerializeField] private GameObject _craftingMenu;
    [SerializeField] private CanvasGroup _introRole;
    [SerializeField] private GameObject _introRoleSaboIcon;
    [SerializeField] private GameObject _introRoleSurvivorIcon;
    #endregion

    // ================== Setup ==================
    #region Setup
    public void OnEnable()
    {
        _playerData = this.GetComponentInParent<PlayerData>();
        _playerHealth = this.GetComponentInParent<PlayerHealth>();

        GameManager.OnStateChange += StateChangeEvent;
        GameManager.OnStateIntro += DisplayRole;
        PlayerHealth.OnDeath += OnDeath;

        TabButtonUI.OnCraftingPressed += ToggleCraft;
    }

    private void Start()
    {
        // These menus have to start active so setup scripts propperly run on them
        _craftingMenu.SetActive(false);
    }

    private void OnDisable()
    {
        GameManager.OnStateChange -= StateChangeEvent;
        GameManager.OnStateIntro -= DisplayRole;
        PlayerHealth.OnDeath -= OnDeath;

        TabButtonUI.OnCraftingPressed -= ToggleCraft;
    }
    #endregion

    

    // ================== Misc UI ==================
    #region Misc UI
    private void OnDeath()
    {
        // Close Menus if player died
        _craftingMenu.SetActive(false);
    }

    public void StateChangeEvent(GameManager.GameState prev, GameManager.GameState current)
    {
        // Close Menus on a state change
        _craftingMenu.SetActive(false);
    }

    // Display Role Intro
    private void DisplayRole()
    {
        _introRole.gameObject.SetActive(true);
        TextMeshProUGUI roleText = _introRole.GetComponentInChildren<TextMeshProUGUI>();

        if (_playerData.GetPlayerTeam() == PlayerData.Team.Survivors)
        {
            roleText.text = "Survivors";
            roleText.color = Color.green;
            _introRoleSurvivorIcon.SetActive(true);
        }
        else if (_playerData.GetPlayerTeam() == PlayerData.Team.Saboteurs)
        {
            roleText.text = "Saboteurs";
            roleText.color = Color.red;
            _introRoleSaboIcon.SetActive(true);
        }

        Sequence IntroRoleSequence = DOTween.Sequence();
        IntroRoleSequence.Append(_introRole.DOFade(1, 0.5f))
          .AppendInterval(3)
          .Append(_introRole.DOFade(0, 0.5f))
          .AppendCallback(() => _introRole.gameObject.SetActive(false));
    }

    // Crafting Open/Close
    public void ToggleCraft()
    {
        if (!_playerHealth.IsLiving())
            return;

        _craftingMenu.SetActive(!_craftingMenu.activeSelf);
    }
    #endregion
}
