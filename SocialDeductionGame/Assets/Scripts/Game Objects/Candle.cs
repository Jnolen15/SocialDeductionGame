using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Candle : MonoBehaviour
{
    // ================= Refrences =================
    [SerializeField] private ParticleSystem _flameFX;
    [SerializeField] private Material _purpleMat;
    [SerializeField] private Material _redMat;
    [SerializeField] private List<MeshRenderer> _candleRenderers;
    [SerializeField] private Transform _candle;
    [SerializeField] private List<GameObject> _suffering;
    [SerializeField] private GameObject _sacrifce;

    // ================= Function =================
    public void SetupCandle(int numSuffering, bool isFinal)
    {
        Show();

        _candle.rotation = Quaternion.Euler(new Vector3(0, Random.Range(0, 360), 0));

        if (isFinal)
        {
            SetCandleMat(_redMat);
            _sacrifce.SetActive(true);
        }
        else
        {
            SetCandleMat(_purpleMat);

            for (int i = 0; i < numSuffering; i++)
            {
                _suffering[i].SetActive(true);
            }
        }

        _flameFX.Stop();
    }

    private void SetCandleMat(Material mat)
    {
        foreach (MeshRenderer renderer in _candleRenderers)
        {
            renderer.material = mat;
        }
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
