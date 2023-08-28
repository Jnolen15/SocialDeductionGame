using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FruitClusterCard : Card
{
    [Header("Cluster Details")]
    [SerializeField] private int _givenCardID;
    private CardManager _cardManager;

    public override void OnPlay(GameObject playLocation)
    {
        PlayerObj player = playLocation.GetComponent<PlayerObj>();

        if (player != null)
        {
            _cardManager = GameObject.FindGameObjectWithTag("CardManager").GetComponent<CardManager>();
            int rand = Random.Range(2, 4);
            for(int i = 0; i < rand; i++)
                _cardManager.GiveCard(_givenCardID);
        }
        else
        {
            Debug.LogError("Card was played on a location it can't do anything with");
        }
    }
}
