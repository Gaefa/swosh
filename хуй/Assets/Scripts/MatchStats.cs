using UnityEngine;

public class MatchStats : MonoBehaviour
{
    public static MatchStats Instance { get; private set; }

    public int playerKills, playerDeaths;
    public int allyKills, allyDeaths;
    public int enemy1Kills, enemy1Deaths;
    public int enemy2Kills, enemy2Deaths;

    // ✅ защита от двойного начисления за один и тот же фраг
    private int lastRegisterFrame = -1;
    private ActorId lastKiller = ActorId.None;
    private ActorId lastVictim = ActorId.None;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void ResetRoundStatsIfNeeded() { /* пока пусто */ }

    public void ResetMatchStats()
    {
        playerKills = playerDeaths = 0;
        allyKills = allyDeaths = 0;
        enemy1Kills = enemy1Deaths = 0;
        enemy2Kills = enemy2Deaths = 0;

        lastRegisterFrame = -1;
        lastKiller = ActorId.None;
        lastVictim = ActorId.None;
    }

    public void RegisterKill(ActorId killer, ActorId victim)
    {
        // ✅ если точно такой же вызов прилетел повторно в том же кадре — игнор
        int frame = Time.frameCount;
        if (frame == lastRegisterFrame && killer == lastKiller && victim == lastVictim)
            return;

        lastRegisterFrame = frame;
        lastKiller = killer;
        lastVictim = victim;

        if (killer != ActorId.None && killer != victim)
            AddKill(killer);

        if (victim != ActorId.None)
            AddDeath(victim);
    }

    private void AddKill(ActorId id)
    {
        switch (id)
        {
            case ActorId.Player: playerKills++; break;
            case ActorId.Ally: allyKills++; break;
            case ActorId.Enemy1: enemy1Kills++; break;
            case ActorId.Enemy2: enemy2Kills++; break;
        }
    }

    private void AddDeath(ActorId id)
    {
        switch (id)
        {
            case ActorId.Player: playerDeaths++; break;
            case ActorId.Ally: allyDeaths++; break;
            case ActorId.Enemy1: enemy1Deaths++; break;
            case ActorId.Enemy2: enemy2Deaths++; break;
        }
    }
}