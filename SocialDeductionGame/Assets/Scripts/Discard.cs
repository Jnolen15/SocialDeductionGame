using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Discard : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private PlayerCardManager _pcm;

    private void Start()
    {
        _pcm = this.GetComponentInParent<PlayerCardManager>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _pcm.EnableDiscard();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _pcm.DisableDiscard();
    }
}
