using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

// Limited Time Object Spawner
public class LTOSpawner : NetworkBehaviour
{
    // ===================== Refrences / Variables =====================
    [SerializeField] private List<LimitedTimeObjectEntry> _ltoList;
    [SerializeField] private List<Transform> _spawnPointList;
    [SerializeField] private LimitedTimeObject _currentLTO;
    [SerializeField] private LocationManager.LocationName _locationName;
    [SerializeField] private int _spawnIncreaseMod;
    private int _daysSinceLastSpawn;
    private bool _hasLTO;
    //private Dictionary<Transform, ILimitedTimeObject> _spawnPointDict;

    [System.Serializable]
    public class LimitedTimeObjectEntry
    {
        public int AvailableAfterDay;
        public float SpawnChance;
        public int Lifetime;
        public GameObject LTOPrefab;
    }

    public delegate void LTOAction(LocationManager.LocationName locationName);
    public static event LTOAction OnLTOSpawned;
    public static event LTOAction OnLTODespawned;

    // ===================== Setup =====================
    #region Setup
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            GameManager.OnStateAfternoon += TestSpawnLTO;
            GameManager.OnStateEvening += TestLTOLifetimes;
        }
    }

    private void Start()
    {
        /*foreach (Transform trans in _spawnPointList)
        {
            _spawnPointDict.Add(trans, null);
        }*/
    }

    public override void OnDestroy()
    {
        if (IsServer)
        {
            GameManager.OnStateAfternoon -= TestSpawnLTO;
            GameManager.OnStateEvening -= TestLTOLifetimes;
        }

        // Invoke the base when using networkobject
        base.OnDestroy();
    }
    #endregion

    // ===================== TESTING =====================
    /*private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            SpawnLTO(_ltoList[0]);
        }
    }*/

    // ===================== Function =====================
    private void TestSpawnLTO()
    {
        if (!IsServer)
            return;

        if (_hasLTO)
            return;

        // Go through LTO list
        foreach (LimitedTimeObjectEntry lto in _ltoList)
        {
            // Tf after day
            if(lto.AvailableAfterDay <= GameManager.Instance.GetCurrentDay())
            {
                // Test to see if its spawned.
                // If not spawned, days since last spawn increases
                // Spawn mod * days since last is added to spawn chance.
                // So the longer since a spawn, the more likely one is

                int rand = Random.Range(0, 101);

                Debug.Log($"<color=yellow>SERVER: </color>Testing LTO Spawn Base {lto.SpawnChance} + " +
                    $"{(_spawnIncreaseMod * _daysSinceLastSpawn)}, Days since last {_daysSinceLastSpawn}. Rolled {rand}");

                if (rand <= (lto.SpawnChance + (_spawnIncreaseMod * _daysSinceLastSpawn)))
                    SpawnLTO(lto);
                else
                    _daysSinceLastSpawn += 1;
            }
        }
    }

    private void SpawnLTO(LimitedTimeObjectEntry lto)
    {
        Debug.Log("<color=yellow>SERVER: </color> Spawning LTO!");

        // If spawned, instantiate prefab, setup: Give it lifetime
        // Spawn it on position and update dictionary
        // Send spawn event for night recap
        int rand = Random.Range(0, _spawnPointList.Count);

        _currentLTO = Instantiate(lto.LTOPrefab, _spawnPointList[rand]).GetComponent<LimitedTimeObject>();
        _currentLTO.GetComponent<NetworkObject>().Spawn();

        //_spawnPointDict[_spawnPointList[rand]] = newLTO;
        _currentLTO.SetupLTO(lto.Lifetime+1);

        SendSpawnEventClientRpc();

        _hasLTO = true;
        _daysSinceLastSpawn = 0;
    }

    private void TestLTOLifetimes()
    {
        if (!IsServer)
            return;

        if (!_hasLTO)
            return;

        // Coutdown life and test to see if died
        if (_currentLTO.CoutdownLife())
        {
            Debug.Log("<color=yellow>SERVER: </color> LTO out of life, destroying");

            // if so despawn it
            //_currentLTO.GetComponent<NetworkObject>().Despawn();
            Destroy(_currentLTO.gameObject);
            //_currentLTO.DestroySelf();

            // Send Despawn event for night recap
            SendDespawnEventClientRpc();

            _currentLTO = null;
            _hasLTO = false;
        }
    }

    [ClientRpc]
    private void SendSpawnEventClientRpc()
    {
        OnLTOSpawned?.Invoke(_locationName);
    }

    [ClientRpc]
    private void SendDespawnEventClientRpc()
    {
        OnLTODespawned?.Invoke(_locationName);
    }
}
