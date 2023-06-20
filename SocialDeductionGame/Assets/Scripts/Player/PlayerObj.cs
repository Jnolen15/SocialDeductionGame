using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerObj : MonoBehaviour, ICardPlayable
{
    // Refrences
    private PlayerHealth _playerHealth;

    // Variables
    [SerializeField] private Card.CardType _cardTypeAccepted;

    void OnEnable()
    {
        _playerHealth = GetComponentInParent<PlayerHealth>();
    }


    // ================== Interface ==================
    public bool CanPlayCardHere(Card cardToPlay)
    {
        if (cardToPlay.GetCardType() == _cardTypeAccepted)
            return true;

        return false;
    }

    // ================== Food ==================
    public void Eat(float servings, int hpGain = 0)
    {
        _playerHealth.ModifyHunger(servings);

        if (hpGain > 0)
            _playerHealth.ModifyHealth(hpGain);
    }
}
