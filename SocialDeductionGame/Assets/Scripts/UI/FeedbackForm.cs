using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class FeedbackForm : MonoBehaviour
{
    // ============== Refrences ==============
    [SerializeField] private GameObject _formContent;
    [SerializeField] private GameObject _submittedMessage;
    [SerializeField] private ToggleGroup _enjoymentToggle;
    [SerializeField] private ToggleGroup _understandingToggle;
    [SerializeField] private ToggleGroup _preferedPlayersToggle;
    [SerializeField] private ToggleGroup _preferedSabosToggle;
    [SerializeField] private TMP_InputField _reportField;
    [SerializeField] private PlayerNamer _playerNamer;
    private string _url = "https://docs.google.com/forms/u/0/d/e/1FAIpQLSdpq2tb7bDDeOYWss6Ry2gRUt8F29uuoL5miulWN2HxDC5OxA/formResponse";

    // ============== Function ==============
    private void OnEnable()
    {
        _formContent.SetActive(true);
        _submittedMessage.SetActive(false);
        _reportField.text = "";
    }

    public void Submit()
    {
        string ejs = "";
        string us = "";
        string pps = "";
        string pss = "";

        foreach(Toggle toggle in _enjoymentToggle.ActiveToggles())
        {
            ejs = toggle.name;
        }

        foreach (Toggle toggle in _understandingToggle.ActiveToggles())
        {
            us = toggle.name;
        }

        foreach (Toggle toggle in _preferedPlayersToggle.ActiveToggles())
        {
            pps = toggle.name;
        }

        foreach (Toggle toggle in _preferedSabosToggle.ActiveToggles())
        {
            pss = toggle.name;
        }

        string pName = "Player";
        if (_playerNamer.GetPlayerName() != null)
            pName = _playerNamer.GetPlayerName();

        Debug.Log($"Submitting feedback: Player {pName} Enjoyment {ejs}, Undestanding {us}, Players {pps}, Sabos {pss}, Open {_reportField.text}");

        StartCoroutine(SubmitFeedbackForm(ejs, us, pps, pss, _reportField.text, pName));
    }

    private void Submitted()
    {
        _formContent.SetActive(false);
        _submittedMessage.SetActive(true);
    }

    IEnumerator SubmitFeedbackForm(string ejs, string us, string pps, string pss, string openFeedback, string pName)
    {
        WWWForm form = new WWWForm();
        form.AddField("entry.1178562165", ejs); // EnjoymentScale
        form.AddField("entry.1125419417", us); // UnderstandingScale
        form.AddField("entry.1882802084", pps); // Prefered Players
        form.AddField("entry.978172810", pss); // Prefered Sabos
        form.AddField("entry.1936252508", openFeedback); // Free Response
        form.AddField("entry.467249274", pName); // Player Name

        UnityWebRequest www = UnityWebRequest.Post(_url, form);

        Submitted();

        yield return www.SendWebRequest();
    }
}
