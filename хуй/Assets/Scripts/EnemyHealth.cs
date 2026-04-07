using UnityEngine;
using System;
using System.Collections;

public class EnemyHealth : MonoBehaviour
{
    public static event Action<EnemyHealth> OnAnyEnemyDied;

    private Renderer rend;
    private Color originalColor;

    public int hp = 3;

    public AudioClip hitClip;
    public AudioClip deathClip;

    private AudioSource audioSource;
    public Team team = Team.Enemy;

    private bool isDead = false;
    public bool IsDead => isDead;

    private ActorId lastHitBy = ActorId.None;
    public ActorId LastHitBy => lastHitBy;

    public ActorId VictimId
    {
        get
        {
            var identity = GetComponent<ActorIdentity>();
            return identity != null ? identity.actorId : ActorId.None;
        }
    }

    private Round2v2Manager roundManager;

    private void Start()
    {
        roundManager = FindObjectOfType<Round2v2Manager>();

        rend = GetComponent<Renderer>();
        if (rend != null)
            originalColor = rend.material.color;

        audioSource = GetComponent<AudioSource>();
    }

    public void TakeDamage(int dmg, ActorId attackerId = ActorId.None)
    {
        if (roundManager != null && roundManager.CurrentState != Round2v2Manager.State.Round)
            return;

        if (isDead) return;

        lastHitBy = attackerId;

        if (audioSource != null && hitClip != null)
            audioSource.PlayOneShot(hitClip);

        if (rend != null)
            StartCoroutine(HitFlash());

        hp = Mathf.Max(hp - dmg, 0);

        if (hp <= 0)
            Die();
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        OnAnyEnemyDied?.Invoke(this);

        if (deathClip != null)
        {
            GameObject tempAudio = new GameObject("DeathSound");
            AudioSource a = tempAudio.AddComponent<AudioSource>();
            a.clip = deathClip;
            a.spatialBlend = 0f;
            a.volume = 1f;
            a.Play();
            Destroy(tempAudio, deathClip.length);
        }

        Destroy(gameObject);
    }

    private IEnumerator HitFlash()
    {
        if (rend == null) yield break;

        rend.material.color = Color.white;
        yield return new WaitForSeconds(0.1f);

        if (rend != null)
            rend.material.color = originalColor;
    }
}
