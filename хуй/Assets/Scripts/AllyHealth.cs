using UnityEngine;

public class AllyHealth : MonoBehaviour
{
    public Team team = Team.Player;

    public int maxHP = 3;
    public int hp = 3;

    private bool isDead = false;
    private ActorId lastHitBy = ActorId.None;

    private Round2v2Manager roundManager;

    private void Start()
    {
        roundManager = FindObjectOfType<Round2v2Manager>();
    }

    public void TakeDamage(int dmg, ActorId attackerId = ActorId.None)
    {
        // урон только в бою
        if (roundManager != null && roundManager.CurrentState != Round2v2Manager.State.Round)
            return;

        if (isDead) return;

        lastHitBy = attackerId;
        hp -= dmg;

        if (hp <= 0)
            Die();
    }

    private void Die()
    {
        isDead = true;

        // статистика — ТОЛЬКО в активном раунде
        if (MatchStats.Instance != null &&
            roundManager != null &&
            roundManager.CurrentState == Round2v2Manager.State.Round)
        {
            var identity = GetComponent<ActorIdentity>();
            ActorId victimId = identity != null ? identity.actorId : ActorId.Ally;

            MatchStats.Instance.RegisterKill(lastHitBy, victimId);
        }

        Destroy(gameObject);
    }
}