using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using TMPro;

public class ShrineLocation : NetworkBehaviour
{
    // ============== Refrences / Variables ==============
    #region Refrences / Variables
    [SerializeField] private SufferingManager _sufferingManager;
    [SerializeField] private GameObject _shrineUI;
    [SerializeField] private TextMeshProUGUI _statusText;
    [SerializeField] private TextMeshProUGUI _substatusText;
    [SerializeField] private GameObject _sacrificeTimer;
    [SerializeField] private Image _sacrificeTimerFill;
    [SerializeField] private Transform _camPosFar;
    [SerializeField] private Transform _camPosClose;
    [SerializeField] private List<Pedestal> _pedestals;
    [SerializeField] private List<Candle> _candles;
    private int _maxLevel;
    private bool _sacrificeActive;
    private Camera _mainCam;
    #endregion

    // ============== Setup ==============
    #region Setup
    public override void OnNetworkSpawn()
    {
        GameManager.OnStateMidnight += SetCamPos;
        GameManager.OnStateMorning += HideStatusText;
        SufferingManager.OnShrineSetup += SetupShrineCandles;
        SufferingManager.OnShrineLevelUp += UpdateShrine;
        SufferingManager.OnSacrificeStarted += StartSacrifice;
        SufferingManager.OnPlayerSacrificed += PlayerSacrificed;

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
        GameManager.OnStateMorning -= HideStatusText;
        SufferingManager.OnShrineSetup -= SetupShrineCandles;
        SufferingManager.OnShrineLevelUp -= UpdateShrine;
        SufferingManager.OnSacrificeStarted -= StartSacrifice;
        SufferingManager.OnPlayerSacrificed -= PlayerSacrificed;

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
    #endregion

    private void Update()
    {
        if (_sacrificeActive)
        {
            _sacrificeTimerFill.fillAmount = (_sufferingManager.GetSacrificeTimer() / 15f);
        }
    }

    // ============== Function ==============
    #region Shrine Visuals
    private void SetCamPos()
    {
        _mainCam.transform.position = _camPosClose.localToWorldMatrix.GetPosition();
        _mainCam.transform.rotation = _camPosClose.localToWorldMatrix.rotation;
    }

    private void SetupShrineCandles(int maxLevel, int[] numSuffering)
    {
        _maxLevel = maxLevel;

        for (int i = 0; i < maxLevel; i++)
        {
            _candles[i].SetupCandle(numSuffering[i], i == maxLevel - 1);
        }
    }

    private void UpdateShrine(int newLevel, int numSuffering, bool deathReset)
    {
        foreach(Candle candle in _candles)
        {
            candle.Extinguish();
        }

        for (int i = 0; i < newLevel; i++)
        {
            _candles[i].Light();
        }

        // Update status Text
        if (deathReset)
            UpdateStatusText("Death resets shrines power.", $"Shrine level 1 of {_maxLevel}.");
        else if (newLevel < _maxLevel)
            UpdateStatusText("The Saboteur's power grows.", $"Shrine level {newLevel} of {_maxLevel}.");
        else
            UpdateStatusText("The island hungers, a sacrifice will be made next midnight.", $"Shrine has reached max level.");

        // Set pedestals non-interactable (in case it was just a sacrifice)
        foreach (Pedestal pedestal in _pedestals)
        {
            pedestal.SetInteractable(false);
        }
        _sacrificeActive = false;
        _sacrificeTimer.SetActive(false);

        // Update pedestals if there was a death
        if (!deathReset) return;
        foreach (Pedestal pedestal in _pedestals)
        {
            if (!pedestal.GetMarkedDead())
            {
                ulong id = pedestal.GetPlayerID();

                if (!PlayerConnectionManager.Instance.GetPlayerLivingByID(id))
                {
                    pedestal.SetPlayerDead();
                }
            }
        }
    }

    private void PlayerSacrificed(ulong pId)
    {
        foreach (Pedestal pedestal in _pedestals)
        {
            if (pedestal.GetPlayerID() == pId)
                pedestal.SetPlayerDead();
        }
    }

    private void StartSacrifice()
    {
        if (PlayerConnectionManager.Instance.GetLocalPlayerTeam() == PlayerData.Team.Saboteurs)
        {
            UpdateStatusText("Choose a sacrifice.", $"Place a skull upon its pedestal.");

            // Set pedestals interacion active
            foreach (Pedestal pedestal in _pedestals)
            {
                pedestal.SetInteractable(true);
            }
        }
        else
        {
            UpdateStatusText("A sacrifice is being chosen.", $"Could it be you?");
        }

        _sacrificeActive = true;
        _sacrificeTimer.SetActive(true);
    }

    public void ChooseSacrifice(ulong playerID)
    {
        ChooseSacrificeServerRpc(playerID);
    }

    [ServerRpc(RequireOwnership = false)]
    public void ChooseSacrificeServerRpc(ulong playerToSacrifce)
    {
        _sufferingManager.SetSacrificeVoteServerRpc(playerToSacrifce);

        ShowPotentialSacrificeClientRpc(playerToSacrifce);
    }

    [ClientRpc]
    public void ShowPotentialSacrificeClientRpc(ulong playerToSacrifce)
    {
        foreach (Pedestal pedestal in _pedestals)
        {
            if (pedestal.GetPlayerID() == playerToSacrifce)
                pedestal.SetSacrificeVote();
            else
                pedestal.ClearSacrificeVote();
        }
    }
    #endregion

    // ============== Status UI ==============
    #region Status UI
    private void UpdateStatusText(string statusText, string substatusText)
    {
        _shrineUI.SetActive(true);

        _statusText.text = statusText;
        _substatusText.text = substatusText;
    }

    private void HideStatusText()
    {
        _shrineUI.SetActive(false);
    }

    #endregion
}
