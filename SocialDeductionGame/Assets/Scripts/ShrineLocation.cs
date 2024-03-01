using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShrineLocation : MonoBehaviour
{
    // ============== Refrences / Variables ==============
    #region Refrences / Variables
    [SerializeField] private Transform _camPosFar;
    [SerializeField] private Transform _camPosClose;
    private Camera _mainCam;
    #endregion

    // ============== Setup ==============
    private void Start()
    {
        GameManager.OnStateMidnight += IntroTransition;

        _mainCam = Camera.main;
    }

    private void OnDisable()
    {
        GameManager.OnStateMidnight -= IntroTransition;
    }

    // ============== Function ==============
    private void IntroTransition()
    {
        _mainCam.transform.position = _camPosFar.localToWorldMatrix.GetPosition();
        _mainCam.transform.rotation = _camPosFar.localToWorldMatrix.rotation;
    }
}
