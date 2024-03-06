using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Candle : MonoBehaviour
{
    // ================= Refrences =================
    [SerializeField] private ParticleSystem _flameFX;
    [SerializeField] private Material _purpleMat;
    [SerializeField] private Material _redMat;
    [SerializeField] private MeshRenderer _renderer;
    [SerializeField] private List<GameObject> _suffering;
    [SerializeField] private GameObject _sacrifce;

    // ================= Function =================
    public void SetupCandle(int numSuffering, bool isFinal)
    {
        Show();

        if (isFinal)
        {
            _renderer.material = _redMat;
            _sacrifce.SetActive(true);
        }
        else
            _renderer.material = _purpleMat;

        for (int i = 0; i < numSuffering; i++)
        {
            _suffering[i].SetActive(true);
        }

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
