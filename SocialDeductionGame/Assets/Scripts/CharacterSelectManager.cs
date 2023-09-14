using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class CharacterSelectManager : NetworkBehaviour
{
    // ============== Refrences ==============
    [SerializeField] private Transform _characterModel;
    private Transform _currentSelectedModel;
    [SerializeField] private List<Material> _characterMatList = new();

    // ================== Variables ==================
    private int _characterMatIndex;
    private int _characterStyleIndex;

    // ============== Setup ==============
    #region Setup
    public override void OnNetworkSpawn()
    {
        if (IsServer)
            PlayerConnectionManager.OnAllPlayersReady += LoadToGameScene;
    }

    private void Start()
    {
        _currentSelectedModel = _characterModel.GetChild(0);
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
            PlayerConnectionManager.OnAllPlayersReady -= LoadToGameScene;
    }

    private void OnApplicationQuit()
    {
        LobbyManager.Instance.DisconnectFromLobby();
    }
    #endregion

    // ============== Scene Management ==============
    #region Scene Management
    private void LoadToGameScene()
    {
        if (!IsServer)
            return;

        Debug.Log("<color=yellow>SERVER: </color> All players ready, loading to game scene");

        // Clean up lobby
        LobbyManager.Instance.DeleteLobby();

        // Load game scene
        SceneLoader.LoadNetwork(SceneLoader.Scene.IslandGameScene);
    }
    #endregion

    // ============== Character Customization ==============
    #region Character Customization
    public void SetPlayerName(string newName)
    {
        Debug.Log("Name Recieved: " + newName);
        PlayerConnectionManager.Instance.UpdatePlayerName(NetworkManager.Singleton.LocalClientId, newName);
    }

    public void SetPlayerVisuals(int style, int mat)
    {
        PlayerConnectionManager.Instance.UpdatePlayerVisuals(NetworkManager.Singleton.LocalClientId, style, mat);
    }

    public void RandomizeCharacter()
    {
        _characterStyleIndex = Random.Range(0, _characterModel.childCount - 1);
        _characterMatIndex = Random.Range(0, _characterMatList.Count);

        // Set last inactive
        _currentSelectedModel.gameObject.SetActive(false);

        // Update Refrence
        _currentSelectedModel = _characterModel.GetChild(_characterStyleIndex);

        // Update model
        _currentSelectedModel.gameObject.SetActive(true);
        _currentSelectedModel.gameObject.GetComponent<SkinnedMeshRenderer>().material = _characterMatList[_characterMatIndex];

        // Update data
        SetPlayerVisuals(_characterStyleIndex, _characterMatIndex);
    }

    public void ChangeCharacterColor(bool next)
    {
        // Next color
        if (next)
            _characterMatIndex++;
        // Previous color
        else
            _characterMatIndex--;

        // Clamp values
        if (_characterMatIndex < 0)
            _characterMatIndex = _characterMatList.Count-1;
        else if (_characterMatIndex >= _characterMatList.Count)
            _characterMatIndex = 0;

        // Set material
        _currentSelectedModel.gameObject.GetComponent<SkinnedMeshRenderer>().material = _characterMatList[_characterMatIndex];

        // Update data
        SetPlayerVisuals(_characterStyleIndex, _characterMatIndex);
    }

    public void ChangeCharacterStyle(bool next)
    {
        // Next color
        if (next)
            _characterStyleIndex++;
        // Previous color
        else
            _characterStyleIndex--;

        // Clamp values
        if (_characterStyleIndex < 0)
            _characterStyleIndex = _characterModel.childCount-2;
        else if (_characterStyleIndex >= _characterModel.childCount-1)
            _characterStyleIndex = 0;

        // Set last inactive
        _currentSelectedModel.gameObject.SetActive(false);

        // Update Refrence
        _currentSelectedModel = _characterModel.GetChild(_characterStyleIndex);

        // Update model
        _currentSelectedModel.gameObject.SetActive(true);
        _currentSelectedModel.gameObject.GetComponent<SkinnedMeshRenderer>().material = _characterMatList[_characterMatIndex];

        // Update data
        SetPlayerVisuals(_characterStyleIndex, _characterMatIndex);
    }
    #endregion
}
