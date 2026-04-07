using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class AllyChase : MonoBehaviour
{
    [Header("Move")]
    public float moveSpeed = 6f;
    public float stopDistance = 4.5f;

    [Header("Shoot")]
    public MagicBullet bulletPrefab;
    public Transform firePoint;
    public float shootRange = 12f;
    public float fireCooldown = 0.25f;

    [Header("Ammo")]
    public int magazineSize = 8;
    public float reloadTime = 1.0f;

    [Header("Teams")]
    public Team team = Team.Player;
    public Team targetTeam = Team.Enemy;

    private float lastShotTime;
    private int ammo;
    private bool isReloading;

    private Rigidbody rb;
    private Transform currentTarget;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        ammo = magazineSize;
    }

    private void Update()
    {
        if (currentTarget == null)
            currentTarget = FindNearestTarget();

        if (currentTarget == null) return;

        Vector3 toTarget = currentTarget.position - transform.position;
        toTarget.y = 0f;

        float dist = toTarget.magnitude;
        if (dist < 0.001f) return;

        Vector3 dir = toTarget.normalized;
        transform.forward = dir;

        if (dist <= shootRange && Time.time >= lastShotTime + fireCooldown)
        {
            if (!isReloading)
            {
                if (ammo <= 0)
                {
                    StartCoroutine(Reload());
                }
                else
                {
                    Shoot(dir);
                    ammo--;
                    lastShotTime = Time.time;

                    if (ammo <= 0)
                        StartCoroutine(Reload());
                }
            }
        }
    }

    private void FixedUpdate()
    {
        if (currentTarget == null) return;

        Vector3 toTarget = currentTarget.position - transform.position;
        toTarget.y = 0f;

        float dist = toTarget.magnitude;
        if (dist <= stopDistance) return;

        Vector3 dir = toTarget.normalized;
        rb.MovePosition(rb.position + dir * moveSpeed * Time.fixedDeltaTime);
    }

    private IEnumerator Reload()
    {
        if (isReloading) yield break;

        isReloading = true;
        yield return new WaitForSeconds(reloadTime);
        ammo = magazineSize;
        isReloading = false;
    }

    private void Shoot(Vector3 dir)
    {
        if (bulletPrefab == null || firePoint == null) return;

        MagicBullet bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.LookRotation(dir));
        bullet.ownerTeam = team;

        // ✅ ВАЖНО: чтобы киллы союзника считались
        var myIdComp = GetComponent<ActorIdentity>();
        bullet.ownerId = myIdComp != null ? myIdComp.actorId : ActorId.None;

        bullet.Launch(dir);
    }

    private Transform FindNearestTarget()
    {
        EnemyHealth[] enemies = FindObjectsOfType<EnemyHealth>();
        float bestDist = float.MaxValue;
        Transform best = null;
        Vector3 pos = transform.position;

        foreach (var e in enemies)
        {
            if (e == null) continue;
            if (e.team != targetTeam) continue;

            float d = (e.transform.position - pos).sqrMagnitude;
            if (d < bestDist)
            {
                bestDist = d;
                best = e.transform;
            }
        }

        return best;
    }
}