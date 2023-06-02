using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResourceCard : Card
{
    public enum ResourceType
    {
        Wood,
        Stone,
        Plant
    }
    [Header("Resource Details")]
    [SerializeField] private ResourceType _resourceType;


    public override void OnPlay()
    {
        Debug.Log("Playng resource card: " + _resourceType);

        throw new System.NotImplementedException();
    }
}
