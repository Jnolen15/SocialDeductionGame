public class GameRules
{
    // ========== Custopmizeable Game Rules ==========
    public int NumSaboteurs;
    public bool RoleReveal;
    public int NumDaysToWin;
    public TimerLengths TimerLength;
    public enum TimerLengths
    {
        Shorter,
        Normal,
        Longer
    }

    public float IntroTimerMax      = 5;
    public float MorningTimerMax    = 140;
    public float AfternoonTimerMax  = 80;
    public float EveningTimerMax    = 120;
    public float NightTimerMax      = 15;
    public float TransitionTimerMax = 2;

    // ========== Creation ==========
    public GameRules()
    {
        NumSaboteurs = 1;
        RoleReveal = true;
        NumDaysToWin = 8;
        TimerLength = TimerLengths.Normal;
    }

    public GameRules(int numSabos, bool revealRoles, int numDays, TimerLengths timerLengths)
    {
        NumSaboteurs = numSabos;
        RoleReveal = revealRoles;
        NumDaysToWin = numDays;
        TimerLength = timerLengths;
    }
}
