using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using TMPro;

public class Campfire : NetworkBehaviour, ICardPlayable
{
    // Variables
    [SerializeField] private CardTag _cardTagAccepted;
    [SerializeField] private NetworkVariable<float> _netServingsStored = new(writePerm: NetworkVariableWritePermission.Server);
    [SerializeField] private NetworkVariable<bool> _netIsPoisoned = new(writePerm: NetworkVariableWritePermission.Server);
    [SerializeField] private float _takeBufferTimeMax;
    private float _takeBufferTimer;
    public enum State
    {
        Extinguished,
        Cooking,
        FoodReady
    }
    [SerializeField] private State _state;

    // Refrences
    [SerializeField] private TextMeshProUGUI _servingsText;
    [SerializeField] private TextMeshPro _stateText;
    [SerializeField] private GameObject _foodMenu;
    [SerializeField] private GameObject _flameObj;
    private CardManager _cardManager;

    public delegate void CampfireAction();
    public static event CampfireAction OnTookFromFire;

    // ================== Setup ==================
    private void Awake()
    {
        _netServingsStored.OnValueChanged += UpdateServingsText;
        GameManager.OnStateMorning += SetStateCooking;
        //GameManager.OnStateAfternoon += SetStateCooking;
        GameManager.OnStateEvening += SetStateFoodReady;
        GameManager.OnStateNight += SetStateExtingushed;

        if(IsServer)
            GameManager.OnStateMorning += RemovePoison;
    }

    public override void OnNetworkSpawn()
    {
        _servingsText.text = _netServingsStored.Value.ToString();
    }

    private void Start()
    {
        _cardManager = GameObject.FindGameObjectWithTag("CardManager").GetComponent<CardManager>();
    }

    public override void OnDestroy()
    {
        _netServingsStored.OnValueChanged -= UpdateServingsText;
        GameManager.OnStateMorning -= SetStateCooking;
        //GameManager.OnStateAfternoon -= SetStateCooking;
        GameManager.OnStateEvening -= SetStateFoodReady;
        GameManager.OnStateNight -= SetStateExtingushed;

        if (IsServer)
            GameManager.OnStateMorning -= RemovePoison;

        // Always invoked the base 
        base.OnDestroy();
    }

    // ================== Update ==================
    private void Update()
    {
        if (_takeBufferTimer > 0)
            _takeBufferTimer -= Time.deltaTime;
    }

    // ================== Text ==================
    private void UpdateServingsText(float prev, float next)
    {
        _servingsText.text = next.ToString();
    }

    // ================== Interface ==================
    public bool CanPlayCardHere(Card cardToPlay)
    {
        if (cardToPlay.HasTag(_cardTagAccepted) && _state == State.Cooking)
            return true;

        return false;
    }

    // ================== Functions ==================
    public void AddFood(int servings)
    {
        AddFoodServerRpc(servings);
    }

    [ServerRpc(RequireOwnership = false)]
    public void AddFoodServerRpc(int servings)
    {
        _netServingsStored.Value += servings;
    }

    public void AddPoisonedFood(int servings)
    {
        AddPoisonedFoodServerRpc(servings);
    }

    [ServerRpc(RequireOwnership = false)]
    public void AddPoisonedFoodServerRpc(int servings)
    {
        _netIsPoisoned.Value = true;
        _netServingsStored.Value += servings;
    }

    private void RemovePoison()
    {
        if (!IsServer)
            return;

        _netIsPoisoned.Value = false;
    }

    // ================== State Management ==================
    #region State Management
    public void SetStateExtingushed()
    {
        _state = State.Extinguished;
        _stateText.text = _state.ToString();
        _flameObj.SetActive(false);
        _foodMenu.SetActive(false);
    }

    public void SetStateCooking()
    {
        _state = State.Cooking;
        _stateText.text = _state.ToString();
        _flameObj.SetActive(true);
    }

    public void SetStateFoodReady()
    {
        _state = State.FoodReady;
        _stateText.text = _state.ToString();
        _flameObj.SetActive(true);
        _foodMenu.SetActive(true);
    }
    #endregion

    // ================== Take Food ==================
    #region Take Food
    public void TakeFood(int ammount)
    {
        // Quick and dirty way to prevent dead players from taking food (Change later)
        if (!PlayerConnectionManager.Instance.GetLocalPlayerLiving())
            return;

        if (_takeBufferTimer > 0)
        {
            Debug.Log("On Cooldown, cant take food");
            return;
        }

        if (_netServingsStored.Value < ammount)
            return;

        Debug.Log("<color=blue>CLIENT: </color>Taking food from fire. Servings: " + ammount);

        _takeBufferTimer = _takeBufferTimeMax;

        AddFoodServerRpc(-ammount);

        OnTookFromFire?.Invoke();

        // Give Poison Food
        if (_netIsPoisoned.Value)
        {
            if (ammount == 1)
                _cardManager.GiveCard(2012);
            else if (ammount == 2)
                _cardManager.GiveCard(2013);
            else if (ammount == 4)
                _cardManager.GiveCard(2014);
        }
        // Give normal food
        else
        {
            if (ammount == 1)
                _cardManager.GiveCard(2004);
            else if (ammount == 2)
                _cardManager.GiveCard(2005);
            else if (ammount == 4)
                _cardManager.GiveCard(2006);
            else
                Debug.LogError("AMMOUNT TAKEN FROM FIRE NOT 1, 2 or 4 " + ammount);
        }
    }
    #endregion
}
