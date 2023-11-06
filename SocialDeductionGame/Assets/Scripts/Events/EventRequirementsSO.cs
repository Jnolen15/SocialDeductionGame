using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Night Event/Event Requirements")]
public class EventRequirementsSO : ScriptableObject
{
    [SerializeField] private CardTag _primaryResource;
    [SerializeField] private CardTag _secondayResource;

    public CardTag GetPrimaryTag()
    {
        return _primaryResource;
    }

    public CardTag GetSecondaryTag()
    {
        return _secondayResource;
    }

    public Vector2 GetRequirements(int numPlayers)
    {
        switch (numPlayers)
        {
            case 6:
                return new Vector2(5, 4); //9 //Old: 3, 3
            case 5:
                return new Vector2(4, 3); //7 //Old: 3, 2
            case 4:
                return new Vector2(3, 3); //6 //Old: 2, 2
            case 3:
                return new Vector2(2, 2); //4 //Old: 2, 1
            case 2:
                return new Vector2(2, 1); //3 //Old: 2, 0
            case 1:
                return new Vector2(1, 1); //2 //Old: 1, 0
            default:
                Debug.LogError("GetRequirements called with not correct number of players: " + numPlayers);
                return new Vector2(0, 0);
        }
    }
}
