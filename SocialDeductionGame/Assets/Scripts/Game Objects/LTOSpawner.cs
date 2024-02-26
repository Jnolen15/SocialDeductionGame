using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

// Limited Time Object Spawner
public class LTOSpawner : NetworkBehaviour
{
    // ===================== Refrences / Variables =====================
    [SerializeField] private List<LimitedTimeObjectEntry> _ltoList;
    [SerializeField] private List<LTOSpawnLocation> _ltoSpawnLocationList;
    [SerializeField] private int _spawnIncreaseMod;

    [System.Serializable]
    public class LimitedTimeObjectEntry
    {
        public GameObject LTOPrefab;
        public int Lifetime;
        public int AvailableAfterDay;
        public float BaseSpawnChance;
        public bool SpawnOnce;
        [Header("Dont set")]
        public float CurrentSpawnChance;
        public int DaysSinceLastSpawn;
        public bool Spawned;
    }

    [System.Serializable]
    public class LTOSpawnLocation
    {
        public LocationManager.LocationName LocationName;
        public Transform SpawnPoint;
        public LimitedTimeObject CurrentLTO;
        public bool HasLTO;
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
    private LimitedTimeObjectEntry GetLTOObject()
    {
        foreach (LimitedTimeObjectEntry lto in _ltoList)
        {
            if (lto.SpawnOnce && lto.Spawned)
                Debug.Log("LTO already Spawned");
            else if (lto.AvailableAfterDay <= GameManager.Instance.GetCurrentDay())
            {
                // The longer since a spawn, the more likely one is
                int rand = Random.Range(0, 101);

                Debug.Log($"<color=yellow>SERVER: </color>Testing LTO Spawn Base {lto.BaseSpawnChance} + " +
                    $"{(_spawnIncreaseMod * lto.DaysSinceLastSpawn)}, Days since last {lto.DaysSinceLastSpawn}. Rolled {rand}");

                if (rand <= (lto.BaseSpawnChance + (_spawnIncreaseMod * lto.DaysSinceLastSpawn)))
                {
                    lto.DaysSinceLastSpawn = 0;
                    lto.Spawned = true;
                    return lto;
                }
                else
                    lto.DaysSinceLastSpawn += 1;
            }
        }

        return null;
    }

    private LTOSpawnLocation GetOpenSpawnLocation()
    {
        foreach (LTOSpawnLocation location in _ltoSpawnLocationList)
        {
            if (!location.HasLTO)
            {
                return location;
            }
        }

        return null;
    }

    private void TestSpawnLTO()
    {
        if (!IsServer)
            return;

        // Check free locations to spawn it at
        LTOSpawnLocation locationToSpawn = GetOpenSpawnLocation();
        if (locationToSpawn == null)
        {
            Debug.Log("All LTO Locations full!");
            return;
        }

        // Check to see if any LTOs can be spawned
        LimitedTimeObjectEntry ltoToSpawn = GetLTOObject();
        if (ltoToSpawn == null)
        {
            Debug.Log("No LTO qualified to spawn");
            return;
        }

        // Spawn it
        SpawnLTO(ltoToSpawn, locationToSpawn);
    }

    private void SpawnLTO(LimitedTimeObjectEntry lto, LTOSpawnLocation location)
    {
        Debug.Log($"<color=yellow>SERVER: </color> Spawning LTO {lto.LTOPrefab.name} at {location.LocationName}");

        location.CurrentLTO = Instantiate(lto.LTOPrefab, location.SpawnPoint).GetComponent<LimitedTimeObject>();
        location.CurrentLTO.GetComponent<NetworkObject>().Spawn();

        location.CurrentLTO.SetupLTO(lto.Lifetime+1, location.LocationName);

        SendSpawnEventClientRpc(location.LocationName);

        location.HasLTO = true;
    }

    private void TestLTOLifetimes()
    {
        if (!IsServer)
            return;

        foreach (LTOSpawnLocation location in _ltoSpawnLocationList)
        {
            if (!location.HasLTO)
                return;

            // Coutdown life and test to see if died
            if (location.CurrentLTO.CoutdownLife())
            {
                Debug.Log("<color=yellow>SERVER: </color> LTO out of life, destroying");

                // if so despawn it
                //_currentLTO.GetComponent<NetworkObject>().Despawn();
                Destroy(location.CurrentLTO.gameObject);
                //_currentLTO.DestroySelf();

                // Send Despawn event for night recap
                SendDespawnEventClientRpc(location.LocationName);

                location.CurrentLTO = null;
                location.HasLTO = false;
            }
        }
    }

    [ClientRpc]
    private void SendSpawnEventClientRpc(LocationManager.LocationName locationName)
    {
        OnLTOSpawned?.Invoke(locationName);
    }

    [ClientRpc]
    private void SendDespawnEventClientRpc(LocationManager.LocationName locationName)
    {
        OnLTODespawned?.Invoke(locationName);
    }
}
