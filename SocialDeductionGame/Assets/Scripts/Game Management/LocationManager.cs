using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LocationManager : MonoBehaviour
{
    public delegate void ChangeLocationAction(string locationName);
    public static event ChangeLocationAction OnLocationChanged;

    // ====================== Player Functions ======================
    #region Player Functions
    public void SetLocation(string locationName)
    {
        OnLocationChanged(locationName);
    }
    #endregion
}
