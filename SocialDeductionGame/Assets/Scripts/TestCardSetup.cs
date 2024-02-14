using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestCardSetup : MonoBehaviour
{
    [SerializeField] private Card _uiCardVisual;
    [SerializeField] private Card _uiCardSelectable;
    [SerializeField] private Card _uiCardPlayable;
    [SerializeField] private Card _gearCardVisual;
    [SerializeField] private Card _gearCardSelectable;
    [SerializeField] private Card _gearCardPlayable;
    void Start()
    {
        _uiCardVisual.SetupUI();
        _uiCardSelectable.SetupSelectable();
        _uiCardPlayable.SetupPlayable();

        _gearCardVisual.SetupUI();
        _gearCardSelectable.SetupSelectable();
        _gearCardPlayable.SetupPlayable();
    }
}
