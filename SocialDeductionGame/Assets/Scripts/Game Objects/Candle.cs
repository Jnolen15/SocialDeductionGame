using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Candle : MonoBehaviour
{
    // ================= Refrences =================
    [SerializeField] private ParticleSystem _flameFX;
    [SerializeField] private Material _purpleMat;
    [SerializeField] private Material _redMat;
    private MeshRenderer _renderer;

    // ================= Function =================
    public void SetupCandle(bool isFinal)
    {
        Show();

        _renderer = this.GetComponent<MeshRenderer>();

        if (isFinal)
            _renderer.material = _redMat;
        else
            _renderer.material = _purpleMat;

        _flameFX.Stop();
    }

    public void Light()
    {
        _flameFX.Play();
    }

    public void Extinguish()
    {
        _flameFX.Stop();
    }

    public void Show()
    {
        this.gameObject.SetActive(true);
    }

    public void Hide()
    {
        this.gameObject.SetActive(false);
    }
}
