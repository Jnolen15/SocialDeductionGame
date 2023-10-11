using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Light Presets/New Light Preset")]
public class LightPreset : ScriptableObject
{
    [ColorUsage(true, true)]
    public Color AmbientColor;
    public Color DirectionalColor;
    public float DirectionalIntensity;
    public Vector3 DirectionalRotation;
    public Color FogColor;
    public float FogDensity;
}
