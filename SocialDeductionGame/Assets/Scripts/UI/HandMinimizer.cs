using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

public class HandMinimizer : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    // =================== Refrences ===================
    [SerializeField] private Transform _hand;
    [SerializeField] private Transform _defaultPos;
    [SerializeField] private Transform _minimizedPos;
    [SerializeField] private bool _minimized;
    [SerializeField] private bool _hovering;

    [SerializeField] private float _bufferTimerMax;
    [SerializeField] private float _bufferTimer;

    // =================== Function ===================
    public void OnPointerEnter(PointerEventData eventData)
    {
        _hovering = true;
        _bufferTimer = _bufferTimerMax;

        if (_minimized)
            Maximize();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_minimized)
            return;

        _hovering = false;
        _bufferTimer = _bufferTimerMax;
    }

    private void Maximize()
    {
        _minimized = false;
        //_hand.position = _defaultPos.position;
        _hand.DOMoveY(_defaultPos.position.y, 0.2f).SetEase(Ease.InSine);
    }

    private void Minimize()
    {
        _minimized = true;
        //_hand.position = _minimizedPos.position;
        _hand.DOMoveY(_minimizedPos.position.y, 0.2f).SetEase(Ease.InSine);
    }

    private void Update()
    {
        if (_hovering)
            return;

        if (_bufferTimer > 0)
            _bufferTimer -= Time.deltaTime;
        else if (!_minimized)
            Minimize();
    }
}
