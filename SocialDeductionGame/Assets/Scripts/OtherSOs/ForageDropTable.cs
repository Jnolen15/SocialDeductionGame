using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Forage Drop Table")]
public class ForageDropTable : ScriptableObject
{
	[System.Serializable]
	public class ForageDropEntry
	{
		// Just to make it easier to read
		public string CardName;

		public int CardID;

		// The higher the weight, the higher chance of being picked
		public float ProbabilityWeight;
	}

	// List of card IDs that may be dropped
	[Header("Card name not verified")]
	[SerializeField]
	private List<ForageDropEntry> CardDrops;

	public List<ForageDropEntry> GetForageDropList()
    {
		return CardDrops;
	}

	public int[] GetForageDropCardIds()
	{
		int[] cardIDs = new int[CardDrops.Count];

        for (int i = 0; i < CardDrops.Count; i++)
        {
			cardIDs[i] = CardDrops[i].CardID;
		}

		return cardIDs;
	}

	public float[] GetForageDropWeights()
	{
		float[] weights = new float[CardDrops.Count];

		for (int i = 0; i < CardDrops.Count; i++)
		{
			weights[i] = CardDrops[i].ProbabilityWeight;
		}

		return weights;
	}
}
