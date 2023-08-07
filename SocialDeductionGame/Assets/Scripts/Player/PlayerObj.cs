using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using TMPro;

public class PlayerObj : NetworkBehaviour, ICardPlayable
{
    // ================== Refrences ==================
    private PlayerData _playerData;
    private PlayerHealth _playerHealth;
    [SerializeField] private TextMeshPro _namePlate;
    [SerializeField] private GameObject _deathIndicator;
    [SerializeField] private GameObject _campfireIcon;
    [SerializeField] private Transform _character;
    [SerializeField] private List<Material> _characterMatList = new();

    // ================== Variables ==================
    [SerializeField] private List<CardTag> _cardTagsAccepted = new();
    private NetworkVariable<int> _netCharacterIndex = new(writePerm: NetworkVariableWritePermission.Owner);
    private NetworkVariable<int> _netCharacterMatIndex = new(writePerm: NetworkVariableWritePermission.Owner);
    private NetworkVariable<bool> _netTookFromFire = new(writePerm: NetworkVariableWritePermission.Owner);

    // ================== Setup ==================
    private void OnEnable()
    {
        _playerHealth = GetComponentInParent<PlayerHealth>();
        _playerData = GetComponentInParent<PlayerData>();

        _playerData._netPlayerName.OnValueChanged += UpdateNamePlate;
        _playerHealth._netIsLiving.OnValueChanged += UpdateDeathIndicator;

        _netCharacterIndex.OnValueChanged += UpdateCharacterModel;
        _netCharacterMatIndex.OnValueChanged += UpdateCharacterMat;
        _netTookFromFire.OnValueChanged += UpdateCampfireIcon;
    }

    private void OnDisable()
    {
        _playerData._netPlayerName.OnValueChanged -= UpdateNamePlate;
        _playerHealth._netIsLiving.OnValueChanged -= UpdateDeathIndicator;

        _netCharacterIndex.OnValueChanged -= UpdateCharacterModel;
        _netCharacterMatIndex.OnValueChanged -= UpdateCharacterMat;
        _netTookFromFire.OnValueChanged -= UpdateCampfireIcon;
        
        if (IsOwner)
            GameManager.OnStateMorning -= ToggleCampfireIconOff;
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            Debug.Log("Randomizing Character");
            _netCharacterIndex.Value = Random.Range(0, _character.childCount - 1);
            _netCharacterMatIndex.Value = Random.Range(0, _characterMatList.Count);

            GameManager.OnStateMorning += ToggleCampfireIconOff;
        }
        else
        {
            Debug.Log("initial Character Setup");
            UpdateCharacterModel(0, _netCharacterIndex.Value);
            UpdateCharacterMat(0, _netCharacterMatIndex.Value);
        }
    }

    private void UpdateCharacterModel(int prev, int next)
    {
        Debug.Log("Updating Character");

        // Set initial inactive
        _character.GetChild(0).gameObject.SetActive(false);

        // Set correct model active
        _character.GetChild(next).gameObject.SetActive(true);
    }

    private void UpdateCharacterMat(int prev, int next)
    {
        _character.GetChild(_netCharacterIndex.Value).gameObject.GetComponent<SkinnedMeshRenderer>().material = _characterMatList[next];
    }

    // ================== Info ==================
    private void UpdateNamePlate(FixedString32Bytes prev, FixedString32Bytes next)
    {
        _namePlate.text = next.ToString();

        if (IsOwner)
            _namePlate.color = Color.green;
    }

    private void UpdateDeathIndicator(bool prev, bool next)
    {
        _deathIndicator.SetActive(!next);
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

    // These two campfire toggle functions are a quick and dirty bandaid solution. Fix later.
    public void ToggleCampfireIconActive()
    {
        _netTookFromFire.Value = true;
    }

    public void ToggleCampfireIconOff()
    {
        Debug.Log("Toggling campfire icon off");
        _netTookFromFire.Value = false;
    }

    private void UpdateCampfireIcon(bool prev, bool next)
    {
        _campfireIcon.SetActive(next);
    }
}
