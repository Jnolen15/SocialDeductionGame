using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Watch Colors/New Watch Color Pallet")]
public class WatchColors : ScriptableObject
{
    [SerializeField] private string _paletteName;
    [Header("Base")]
    [SerializeField] private Color _colorBase;
    [Header("Primary")]
    [SerializeField] private Color _colorPrimary;
    [Header("Secondary")]
    [SerializeField] private Color _colorSecondary;

    public string GetPaletteName()
    {
        return _paletteName;
    }

    public Color GetBaseColor()
    {
        return _colorBase;
    }

    public Color GetPrimaryColor()
    {
        return _colorPrimary;
    }

    public Color GetSecondaryColor()
    {
        return _colorSecondary;
    }
}
