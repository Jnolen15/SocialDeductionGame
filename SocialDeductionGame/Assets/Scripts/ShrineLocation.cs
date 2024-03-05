using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShrineLocation : MonoBehaviour
{
    // ============== Refrences / Variables ==============
    #region Refrences / Variables
    [SerializeField] private Transform _camPosFar;
    [SerializeField] private Transform _camPosClose;
    [SerializeField] private List<Candle> _candles;
    private bool _candlesSetup;
    private Camera _mainCam;
    #endregion

    // ============== Setup ==============
    private void Start()
    {
        GameManager.OnStateMidnight += SetCamPos;
        SufferingManager.OnShrineLevelUp += UpdateShrineCandles;

        _mainCam = Camera.main;
    }

    private void OnDisable()
    {
        GameManager.OnStateMidnight -= SetCamPos;
        SufferingManager.OnShrineLevelUp += UpdateShrineCandles;
    }

    // ============== Function ==============
    private void SetCamPos()
    {
        _mainCam.transform.position = _camPosFar.localToWorldMatrix.GetPosition();
        _mainCam.transform.rotation = _camPosFar.localToWorldMatrix.rotation;
    }

    private void SetupShrineCandles(int maxLevel)
    {
        for (int i = 0; i < maxLevel; i++)
        {
            _candles[i].SetupCandle(i == maxLevel-1);
        }

        _candlesSetup = true;
    }

    private void UpdateShrineCandles(int maxLevel, int newLevel, int numSuffering, bool deathReset)
    {
        if (!_candlesSetup)
            SetupShrineCandles(maxLevel);

        foreach(Candle candle in _candles)
        {
            candle.Extinguish();
        }

        for (int i = 0; i < newLevel; i++)
        {
            _candles[i].Light();
        }
    }
}
