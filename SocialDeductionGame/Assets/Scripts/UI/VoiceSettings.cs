using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoiceSettings : MonoBehaviour
{
    // =================== Refrences ===================

    // =================== Functions ===================
    public void OnInputSliderChanged(float value)
    {
        Debug.Log("Adjusting input volume to " + value);
        VivoxManager.Instance.AdjustInputVolume((int)value);
    }
}
