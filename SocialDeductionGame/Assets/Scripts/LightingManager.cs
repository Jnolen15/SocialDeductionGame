using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightingManager : MonoBehaviour
{
    // Code inspired by Porboably Spoonie
    // https://www.youtube.com/watch?v=m9hj9PdO328

    // ============== References ==============
    [SerializeField] private Light _directionalLight;
    [SerializeField] private LightPreset _dayLightPreset;
    [SerializeField] private LightPreset _afternoonLightPreset;
    [SerializeField] private LightPreset _nightLightPreset;

    // ============== Variables ==============
    private void Awake()
    {
        GameManager.OnStateChange += UpdateLighting;
    }

    private void Start()
    {
        //Try to get directional light from lighting tab
        if (RenderSettings.sun != null)
        {
            _directionalLight = RenderSettings.sun;
        }
        //Get first directional light in scene
        else
        {
            Light[] lights = GameObject.FindObjectsOfType<Light>();
            foreach (Light light in lights)
            {
                if (light.type == LightType.Directional)
                {
                    _directionalLight = light;
                    return;
                }
            }
        }
    }

    private void OnDisable()
    {
        GameManager.OnStateChange -= UpdateLighting;
    }

    private void UpdateLighting(GameManager.GameState prev, GameManager.GameState current)
    {
        if (current == GameManager.GameState.Pregame)
            return;

        LightPreset preset = _dayLightPreset;
        if (current == GameManager.GameState.Morning || current == GameManager.GameState.AfternoonTransition)
            preset = _dayLightPreset;
        else if (current == GameManager.GameState.Afternoon || current == GameManager.GameState.EveningTransition)
            preset = _afternoonLightPreset;
        else if (current == GameManager.GameState.Evening || current == GameManager.GameState.NightTransition 
                || current == GameManager.GameState.Night || current == GameManager.GameState.MorningTransition)
            preset = _nightLightPreset;

        //Set ambient and fog
        RenderSettings.ambientLight = preset.AmbientColor;
        RenderSettings.fogColor = preset.FogColor;
        RenderSettings.fogDensity = preset.FogDensity;

        //If directional light is set then rotate and set it's color
        if (_directionalLight != null)
        {
            _directionalLight.color = preset.DirectionalColor;
            _directionalLight.intensity = preset.DirectionalIntensity;
            _directionalLight.transform.localRotation = Quaternion.Euler(preset.DirectionalRotation);
        }
        else
        {
            Debug.LogError("No Direction Light Refrence");
        }

    }
}
