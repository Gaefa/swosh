using System.Collections.Generic;
using UnityEngine;

public class EnemyKillCounter2v2 : MonoBehaviour
{
    private readonly HashSet<int> processedVictims = new HashSet<int>();
    private Round2v2Manager roundManager;

    private void Awake()
    {
        roundManager = FindObjectOfType<Round2v2Manager>();
    }

    private void OnEnable()
    {
        EnemyHealth.OnAnyEnemyDied += HandleEnemyDied;
    }

    private void OnDisable()
    {
        EnemyHealth.OnAnyEnemyDied -= HandleEnemyDied;
    }

    private void HandleEnemyDied(EnemyHealth enemy)
    {
        if (enemy == null) return;

        // считаем киллы только в активном раунде
        if (roundManager != null && roundManager.CurrentState != Round2v2Manager.State.Round)
            return;

        int victimInstanceId = enemy.gameObject.GetInstanceID();
        if (processedVictims.Contains(victimInstanceId))
            return;

        processedVictims.Add(victimInstanceId);

        if (MatchStats.Instance != null)
        {
            MatchStats.Instance.RegisterKill(enemy.LastHitBy, enemy.VictimId);
        }
    }
}