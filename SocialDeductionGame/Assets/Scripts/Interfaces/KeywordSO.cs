using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Keyword")]
public class KeywordSO : ScriptableObject
{
    [SerializeField] private string _keywordName;
    public string KeywordName 
    {
        get { return _keywordName; }
        private set { _keywordName = value; } 
    }

    [TextArea]
    [SerializeField] private string _keywordDescription;
    public string KeywordDescription
    {
        get { return _keywordDescription; }
        private set { _keywordDescription = value; }
    }
}
