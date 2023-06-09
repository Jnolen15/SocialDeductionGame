using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Code inspired by Hyper Fox Studios https://hyperfoxstudios.com/category/tutorial/

[System.Serializable]
public class CardDropTable
{
    // Card Drop Rate Table Entry
	[System.Serializable]
	public class CardDropEntry
    {
        public int CardID;

        // The higher the weight, the higher chance of being picked
        public float ProbabilityWeight;

        // Displayed only as an information. DO not be set manually via inspector!
        public float ProbabilityPercent;

		// Assigned via ValidateTable function. They represent the range where the item will be picked.
		[HideInInspector]
        public float ProbabilityRangeFrom;
        [HideInInspector]
        public float ProbabilityRangeTo;
    }

	// List of card IDs that may be dropped
	[SerializeField]
	public List<CardDropEntry> CardDrops;

	// Sum of all weights of items.
	private float _probabilityTotalWeight;

	/// <summary>
	/// Calculates the percentage and asigns the probabilities how many times
	/// the items can be picked. Function used also to validate data when tweaking numbers in editor.
	/// </summary>	
	public void ValidateTable()
	{
		if (CardDrops == null || CardDrops.Count <= 0)
			return;

		float currentProbabilityWeightMaximum = 0f;

		// Sets the weight ranges of the selected items.
		foreach (CardDropEntry cardDrop in CardDrops)
		{
			// Prevent usage of negative weight.
			if (cardDrop.ProbabilityWeight < 0f)
			{
				Debug.LogError("You can't have negative weight on an item. Reseting item's weight to 0.");
				cardDrop.ProbabilityWeight = 0f;
			}
			else
			{
				cardDrop.ProbabilityRangeFrom = currentProbabilityWeightMaximum;
				currentProbabilityWeightMaximum += cardDrop.ProbabilityWeight;
				cardDrop.ProbabilityRangeTo = currentProbabilityWeightMaximum;
			}

		}

		_probabilityTotalWeight = currentProbabilityWeightMaximum;

		// Calculate percentage of item drop select rate.
		foreach (CardDropEntry lootDropItem in CardDrops)
		{
			lootDropItem.ProbabilityPercent = ((lootDropItem.ProbabilityWeight) / _probabilityTotalWeight) * 100;
		}
	}

	/// <summary>
	/// Picks and returns the loot drop item based on it's probability.
	/// </summary>
	public int PickCardDrop()
	{
		float randomNum = Random.Range(0, _probabilityTotalWeight);

		// Find an item thats range contains randomNum
		foreach (CardDropEntry cardDrop in CardDrops)
		{
			// If the random number matches the item's range, return card id
			if (randomNum > cardDrop.ProbabilityRangeFrom && randomNum < cardDrop.ProbabilityRangeTo)
			{
				return cardDrop.CardID;
			}
		}

		Debug.LogError("Item couldn't be picked... Be sure that all of your active loot drop tables have assigned at least one item!");
		return 0;
	}
}
