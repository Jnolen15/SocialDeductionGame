using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class Lockbox : LimitedTimeObject, ICardPlayable
{
    // ================ Variables ================
    [Header("Lockbox details")]
    [SerializeField] private int _keyCardID;
    [SerializeField] private float _KeyWeight;
    [SerializeField] private NetworkVariable<int> _numLocks = new(writePerm: NetworkVariableWritePermission.Server);
    [SerializeField] private NetworkVariable<bool> _opened = new(writePerm: NetworkVariableWritePermission.Server);
    [SerializeField] private List<GameObject> _locks;
    private CardManager _cardManager;

    // ================ Setup ================
    void Start()
    {
        _numLocks.OnValueChanged += LockRemoved;

        if (!IsServer)
            return;

        _cardManager = GameObject.FindGameObjectWithTag("CardManager").GetComponent<CardManager>();
        _cardManager.InjectCards(_location, _keyCardID, _KeyWeight, 4);
    }

    private void OnDisable()
    {
        _numLocks.OnValueChanged -= LockRemoved;
    }

    // ================ Lockbox Function ================
    public void RemoveLock()
    {
        RemoveLockServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    public void RemoveLockServerRpc()
    {
        Debug.Log("Lock Removed!");

        _numLocks.Value--;

        if (_numLocks.Value <= 0)
            _opened.Value = true;
    }

    public void LockRemoved(int prev, int current)
    {
        if (_locks.Count <= 0)
            return;

        GameObject lockToBreak = _locks[0];
        _locks.RemoveAt(0);

        Destroy(lockToBreak);
    }

    // ================ Interface ================
    public bool CanPlayCardHere(Card cardToPlay)
    {
        if (cardToPlay.HasTag("Key") && !_opened.Value)
            return true;

        return false;
    }
}
