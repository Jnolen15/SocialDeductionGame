using System.Collections;
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
    private Camera _mainCam;

    // ================== Setup ==================
    private void OnEnable()
    {
        GameManager.OnStateIntro += HideWaitingForPlayersTS;
        GameManager.OnStateChange += Transition;
        //ExileManager.OnTrialVoteStarted += TransitionBlackFade;
        //ExileManager.OnTrialVoteEnded += TransitionBlackFade;
    }

    private void Start()
    {
        _mainCam = Camera.main;
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
                WaveTransitionOut();
                break;
            case GameManager.GameState.AfternoonTransition:
                WaveTransitionIn();
                break;
            case GameManager.GameState.Afternoon:
                WaveTransitionOut();
                break;
            case GameManager.GameState.EveningTransition:
                WaveTransitionIn();
                break;
            case GameManager.GameState.Evening:
                WaveTransitionOut();
                break;
            case GameManager.GameState.NightTransition:
                WaveTransitionIn();
                break;
            case GameManager.GameState.Night:
                WaveTransitionOut();
                break;
            case GameManager.GameState.MidnightTransition:
                FadeTransitionIn();
                break;
            case GameManager.GameState.Midnight:
                FadeTransitionOut();
                break;
            case GameManager.GameState.MorningTransition:
                WaveTransitionIn();
                break;
            case GameManager.GameState.GameOver:
                WaveTransitionOut();
                break;
        }
    }

    [Button]
    private void WaveTransitionIn()
    {
        _randSound.PlayRandom();

        _transitionWave.gameObject.SetActive(true);

        _transitionWave.DOLocalMoveY(_upPos, 1).SetEase(Ease.OutBack, 1);
    }

    [Button]
    private void WaveTransitionOut()
    {
        _transitionWave.DOLocalMoveY(_hiddenPos, 1).SetEase(Ease.InOutBack).OnComplete(() => { _transitionWave.gameObject.SetActive(false); });
    }

    public void FadeTransitionIn()
    {
        _transitionBlack.gameObject.SetActive(true);

        float yPos = _mainCam.transform.position.y - 1.5f;
        _mainCam.transform.DOMoveY(yPos, 1f);
        _transitionBlack.DOFade(1, 1f);
    }

    public void FadeTransitionOut()
    {
        StartCoroutine(FadeTransitionWithFade());
    }

    private IEnumerator FadeTransitionWithFade()
    {
        _transitionBlack.gameObject.SetActive(true);

        yield return new WaitForSeconds(0.1f);

        float yPos = _mainCam.transform.position.y - 1.5f;
        _mainCam.transform.DOMoveY(yPos, 1f);
        _transitionBlack.DOFade(0, 1f).OnComplete(
            () => { _transitionBlack.gameObject.SetActive(false); } );
    }

    #endregion
}
