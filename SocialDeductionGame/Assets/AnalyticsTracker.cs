using System.Collections;
using System.Collections.Generic;
using Unity.Services.Analytics;
using UnityEngine;

public class AnalyticsTracker : MonoBehaviour
{
    // ============== Singleton pattern ==============
    #region Singleton
    public static AnalyticsTracker Instance { get; private set; }
    private void InitializeSingleton()
    {
        if (Instance != null && Instance != this)
            Destroy(gameObject);
        else
            Instance = this;

        DontDestroyOnLoad(gameObject);
    }

    private void Awake()
    {
        InitializeSingleton();
    }
    #endregion

    // ================== Analytics Functions ==================
    #region Lobby Actions
    public void TrackLobbyCreated()
    {
        AnalyticsService.Instance.RecordEvent("LobbyCreated");
    }

    public void TrackQuickJoin()
    {
        AnalyticsService.Instance.RecordEvent("LobbyQuickJoin");
    }

    public void TrackCodeJoin()
    {
        AnalyticsService.Instance.RecordEvent("LobbyJoinWithCode");
    }

    public void TrackIDJoin()
    {
        AnalyticsService.Instance.RecordEvent("LobbyJoinWithID");
    }

    public void TrackGameSettings(int numSabos, int survivorWinDay, string timerLengths)
    {
        CustomEvent gameSettingsEvent = new CustomEvent("GameSettings")
        {
            { "NumSabos", numSabos },
            { "EndingDay", survivorWinDay },
            { "TimerLengths", timerLengths },
        };
        AnalyticsService.Instance.RecordEvent(gameSettingsEvent);
    }
    #endregion

    #region Game Actions
    public void TrackCacheOpen(int day)
    {
        CustomEvent cacheEvent = new CustomEvent("CacheOpened")
                {
                    { "EndingDay", day },
                };
        AnalyticsService.Instance.RecordEvent(cacheEvent);
    }

    public void TrackTotemActivated(int day)
    {
        CustomEvent totemEvent = new CustomEvent("TotemActivated")
                {
                    { "EndingDay", day },
                };
        AnalyticsService.Instance.RecordEvent(totemEvent);
    }

    public void TrackStockpileResult(bool wasSuccessful, bool earnedBonus, bool wasSabotaged)
    {
        CustomEvent stockpileEvent = new CustomEvent("StockpileRecap")
        {
            { "ActionSuccessful", wasSuccessful },
            { "EarnedBonus", earnedBonus },
            { "WasSabotaged", wasSabotaged },
        };
        AnalyticsService.Instance.RecordEvent(stockpileEvent);
    }

    public void TrackPlayerExiled(bool wasSurvivor, int day)
    {
        CustomEvent exileEvent = new CustomEvent("ExilePlayer")
        {
            { "WasSurvivor", wasSurvivor },
            { "WasSabo", !wasSurvivor },
            { "EndingDay", day },
        };
        AnalyticsService.Instance.RecordEvent(exileEvent);
    }

    public void TrackPlayerDeath(int day)
    {
        CustomEvent deathEvent = new CustomEvent("PlayerDeath")
            {
                { "EndingDay", day },
            };
        AnalyticsService.Instance.RecordEvent(deathEvent);
    }
    
    public void TrackPlayerTakeDamage(int ammount, string source)
    {
        CustomEvent damageEvent = new CustomEvent("PlayerTakenDamage")
            {
                { "SourceAmmount", ammount },
                { "DamageSource", source },
            };
        AnalyticsService.Instance.RecordEvent(damageEvent);
    }

    public void TrackGameEnd(bool survivorWin, int day, int players, int sabos)
    {
        CustomEvent winEvent = new CustomEvent("GameEnd")
        {
            { "SurvivorWin", survivorWin },
            { "SaboteurWin", !survivorWin },
            { "EndingDay", day },
            { "NumPlayers", players },
            { "NumSabos", sabos },
        };
        AnalyticsService.Instance.RecordEvent(winEvent);
    }
    #endregion
}
