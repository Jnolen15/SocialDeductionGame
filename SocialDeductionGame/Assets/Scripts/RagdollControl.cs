using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RagdollControl : MonoBehaviour
{
    [SerializeField] private float _thrust;
    private Rigidbody[] _rigidbodies;
    private Animator _animator;

    void Start()
    {
        _rigidbodies = this.GetComponentsInChildren<Rigidbody>();
        _animator = GetComponent<Animator>();

        DisableRagdoll();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            EnableRagdoll();
            _rigidbodies[0].AddForce(-transform.forward * _thrust, ForceMode.Impulse);
        }
    }

    private void DisableRagdoll()
    {
        foreach(Rigidbody rb in _rigidbodies)
        {
            rb.isKinematic = true;
        }

        _animator.enabled = true;
    }

    private void EnableRagdoll()
    {
        foreach (Rigidbody rb in _rigidbodies)
        {
            rb.isKinematic = false;
        }

        _animator.enabled = false;
    }
}
