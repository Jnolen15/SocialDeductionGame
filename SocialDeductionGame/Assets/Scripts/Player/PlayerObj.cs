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
    [SerializeField] private GameObject _readyIcon;
    [SerializeField] private Transform _character;
    [SerializeField] private List<Material> _characterMatList = new();

    // ================== Variables ==================
    [SerializeField] private List<CardTag> _cardTagsAccepted = new();
    private NetworkVariable<int> _netCharacterIndex = new(writePerm: NetworkVariableWritePermission.Owner);
    private NetworkVariable<int> _netCharacterMatIndex = new(writePerm: NetworkVariableWritePermission.Owner);
    private NetworkVariable<bool> _netTookFromFire = new(writePerm: NetworkVariableWritePermission.Owner);
    private NetworkVariable<bool> _netIsReady = new(writePerm: NetworkVariableWritePermission.Owner);

    // ================== Setup ==================
    private void OnEnable()
    {
        _playerHealth = GetComponentInParent<PlayerHealth>();
        _playerData = GetComponentInParent<PlayerData>();

        _playerData._netPlayerName.OnValueChanged += UpdateNamePlate;
        _playerHealth._netIsLiving.OnValueChanged += UpdateDeathIndicator;

        _netTookFromFire.OnValueChanged += UpdateCampfireIcon;
        _netIsReady.OnValueChanged += UpdateReadyIcon;
    }

    private void OnDisable()
    {
        _playerData._netPlayerName.OnValueChanged -= UpdateNamePlate;
        _playerHealth._netIsLiving.OnValueChanged -= UpdateDeathIndicator;

        _netTookFromFire.OnValueChanged -= UpdateCampfireIcon;
        _netIsReady.OnValueChanged -= UpdateReadyIcon;

        if (IsOwner)
        {
            GameManager.OnStateMorning -= ToggleCampfireIconOff;
            GameManager.OnStateChange -= ToggleReadyIconIconOff;
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            GameManager.OnStateMorning += ToggleCampfireIconOff;
            GameManager.OnStateChange += ToggleReadyIconIconOff;
        }
    }

    [ClientRpc]
    public void UpdateCharacterModelClientRPC(int styleIndex, int materialIndex)
    {
        Debug.Log("Updating Character");

        // Set initial inactive
        _character.GetChild(0).gameObject.SetActive(false);

        // Set correct model active
        Transform model = _character.GetChild(styleIndex);
        model.gameObject.SetActive(true);

        // Set Material
        model.gameObject.GetComponent<SkinnedMeshRenderer>().material = _characterMatList[materialIndex];
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

        if (cardToPlay.HasAnyTag(_cardTagsAccepted))
            return true;

        return false;
    }

    // ================== Ready ==================
    public void ToggleReadyIconActive()
    {
        _netIsReady.Value = true;
    }

    public void ToggleReadyIconIconOff()
    {
        Debug.Log("Toggling ready icon off");
        _netIsReady.Value = false;
    }

    private void UpdateReadyIcon(bool prev, bool next)
    {
        _readyIcon.SetActive(next);
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
