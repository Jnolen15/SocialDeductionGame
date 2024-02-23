using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class ForageDeck : NetworkBehaviour
{
    // ============== Parameters / Refrences / Variables ==============
    #region P / R / V
    [Header("Card Parameters")]
    [SerializeField] private ForageDropTable _forageDrops;
    [SerializeField] private CardDropTable _cardDropTable = new CardDropTable();
    [SerializeField] private int _uselessCardID;
    [Header("Location")]
    [SerializeField] private LocationManager.LocationName _locationName;
    [Header("Danger Parameters")]
    [SerializeField] private AnimationCurve _dangerLevelDrawChances;
    #endregion

    // ============== Setup ==============
    #region Setup
    private void Start()
    {
        Debug.Log($"Setting up {_locationName}'s forage card deck");
        _cardDropTable.AddCards(_forageDrops.GetForageDropCardIds(), _forageDrops.GetForageDropWeights());
    }
    #endregion

    // ============== Choose and Deal ==============
    #region Choose and Deal
    public List<int> DrawCards(int numToDeal, int uselessOdds, bool totemActive, float dangerLevel, Hazard.DangerLevel dangerTier)
    {
        Debug.Log($"Getting cards from {_locationName}'s deck");

        List<int> cardIDList = new();

        int hazardCardID = HazardTest(dangerLevel, dangerTier);
        if (hazardCardID == 0)// If no hazard drawn
        {
            for (int i = 0; i < numToDeal; i++)
                cardIDList.Add(ChooseCard(uselessOdds, totemActive));
        }
        else// If hazard drawn
        {
            for (int i = 0; i < (numToDeal - 1); i++)
                cardIDList.Add(ChooseCard(uselessOdds, totemActive));

            cardIDList.Insert(Random.Range(0, cardIDList.Count), hazardCardID);
        }

        return cardIDList;
    }

    private int HazardTest(float dangerLevel, Hazard.DangerLevel dangerTier)
    {
        // Roll Hazard chances
        float hazardChance = _dangerLevelDrawChances.Evaluate(dangerLevel * 0.01f);
        Debug.Log($"<color=blue>CLIENT: </color> Player DL: {dangerLevel}, hazard chance: {hazardChance}, hazard level {dangerTier}. Rolling.");
        float rand = (Random.Range(0, 100) * 0.01f);

        // Hazard
        if (hazardChance >= rand)
        {
            Debug.Log($"<color=blue>CLIENT: </color> Rolled: {rand}, hazard encountered!");
            return CardDatabase.Instance.GetRandHazard(dangerTier);
        }
        // No Hazard
        else
        {
            Debug.Log($"<color=blue>CLIENT: </color> Rolled: {rand}, no hazard!");
            return 0;
        }
    }

    private int ChooseCard(int uselessOdds, bool totemActive)
    {
        int cardID = -1;

        int rand = (Random.Range(0, 100));

        // Test for useless card
        Debug.Log($"Useless Odds are {uselessOdds}, rolled a {rand}");
        if (uselessOdds >= rand)
        {
            cardID = _uselessCardID;
            Debug.Log("Picked Useless Card " + cardID);
        }
        // If totem is active, chance to spawn key
        else if (totemActive)
        {
            int keyRand = Random.Range(0, 5);
            Debug.Log($"Totem active, testing for key spawn. Rolled {keyRand}");
            if (keyRand == 4)
                cardID = 1005;
        }

        if (cardID == -1) // Pick and deal random foraged card if useless or key was not picked
        {
            cardID = _cardDropTable.PickCardDrop();
            Debug.Log("Picked Card " + cardID);
        }

        cardID = CardDatabase.Instance.VerifyCard(cardID);

        return cardID;
    }
    #endregion
}
