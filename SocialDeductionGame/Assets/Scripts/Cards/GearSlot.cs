using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class GearSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ICardUIPlayable
{
    [SerializeField] private int _gearSlot;
    [SerializeField] private GameObject _pocketClosed;
    [SerializeField] private GameObject _pocketOpenFront;
    [SerializeField] private GameObject _pocketOpenBack;
    [SerializeField] private Transform _cardZone;
    private PlayerCardManager _pcm;

    private void Start()
    {
        _pcm = this.GetComponentInParent<PlayerCardManager>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        //_pcm.HoveringGearSlot(_gearSlot);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        //_pcm.EndHoveringGearSlot();
    }

    public Gear EqipGearCard(int gearID)
    {
        OpenPocketVisuals();

        GameObject newGear = Instantiate(CardDatabase.Instance.GetCard(gearID), _cardZone);
        Gear newGearCard = newGear.GetComponent<Gear>();

        newGearCard.SetupUI();

        return newGearCard;
    }

    public void Unequip(int gearID)
    {
        ClosedPocketVisuals();

        _pcm.UnequipGear(_gearSlot, gearID);
    }

    private void OpenPocketVisuals()
    {
        _pocketClosed.SetActive(false);
        _pocketOpenBack.SetActive(true);
        _pocketOpenFront.SetActive(true);
    }

    private void ClosedPocketVisuals()
    {
        _pocketClosed.SetActive(true);
        _pocketOpenBack.SetActive(false);
        _pocketOpenFront.SetActive(false);
    }

    // ============== ICardUIPlayable ==============
    public bool CanPlayCardHere(Card cardToPlay)
    {
        if (cardToPlay.HasTag("Gear"))
        {
            return true;
        }
        else
        {
            Debug.Log("Card is not gear, can't equip");
            return false;
        }
    }

    public void PlayCardHere(int cardID)
    {
        _pcm.EquipGear(_gearSlot, cardID);
    }
}
