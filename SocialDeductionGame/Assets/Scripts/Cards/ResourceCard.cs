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

    public override void OnPlay(GameObject playLocation)
    {
        ResourcePile resourcePile = playLocation.GetComponent<ResourcePile>();

        if (resourcePile != null)
        {
            Debug.Log("Playng resource card: " + _resourceType);
            resourcePile.AddResources(_resourceType);
        }
        else
        {
            Debug.LogError("Card was played on a location it can't do anything with");
        }

        Destroy(gameObject);
    }
}
