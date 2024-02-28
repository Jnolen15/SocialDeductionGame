using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening;

public class SufferingUI : MonoBehaviour
{
    // ================== Refrences / Variables ==================
    [SerializeField] private GameObject _sufferingUI;
    [SerializeField] private TextMeshProUGUI _sufferingNumTxt;
    [SerializeField] private CanvasGroup _sufferingReason;
    [SerializeField] private TextMeshProUGUI _sufferingReasonTxt;
    private bool _isSabo;
    private Sequence _sufferingReasonSequence;

    // ================== Setup ==================
    #region Setup
    private void Start()
    {
        GameManager.OnStateIntro += Setup;
        SufferingManager.OnSufferingModified += UpdateSufferingUI;
    }

    private void OnDisable()
    {
        GameManager.OnStateIntro -= Setup;
        SufferingManager.OnSufferingModified -= UpdateSufferingUI;
    }

    private void Setup()
    {
        if (PlayerConnectionManager.Instance.GetLocalPlayerTeam() == PlayerData.Team.Saboteurs)
        {
            _isSabo = true;
            _sufferingUI.SetActive(true);
        }
    }
    #endregion

    // ================== UI ==================
    #region UI
    private void UpdateSufferingUI(int changedVal, int newTotal, int reasonCode)
    {
        if (!_isSabo)
            return;

        _sufferingNumTxt.text = newTotal.ToString();

        string msg;
        switch (reasonCode)
        {
            case 0:
                msg = $"+{changedVal} Suffering, Test Reason";
                break;
            case 101:
                msg = $"+{changedVal} Daily Suffering";
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
                msg = $"Suffering Changed By {changedVal}";
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
}
