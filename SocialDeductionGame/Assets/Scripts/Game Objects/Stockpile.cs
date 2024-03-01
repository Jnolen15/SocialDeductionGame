using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using TMPro;

public class Stockpile : NetworkBehaviour, ICardPlayable
{
    // ================== Refrences ==================
    [SerializeField] private GameObject _saboNotif;
    [SerializeField] private GameObject _numCardsPannel;
    [SerializeField] private GameObject _addWasteButton;
    [SerializeField] private TextMeshProUGUI _numCards;
    [SerializeField] private ParticleSystem _dustFX;
    [SerializeField] private PlayRandomSound _randSound;

    // ================== Variables ==================
    [SerializeField] private int _wasteCardID;
    [SerializeField] private bool _acceptingCards;
    [SerializeField] private List<int> _stockpileCardIDs = new();
    [SerializeField] private List<ulong> _contributorIDs = new();
    [SerializeField] private NetworkVariable<int> _netCardsInStockpile = new(writePerm: NetworkVariableWritePermission.Server);
    private PlayerData.Team _localTeam;

    // ================== Setup ==================
    #region Setup
    public override void OnNetworkSpawn()
    {
        _netCardsInStockpile.OnValueChanged += UpdateCardsText;

        GameManager.OnStateIntro += SetSabotuersView;
        GameManager.OnStateMorning += ClearAll;
        GameManager.OnStateMorning += ToggleAcceptingCards;
        GameManager.OnStateAfternoon += ShowAddWasteButton;
        GameManager.OnStateEvening += ToggleAcceptingCards;
    }

    public override void OnDestroy()
    {
        _netCardsInStockpile.OnValueChanged -= UpdateCardsText;

        GameManager.OnStateIntro -= SetSabotuersView;
        GameManager.OnStateMorning -= ClearAll;
        GameManager.OnStateMorning -= ToggleAcceptingCards;
        GameManager.OnStateAfternoon -= ShowAddWasteButton;
        GameManager.OnStateEvening -= ToggleAcceptingCards;

        // Always invoked the base 
        base.OnDestroy();
    }
    #endregion

    // ================== Card Count ==================
    #region Card Count
    private void SetSabotuersView()
    {
        if (PlayerConnectionManager.Instance.GetLocalPlayerTeam() == PlayerData.Team.Saboteurs)
        {
            //_numCardsPannel.SetActive(true);
            _saboNotif.SetActive(true);
            _localTeam = PlayerConnectionManager.Instance.GetLocalPlayerTeam();
        }
    }

    private void UpdateCardsText(int prev, int next)
    {
        _numCards.text = next.ToString();
    }

    public void HideSaboNotif()
    {
        _saboNotif.SetActive(false);
    }
    #endregion

    // ================== Interface ==================
    #region interface
    // Stockpile accepts any card types ATM
    public bool CanPlayCardHere(Card cardToPlay)
    {
        return _acceptingCards;
    }
    #endregion

    // ================== Cards ==================
    #region Cards
    // only accepts cards during afternoon phase
    private void ToggleAcceptingCards()
    {
        _acceptingCards = !_acceptingCards;

        if (!_acceptingCards)
            HideAddWasteButton();
    }

    private void ClearAll()
    {
        _contributorIDs.Clear();
        _stockpileCardIDs.Clear();

        if(IsServer)
            _netCardsInStockpile.Value = 0;
    }

    public void AddCard(int cardID, ulong playerID)
    {
        AddCardsServerRpc(cardID, playerID);
    }

    // Add card and player ID to server
    [ServerRpc(RequireOwnership = false)]
    public void AddCardsServerRpc(int cardID, ulong playerID)
    {
        _stockpileCardIDs.Add(cardID);
        if(!_contributorIDs.Contains(playerID))
            _contributorIDs.Add(playerID);
        _netCardsInStockpile.Value++;

        AddCardsClientRpc();
    }

    [ClientRpc]
    public void AddCardsClientRpc()
    {
        _randSound.PlayRandom();

        _dustFX.Emit(10);
    }
    #endregion

    // ================== Add Waste ==================
    #region Add Waste
    private void ShowAddWasteButton()
    {
        if (_localTeam == PlayerData.Team.Saboteurs)
            _addWasteButton.SetActive(true);
    }

    private void HideAddWasteButton()
    {
        _addWasteButton.SetActive(false);
    }

    public void AddWaste()
    {
        if (!PlayerConnectionManager.Instance.GetLocalPlayerLiving())
            return;

        if (SufferingManager.Instance.GetCurrentSufffering() >= 1)
        {
            SufferingManager.Instance.ModifySuffering(-1, 205, false);
            AddCard(_wasteCardID, PlayerConnectionManager.Instance.GetLocalPlayersID());
        }
    }
    #endregion

    // ================== Helpers ==================
    #region Helpers
    public int GetNumCards()
    {
        return _netCardsInStockpile.Value;
    }

    // Gets and removes the top card of thes stockpile
    public int GetTopCard()
    {
        if (_stockpileCardIDs.Count <= 0)
            return -1;

        int topCard = _stockpileCardIDs[0];
        _stockpileCardIDs.RemoveAt(0);
        _netCardsInStockpile.Value--;

        //Debug.Log("Pop: " + topCard);

        return topCard;
    }

    public ulong[] GetContributorIDs()
    {
        _contributorIDs.Sort(); // Sorts list so it isnt in order of who contributed first
        return _contributorIDs.ToArray();
    }
    #endregion
}
