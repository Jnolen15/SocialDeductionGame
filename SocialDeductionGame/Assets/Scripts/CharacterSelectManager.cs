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
    private void Awake()
    {
        PlayerConnectionManager.OnAllPlayersReady += ProgressState;
    }

    private void Start()
    {
        _currentSelectedModel = _characterModel.GetChild(0);
    }

    private void OnDisable()
    {
        PlayerConnectionManager.OnAllPlayersReady -= ProgressState;
    }

    // ============== Scene Management ==============
    private void ProgressState()
    {
        if (!IsServer)
            return;

        SceneLoader.LoadNetwork(SceneLoader.Scene.SampleScene);
    }

    // ============== Character Customization ==============
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
    }
}
