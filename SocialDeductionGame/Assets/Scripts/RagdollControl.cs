using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RagdollControl : MonoBehaviour
{
    // ================== Refrences / Variables ==================
    [SerializeField] private Transform _character;
    [SerializeField] private List<Material> _characterMatList = new();
    [SerializeField] private float _thrust;
    private GameObject _model;
    private Rigidbody[] _rigidbodies;
    private Animator _animator;

    // ================== Setup ==================
    public void Setup(int styleIndex, int materialIndex)
    {
        _rigidbodies = this.GetComponentsInChildren<Rigidbody>();
        _animator = GetComponent<Animator>();

        DisableRagdoll();

        // Set style
        _character.GetChild(0).gameObject.SetActive(false);

        // Set correct model active
        _model = _character.GetChild(styleIndex).gameObject;
        _model.SetActive(true);

        // Set Material
        _model.GetComponent<SkinnedMeshRenderer>().material = _characterMatList[materialIndex];
    }

    // ================== Function ==================
    private void DisableRagdoll()
    {
        foreach(Rigidbody rb in _rigidbodies)
        {
            rb.isKinematic = true;
        }

        _animator.enabled = true;
    }

    public void EnableRagdoll()
    {
        foreach (Rigidbody rb in _rigidbodies)
        {
            rb.isKinematic = false;
        }

        if (_rigidbodies.Length > 0)
            _rigidbodies[0].AddForce(transform.forward * _thrust, ForceMode.Impulse);

        _animator.enabled = false;
    }
}
