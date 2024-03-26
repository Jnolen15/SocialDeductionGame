using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

public class BugReport : MonoBehaviour
{
    // ============== Refrences ==============
    [SerializeField] private GameObject _formContent;
    [SerializeField] private GameObject _submittedMessage;
    [SerializeField] private GameObject _submitButton;
    [SerializeField] private TMP_InputField _reportField;
    private string _url = "https://docs.google.com/forms/u/0/d/e/1FAIpQLSe-ytpO01iO19FEfkX2Wx0rqWGqm2cjjKhkYE7BMe89gcrdOg/formResponse";

    // ============== Function ==============
    private void OnEnable()
    {
        _submitButton.SetActive(false);
        _formContent.SetActive(true);
        _submittedMessage.SetActive(false);
        _reportField.text = "";
    }

    public void OnEditbugReportInputField(string attemptedVal)
    {
        if (attemptedVal.Length >= 20)
        {
            _submitButton.SetActive(true);
        }
        else
        {
            _submitButton.SetActive(false);
        }
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
        // Response
        form.AddField("entry.1058178460", str);

        // Log File (this was removed as it was often too long)
        //form.AddField("entry.1238921461", ReadPlayerLogFile());

        UnityWebRequest www = UnityWebRequest.Post(_url, form);

        Submitted();

        yield return www.SendWebRequest();
    }

    string ReadPlayerLogFile()
    {
        string logFilePath = Path.Combine(Application.persistentDataPath, "Player.log");

        try
        {
            using FileStream fileStream = new FileStream(logFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using StreamReader streamReader = new StreamReader(fileStream);
            return streamReader.ReadToEnd();
        }
        catch (IOException ex)
        {
            //Debug.LogError($"Error reading log file: {ex.Message}");
            return $"Error reading log file: {ex.Message}";
        }
    }
}
