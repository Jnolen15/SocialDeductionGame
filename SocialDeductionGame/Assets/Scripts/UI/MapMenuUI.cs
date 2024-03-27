using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using Sirenix.OdinInspector;

public class MapMenuUI : MonoBehaviour
{
    // ================== Refrences ==================
    #region Refrences
    [Header("Colors")]
    [SerializeField] private WatchColors _gpsColors;
    [SerializeField] private List<WatchColors> _gpsColorList;
    [Header("UI Refrences")]
    [SerializeField] private GameObject _book;
    [SerializeField] private GameObject _gps;
    [SerializeField] private Transform _map;
    [SerializeField] private GameObject _movePointsDisplay;
    [SerializeField] private CanvasGroup _movePointsWarning;
    [SerializeField] private List<Image> _movePointList;
    [SerializeField] private List<LocationInfoEntry> _locationInfoList;
    private int _locationListIndex;
    private PlayerData _pData;

    [System.Serializable]
    public class LocationInfoEntry
    {
        public LocationManager.LocationName Location;
        public GameObject Page;
        public Vector3 MapPos;

        public void SetDisabled()
        {
            Page.gameObject.SetActive(false);
        }

        public Vector3 SetEnabled()
        {
            Page.gameObject.SetActive(true);
            return MapPos;
        }
    }
    #endregion

    // ================== Setup ==================
    #region Setup
    public void OnEnable()
    {
        TabButtonUI.OnMapPressed += ToggleMap;
        GameManager.OnStateChange += StateChangeEvent;
        PlayerHealth.OnDeath += Hide;
        PlayerData.OnMovePointsModified += UpdateMovePoints;
        PlayerData.OnMaxMovePointsModified += ChangeMaxMovePoints;
        PlayerData.OnNoMoreMovePoints += ShowNoMPWarning;
    }

    private void Start()
    {
        int colorIndex = PlayerPrefs.GetInt("WatchColor");
        if (colorIndex < _gpsColorList.Count)
            SetColorPallet(_gpsColorList[colorIndex]);
        else
            Debug.LogWarning("Watch color index out of bounds " + colorIndex + " of " + _gpsColorList.Count);

        MoveSpecific(0);
        Hide();

        _pData = this.GetComponentInParent<PlayerData>();
    }

    private void OnDisable()
    {
        TabButtonUI.OnMapPressed -= ToggleMap;
        GameManager.OnStateChange -= StateChangeEvent;
        PlayerHealth.OnDeath -= Hide;
        PlayerData.OnMovePointsModified -= UpdateMovePoints;
        PlayerData.OnMaxMovePointsModified -= ChangeMaxMovePoints;
        PlayerData.OnNoMoreMovePoints -= ShowNoMPWarning;
    }
    #endregion

    // ================== Basic UI ==================
    #region Basic UI
    public void ToggleMap()
    {
        if (!_book.activeSelf)
            Show();
        else
            Hide();
    }

    public void Hide()
    {
        _book.SetActive(false);
        _gps.SetActive(false);
    }

    public void Show()
    {
        _book.SetActive(true);
        _gps.SetActive(true);
    }

    public void StateChangeEvent(GameManager.GameState prev, GameManager.GameState current)
    {
        Hide();
    }
    #endregion

    // ================== UI Function ==================
    #region UI Function
    public void MoveNext()
    {
        _locationListIndex++;

        if (_locationListIndex >= _locationInfoList.Count)
            _locationListIndex = 0;

        UpdateLocation();
    }

    public void MovePrevious()
    {
        _locationListIndex--;

        if (_locationListIndex <= -1)
            _locationListIndex = _locationInfoList.Count - 1;

        UpdateLocation();
    }

    public void MoveSpecific(int index)
    {
        _locationListIndex = index;

        if (_locationListIndex <= -1 || _locationListIndex >= _locationInfoList.Count)
        {
            Debug.LogWarning("MapMenuUI MoveSpecific: Index out of range");
            return;
        }

        UpdateLocation();
    }

    public void TravelToLocation()
    {
        Debug.Log("GPS Travel button pressed attempting to move to " + _locationInfoList[_locationListIndex].Location.ToString());

        if (_pData != null)
            _pData.MoveLocation(_locationInfoList[_locationListIndex].Location.ToString());
        else
            Debug.LogError("No Player Data refrence in MapMenuUI");
    }

    private void UpdateLocation()
    {
        for (int i = 0; i < _locationInfoList.Count; i++)
        {
            if (i == _locationListIndex)
            {
                UpdateGPS(_locationInfoList[i].SetEnabled());
            }
            else
            {
                _locationInfoList[i].SetDisabled();
            }
        }
    }

    private void UpdateGPS(Vector3 pos)
    {
        _map.DOKill();

        _map.DOLocalMove(new Vector2(pos.x, pos.y), 0.5f).SetEase(Ease.OutSine);
        _map.localScale = new Vector2(pos.z, pos.z);
    }

    private void UpdateMovePoints(int ModifiedAmmount, int newTotal)
    {
        if (ModifiedAmmount == 0)
            return;

        // Update segments
        int place = 0;
        foreach (Image image in _movePointList)
        {
            // Update color
            if (place <= newTotal - 1)
                image.color = _gpsColors.GetPrimaryColor();
            else
                image.color = _gpsColors.GetSecondaryColor();

            place++;
        }
    }

    private void ChangeMaxMovePoints(int prev, int newTotal)
    {
        if(newTotal > 4)
        {
            Debug.LogWarning("MapMenuUI ChangeMaxMovePoints: New max move points is greater than 4, cant show on UI!");
            return;
        }

        // Destroy old
        foreach(Image mp in _movePointList)
        {
            mp.gameObject.SetActive(false);
        }

        // Create new
        for(int i = 0; i < newTotal; i++)
        {
            _movePointList[i].gameObject.SetActive(true);
        }
    }

    private void ShowNoMPWarning(int nan, int wan)
    {
        _movePointsDisplay.SetActive(false);
        _movePointsWarning.DOKill();
        _movePointsWarning.gameObject.SetActive(true);

        Sequence WarningSequence = DOTween.Sequence();
        WarningSequence.Append(_movePointsWarning.DOFade(1, 0.2f))
          .AppendInterval(1)
          .Append(_movePointsWarning.DOFade(0, 0.2f))
          .AppendCallback(() => _movePointsWarning.gameObject.SetActive(false))
          .AppendCallback(() => _movePointsDisplay.SetActive(true));
    }
    #endregion

    // ================== Colors ==================
    #region Colors
    public void SetColorPallet(WatchColors colors)
    {
        Debug.Log("Setting GPS color");

        _gpsColors = colors;

        ColorSetter[] coloredObjs = this.GetComponentsInChildren<ColorSetter>(true);

        foreach (ColorSetter cs in coloredObjs)
        {
            cs.SetColor(colors);
        }
    }

    [Button("TestSetColors")]
    private void TestSetColors(WatchColors colors)
    {
        if (!colors)
            return;

        SetColorPallet(colors);
    }
    #endregion
}
