// using System.Collections;
// using TMPro;
// using UnityEngine;
// using UnityEngine.EventSystems;
//
// public enum WeaponType { Fast, Heavy }
//
// public class PlayerShoot : MonoBehaviour
// {
//     [Header("Shoot")]
//     public MagicBullet bulletPrefab;
//     public Transform firePoint;
//     public float fireCooldown = 0.15f;
//
//     [Header("Aim")]
//     public Camera aimCamera; // <-- назначь сюда Main Camera (в инспекторе)
//
//     [Header("Sound")]
//     public AudioClip shootClip;
//     private AudioSource audioSource;
//
//     [Header("Ammo")]
//     public int magazineSize = 10;
//
//     [Tooltip("Сколько патронов/зарядов лежит в запасе (RESERVE)")]
//     public int reserveAmmo = 40;
//
//     [Tooltip("Максимальный запас (RESERVE) для текущего оружия")]
//     public int maxReserveAmmo = 40;
//
//     public float reloadTime = 1.2f;
//     public TMP_Text ammoText;
//
//     private int ammo;
//     private bool isReloading;
//     private float lastFireTime;
//
//     private void Awake()
//     {
//         audioSource = GetComponent<AudioSource>();
//
//         if (magazineSize < 1) magazineSize = 1;
//         if (reserveAmmo < 0) reserveAmmo = 0;
//         if (maxReserveAmmo < 0) maxReserveAmmo = 0;
//
//         if (maxReserveAmmo == 0 && reserveAmmo > 0)
//             maxReserveAmmo = reserveAmmo;
//
//         if (reserveAmmo > maxReserveAmmo)
//             maxReserveAmmo = reserveAmmo;
//
//         ammo = magazineSize;
//         reserveAmmo = maxReserveAmmo;
//         UpdateAmmoUI();
//
//         if (aimCamera == null) aimCamera = Camera.main;
//     }
//
//     private void Update()
//     {
//         if (Time.timeScale == 0f) return;
//         if (isReloading) return;
//
//         if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
//             return;
//
//         if (Input.GetKeyDown(KeyCode.R))
//         {
//             TryStartReload();
//             return;
//         }
//
//         if (Input.GetMouseButton(0) && Time.time >= lastFireTime + fireCooldown)
//         {
//             if (ammo <= 0)
//             {
//                 TryStartReload();
//                 return;
//             }
//
//             Shoot();
//             ammo--;
//             UpdateAmmoUI();
//             lastFireTime = Time.time;
//         }
//     }
//
//     // ✅ ПОЛНОСТЬЮ ГОТОВЫЙ Shoot(): создаёт пулю, назначает ownerId, запускает
//     private void Shoot()
//     {
//         if (bulletPrefab == null || firePoint == null) return;
//
//         Vector3 dir = GetAimDirection();
//
//         MagicBullet bullet = Instantiate(
//             bulletPrefab,
//             firePoint.position,
//             Quaternion.LookRotation(dir, Vector3.up)
//         );
//
//         // ✅ ВАЖНО: помечаем пулю, кто её выпустил
//         var myIdentity = GetComponentInParent<ActorIdentity>();
//         bullet.ownerId = (myIdentity != null) ? myIdentity.actorId : ActorId.None;
//
//         // Если у тебя есть Team-логика — оставляем как было
//         bullet.ownerTeam = Team.Player;
//
//         bullet.Launch(dir);
//
//         if (audioSource != null && shootClip != null)
//             audioSource.PlayOneShot(shootClip);
//     }
//
//     private Vector3 GetAimDirection()
//     {
//         if (aimCamera == null)
//             return transform.forward;
//
//         Ray ray = aimCamera.ScreenPointToRay(Input.mousePosition);
//         Plane plane = new Plane(Vector3.up, new Vector3(0f, firePoint.position.y, 0f));
//
//         float enter;
//         if (plane.Raycast(ray, out enter))
//         {
//             Vector3 hit = ray.GetPoint(enter);
//             Vector3 dir = hit - firePoint.position;
//             dir.y = 0f;
//
//             if (dir.sqrMagnitude > 0.0001f)
//                 return dir.normalized;
//         }
//
//         return transform.forward;
//     }
//
//     private IEnumerator Reload()
//     {
//         if (isReloading) yield break;
//         if (ammo >= magazineSize) yield break;
//         if (reserveAmmo <= 0) yield break;
//
//         isReloading = true;
//
//         if (ammoText != null)
//             ammoText.text = "Reloading...";
//
//         yield return new WaitForSeconds(reloadTime);
//
//         int need = magazineSize - ammo;
//         int taken = Mathf.Min(need, reserveAmmo);
//
//         ammo += taken;
//         reserveAmmo -= taken;
//
//         isReloading = false;
//         UpdateAmmoUI();
//     }
//
//     private void UpdateAmmoUI()
//     {
//         if (ammoText != null)
//             ammoText.text = $"{ammo}/{reserveAmmo}";
//     }
//
//     private void TryStartReload()
//     {
//         if (isReloading) return;
//         if (ammo >= magazineSize) return;
//         if (reserveAmmo <= 0) return;
//
//         StartCoroutine(Reload());
//     }
//
//     public void SetWeapon(WeaponType type)
//     {
//         if (bulletPrefab == null) return;
//
//         if (type == WeaponType.Fast)
//         {
//             fireCooldown = 0.15f;
//             bulletPrefab.damage = 1;
//             bulletPrefab.lifeTime = 2f;
//             bulletPrefab.speed = 18f;
//
//             magazineSize = 10;
//             maxReserveAmmo = 40;
//             reloadTime = 1.2f;
//         }
//         else
//         {
//             fireCooldown = 0.6f;
//             bulletPrefab.damage = 3;
//             bulletPrefab.lifeTime = 2.2f;
//             bulletPrefab.speed = 16f;
//
//             magazineSize = 5;
//             maxReserveAmmo = 20;
//             reloadTime = 1.6f;
//         }
//
//         ammo = magazineSize;
//         reserveAmmo = maxReserveAmmo;
//         isReloading = false;
//         UpdateAmmoUI();
//     }
//
//     public void ResetWeapon()
//     {
//         StopAllCoroutines();
//         isReloading = false;
//
//         ammo = magazineSize;
//         reserveAmmo = maxReserveAmmo;
//
//         lastFireTime = 0f;
//         UpdateAmmoUI();
//     }
// }