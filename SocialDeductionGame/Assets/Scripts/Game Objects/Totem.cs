using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Totem : NetworkBehaviour, ICardPlayable
{
    // ================== Refrences ==================
    [SerializeField] private GameObject _totemEffects;
    [SerializeField] private GameObject _totemButton;
    [SerializeField] private Transform _tagZone;
    [SerializeField] private GameObject _tagPref;

    // ================== Variables ==================
    [SerializeField] private int _tagLimit;
    [SerializeField] private LocationManager.LocationName _locationName;
    [SerializeField] private NetworkVariable<bool> _netIsPrepped = new(writePerm: NetworkVariableWritePermission.Server);
    [SerializeField] private NetworkVariable<bool> _netIsActive = new(writePerm: NetworkVariableWritePermission.Server);
    [SerializeField] private List<CardTag> _currentTags = new();
    [SerializeField] private Dictionary<string, GameObject> _localTags = new();

    public delegate void TotemAction(LocationManager.LocationName locationName);
    public static event TotemAction OnLocationTotemEnable;
    public static event TotemAction OnLocationTotemDisable;

    // ================== Setup ==================
    public override void OnNetworkSpawn()
    {
        GameManager.OnStateIntro += InitialVisibiltyToggle;
        _netIsActive.OnValueChanged += ToggleVisibility;
        
        if(IsServer)
            GameManager.OnStateNight += ToggleActive;
    }

    public override void OnDestroy()
    {
        GameManager.OnStateIntro -= InitialVisibiltyToggle;
        _netIsActive.OnValueChanged -= ToggleVisibility;

        if (IsServer)
            GameManager.OnStateNight -= ToggleActive;

        // Invoke the base when using networkobject
        base.OnDestroy();
    }

    // ================== Interface ==================
    // Totem accepts any card types
    public bool CanPlayCardHere(Card cardToPlay)
    {
        if (GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerData>().GetPlayerTeam() == PlayerData.Team.Saboteurs)
            return true;
        else
            return _netIsActive.Value;
    }

    // ================== Functions ==================
    private void InitialVisibiltyToggle()
    {
        ToggleVisibility(false, false);
    }

    private void ToggleVisibility(bool prev, bool current)
    {
        Debug.Log("Toggling totem visibility " + current);

        // Set totem active
        if (current)
        {
            _totemEffects.SetActive(true);
            _totemButton.SetActive(true);
            OnLocationTotemEnable?.Invoke(_locationName);
        }
        // Set totem deactive
        else
        {
            _currentTags.Clear();
            _localTags.Clear();
            _totemEffects.SetActive(false);
            _totemButton.SetActive(false);
            OnLocationTotemDisable?.Invoke(_locationName);
        }

        // If player is sabo, can see button while deactive
        if (GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerData>().GetPlayerTeam() == PlayerData.Team.Saboteurs)
            _totemButton.SetActive(true);
    }

    // Call from server
    private void ToggleActive()
    {
        if (!IsServer)
            return;

        if (_netIsPrepped.Value)
        {
            _netIsPrepped.Value = false;
            _netIsActive.Value = true;
        }
    }

    public void AddCard(int cardID)
    {
        if (_netIsActive.Value)
            AddCardSurvivorsServerRpc(cardID);
        else
            AddCardSaboServerRpc(cardID);
    }

    [ServerRpc(RequireOwnership = false)]
    private void AddCardSaboServerRpc(int cardID)
    {
        Debug.Log("<color=yellow>SERVER: </color>Adding card to unactivated Totem");
        _netIsPrepped.Value = true;

        // Loop through card tag list and add new card tags from given card
        foreach(CardTag tag in ExtractTags(cardID))
        {
            if (_currentTags.Count >= _tagLimit)
                break;

            if (!_currentTags.Contains(tag))
            {
                Debug.Log("<color=yellow>SERVER: </color>Added new tag to totem " + tag.name);
                _currentTags.Add(tag);
            }
        }

        AddToTagListClientRpc(cardID);
    }

    [ClientRpc]
    private void AddToTagListClientRpc(int cardID)
    {
        Debug.Log("Adding card to unactivated Totem locally");
        
        // Loop through card tag list and add new card tags from given card
        foreach (CardTag tag in ExtractTags(cardID))
        {
            if (_localTags.Count >= _tagLimit)
                break;

            if (!_localTags.ContainsKey(tag.Name))
            {
                Debug.Log("<color=blue>CLIENT: </color>Added new tag to totem " + tag.name);
                TagIcon tagIcon = Instantiate(_tagPref, _tagZone).GetComponent<TagIcon>();
                tagIcon.SetupIcon(tag.visual, tag.Name);
                _localTags.Add(tag.Name, tagIcon.gameObject);
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void AddCardSurvivorsServerRpc(int cardID)
    {
        Debug.Log("Adding card to activated Totem");

        // Loop through card tag list and remove matching tags
        foreach (CardTag tag in ExtractTags(cardID))
        {
            if (_currentTags.Contains(tag))
            {
                Debug.Log("<color=yellow>SERVER: </color>Removing card tag from totem " + tag.name);
                _currentTags.Remove(tag);
            }

            if(_currentTags.Count == 0)
            {
                Debug.Log("<color=yellow>SERVER: </color>All tags added, shutting down totem");
                _netIsActive.Value = false;
            }
        }

        RemoveFromTagListSurvivorsClientRpc(cardID);
    }

    [ClientRpc]
    private void RemoveFromTagListSurvivorsClientRpc(int cardID)
    {
        Debug.Log("Adding card to activated Totem locally");

        // Loop through card tag list and remove matching tags
        foreach (CardTag tag in ExtractTags(cardID))
        {
            if (_localTags.ContainsKey(tag.Name))
            {
                Debug.Log("<color=blue>CLIENT: </color>Removing card tag from totem " + tag.name);
                Destroy(_localTags[tag.Name]);
                _localTags.Remove(tag.Name);
            }
        }
    }

    private List<CardTag> ExtractTags(int cardId)
    {
        return CardDatabase.Instance.GetCard(cardId).GetComponent<Card>().GetCardTags();
    }
}
