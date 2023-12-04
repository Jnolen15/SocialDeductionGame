using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using DG.Tweening;

public class ForageUI : MonoBehaviour
{
    // ===================== Refrernces =====================
    [SerializeField] private Transform _cardZone;
    [SerializeField] private GameObject _forageButton;
    [SerializeField] private GameObject _totemWarning;
    [SerializeField] private TextMeshProUGUI _threatLevelText;
    [SerializeField] private TextMeshProUGUI _dangerText;
    [SerializeField] private Image _dangerIcon;
    [SerializeField] private Sprite[] _dangerIconStages;
    [SerializeField] private Color _lowColor = new Color32(233, 195, 41, 255);
    [SerializeField] private Color _medColor = new Color32(217, 116, 24, 255);
    [SerializeField] private Color _highColor = new Color32(206, 60, 24, 255);
    [SerializeField] private GameObject _takeNoneButton;
    [SerializeField] private CanvasGroup _clawMarks;
    private Forage _forage;

    // ===================== Setup =====================
    #region Setup
    private void Start()
    {
        _forage = GetComponentInParent<Forage>();

        Totem.OnTotemMenuOpened += OnTotemOpened;
        Totem.OnTotemMenuClosed += OnTotemClosed;
    }

    private void OnDestroy()
    {
        Totem.OnTotemMenuOpened -= OnTotemOpened;
        Totem.OnTotemMenuClosed -= OnTotemClosed;
    }
    #endregion

    // ===================== Functions =====================
    #region Functions
    public void Show()
    {
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private void OnTotemOpened(LocationManager.LocationName locationName)
    {
        if (locationName == _forage.GetForageLocation())
            Hide();
    }

    private void OnTotemClosed(LocationManager.LocationName locationName)
    {
        if (locationName == _forage.GetForageLocation())
            Show();
    }

    public void ShowCards()
    {
        _forageButton.SetActive(false);
        _takeNoneButton.SetActive(true);
        _cardZone.gameObject.SetActive(true);
    }

    public void HideCards()
    {
        _forageButton.SetActive(true);
        _takeNoneButton.SetActive(false);
        _cardZone.gameObject.SetActive(false);
    }

    public void DealCardObjects(List<GameObject> cardObjs)
    {
        ShowCards();

        foreach (GameObject cardObj in cardObjs)
        {
            cardObj.transform.SetParent(_cardZone);
            cardObj.transform.localScale = Vector3.one;
        }
    }

    public void ClearCards()
    {
        foreach (Transform child in _cardZone)
        {
            Destroy(child.gameObject);
        }
    }

    public void UpdateDangerUI(float current, bool totemActive)
    {
        _dangerText.text = current.ToString("F0");

        if (current <= 40)
        {
            _threatLevelText.text = "Low";
            //_threatLevelText.color = _lowColor;
            _dangerText.color = _lowColor;
            _forageButton.GetComponent<Image>().color = _lowColor;
            _dangerIcon.sprite = _dangerIconStages[0];
        }
        else if (40 < current && current <= 80)
        {
            _threatLevelText.text = "Medium";
            //_threatLevelText.color = _medColor;
            _dangerText.color = _medColor;
            _forageButton.GetComponent<Image>().color = _medColor;
            _dangerIcon.sprite = _dangerIconStages[1];
        }
        else if (80 < current)
        {
            _threatLevelText.text = "High";
            //_threatLevelText.color = _highColor;
            _dangerText.color = _highColor;
            _forageButton.GetComponent<Image>().color = _highColor;
            _dangerIcon.sprite = _dangerIconStages[2];
        }
    }

    public void UpdateTotemWarning(bool totemActive)
    {
        if (totemActive)
            _totemWarning.SetActive(true);
        else
            _totemWarning.SetActive(false);
    }

    public void PunchCard(GameObject card)
    {
        card.transform.DOKill();
        card.transform.DOPunchScale(new Vector3(0.3f, 0.3f, 0.3f), 0.5f, 6, 0.8f);
    }

    public void ShowClawMarks()
    {
        _clawMarks.gameObject.SetActive(true);

        Sequence ClawSequence = DOTween.Sequence();
        ClawSequence.Append(_clawMarks.transform.DOScale(new Vector3(1f, 1f, 1f), 0.1f))
          .AppendInterval(1)
          .Append(_clawMarks.DOFade(0, 2f).OnComplete(() => CloseClaw()));
    }

    public void CloseClaw()
    {
        _clawMarks.transform.DOKill();
        _clawMarks.DOKill();
        _clawMarks.alpha = 1;
        _clawMarks.transform.localScale = new Vector3(1.1f, 1.1f, 1.1f);
        _clawMarks.gameObject.SetActive(false);
    }
    #endregion
}
