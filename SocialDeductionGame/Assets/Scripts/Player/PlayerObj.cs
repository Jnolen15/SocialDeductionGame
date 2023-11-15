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
    [SerializeField] private GameObject _saboIcon;
    [SerializeField] private GameObject _speakingIcon;
    [SerializeField] private Transform _character;
    [SerializeField] private List<Material> _characterMatList = new();
    [SerializeField] private Material _ghostMat;
    private GameObject _model;

    // ================== Variables ==================
    [SerializeField] private List<CardTag> _cardTagsAccepted = new();
    private NetworkVariable<int> _netCharacterIndex = new(writePerm: NetworkVariableWritePermission.Owner);
    private NetworkVariable<int> _netCharacterMatIndex = new(writePerm: NetworkVariableWritePermission.Owner);
    private NetworkVariable<bool> _netTookFromFire = new(writePerm: NetworkVariableWritePermission.Owner);
    private NetworkVariable<bool> _netIsReady = new(writePerm: NetworkVariableWritePermission.Owner);
    private NetworkVariable<bool> _netSpeaking = new(writePerm: NetworkVariableWritePermission.Owner);

    // ================== Setup ==================
    public override void OnNetworkSpawn()
    {
        GameManager.OnStateIntro += UpdateNameSaboColor;

        if (IsOwner)
        {
            GameManager.OnStateMorning += ToggleCampfireIconOff;
            GameManager.OnStateChange += ToggleReadyIconIconOff;

            VivoxClient.OnBeginSpeaking += ToggleSpeakingIconActive;
            VivoxClient.OnEndSpeaking += ToggleSpeakingIconOff;
        }
    }

    private void OnEnable()
    {
        _playerHealth = GetComponentInParent<PlayerHealth>();
        _playerData = GetComponentInParent<PlayerData>();

        _playerData._netPlayerName.OnValueChanged += UpdateNamePlate;
        _playerHealth._netIsLiving.OnValueChanged += OnLivingChanged;

        _netTookFromFire.OnValueChanged += UpdateCampfireIcon;
        _netIsReady.OnValueChanged += UpdateReadyIcon;
        _netSpeaking.OnValueChanged += UpdateSpeakingIcon;
    }

    private void OnDisable()
    {
        _playerData._netPlayerName.OnValueChanged -= UpdateNamePlate;
        _playerHealth._netIsLiving.OnValueChanged -= OnLivingChanged;

        _netTookFromFire.OnValueChanged -= UpdateCampfireIcon;
        _netIsReady.OnValueChanged -= UpdateReadyIcon;
        _netSpeaking.OnValueChanged -= UpdateSpeakingIcon;

        GameManager.OnStateIntro -= UpdateNameSaboColor;

        if (IsOwner)
        {
            GameManager.OnStateMorning -= ToggleCampfireIconOff;
            GameManager.OnStateChange -= ToggleReadyIconIconOff;

            VivoxClient.OnBeginSpeaking -= ToggleSpeakingIconActive;
            VivoxClient.OnEndSpeaking -= ToggleSpeakingIconOff;
        }
    }

    [ClientRpc]
    public void UpdateCharacterModelClientRPC(int styleIndex, int materialIndex)
    {
        Debug.Log("Updating Character");

        // Set initial inactive
        _character.GetChild(0).gameObject.SetActive(false);

        // Set correct model active
        _model = _character.GetChild(styleIndex).gameObject;
        _model.SetActive(true);

        // Set Material
        _model.GetComponent<SkinnedMeshRenderer>().material = _characterMatList[materialIndex];
    }

    // ================== Info ==================
    //
    private void UpdateNameSaboColor()
    {
        if (_playerData.GetPlayerTeam() == PlayerData.Team.Saboteurs
                && GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerData>().GetPlayerTeam() == PlayerData.Team.Saboteurs)
        {
            if(!IsOwner)
                _namePlate.color = Color.red;

            _saboIcon.SetActive(true);
        }
    }

    private void UpdateNamePlate(FixedString32Bytes prev, FixedString32Bytes next)
    {
        _namePlate.text = next.ToString();

        if (IsOwner)
            _namePlate.color = Color.green;
    }

    private void OnLivingChanged(bool prev, bool next)
    {
        _deathIndicator.SetActive(!next);

        if(_model)
            _model.gameObject.GetComponent<SkinnedMeshRenderer>().material = _ghostMat;
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

    // ================== Icons ==================
    #region Icons
    public void ToggleReadyIconActive()
    {
        _netIsReady.Value = true;
    }

    public void ToggleReadyIconIconOff(GameManager.GameState prev, GameManager.GameState current)
    {
        Debug.Log("Toggling ready icon off");
        _netIsReady.Value = false;
    }

    private void UpdateReadyIcon(bool prev, bool next)
    {
        _readyIcon.SetActive(next);
    }

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

    public void ToggleSpeakingIconActive(VivoxManager.ChannelSeshName channel)
    {
        _netSpeaking.Value = true;
    }

    public void ToggleSpeakingIconOff(VivoxManager.ChannelSeshName channel)
    {
        Debug.Log("Toggling speaking icon off");
        _netSpeaking.Value = false;
    }

    private void UpdateSpeakingIcon(bool prev, bool next)
    {
        _speakingIcon.SetActive(next);
    }
    #endregion

    // ================== Food ==================
    public void Eat(int servings, int hpGain = 0)
    {
        if (!IsOwner)
            return;

        _playerHealth.ModifyHunger(servings);

        if (hpGain > 0)
            _playerHealth.ModifyHealth(hpGain);
    }
}
