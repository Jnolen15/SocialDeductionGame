using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VivoxUnity;

public class VoiceSettings : MonoBehaviour
{
    // =================== Refrences ===================
    [SerializeField] private Slider _inputVolSlider;
    [SerializeField] private TMP_Dropdown _inputDropdown;
    [SerializeField] private TMP_Dropdown _outputDropdown;

    private IAudioDevices _inputDevices;
    private IAudioDevices _outputDevices;

    // =================== Setup ===================
    #region Setup
    private void OnEnable()
    {
        SetupInputOptions();
        SetupOutputOptions();

        _inputVolSlider.value = PlayerPrefs.GetFloat("InputVol");
    }

    private void SetupInputOptions()
    {
        Debug.Log("Filling input device dropdown");

        List<TMP_Dropdown.OptionData> inputOptions = new();

        Client client = VivoxManager.Instance.GetClientData();
        _inputDevices = client.AudioInputDevices;

        foreach (IAudioDevice device in _inputDevices.AvailableDevices)
        {
            inputOptions.Add(new TMP_Dropdown.OptionData(device.Name));
        }

        _inputDropdown.ClearOptions();
        _inputDropdown.AddOptions(inputOptions);

        _inputDropdown.value = PlayerPrefs.GetInt("InputKey");
    }

    private void SetupOutputOptions()
    {
        Debug.Log("Filling output device dropdowns");

        List<TMP_Dropdown.OptionData> outputOptions = new();

        Client client = VivoxManager.Instance.GetClientData();
        _outputDevices = client.AudioOutputDevices;

        foreach (IAudioDevice device in _outputDevices.AvailableDevices)
        {
            outputOptions.Add(new TMP_Dropdown.OptionData(device.Name));
        }

        _outputDropdown.ClearOptions();
        _outputDropdown.AddOptions(outputOptions);

        _outputDropdown.value = PlayerPrefs.GetInt("OutputKey");
    }
    #endregion

    // =================== Functions ===================
    public void OnInputSliderChanged(float value)
    {
        Debug.Log("Adjusting input volume to " + value);

        PlayerPrefs.SetFloat("InputVol", value);

        VivoxManager.Instance.AdjustInputVolume((int)value);
    }

    public void ChooseInputDevice(int index)
    {
        PlayerPrefs.SetInt("InputKey", index);

        TMP_Dropdown.OptionData selected = _inputDropdown.options[index];

        VivoxManager.Instance.SetInputDevice(GetDeviceWithName(selected.text, _inputDevices));
    }

    public void ChooseOutputDevice(int index)
    {
        PlayerPrefs.SetInt("OutputKey", index);

        TMP_Dropdown.OptionData selected = _outputDropdown.options[index];

        VivoxManager.Instance.SetOutputDevice(GetDeviceWithName(selected.text, _outputDevices));
    }

    private IAudioDevice GetDeviceWithName(string name, IAudioDevices devices)
    {
        foreach (string deviceKey in devices.AvailableDevices.Keys)
        {
            if (devices.AvailableDevices[deviceKey].Name == name)
                return devices.AvailableDevices[deviceKey];
        }

        Debug.LogError("Device with name " + name + " not found!");
        return null;
    }
}
