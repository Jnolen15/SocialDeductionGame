using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class TrailerCardShow : MonoBehaviour
{
    [SerializeField] private NightEventCardVisual eventOne;
    [SerializeField] private NightEventCardVisual eventTwo;
    [SerializeField] private NightEventCardVisual eventThree;
    [SerializeField] private int eventIDOne;
    [SerializeField] private int eventIDTwo;
    [SerializeField] private int eventIDThree;
    [SerializeField] private List<CanvasGroup> cards = new List<CanvasGroup>();

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            eventOne.Setup(eventIDOne, 6);
            eventTwo.Setup(eventIDTwo, 5);
            eventThree.Setup(eventIDThree, 4);

            StartCoroutine(DealCardObjectsAnimated(cards));
        }
    }

    private IEnumerator DealCardObjectsAnimated(List<CanvasGroup> cardObjs)
    {
        foreach (CanvasGroup cardObj in cardObjs)
        {
            cardObj.alpha = 0;
        }

        yield return new WaitForSeconds(0.5f);

        foreach (CanvasGroup cardObj in cardObjs)
        {
            cardObj.DOFade(1, 0.2f);

            PunchCard(cardObj.gameObject, 0.2f, 0.4f);

            yield return new WaitForSeconds(0.2f);
        }
    }

    public void PunchCard(GameObject card, float size, float duration)
    {
        card.transform.DOKill();
        card.transform.DOPunchScale(new Vector3(size, size, size), duration, 6, 0.8f);
    }
}
