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
    [SerializeField] private TextMeshProUGUI _numCards;
    [SerializeField] private ParticleSystem _dustFX;
    [SerializeField] private PlayRandomSound _randSound;

    // ================== Variables ==================
    [SerializeField] private bool _acceptingCards;
    [SerializeField] private List<int> _stockpileCardIDs = new();
    [SerializeField] private List<ulong> _contributorIDs = new();
    [SerializeField] private NetworkVariable<int> _netCardsInStockpile = new(writePerm: NetworkVariableWritePermission.Server);

    // ================== Setup ==================
    public override void OnNetworkSpawn()
    {
        _netCardsInStockpile.OnValueChanged += UpdateCardsText;

        GameManager.OnStateIntro += SetNumVisible;
        GameManager.OnStateMorning += ClearAll;
        GameManager.OnStateMorning += ToggleAcceptingCards;
        GameManager.OnStateEvening += ToggleAcceptingCards;
    }

    public override void OnDestroy()
    {
        _netCardsInStockpile.OnValueChanged -= UpdateCardsText;

        GameManager.OnStateIntro -= SetNumVisible;
        GameManager.OnStateMorning -= ClearAll;
        GameManager.OnStateMorning -= ToggleAcceptingCards;
        GameManager.OnStateEvening -= ToggleAcceptingCards;

        // Always invoked the base 
        base.OnDestroy();
    }

    // ================== Text ==================
    private void SetNumVisible()
    {
        if (PlayerConnectionManager.Instance.GetLocalPlayerTeam() == PlayerData.Team.Saboteurs)
        {
            _numCardsPannel.SetActive(true);
            _saboNotif.SetActive(true);
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

    // ================== Interface ==================
    // Stockpile accepts any card types ATM
    public bool CanPlayCardHere(Card cardToPlay)
    {
        return _acceptingCards;
    }


    // ================== Cards ==================
    #region Cards
    // only accepts cards during afternoon phase
    private void ToggleAcceptingCards()
    {
        _acceptingCards = !_acceptingCards;
    }

    private void ClearAll()
    {
        _contributorIDs.Clear();
        _stockpileCardIDs.Clear();
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
