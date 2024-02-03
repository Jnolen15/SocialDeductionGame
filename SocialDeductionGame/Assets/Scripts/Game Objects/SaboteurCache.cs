using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using DG.Tweening;
using Unity.Services.Analytics;

public class SaboteurCache : LimitedTimeObject, ICardPicker
{
    // ========================= Refrences / Variables =========================
    [SerializeField] private GameObject _openButton;
    [SerializeField] private GameObject _pickCards;
    [SerializeField] private Transform _cardZone;
    [SerializeField] private GameObject _closedObj;
    [SerializeField] private GameObject _openedObj;
    [SerializeField] private ParticleSystem _dustFX;
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _openSound;
    [SerializeField] private CardDropTable _cardDropTable = new CardDropTable();
    private bool _opened;
    private PlayerData.Team _localTeam;
    private CardManager _cardManager;

    // ========================= Setup =========================
    #region Setup
    void OnValidate()
    {
        _cardDropTable.ValidateTable();
    }

    private void Start()
    {
        _cardDropTable.ValidateTable();
        _cardManager = GameObject.FindGameObjectWithTag("CardManager").GetComponent<CardManager>();

        _localTeam = PlayerConnectionManager.Instance.GetLocalPlayerTeam();
        if (_localTeam == PlayerData.Team.Saboteurs)
            _openButton.SetActive(true);
    }
    #endregion

    // ========================= Function =========================
    #region Open Cache
    public void AttemptOpen()
    {
        if (_localTeam != PlayerData.Team.Saboteurs || _opened)
            return;

        if (!PlayerConnectionManager.Instance.GetLocalPlayerLiving())
            return;

        if (SufferingManager.Instance.GetCurrentSufffering() >= 2)
        {
            SufferingManager.Instance.ModifySuffering(-2, 204, false);
            OpenCacheServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void OpenCacheServerRpc(ServerRpcParams serverRpcParams = default)
    {
        // Get client data
        ulong clientID = serverRpcParams.Receive.SenderClientId;
        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { clientID }
            }
        };

        // Track analytics
        int curDay = GameManager.Instance.GetCurrentDay();
        AnalyticsTracker.Instance.TrackCacheOpen(curDay);

        OpenCacheClientRpc(clientRpcParams);
        SetCacheOpenedClientRpc();
    }

    [ClientRpc]
    private void SetCacheOpenedClientRpc()
    {
        _opened = true;
        _openButton.SetActive(false);
        _closedObj.SetActive(false);
        _openedObj.SetActive(true);

        _audioSource.PlayOneShot(_openSound);
        _dustFX.Emit(15);
    }
    #endregion

    #region PickCards
    [ClientRpc]
    private void OpenCacheClientRpc(ClientRpcParams clientRpcParams = default)
    {
        _pickCards.SetActive(true);
        ForageUI.HideForageUI?.Invoke();
        DealCards();
    }

    public void DealCards()
    {
        Debug.Log(gameObject.name + " Dealing Cache cards");

        int numToDeal = 3;

        List<GameObject> cardObjList = ChooseCardsUnique(numToDeal);

        //for (int i = 0; i < (numToDeal); i++)
        //    cardObjList.Add(ChooseCard());

        DealCardObjects(cardObjList);
    }

    private List<GameObject> ChooseCardsUnique(int numToPick)
    {
        List<GameObject> cards = new();

        // Pick and deal random card
        List<int> cardIds = _cardDropTable.PickCardDropsUnique(numToPick);

        // Put card on screen
        foreach (int cardID in cardIds)
        {
            GameObject cardObj = Instantiate(CardDatabase.Instance.GetCard(cardID), _cardZone);
            cardObj.GetComponent<Card>().SetupSelectable();
            cards.Add(cardObj);
        }

        return cards;
    }

    private GameObject ChooseCard()
    {
        int cardID;

        // Pick and deal random card
        cardID = _cardDropTable.PickCardDrop();
        Debug.Log("Picked Cache Card " + cardID);

        // Put card on screen
        GameObject cardObj = Instantiate(CardDatabase.Instance.GetCard(cardID), transform);
        cardObj.GetComponent<Card>().SetupSelectable();

        return cardObj;
    }

    public void DealCardObjects(List<GameObject> cardObjs)
    {
        List<CanvasGroup> canvasCards = new();
        foreach (GameObject cardObj in cardObjs)
        {
            cardObj.transform.SetParent(_cardZone);
            cardObj.transform.localScale = Vector3.one;

            CanvasGroup cardCanvas = cardObj.GetComponentInChildren<CanvasGroup>();
            cardCanvas.alpha = 0;
            cardCanvas.blocksRaycasts = false;
            canvasCards.Add(cardCanvas);
        }

        StartCoroutine(DealCardObjectsAnimated(canvasCards));
    }

    private IEnumerator DealCardObjectsAnimated(List<CanvasGroup> cardObjs)
    {
        foreach (CanvasGroup cardObj in cardObjs)
        {
            cardObj.DOFade(1, 0.2f);
            PunchCard(cardObj.gameObject, 0.2f, 0.4f);

            yield return new WaitForSeconds(0.1f);
        }

        foreach (CanvasGroup cardObj in cardObjs)
            cardObj.blocksRaycasts = true;
    }

    public void ClearCards()
    {
        foreach (Transform child in _cardZone)
        {
            Destroy(child.gameObject);
        }
    }

    public void PunchCard(GameObject card, float size, float duration)
    {
        card.transform.DOKill();
        card.transform.DOPunchScale(new Vector3(size, size, size), duration, 6, 0.8f);
    }
    #endregion

    #region ICardPicker
    public void PickCard(Card card)
    {
        // Give cards to Card Manager
        _cardManager.GiveCard(card.GetCardID());

        ClearCards();
        _pickCards.SetActive(false);
        ForageUI.ShowForageUI?.Invoke();
    }
    #endregion
}
