using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Discard : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ICardUIPlayable
{
    private CanvasGroup _canvasGroup;

    private void Start()
    {
        _canvasGroup = this.GetComponent<CanvasGroup>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _canvasGroup.alpha = 1;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _canvasGroup.alpha = 0.8f;
    }

    // ============== ICardUIPlayable ==============
    #region UIPlayable Interface
    public bool CanPlayCardHere(Card cardToPlay)
    {
        Debug.Log(cardToPlay.GetCardName() + " played to discards");

        return true;
    }

    public void PlayCardHere(int cardID)
    {
        Debug.Log("Card discarded!");
    }
    #endregion
}
