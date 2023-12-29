using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardHighlight : MonoBehaviour
{
    // ================== Refrences / Variables ==================
    [SerializeField] private GameObject _highlight;
    [SerializeField] private List<CardTag> _playableTag;
    [SerializeField] private bool _acceptAnyTag;
    [SerializeField] private GameManager.GameState _state;
    [SerializeField] private bool _acceptAnyState;
    private bool _setup;

    // ================== Setup ==================
    private void OnEnable()
    {
        if (!_setup)
            Setup();
    }

    private void Setup()
    {
        CardInteraction.OnCardHighlighted += OnCardHighlighted;
        CardInteraction.OnCardUnhighlighted += OnCardUnhighlighted;

        _setup = true;
    }

    private void OnDestroy()
    {
        CardInteraction.OnCardHighlighted -= OnCardHighlighted;
        CardInteraction.OnCardUnhighlighted -= OnCardUnhighlighted;
    }

    // ================== Function ==================
    private void OnCardHighlighted(Card cardHighlighted)
    {
        Debug.Log("OnCardHighlighted", gameObject);

        if (!cardHighlighted.HasAnyTag(_playableTag) && !_acceptAnyTag)
            return;

        if (GameManager.Instance.GetCurrentGameState() != _state && !_acceptAnyState)
            return;

        _highlight.SetActive(true);
    }

    private void OnCardUnhighlighted(Card cardHighlighted)
    {
        if (!cardHighlighted.HasAnyTag(_playableTag) && !_acceptAnyTag)
            return;

        if (GameManager.Instance.GetCurrentGameState() != _state && !_acceptAnyState)
            return;

        _highlight.SetActive(false);
    }
}
