using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class EnemyChase : MonoBehaviour
{
    public enum BotDifficulty { Easy, Hard }

    [Header("Team")]
    public Team team = Team.Enemy;         // кто я
    public Team targetTeam = Team.Player;  // кого атакую

    [Header("Movement")]
    public float speed = 3f;

    [Header("Melee Attack (если без стрельбы)")]
    public float attackDistance = 1.2f;
    public float attackCooldown = 1f;
    public int damage = 1;

    [Header("Ranged Shoot (если задан bulletPrefab)")]
    public MagicBullet bulletPrefab;
    public Transform firePoint;
    public float shootRange = 12f;
    public float fireCooldown = 0.25f;

    [Header("Ammo")]
    public int magazineSize = 5;
    public float reloadTime = 1.4f;

    [Header("Obstacle Masks")]
    public LayerMask obstacleMask;   // Walls + Obstacles
    public LayerMask wallsMask;      // Only Walls
    public LayerMask obstaclesMask;  // Only Obstacles

    [Header("Avoidance")]
    public float lookAhead = 1.3f;
    public float sideLook = 1.2f;
    [Range(0f, 1f)] public float steerStrength = 0.85f;

    [Header("Anti-Stuck Memory")]
    public float avoidMemoryTime = 0.5f;
    private int avoidSide = 0;
    private float avoidSideUntil = 0f;

    [Header("Pressure / Strafe (CS feel)")]
    public float strafeStartDistance = 4f;
    [Range(0f, 1f)] public float strafeStrength = 0.45f;
    public float strafeSwitchTime = 1.6f;
    private int strafeSide = 1;
    private float nextStrafeSwitchTime = 0f;

    [Header("Vision")]
    public float visionHeight = 0.2f;
    public float maxVisionDistance = 30f;

    [Header("Direction Smoothing")]
    public float dirSmooth = 12f;
    private Vector3 smoothedDir = Vector3.forward;

    [Header("Anti-Stuck Escape")]
    public float stuckCheckTime = 0.35f;
    public float stuckMoveEpsilon = 0.05f;
    public float escapeTime = 0.6f;

    private Vector3 lastPos;
    private float stuckTimer;
    private float escapeUntil;
    private Vector3 escapeDir;

    [Header("Wall Follow")]
    public float wallFollowNoProgressTime = 1.2f;

    private int wallFollowSide = 0;
    private float noProgressTimer = 0f;
    private float lastDistToTarget;

    [Header("Repulsion")]
    public float repulsionRadius = 0.75f;
    public float repulsionStrength = 1.6f;
    public float repulsionMax = 0.9f;
    public int repulsionMaxColliders = 8;
    private readonly Collider[] repulsionHits = new Collider[16];

    private Transform target;
    private PlayerHealth playerHealth; // если цель — игрок
    private Rigidbody rb;
    private float lastAttackTime;
    private float lastShotTime;

    private int ammo;
    private bool isReloading;

    public void ApplyDifficulty(BotDifficulty difficulty)
    {
        if (difficulty == BotDifficulty.Easy)
        {
            speed *= 0.85f;
            attackCooldown *= 1.15f;
            strafeStrength *= 0.65f;
            strafeSwitchTime *= 1.25f;
            strafeStartDistance *= 0.85f;

            fireCooldown *= 1.10f;
            shootRange *= 0.95f;

            dirSmooth *= 0.9f;
            repulsionStrength *= 0.9f;
        }
        else
        {
            speed *= 1.10f;
            attackCooldown *= 0.90f;
            strafeStrength *= 1.20f;
            strafeSwitchTime *= 0.85f;
            strafeStartDistance *= 1.10f;

            fireCooldown *= 0.90f;
            shootRange *= 1.05f;

            dirSmooth *= 1.05f;
            repulsionStrength *= 1.05f;
        }

        strafeStrength = Mathf.Clamp01(strafeStrength);
        steerStrength = Mathf.Clamp01(steerStrength);
        dirSmooth = Mathf.Clamp(dirSmooth, 6f, 20f);
        attackCooldown = Mathf.Clamp(attackCooldown, 0.25f, 5f);
        fireCooldown = Mathf.Clamp(fireCooldown, 0.05f, 5f);
        speed = Mathf.Clamp(speed, 0.5f, 12f);
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        ammo = magazineSize;
    }

    private void Start()
    {
        lastPos = rb.position;
        smoothedDir = transform.forward;
        AcquireTarget();

        if (target != null)
            lastDistToTarget = Vector3.Distance(Flat(rb.position), Flat(target.position));
    }

    private void FixedUpdate()
    {
        if (target == null)
            AcquireTarget();
        if (target == null) return;

        Vector3 toTarget = target.position - transform.position;
        toTarget.y = 0f;
        float dist = toTarget.magnitude;

        UpdateStuckState();

        if (Time.time < escapeUntil)
        {
            Vector3 dir = EscapeSteer(escapeDir);

            // ✅ ВАЖНО: поворачиваемся куда движемся (как Ally)
            if (dir != Vector3.zero)
                transform.forward = dir;

            Move(dir);
            return;
        }

        Vector3 desiredDir = (dist > 0.001f) ? toTarget.normalized : Vector3.zero;
        bool hasLOS = HasLineOfSightToTarget();

        // ==== Стрельба (если задан bulletPrefab) ====
        if (bulletPrefab != null && firePoint != null)
        {
            if (hasLOS && dist <= shootRange && Time.time >= lastShotTime + fireCooldown)
            {
                if (!isReloading)
                {
                    if (ammo <= 0)
                    {
                        StartCoroutine(Reload());
                    }
                    else
                    {
                        Shoot(desiredDir);
                        ammo--;
                        lastShotTime = Time.time;

                        if (ammo <= 0)
                            StartCoroutine(Reload());
                    }
                }
            }
        }
        else
        {
            // ==== Мили ====
            if (dist <= attackDistance)
            {
                rb.linearVelocity = Vector3.zero;
                TryAttack();
                return;
            }
        }

        if (hasLOS && dist <= strafeStartDistance && desiredDir != Vector3.zero)
        {
            if (Time.time >= nextStrafeSwitchTime)
            {
                strafeSide = (Random.value > 0.5f) ? 1 : -1;
                nextStrafeSwitchTime = Time.time + strafeSwitchTime;
            }

            Vector3 side = new Vector3(-desiredDir.z, 0f, desiredDir.x) * strafeSide;
            desiredDir = (desiredDir + side * strafeStrength).normalized;
        }

        desiredDir = MoveWithAvoidanceAndWallFollow(desiredDir, hasLOS);

        Vector3 repel = ComputeRepulsion();
        if (repel != Vector3.zero)
            desiredDir = (desiredDir + repel).normalized;

        smoothedDir = Vector3.Lerp(smoothedDir, desiredDir, dirSmooth * Time.fixedDeltaTime).normalized;

        // ✅ ГЛАВНЫЙ ФИКС: всегда смотрим туда, куда движемся (как Ally)
        if (smoothedDir != Vector3.zero)
            transform.forward = smoothedDir;

        Move(smoothedDir);

        float curDist = Vector3.Distance(Flat(rb.position), Flat(target.position));
        if (curDist < lastDistToTarget - 0.05f) noProgressTimer = 0f;
        else noProgressTimer += Time.fixedDeltaTime;

        lastDistToTarget = curDist;

        if (wallFollowSide != 0 && noProgressTimer >= wallFollowNoProgressTime)
        {
            wallFollowSide *= -1;
            noProgressTimer = 0f;
        }
    }

    private IEnumerator Reload()
    {
        if (isReloading) yield break;

        isReloading = true;
        yield return new WaitForSeconds(reloadTime);
        ammo = magazineSize;
        isReloading = false;
    }

    private void AcquireTarget()
    {
        target = null;
        playerHealth = null;

        float best = float.MaxValue;
        Vector3 pos = transform.position;

        var pObj = GameObject.FindGameObjectWithTag("Player");
        if (pObj != null)
        {
            var ph = pObj.GetComponent<PlayerHealth>();
            if (ph != null && ph.team == targetTeam)
            {
                float d = (pObj.transform.position - pos).sqrMagnitude;
                best = d;
                target = pObj.transform;
                playerHealth = ph;
            }
        }

        var allies = FindObjectsOfType<AllyHealth>();
        foreach (var a in allies)
        {
            if (a == null) continue;
            if (a.team != targetTeam) continue;

            float d = (a.transform.position - pos).sqrMagnitude;
            if (d < best)
            {
                best = d;
                target = a.transform;
                playerHealth = null; // это не PlayerHealth
            }
        }
    }

    private Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

    private void Move(Vector3 dir)
    {
        if (dir == Vector3.zero) return;
        rb.MovePosition(rb.position + dir * speed * Time.fixedDeltaTime);
    }

    private bool HasLineOfSightToTarget()
    {
        if (target == null) return false;

        Vector3 origin = transform.position + Vector3.up * visionHeight;
        Vector3 to = (target.position + Vector3.up * visionHeight) - origin;
        to.y = 0f;

        float d = to.magnitude;
        if (d < 0.01f) return true;
        if (d > maxVisionDistance) return false;

        Vector3 dir = to / d;
        return !Physics.Raycast(origin, dir, d, obstacleMask);
    }

    private Vector3 MoveWithAvoidanceAndWallFollow(Vector3 desiredDir, bool hasLOS)
    {
        if (desiredDir == Vector3.zero) return desiredDir;

        Vector3 origin = transform.position + Vector3.up * visionHeight;

        if (hasLOS)
            wallFollowSide = 0;

        Vector3 left = new Vector3(-desiredDir.z, 0f, desiredDir.x);
        Vector3 right = new Vector3(desiredDir.z, 0f, -desiredDir.x);

        if (Physics.Raycast(origin, desiredDir, out RaycastHit hit, lookAhead, obstacleMask))
        {
            int layer = hit.collider.gameObject.layer;

            if (((1 << layer) & obstaclesMask.value) != 0)
            {
                bool leftBlocked = Physics.Raycast(origin, left, sideLook, obstacleMask);
                bool rightBlocked = Physics.Raycast(origin, right, sideLook, obstacleMask);

                if (Time.time < avoidSideUntil && avoidSide != 0)
                    desiredDir = (avoidSide < 0) ? left : right;
                else
                {
                    if (!leftBlocked && rightBlocked) avoidSide = -1;
                    else if (!rightBlocked && leftBlocked) avoidSide = +1;
                    else avoidSide = (Random.value > 0.5f) ? -1 : +1;

                    avoidSideUntil = Time.time + avoidMemoryTime;
                    desiredDir = (avoidSide < 0) ? left : right;
                }

                return desiredDir.normalized;
            }

            if (((1 << layer) & wallsMask.value) != 0)
            {
                Vector3 n = hit.normal; n.y = 0f;
                if (n.sqrMagnitude > 0.0001f) n.Normalize();

                Vector3 tangent = Vector3.Cross(Vector3.up, n).normalized;

                if (hasLOS)
                {
                    if (Vector3.Dot(tangent, desiredDir) < 0f) tangent = -tangent;
                    return Vector3.Lerp(desiredDir, tangent, steerStrength).normalized;
                }

                if (wallFollowSide == 0)
                    wallFollowSide = (Random.value > 0.5f) ? 1 : -1;

                Vector3 follow = (wallFollowSide > 0) ? tangent : -tangent;
                Vector3 combined = (follow + n * 0.2f).normalized;

                return Vector3.Lerp(desiredDir, combined, steerStrength).normalized;
            }
        }

        avoidSide = 0;
        return desiredDir;
    }

    private Vector3 ComputeRepulsion()
    {
        Vector3 origin = rb.position + Vector3.up * 0.1f;

        int count = Physics.OverlapSphereNonAlloc(origin, repulsionRadius, repulsionHits, obstacleMask);
        if (count <= 0) return Vector3.zero;

        Vector3 push = Vector3.zero;
        int used = 0;

        for (int i = 0; i < count && used < repulsionMaxColliders; i++)
        {
            Collider c = repulsionHits[i];
            if (c == null || c.isTrigger) continue;

            Vector3 closest = c.ClosestPoint(origin);
            Vector3 away = origin - closest;
            away.y = 0f;

            float d = away.magnitude;
            if (d < 0.0001f) continue;

            float t = Mathf.Clamp01((repulsionRadius - d) / repulsionRadius);
            push += (away / d) * t;
            used++;
        }

        if (push == Vector3.zero) return Vector3.zero;

        push = push.normalized * Mathf.Min(repulsionMax, push.magnitude) * repulsionStrength;
        return push;
    }

    private void UpdateStuckState()
    {
        float moved = Vector3.Distance(rb.position, lastPos);
        stuckTimer += Time.fixedDeltaTime;

        if (stuckTimer >= stuckCheckTime)
        {
            if (moved < stuckMoveEpsilon)
            {
                escapeUntil = Time.time + escapeTime;

                Vector2 r = Random.insideUnitCircle.normalized;
                escapeDir = new Vector3(r.x, 0f, r.y);
                if (escapeDir.sqrMagnitude < 0.01f)
                    escapeDir = transform.forward;

                wallFollowSide = 0;
                noProgressTimer = 0f;
            }

            lastPos = rb.position;
            stuckTimer = 0f;
        }
    }

    private Vector3 EscapeSteer(Vector3 dir)
    {
        Vector3 origin = transform.position + Vector3.up * visionHeight;

        if (Physics.Raycast(origin, dir, out RaycastHit hit, lookAhead, obstacleMask))
        {
            Vector3 n = hit.normal; n.y = 0f;
            if (n.sqrMagnitude > 0.0001f) n.Normalize();
            dir = Vector3.Reflect(dir, n).normalized;
        }

        smoothedDir = Vector3.Lerp(smoothedDir, dir, dirSmooth * Time.fixedDeltaTime).normalized;
        return smoothedDir;
    }

    private void TryAttack()
    {
        if (Time.time < lastAttackTime + attackCooldown) return;

        var myIdComp = GetComponent<ActorIdentity>();
        ActorId myId = myIdComp != null ? myIdComp.actorId : ActorId.None;

        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damage, myId);
            lastAttackTime = Time.time;
            return;
        }

        if (target != null)
        {
            var ally = target.GetComponent<AllyHealth>();
            if (ally != null)
            {
                ally.TakeDamage(damage, myId);
                lastAttackTime = Time.time;
            }
        }
    }

    private void Shoot(Vector3 dir)
    {
        if (bulletPrefab == null || firePoint == null) return;

        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;
        dir.Normalize();

        // ✅ тут оставляем, но уже не единственный поворот
        transform.forward = dir;

        MagicBullet bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.LookRotation(dir));
        bullet.ownerTeam = team;

        var myIdComp = GetComponent<ActorIdentity>();
        bullet.ownerId = myIdComp != null ? myIdComp.actorId : ActorId.None;

        bullet.Launch(dir);
    }
}