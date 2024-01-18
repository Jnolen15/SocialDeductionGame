using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class GameSettings : MonoBehaviour
{
    // ============= Refrences =============
    [SerializeField] private Toggle _fullscreenToggle;
    [SerializeField] private TextMeshProUGUI _resolutionText;
    [SerializeField] private List<ResolutionEntry> _resolutionList = new List<ResolutionEntry>();
    private int _resIndex;

    [System.Serializable]
    public class ResolutionEntry
    {
        public int Width;
        public int Height;

        public ResolutionEntry(int width, int height)
        {
            Width = width;
            Height = height;
        }

        public override string ToString()
        {
            return Width.ToString() + " X " + Height.ToString();
        }
    }

    // ============= Setup =============
    void Start()
    {
        // If player's default resolution does not already exist make it an option
        bool foundRes = false;
        foreach(ResolutionEntry resEntry in _resolutionList)
        {
            if(Screen.width == resEntry.Width && Screen.height == resEntry.Height)
            {
                foundRes = true;
                UpdateSelectedRes(resEntry);
            }
        }

        if (!foundRes)
        {
            Debug.Log("Resolution not found, creating new entry");

            ResolutionEntry resEntry = new(Screen.width, Screen.height);
            _resolutionList.Add(resEntry);
            UpdateSelectedRes(resEntry);
        }
    }

    // ============= UI Functions =============
    private void UpdateSelectedRes(ResolutionEntry resEntry)
    {
        _resolutionText.text = resEntry.ToString();
    }

    public void NextRes()
    {
        _resIndex++;

        if (_resIndex >= _resolutionList.Count)
            _resIndex = _resolutionList.Count - 1;

        UpdateSelectedRes(_resolutionList[_resIndex]);
    }

    public void PrevRes()
    {
        _resIndex--;

        if (_resIndex < 0)
            _resIndex = 0;

        UpdateSelectedRes(_resolutionList[_resIndex]);
    }

    public void ApplySettings()
    {
        Screen.SetResolution(_resolutionList[_resIndex].Width, _resolutionList[_resIndex].Height, _fullscreenToggle.isOn);
    }
}
