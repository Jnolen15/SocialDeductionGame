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
    [SerializeField] private List<int> _cardIDs;
    [SerializeField] private NetworkVariable<int> _numLocks = new(writePerm: NetworkVariableWritePermission.Server);
    [SerializeField] private NetworkVariable<bool> _opened = new(writePerm: NetworkVariableWritePermission.Server);
    [SerializeField] private List<GameObject> _locks;
    [SerializeField] private GameObject _closedCrate;
    [SerializeField] private GameObject _openedCrate;
    private CardManager _cardManager;

    // ================ Setup ================
    void Start()
    {
        _numLocks.OnValueChanged += LockRemoved;
        _opened.OnValueChanged += ShowOpenedCrate;

        _cardManager = GameObject.FindGameObjectWithTag("CardManager").GetComponent<CardManager>();

        if (IsServer)
            _cardManager.InjectCards(_location, _keyCardID, _KeyWeight, 4);
    }

    private void OnDisable()
    {
        _numLocks.OnValueChanged -= LockRemoved;
        _opened.OnValueChanged -= ShowOpenedCrate;
    }

    // ================ Lockbox Function ================
    public void RemoveLock()
    {
        RemoveLockServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    public void RemoveLockServerRpc(ServerRpcParams serverRpcParams = default)
    {
        // Get client data
        var clientId = serverRpcParams.Receive.SenderClientId;
        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { clientId }
            }
        };

        Debug.Log("Lock Removed!");

        _numLocks.Value--;

        if (_numLocks.Value <= 0)
        {
            _opened.Value = true;

            GiveCardClientRpc(PickCard(), clientRpcParams);
        }
    }

    private int PickCard()
    {
        if (!IsServer)
            return 0;

        if (_cardIDs.Count < 1)
            return 0;

        int rand = Random.Range(0, _cardIDs.Count);
        return _cardIDs[rand];
    }

    public void LockRemoved(int prev, int current)
    {
        if (_locks.Count <= 0)
            return;

        GameObject lockToBreak = _locks[0];
        _locks.RemoveAt(0);

        Destroy(lockToBreak);
    }

    // ================ Opening ================
    private void ShowOpenedCrate(bool prev, bool current)
    {
        _closedCrate.SetActive(false);
        _openedCrate.SetActive(true);
    }

    [ClientRpc]
    private void GiveCardClientRpc(int cardID, ClientRpcParams clientRpcParams = default)
    {
        _cardManager.GiveCard(cardID);
    }

    // ================ Interface ================
    public bool CanPlayCardHere(Card cardToPlay)
    {
        if (cardToPlay.HasTag("Key") && !_opened.Value)
            return true;

        return false;
    }
}
