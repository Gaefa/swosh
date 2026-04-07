using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class MagicBullet : MonoBehaviour
{
    public float speed = 18f;
    public float lifeTime = 2f;
    public int damage = 1;

    public ActorId ownerId = ActorId.None;
    public Team ownerTeam = Team.Player;

    [Header("VFX")]
    [SerializeField] private GameObject hitEnemyVFX;
    [SerializeField] private GameObject hitWallVFX;
    [SerializeField] private float vfxOffset = 0.02f;

    private Rigidbody rb;
    private Round2v2Manager roundManager;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        int bulletLayer = LayerMask.NameToLayer("Bullet");
        if (bulletLayer != -1)
        {
            gameObject.layer = bulletLayer;
            Physics.IgnoreLayerCollision(bulletLayer, bulletLayer, true);
        }

        roundManager = FindObjectOfType<Round2v2Manager>();
    }

    private void OnEnable()
    {
        rb.linearVelocity = Vector3.zero;

        CancelInvoke(nameof(DestroySelf));
        Invoke(nameof(DestroySelf), lifeTime);
    }

    private void OnDisable()
    {
        CancelInvoke(nameof(DestroySelf));
    }

    public void Launch(Vector3 direction)
    {
        // ✅ НЕ режем Y — теперь пуля летит туда, куда смотришь
        if (direction.sqrMagnitude < 0.0001f)
            direction = transform.forward;

        rb.linearVelocity = direction.normalized * speed;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == gameObject.layer)
            return;

        if (roundManager != null && roundManager.CurrentState != Round2v2Manager.State.Round)
        {
            SpawnVFX(hitWallVFX);
            DestroySelf();
            return;
        }

        var enemy = other.GetComponentInParent<EnemyHealth>();
        if (enemy != null && enemy.team != ownerTeam)
        {
            enemy.TakeDamage(damage, ownerId);
            SpawnVFX(hitEnemyVFX);
            DestroySelf();
            return;
        }

        var player = other.GetComponentInParent<PlayerHealth>();
        if (player != null && player.team != ownerTeam)
        {
            player.TakeDamage(damage, ownerId);
            SpawnVFX(hitEnemyVFX);
            DestroySelf();
            return;
        }

        var ally = other.GetComponentInParent<AllyHealth>();
        if (ally != null && ally.team != ownerTeam)
        {
            ally.TakeDamage(damage, ownerId);
            SpawnVFX(hitEnemyVFX);
            DestroySelf();
            return;
        }

        SpawnVFX(hitWallVFX);
        DestroySelf();
    }

    private void SpawnVFX(GameObject vfxPrefab)
    {
        if (vfxPrefab == null) return;

        Vector3 dir = rb.linearVelocity.sqrMagnitude > 0.0001f ? rb.linearVelocity.normalized : transform.forward;
        Vector3 pos = transform.position - dir * vfxOffset;

        // ✅ НЕ прибиваем Y к 0.35 — иначе VFX тоже “в плоскости”
        // pos.y = 0.35f;

        var vfx = Instantiate(vfxPrefab, pos, Quaternion.LookRotation(-dir, Vector3.up));

        var ps = vfx.GetComponentInChildren<ParticleSystem>();
        if (ps != null)
        {
            var main = ps.main;
            main.useUnscaledTime = true;

            float life = main.duration;
            life += main.startLifetime.mode == ParticleSystemCurveMode.TwoConstants
                ? main.startLifetime.constantMax
                : main.startLifetime.constant;

            var killer = vfx.AddComponent<VFXAutoDestroyRealtime>();
            killer.life = Mathf.Max(0.5f, life + 0.1f);
        }
        else
        {
            var killer = vfx.AddComponent<VFXAutoDestroyRealtime>();
            killer.life = 2f;
        }
    }

    private void DestroySelf()
    {
        Destroy(gameObject);
    }
}