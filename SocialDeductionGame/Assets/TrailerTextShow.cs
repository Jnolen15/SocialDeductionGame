using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class TrailerTextShow : MonoBehaviour
{
    [SerializeField] private List<CanvasGroup> objs = new List<CanvasGroup>();

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            StartCoroutine(DealCardObjectsAnimated(objs));
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

            yield return new WaitForSeconds(0.3f);
        }
    }

    public void PunchCard(GameObject card, float size, float duration)
    {
        card.transform.DOKill();
        card.transform.DOPunchScale(new Vector3(size, size, size), duration, 6, 0.8f);
    }
}
