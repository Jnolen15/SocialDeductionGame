using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;

public class TransitionManager : MonoBehaviour
{
    // ================== Refrences ==================
    [Header("Transition Screens")]
    [SerializeField] private GameObject _waitingForPlayersTS;
    [SerializeField] private Transform _transitionWave;
    [SerializeField] private CanvasGroup _transitionBlack;
    [SerializeField] private int _hiddenPos;
    [SerializeField] private int _upPos;
    [SerializeField] private PlayRandomSound _randSound;

    // ================== Setup ==================
    private void OnEnable()
    {
        GameManager.OnStateIntro += HideWaitingForPlayersTS;
        GameManager.OnStateChange += Transition;
        //ExileManager.OnTrialVoteStarted += TransitionBlackFade;
        //ExileManager.OnTrialVoteEnded += TransitionBlackFade;
    }

    private void OnDisable()
    {
        GameManager.OnStateIntro -= HideWaitingForPlayersTS;
        GameManager.OnStateChange -= Transition;
        //ExileManager.OnTrialVoteStarted -= TransitionBlackFade;
        //ExileManager.OnTrialVoteEnded -= TransitionBlackFade;
    }

    // ================== State Transitions ==================
    #region State Transitions
    private void HideWaitingForPlayersTS()
    {
        _waitingForPlayersTS.SetActive(false);
    }

    private void Transition(GameManager.GameState prev, GameManager.GameState current)
    {
        switch (current)
        {
            case GameManager.GameState.Morning:
                TransitionOut();
                break;
            case GameManager.GameState.AfternoonTransition:
                TransitionIn();
                break;
            case GameManager.GameState.Afternoon:
                TransitionOut();
                break;
            case GameManager.GameState.EveningTransition:
                TransitionIn();
                break;
            case GameManager.GameState.Evening:
                TransitionOut();
                break;
            case GameManager.GameState.NightTransition:
                TransitionIn();
                break;
            case GameManager.GameState.Night:
                TransitionOut();
                break;
            case GameManager.GameState.MorningTransition:
                TransitionIn();
                break;
            case GameManager.GameState.GameOver:
                TransitionOut();
                break;
        }
    }

    [Button]
    private void TransitionIn()
    {
        _randSound.PlayRandom();

        _transitionWave.gameObject.SetActive(true);

        _transitionWave.DOLocalMoveY(_upPos, 1).SetEase(Ease.OutBack, 1);
    }

    [Button]
    private void TransitionOut()
    {
        _transitionWave.DOLocalMoveY(_hiddenPos, 1).SetEase(Ease.InOutBack).OnComplete(() => { _transitionWave.gameObject.SetActive(false); });
    }

    public void TransitionBlackFade()
    {
        _transitionBlack.gameObject.SetActive(true);

        _transitionBlack.DOFade(1, 0.5f).OnComplete(
            () => _transitionBlack.DOFade(0, 1f).OnComplete(
                () => { _transitionBlack.gameObject.SetActive(false); }
                )
            );
    }
    #endregion
}
