using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using DG.Tweening;
using UnityEngine.UI;

public class TotemSlot : NetworkBehaviour, ICardUIPlayable
{
    // ================== Refrences ==================
    [SerializeField] private Transform _tagZone;
    [SerializeField] private Transform _cardZone;
    [SerializeField] private GameObject _tagPref;
    [SerializeField] private Image _runeImg;
    [SerializeField] private Color _runeEmptyColor;
    [SerializeField] private Color _runeActiveColor;
    [SerializeField] private Color _runeWrongColor;
    private TotemSounds _totemSounds;

    // ================== Variables ==================
    [SerializeField] private NetworkVariable<int> _netSaboCard = new(writePerm: NetworkVariableWritePermission.Server);
    [SerializeField] private NetworkVariable<int> _netSurvivorCard = new(writePerm: NetworkVariableWritePermission.Server);
    [SerializeField] private NetworkVariable<bool> _netSlotActive = new(writePerm: NetworkVariableWritePermission.Server);
    [SerializeField] private NetworkVariable<bool> _netCorrectCardAdded = new(writePerm: NetworkVariableWritePermission.Server);
    [SerializeField] private List<CardTag> _currentTags = new();
    private GameObject _cardObj;
    private Totem _totem;

    // ================== Setup ==================
    #region Setup
    public void Setup(Totem totem)
    {
        _totem = totem;

        _totemSounds = this.GetComponentInParent<TotemSounds>();
    }

    [ServerRpc]
    public void TotemDeactivatedServerRpc()
    {
        _netSaboCard.Value = 0;
        _netSurvivorCard.Value = 0;
        _netSlotActive.Value = false;
        _netCorrectCardAdded.Value = false;
        _currentTags.Clear();
        TotemDeactivatedClientRpc();
    }

    [ClientRpc]
    public void TotemDeactivatedClientRpc()
    {
        foreach (Transform child in _tagZone)
            Destroy(child.gameObject);

        if (_cardObj != null)
            Destroy(_cardObj);

        _cardObj = null;

        _runeImg.DOKill();
        _runeImg.DOColor(_runeEmptyColor, 1f);
    }

    [ServerRpc]
    public void TotemActivatedServerRpc()
    {
        _netSurvivorCard.Value = 0;
        if (_netSlotActive.Value) // No card here
            _netCorrectCardAdded.Value = false;
        else
            _netCorrectCardAdded.Value = true;

        TotemActivatedClientRpc();
    }

    [ClientRpc]
    public void TotemActivatedClientRpc()
    {
        if (_cardObj != null)
            Destroy(_cardObj);

        _cardObj = null;

        if (_netSlotActive.Value)
        {
            _runeImg.DOKill();
            _runeImg.DOColor(_runeActiveColor, 1f);
        }
    }
    #endregion

    // ================== Helpers ==================
    #region Helpers
    public bool HasSaboCard()
    {
        return _netSlotActive.Value;
    }

    public bool GetCardSatesfied()
    {
        return _netCorrectCardAdded.Value;
    }
    #endregion

    // ================== Interface ==================
    #region Interface
    public bool CanPlayCardHere(Card cardToPlay)
    {
        // Test if card tags match
        if (_totem.GetTotemActive())
        {
            if (!_netSlotActive.Value)
                return false;
            else if (TestForMatchingTags(cardToPlay.GetCardID()))
            {
                if (_totemSounds)
                    _totemSounds.PlayAddCorrect();

                return true;
            }
            else
            {
                // Flash rune red
                _runeImg.DOKill();
                _runeImg.DOColor(_runeWrongColor, 1f).OnComplete(() => _runeImg.DOColor(_runeActiveColor, 1f));

                if (_totemSounds)
                    _totemSounds.PlayAddIncorrect();

                return false;
            }
        }
        else if (!_totem.GetTotemOnCooldown() && !_totem.GetTotemPrepped())
        {
            if (cardToPlay.HasTag("NonTotemable"))
            {
                if (_totemSounds)
                    _totemSounds.PlayAddIncorrect();

                return false;
            }
            else
            {
                if (_totemSounds)
                    _totemSounds.PlayAddCorrect();

                return true;
            }
        }
        else
        {
            Debug.Log("Totem is inactive / on cooldown / already prepped");
            return false;
        }
    }

    public void PlayCardHere(int cardID)
    {
        Debug.Log($"Card {cardID} played on totem slot");
        AddCard(cardID);
    }
    #endregion

    // ================== Function ==================
    #region Function
    public void AddCard(int cardID)
    {
        if (_totem.GetTotemActive())
            AddCardSurvivorsServerRpc(cardID);
        else
            AddCardSaboServerRpc(cardID);
    }

    [ServerRpc(RequireOwnership = false)]
    private void AddCardSaboServerRpc(int cardID)
    {
        Debug.Log("<color=yellow>SERVER: </color>Adding card to unactivated Totem slot");

        // Add card
        _netSaboCard.Value = cardID;
        _netSlotActive.Value = true;

        _totem.CardAddedToInactiveTotem();

        ShowCardClientRpc(cardID);
        AddTagsClientRpc(cardID);
    }

    [ServerRpc(RequireOwnership = false)]
    private void AddCardSurvivorsServerRpc(int cardID)
    {
        Debug.Log("Adding card to activated Totem");

        if (TestForMatchingTags(cardID))
        {
            // LOCK IN
            _netSurvivorCard.Value = cardID;
            _netCorrectCardAdded.Value = true;
            ShowCardClientRpc(cardID);

            _totem.CardAddedToActiveTotem();
        }
        else
        {
            Debug.Log("Cards did not match! Should not see this message, as match is tested when in CanPlayCardHere function");
            // Show dosn't match message
        }
    }

    [ClientRpc]
    private void ShowCardClientRpc(int cardID)
    {
        Debug.Log("Adding card to activated Totem locally");

        _cardObj = Instantiate(CardDatabase.Instance.GetCard(cardID), _cardZone);
        Card newCard = _cardObj.GetComponent<Card>();
        newCard.SetupUI();
    }

    [ClientRpc]
    private void AddTagsClientRpc(int cardID)
    {
        Debug.Log("Adding card to unactivated Totem slot locally");

        _currentTags.AddRange(ExtractTags(cardID));

        // Instantiate Tags
        foreach (CardTag tag in ExtractTags(cardID))
        {
            Debug.Log("<color=blue>CLIENT: </color>Added new tag to totem " + tag.name);
            TagIcon tagIcon = Instantiate(_tagPref, _tagZone).GetComponent<TagIcon>();
            tagIcon.SetupIcon(tag.visual, tag.Name);
        }
    }

    private List<CardTag> ExtractTags(int cardId)
    {
        return CardDatabase.Instance.GetCard(cardId).GetComponent<Card>().GetCardTags();
    }

    private bool TestForMatchingTags(int cardId)
    {
        // Test to see if tags from given card match tags of saboCard
        foreach (CardTag tag in _currentTags)
        {
            if (!ExtractTags(cardId).Contains(tag))
            {
                Debug.Log($"<color=yellow>SERVER: </color>Given card did not contain tag {tag.name}");
                return false;
            }
        }

        return true;
    }
    #endregion
}
