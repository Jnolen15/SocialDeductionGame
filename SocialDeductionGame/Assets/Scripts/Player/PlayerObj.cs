using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using TMPro;

public class PlayerObj : NetworkBehaviour, ICardPlayable
{
    // Refrences
    private PlayerData _playerData;
    private PlayerHealth _playerHealth;
    [SerializeField] private TextMeshPro _namePlate;

    // Variables
    [SerializeField] private List<CardTag> _cardTagsAccepted = new();

    private void OnEnable()
    {
        _playerHealth = GetComponentInParent<PlayerHealth>();
        _playerData = GetComponentInParent<PlayerData>();

        _playerData._playerName.OnValueChanged += UpdateNamePlate;
    }

    private void OnDisable()
    {
        _playerData._playerName.OnValueChanged -= UpdateNamePlate;
    }

    private void UpdateNamePlate(FixedString32Bytes prev, FixedString32Bytes next)
    {
        _namePlate.text = next.ToString();
    }

    // ================== Interface ==================
    public bool CanPlayCardHere(Card cardToPlay)
    {
        if (!IsOwner)
            return false;

        if (_cardTagsAccepted.Contains(cardToPlay.GetPrimaryTag()))
            return true;

        return false;
    }

    // ================== Food ==================
    public void Eat(float servings, int hpGain = 0)
    {
        if (!IsOwner)
            return;

        _playerHealth.ModifyHunger(servings);

        if (hpGain > 0)
            _playerHealth.ModifyHealth(hpGain);
    }
}
