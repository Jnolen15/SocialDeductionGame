using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using TMPro;
using DG.Tweening;

public class SufferingManager : NetworkBehaviour
{
    // ============== Singleton pattern ==============
    #region Singleton
    public static SufferingManager Instance { get; private set; }
    private void InitializeSingleton()
    {
        if (Instance != null && Instance != this)
            Destroy(gameObject);
        else
            Instance = this;
    }
    #endregion

    // ================== Refrences / Variables ==================
    [Header("Suffering")]
    [SerializeField] private GameObject _sufferingUI;
    [SerializeField] private TextMeshProUGUI _sufferingNumTxt;
    [SerializeField] private CanvasGroup _sufferingReason;
    [SerializeField] private TextMeshProUGUI _sufferingReasonTxt;

    [SerializeField] private NetworkVariable<int> _netSufferning = new(writePerm: NetworkVariableWritePermission.Server);

    private bool _isSabo;
    private Sequence _sufferingReasonSequence;

    public delegate void SufferingValueModified(int ModifiedAmmount, int newTotal);
    public static event SufferingValueModified OnSufferingModified;

    // ================== Setup ==================
    #region Setup
    public override void OnNetworkSpawn()
    {
        GameManager.OnStateIntro += Setup;

        if(IsServer)
            GameManager.OnStateMorning += DailySuffering;

        InitializeSingleton();
    }

    private void OnDisable()
    {
        GameManager.OnStateIntro -= Setup;

        if(IsServer)
            GameManager.OnStateMorning -= DailySuffering;
    }

    private void Setup()
    {
        if (GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerData>().GetPlayerTeam() == PlayerData.Team.Saboteurs)
        {
            _isSabo = true;
            _sufferingUI.SetActive(true);
        }
    }
    #endregion

    // FOR TESTING
    private void Update()
    {
        if (!LogViewer.Instance.GetDoCheats())
            return;

        if (Input.GetKeyDown(KeyCode.Comma))
        {
            ModifySuffering(1, Random.Range(101, 105), true);
        }

        if (Input.GetKeyDown(KeyCode.Period))
        {
            ModifySuffering(-1, Random.Range(201, 205), true);
        }
    }

    // ================== Suffering ==================
    // Suffering Increment / Decrement
    #region Suffering
    public int GetCurrentSufffering()
    {
        if (!_isSabo)
            return -1;

        return _netSufferning.Value;
    }

    public void ModifySuffering(int ammount, int reasonCode, bool ServerOverride)
    {
        if (ServerOverride && IsServer)
            Debug.Log("<color=yellow>SERVER: </color> Modify Suffering Server Overide");
        else if (!_isSabo)
            return;

        ModifySufferingServerRPC(ammount, reasonCode, true);
    }

    [ServerRpc(RequireOwnership = false)]
    private void ModifySufferingServerRPC(int ammount, int reasonCode, bool add, ServerRpcParams serverRpcParams = default)
    {
        Debug.Log($"Suffering incremented by {ammount}");

        // temp for calculations
        int tempSuffering = _netSufferning.Value;

        if (add)
            tempSuffering += ammount;
        else
            tempSuffering = ammount;

        // Clamp Suffering within bounds
        if (tempSuffering < 0)
            tempSuffering = 0;
        else if (tempSuffering > 9)
            tempSuffering = 9;

        _netSufferning.Value = tempSuffering;

        UpdateSufferingUIClientRpc(ammount, _netSufferning.Value, reasonCode);
    }
    #endregion

    // UI
    #region UI
    [ClientRpc]
    private void UpdateSufferingUIClientRpc(int changedVal, int newVal, int reasonCode)
    {
        if (!_isSabo)
            return;

        _sufferingNumTxt.text = newVal.ToString();

        // Pick Reason Text
        string msg;
        switch (reasonCode)
        {
            case 0:
                msg = $"+{changedVal} Suffering, Test Reason";
                break;
            case 101:
                msg = $"+{changedVal} Daily Suffering";
                break;
            case 102:
                msg = $"+{changedVal} Stockpile Sabotage Attempt";
                break;
            case 103:
                msg = $"+{changedVal} Successful Stockpile Sabotage";
                break;
            case 104:
                msg = $"+{changedVal} Survivor Exiled";
                break;
            case 201:
                msg = $"{changedVal} Totem Awoken";
                break;
            case 202:
                msg = $"{changedVal} Night Event Re-Roll";
                break;
            case 203:
                msg = $"{changedVal} Night Event Enhanced";
                break;
            case 204:
                msg = $"{changedVal} Cache Opened";
                break;
            default:
                msg = $"Suffering Incremented By {changedVal}";
                break;
        }

        AnimateReason(msg);
    }

    private void AnimateReason(string msg)
    {
        _sufferingReason.gameObject.SetActive(true);
        _sufferingReasonTxt.text = msg;
        _sufferingReason.alpha = 1;

        if (!_sufferingReasonSequence.IsActive())
            CreateReasonAnimation();
        else if (_sufferingReasonSequence.IsPlaying())
            _sufferingReasonSequence.Restart();
    }

    private void CreateReasonAnimation()
    {
        _sufferingReasonSequence = DOTween.Sequence();
        _sufferingReasonSequence.Append(_sufferingReason.transform.DOLocalJump(_sufferingReason.transform.localPosition, 10f, 1, 0.25f))
          .AppendInterval(3)
          .Append(_sufferingReason.DOFade(0, 0.2f))
          .AppendCallback(() => _sufferingReason.gameObject.SetActive(false));
    }
    #endregion

    // Misc
    #region Misc
    // Earn daily suffering per sabo
    private void DailySuffering()
    {
        if (!IsServer)
            return;

        ModifySuffering(PlayerConnectionManager.Instance.GetNumLivingOnTeam(PlayerData.Team.Saboteurs), 101, true);
    }
    #endregion
}
