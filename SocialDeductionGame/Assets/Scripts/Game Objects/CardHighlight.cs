using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardHighlight : MonoBehaviour
{
    // ================== Refrences / Variables ==================
    [SerializeField] private GameObject _highlight;
    [SerializeField] private List<CardTag> _playableTag;
    [SerializeField] private bool _acceptAnyTag;
    [SerializeField] private List<GameManager.GameState> _states;
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

    // ================== Changing ==================
    public void UpdateCardTags(List<CardTag> newTags)
    {
        _playableTag.Clear();
        _playableTag.AddRange(newTags);
    }

    // ================== Function ==================
    private void OnCardHighlighted(Card cardHighlighted)
    {
        //Debug.Log("OnCardHighlighted", gameObject);

        if (!cardHighlighted.HasAnyTag(_playableTag) && !_acceptAnyTag)
            return;

        if (_acceptAnyState)
            _highlight.SetActive(true);
        else
        {
            foreach(GameManager.GameState state in _states)
            {
                if(state == GameManager.Instance.GetCurrentGameState())
                {
                    _highlight.SetActive(true);
                    break;
                }
            }
        }
    }

    private void OnCardUnhighlighted(Card cardHighlighted)
    {
        _highlight.SetActive(false);
    }
}
