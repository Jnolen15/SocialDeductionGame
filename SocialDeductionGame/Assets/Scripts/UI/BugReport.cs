using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

public class BugReport : MonoBehaviour
{
    // ============== Refrences ==============
    [SerializeField] private GameObject _formContent;
    [SerializeField] private GameObject _submittedMessage;
    [SerializeField] private TMP_InputField _reportField;
    private string _url = "https://docs.google.com/forms/u/0/d/e/1FAIpQLSe-ytpO01iO19FEfkX2Wx0rqWGqm2cjjKhkYE7BMe89gcrdOg/formResponse";

    // ============== Function ==============
    private void OnEnable()
    {
        _formContent.SetActive(true);
        _submittedMessage.SetActive(false);
        _reportField.text = "";
    }

    public void Submit()
    {
        StartCoroutine(SubmitBugReport(_reportField.text));
    }

    private void Submitted()
    {
        _formContent.SetActive(false);
        _submittedMessage.SetActive(true);
    }

    IEnumerator SubmitBugReport(string str)
    {
        WWWForm form = new WWWForm();
        form.AddField("entry.1058178460", str);

        UnityWebRequest www = UnityWebRequest.Post(_url, form);

        Submitted();

        yield return www.SendWebRequest();
    }
}
