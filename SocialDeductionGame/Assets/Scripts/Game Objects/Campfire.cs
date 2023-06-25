using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using TMPro;

public class Campfire : NetworkBehaviour, ICardPlayable
{
    // Variables
    [SerializeField] private Card.CardType _cardTypeAccepted;
    [SerializeField] private NetworkVariable<float> _netServingsStored = new(writePerm: NetworkVariableWritePermission.Server);
    public enum State
    {
        Extinguished,
        Cooking,
        FoodReady
    }
    [SerializeField] private State _state;

    // Refrences
    [SerializeField] private TextMeshPro _servingsText;
    [SerializeField] private TextMeshPro _stateText;
    [SerializeField] private GameObject _foodMenu;
    private CardManager _cardManager;


    // ================== Setup ==================
    private void Awake()
    {
        _netServingsStored.OnValueChanged += UpdateServingsText;
        GameManager.OnStateMorning += SetStateExtingushed;
        GameManager.OnStateAfternoon += SetStateCooking;
        GameManager.OnStateEvening += SetStateFoodReady;
        GameManager.OnStateNight += SetStateExtingushed;
    }

    public override void OnNetworkSpawn()
    {
        _servingsText.text = "Servings: " + _netServingsStored.Value;
    }

    private void OnEnable()
    {
        _cardManager = GameObject.FindGameObjectWithTag("CardManager").GetComponent<CardManager>();
    }

    private void OnDisable()
    {
        _netServingsStored.OnValueChanged -= UpdateServingsText;
        GameManager.OnStateMorning -= SetStateExtingushed;
        GameManager.OnStateAfternoon -= SetStateCooking;
        GameManager.OnStateEvening -= SetStateFoodReady;
        GameManager.OnStateNight -= SetStateExtingushed;
    }

    // ================== Text ==================
    private void UpdateServingsText(float prev, float next)
    {
        _servingsText.text = "Servings: " + next;
    }

    // ================== Interface ==================
    public bool CanPlayCardHere(Card cardToPlay)
    {
        if (cardToPlay.GetCardType() == _cardTypeAccepted && _state == State.Cooking)
            return true;

        return false;
    }

    // ================== Functions ==================
    public void AddFood(float servings)
    {
        AddFoodServerRpc(servings);
    }

    [ServerRpc(RequireOwnership = false)]
    public void AddFoodServerRpc(float servings)
    {
        _netServingsStored.Value += servings;
    }

    // ================== State Management ==================
    #region State Management
    public void SetStateExtingushed()
    {
        _state = State.Extinguished;
        _stateText.text = _state.ToString();
        _foodMenu.SetActive(false);
    }

    public void SetStateCooking()
    {
        _state = State.Cooking;
        _stateText.text = _state.ToString();
    }

    public void SetStateFoodReady()
    {
        _state = State.FoodReady;
        _stateText.text = _state.ToString();
        _foodMenu.SetActive(true);
    }
    #endregion

    // ================== Take Food ==================
    #region Take Food
    public void TakeHalf()
    {
        if (_netServingsStored.Value < 0.5f)
            return;

        AddFoodServerRpc(-0.5f);
        _cardManager.GiveCard(2004);
    }

    public void TakeOne()
    {
        if (_netServingsStored.Value < 1)
            return;

        AddFoodServerRpc(-1);
        _cardManager.GiveCard(2005);
    }

    public void TakeTwo()
    {
        if (_netServingsStored.Value < 2)
            return;

        AddFoodServerRpc(-2);
        _cardManager.GiveCard(2006);
    }
    #endregion
}
