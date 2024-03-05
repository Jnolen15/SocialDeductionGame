using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class ShrineLocation : NetworkBehaviour
{
    // ============== Refrences / Variables ==============
    #region Refrences / Variables
    [SerializeField] private Transform _camPosFar;
    [SerializeField] private Transform _camPosClose;
    [SerializeField] private List<Pedestal> _pedestals;
    [SerializeField] private List<Candle> _candles;
    private bool _candlesSetup;
    private Camera _mainCam;
    #endregion

    // ============== Setup ==============
    public override void OnNetworkSpawn()
    {
        GameManager.OnStateMidnight += SetCamPos;
        SufferingManager.OnShrineLevelUp += UpdateShrine;

        if (IsServer)
            GameManager.OnStateIntro += SetupShrine;
    }

    private void Start()
    {
        _mainCam = Camera.main;
    }

    private void OnDisable()
    {
        GameManager.OnStateMidnight -= SetCamPos;
        SufferingManager.OnShrineLevelUp -= UpdateShrine;

        if (IsServer)
            GameManager.OnStateIntro -= SetupShrine;
    }

    private void SetupShrine()
    {
        // Call to clients to setup pedestals
        SetupShrineClientRpc(PlayerConnectionManager.Instance.GetPlayerIDs().ToArray());
    }

    [ClientRpc]
    private void SetupShrineClientRpc(ulong[] playerIDs)
    {
        Debug.Log($"<color=pink>Setting up {playerIDs.Length} pedestals</color>");

        for (int i = 0; i < playerIDs.Length; i++)
        {
            string pName = PlayerConnectionManager.Instance.GetPlayerNameByID(playerIDs[i]);

            _pedestals[i].Show();
            _pedestals[i].SetupPedestal(playerIDs[i], pName);
        }
    }

    // ============== Function ==============
    private void SetCamPos()
    {
        _mainCam.transform.position = _camPosClose.localToWorldMatrix.GetPosition();
        _mainCam.transform.rotation = _camPosClose.localToWorldMatrix.rotation;
    }

    private void SetupShrineCandles(int maxLevel)
    {
        for (int i = 0; i < maxLevel; i++)
        {
            _candles[i].SetupCandle(i == maxLevel-1);
        }

        _candlesSetup = true;
    }

    private void UpdateShrine(int maxLevel, int newLevel, int numSuffering, bool deathReset)
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

        if (!deathReset) return;

        foreach (Pedestal pedestal in _pedestals)
        {
            if (!pedestal.GetSkullActive())
            {
                ulong id = pedestal.GetPlayerID();

                if (!PlayerConnectionManager.Instance.GetPlayerLivingByID(id))
                {
                    pedestal.SetPlayerDead();
                }
            }
        }
    }
}
