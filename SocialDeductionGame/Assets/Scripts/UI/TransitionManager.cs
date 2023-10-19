using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class TransitionManager : MonoBehaviour
{
    // ================== Refrences ==================
    [Header("Transition Screens")]
    [SerializeField] private GameObject _waitingForPlayersTS;
    [SerializeField] private CanvasGroup _morningTS;
    [SerializeField] private CanvasGroup _afternoonTS;
    [SerializeField] private CanvasGroup _eveningTS;
    [SerializeField] private CanvasGroup _nightTS;

    // ================== Setup ==================
    private void OnEnable()
    {
        GameManager.OnStateIntro += HideWaitingForPlayersTS;
        GameManager.OnStateChange += Transition;
    }

    private void OnDisable()
    {
        GameManager.OnStateIntro -= HideWaitingForPlayersTS;
        GameManager.OnStateChange -= Transition;
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
                TransitionOut(_morningTS);
                break;
            case GameManager.GameState.AfternoonTransition:
                TransitionIn(_afternoonTS);
                break;
            case GameManager.GameState.Afternoon:
                TransitionOut(_afternoonTS);
                break;
            case GameManager.GameState.EveningTransition:
                TransitionIn(_eveningTS);
                break;
            case GameManager.GameState.Evening:
                TransitionOut(_eveningTS);
                break;
            case GameManager.GameState.NightTransition:
                TransitionIn(_nightTS);
                break;
            case GameManager.GameState.Night:
                TransitionOut(_nightTS);
                break;
            case GameManager.GameState.MorningTransition:
                TransitionIn(_morningTS);
                break;
        }
    }

    private void TransitionIn(CanvasGroup transitionScreen)
    {
        transitionScreen.gameObject.SetActive(true);
        transitionScreen.DOFade(1f, 0.5f);
    }

    private void TransitionOut(CanvasGroup transitionScreen)
    {
        transitionScreen.DOFade(0f, 0.5f).OnComplete(() => { transitionScreen.gameObject.SetActive(false); });
    }
    #endregion
}
