using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class GearSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private int _gearSlot;
    private PlayerCardManager _pcm;

    private void Start()
    {
        _pcm = this.GetComponentInParent<PlayerCardManager>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _pcm.HoveringGearSlot(_gearSlot);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _pcm.EndHoveringGearSlot();
    }
}
