using System.Collections;
using TMPro;
using UnityEngine;

public enum WeaponType
{
    Fast,
    Heavy,
    Roklet
}

public class MobileShoot : MonoBehaviour
{
    [Header("Weapon Visuals (FPP Hands)")]
    public GameObject knifeWeaponObject;
    public GameObject fastWeaponObject;
    public GameObject heavyWeaponObject;
    public GameObject rokletWeaponObject;

    [Header("Weapon Visuals (TPP Undead WeaponHolder)")]
    public GameObject tppWeaponHolderRoot;
    public GameObject knifeWeaponObjectTPP;
    public GameObject fastWeaponObjectTPP;
    public GameObject heavyWeaponObjectTPP;
    public GameObject rokletWeaponObjectTPP;

    [Header("Start")]
    [Tooltip("Если true — при старте будет нож по умолчанию.")]
    public bool startWithKnife = true;

    [Header("Shoot")]
    public MagicBullet bulletPrefab;
    public Transform firePoint;
    public float fireCooldown = 0.15f;

    [Header("Knife")]
    public float knifeRange = 2f;
    public int knifeDamage = 2;
    public float knifeCooldown = 0.5f;

    [Header("Aim")]
    public Camera aimCamera;
    public float aimMaxDistance = 120f;
    public LayerMask aimMask = ~0;

    [Header("Impact")]
    public GameObject impactPrefab;

    [Header("Sound")]
    public AudioClip shootClip;
    private AudioSource audioSource;

    [Header("Ammo")]
    [SerializeField] private int magazineSize = 10;
    [SerializeField] private int maxReserveAmmo = 40;
    [SerializeField] private float reloadTime = 1.2f;

    [Header("Ammo UI")]
    public TMP_Text ammoCurrentText;
    public TMP_Text ammoReserveText;

    [Header("Lock")]
    [Tooltip("Блокирует ввод (например, когда игрок мёртв или открыт магазин/меню).")]
    public bool inputLocked = false;

    [Header("Recoil")]
    public WeaponRecoil recoil;

    [Header("Hands Animator (FPP)")]
    [SerializeField] private Animator handsAnimator;
    [SerializeField] private string showTriggerName = "Show";
    [SerializeField] private string reloadTriggerName = "Reload";
    [SerializeField] private string knifeAttackTriggerName = "Attack";
    [SerializeField] private string weaponIdIntName = "WeaponID";

    [Header("FPP State Names")]
    [SerializeField] private string knifeStateName = "Idle_UnArmed";
    [SerializeField] private string fastStateName = "Armed_anim_FlashBorn";
    [SerializeField] private string heavyStateName = "Armed_anim_ThunderComing";
    [SerializeField] private string rokletStateName = "Armed_anim_Roklet";

    [Header("TPP Animator")]
    [SerializeField] private Animator undeadAnimator;
    [SerializeField] private string tppWeaponIdName = "WeaponIDUA";

    [Header("TPP State Names")]
    [SerializeField] private string knifeTPPStateName = "Idle";
    [SerializeField] private string fastTPPStateName = "Idle_FlashBorn";
    [SerializeField] private string heavyTPPStateName = "Idle_ThunderComing";
    [SerializeField] private string rokletTPPStateName = "Idle_Roklet";

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private int ammoInMag;
    private int reserveAmmo;
    private bool isReloading;
    private float lastFireTime;

    private int currentDamage = 1;
    private float currentBulletLifeTime = 2f;
    private float currentBulletSpeed = 18f;

    // null = knife
    private WeaponType? currentWeapon = null;

    private const int WeaponId_Knife = 0;
    private const int WeaponId_Fast = 1;
    private const int WeaponId_Heavy = 2;
    private const int WeaponId_Roklet = 3;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        if (aimCamera == null)
            aimCamera = Camera.main;

        if (recoil == null)
        {
            recoil = GetComponentInChildren<WeaponRecoil>(true);
            if (recoil == null)
                recoil = GetComponentInParent<WeaponRecoil>();
        }

        if (tppWeaponHolderRoot != null)
            tppWeaponHolderRoot.SetActive(true);

        isReloading = false;
        lastFireTime = 0f;

        EquipKnifeDefault();

        UpdateAmmoUI();
        Log($"[MobileShoot] Awake | ammo={ammoInMag}/{magazineSize} reserve={reserveAmmo} weapon={(currentWeapon.HasValue ? currentWeapon.ToString() : "Knife")}");
    }

    public void SetTPPAnimator(Animator anim)
    {
        undeadAnimator = anim;

        if (undeadAnimator == null)
        {
            Log("[TPP Anim] SetTPPAnimator -> NULL");
            return;
        }

        if (currentWeapon == null)
        {
            SetTPPWeaponId(WeaponId_Knife);
            ForceTPPToWeaponIdle(null);
        }
        else
        {
            int weaponId = GetWeaponId(currentWeapon.Value);
            SetTPPWeaponId(weaponId);
            ForceTPPToWeaponIdle(currentWeapon);
        }

        Log($"[TPP Anim] switched animator -> {undeadAnimator.name}");
    }

    public void ShootPressed()
    {
        if (Time.timeScale == 0f) return;
        if (inputLocked) return;
        if (isReloading) return;

        // 🔪 Если нож в руках — бьём ножом той же кнопкой Shoot
        if (currentWeapon == null)
        {
            if (Time.time < lastFireTime + knifeCooldown) return;

            DoKnifeAttack();
            lastFireTime = Time.time;
            return;
        }

        if (Time.time < lastFireTime + fireCooldown) return;

        if (firePoint == null)
        {
            Log("[Shoot] blocked: firePoint null");
            return;
        }

        if (ammoInMag <= 0)
        {
            TryStartReload(true);
            return;
        }

        DoShoot();

        ammoInMag--;
        if (ammoInMag < 0) ammoInMag = 0;

        UpdateAmmoUI();
        lastFireTime = Time.time;

        Log($"[Shoot] fired | ammo={ammoInMag}/{magazineSize} reserve={reserveAmmo}");

        if (ammoInMag <= 0)
            TryStartReload(true);
    }

    public void ShowOrReloadPressed()
    {
        if (!isActiveAndEnabled) return;

        var ph = GetComponentInChildren<PlayerHealth>(true);
        if (ph != null && ph.IsDead) return;

        if (!Round2v2Manager.IsActionAllowedForAlivePlayer())
            return;

        if (currentWeapon == null)
        {
            PlayShow();
            return;
        }

        if (isReloading)
        {
            Log("[Action] blocked: reloading");
            return;
        }

        if (ammoInMag >= magazineSize)
        {
            PlayShow();
            return;
        }

        TryStartReload(true);
    }

    public void ReloadPressed()
    {
        ShowOrReloadPressed();
    }

    private void PlayShow()
    {
        if (handsAnimator == null)
        {
            Debug.LogWarning("[Anim] Show blocked: handsAnimator NULL");
            return;
        }

        handsAnimator.ResetTrigger(reloadTriggerName);
        handsAnimator.SetTrigger(showTriggerName);
        Log($"[Anim] Trigger: {showTriggerName}");
    }

    private void PlayReload()
    {
        if (handsAnimator == null)
        {
            Debug.LogWarning("[Anim] Reload blocked: handsAnimator NULL");
            return;
        }

        handsAnimator.ResetTrigger(showTriggerName);
        handsAnimator.SetTrigger(reloadTriggerName);
        Log($"[Anim] Trigger: {reloadTriggerName}");
    }

    private void SetHandsWeaponId(int id)
    {
        if (handsAnimator == null) return;
        handsAnimator.SetInteger(weaponIdIntName, id);
        Log($"[FPP Anim] {weaponIdIntName} = {id}");
    }

    private void SetTPPWeaponId(int id)
    {
        if (undeadAnimator == null) return;
        undeadAnimator.SetInteger(tppWeaponIdName, id);
        Log($"[TPP Anim] {tppWeaponIdName} = {id}");
    }

    private string GetHandsIdleStateName(WeaponType? type)
    {
        if (type == null) return knifeStateName;
        if (type == WeaponType.Fast) return fastStateName;
        if (type == WeaponType.Heavy) return heavyStateName;
        return rokletStateName;
    }

    private string GetTPPIdleStateName(WeaponType? type)
    {
        if (type == null) return knifeTPPStateName;
        if (type == WeaponType.Fast) return fastTPPStateName;
        if (type == WeaponType.Heavy) return heavyTPPStateName;
        return rokletTPPStateName;
    }

    private void ForceHandsToWeaponIdle(WeaponType? type)
    {
        if (handsAnimator == null)
        {
            Debug.LogWarning("[FPP Anim] ForceHandsToWeaponIdle blocked: handsAnimator NULL");
            return;
        }

        handsAnimator.ResetTrigger(showTriggerName);
        handsAnimator.ResetTrigger(reloadTriggerName);

        string stateName = GetHandsIdleStateName(type);
        int stateHash = Animator.StringToHash(stateName);

        if (!handsAnimator.HasState(0, stateHash))
        {
            Debug.LogWarning($"[FPP Anim] State not found in layer 0: {stateName}");
            return;
        }

        handsAnimator.Play(stateHash, 0, 0f);
        handsAnimator.Update(0f);

        Log($"[FPP Anim] Force state -> {stateName}");
    }

    private void ForceTPPToWeaponIdle(WeaponType? type)
    {
        if (undeadAnimator == null)
        {
            Debug.LogWarning("[TPP Anim] ForceTPPToWeaponIdle blocked: undeadAnimator NULL");
            return;
        }

        string stateName = GetTPPIdleStateName(type);
        int stateHash = Animator.StringToHash(stateName);

        if (!undeadAnimator.HasState(0, stateHash))
        {
            Debug.LogWarning($"[TPP Anim] State not found in layer 0: {stateName}");
            return;
        }

        undeadAnimator.Play(stateHash, 0, 0f);
        undeadAnimator.Update(0f);

        Log($"[TPP Anim] Force state -> {stateName}");
    }

    private void TryStartReload(bool playReloadAnimation)
    {
        if (currentWeapon == null) return;
        if (isReloading) return;
        if (ammoInMag >= magazineSize) return;
        if (reserveAmmo <= 0) return;

        if (playReloadAnimation)
            PlayReload();

        StartCoroutine(ReloadRoutine());
    }

    private IEnumerator ReloadRoutine()
    {
        isReloading = true;

        if (ammoCurrentText != null)
            ammoCurrentText.text = "…";

        yield return new WaitForSeconds(reloadTime);

        int need = magazineSize - ammoInMag;
        int taken = Mathf.Min(need, reserveAmmo);

        ammoInMag += taken;
        reserveAmmo -= taken;

        if (ammoInMag > magazineSize) ammoInMag = magazineSize;
        if (reserveAmmo < 0) reserveAmmo = 0;

        isReloading = false;
        UpdateAmmoUI();
    }

    private void DoKnifeAttack()
    {
        PlayKnifeAttack();

        if (aimCamera == null)
            aimCamera = Camera.main;

        Ray ray = aimCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (Physics.Raycast(ray, out RaycastHit hit, knifeRange, aimMask, QueryTriggerInteraction.Ignore))
        {
            var myIdentity = GetComponentInParent<ActorIdentity>();
            ActorId attackerId = myIdentity != null ? myIdentity.actorId : ActorId.None;

            var enemyHealth = hit.collider.GetComponentInParent<EnemyHealth>();
            if (enemyHealth != null && !enemyHealth.IsDead)
            {
                enemyHealth.TakeDamage(knifeDamage, attackerId);
                Log("[Knife] hit enemy");

                if (impactPrefab != null)
                    Instantiate(impactPrefab, hit.point, Quaternion.LookRotation(hit.normal));

                return;
            }

            var playerHealth = hit.collider.GetComponentInParent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(knifeDamage, attackerId);
                Log("[Knife] hit player");

                if (impactPrefab != null)
                    Instantiate(impactPrefab, hit.point, Quaternion.LookRotation(hit.normal));

                return;
            }

            var allyHealth = hit.collider.GetComponentInParent<AllyHealth>();
            if (allyHealth != null)
            {
                allyHealth.TakeDamage(knifeDamage, attackerId);
                Log("[Knife] hit ally");

                if (impactPrefab != null)
                    Instantiate(impactPrefab, hit.point, Quaternion.LookRotation(hit.normal));

                return;
            }

            Log("[Knife] hit non-damageable");
        }
        else
        {
            Log("[Knife] miss");
        }
    }

    private void PlayKnifeAttack()
    {
        if (handsAnimator == null)
        {
            Debug.LogWarning("[Anim] Knife attack blocked: handsAnimator NULL");
            return;
        }

        handsAnimator.ResetTrigger(showTriggerName);
        handsAnimator.ResetTrigger(reloadTriggerName);
        handsAnimator.ResetTrigger(knifeAttackTriggerName);
        handsAnimator.SetTrigger(knifeAttackTriggerName);

        Log($"[Anim] Trigger: {knifeAttackTriggerName}");
    }

    private void DoShoot()
    {
        if (bulletPrefab == null || firePoint == null) return;

        if (aimCamera == null)
            aimCamera = Camera.main;

        Ray ray = aimCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        Vector3 dir = ray.direction;

        if (Physics.Raycast(ray, out RaycastHit hit, aimMaxDistance, aimMask, QueryTriggerInteraction.Ignore))
        {
            dir = (hit.point - firePoint.position).normalized;

            if (impactPrefab != null)
                Instantiate(impactPrefab, hit.point, Quaternion.LookRotation(hit.normal));
        }
        else
        {
            dir = aimCamera.transform.forward.normalized;
        }

        MagicBullet bullet = Instantiate(
            bulletPrefab,
            firePoint.position,
            Quaternion.LookRotation(dir, Vector3.up)
        );

        bullet.damage = currentDamage;
        bullet.lifeTime = currentBulletLifeTime;
        bullet.speed = currentBulletSpeed;

        var myIdentity = GetComponentInParent<ActorIdentity>();
        bullet.ownerId = (myIdentity != null) ? myIdentity.actorId : ActorId.None;
        bullet.ownerTeam = Team.Player;

        bullet.Launch(dir);

        if (audioSource != null && shootClip != null)
            audioSource.PlayOneShot(shootClip);

        if (recoil != null)
        {
            float strength = 1f;
            if (currentWeapon == WeaponType.Fast) strength = 0.75f;
            else if (currentWeapon == WeaponType.Heavy) strength = 1.35f;
            else if (currentWeapon == WeaponType.Roklet) strength = 1.05f;

            recoil.Kick(strength);
        }
    }

    private void UpdateAmmoUI()
    {
        if (ammoCurrentText != null)
            ammoCurrentText.text = ammoInMag.ToString();

        if (ammoReserveText != null)
            ammoReserveText.text = reserveAmmo.ToString();
    }

    public void SetWeapon(WeaponType type)
    {
        StopAllCoroutines();
        isReloading = false;

        currentWeapon = type;

        int weaponId = GetWeaponId(type);

        SetWeaponVisuals(type);
        ApplyWeaponStats(type);

        ammoInMag = magazineSize;
        reserveAmmo = maxReserveAmmo;
        lastFireTime = 0f;

        SetHandsWeaponId(weaponId);
        SetTPPWeaponId(weaponId);

        ForceHandsToWeaponIdle(type);
        ForceTPPToWeaponIdle(type);

        UpdateAmmoUI();

        Log($"[Weapon] equipped {type}");
    }

    public void UnequipWeapon()
    {
        StopAllCoroutines();
        isReloading = false;
        EquipKnifeDefault();
        Log("[Weapon] switched to knife");
    }

    private void EquipKnifeDefault()
    {
        currentWeapon = null;

        SetWeaponVisuals(null);

        ammoInMag = 0;
        reserveAmmo = 0;
        lastFireTime = 0f;

        SetHandsWeaponId(WeaponId_Knife);
        SetTPPWeaponId(WeaponId_Knife);

        ForceHandsToWeaponIdle(null);
        ForceTPPToWeaponIdle(null);

        UpdateAmmoUI();
    }

    private int GetWeaponId(WeaponType type)
    {
        if (type == WeaponType.Fast) return WeaponId_Fast;
        if (type == WeaponType.Heavy) return WeaponId_Heavy;
        return WeaponId_Roklet;
    }

    private void ApplyWeaponStats(WeaponType type)
    {
        if (type == WeaponType.Fast)
        {
            fireCooldown = 0.15f;
            currentDamage = 1;
            currentBulletLifeTime = 2f;
            currentBulletSpeed = 18f;
            magazineSize = 10;
            maxReserveAmmo = 40;
            reloadTime = 1.2f;
        }
        else if (type == WeaponType.Heavy)
        {
            fireCooldown = 0.6f;
            currentDamage = 3;
            currentBulletLifeTime = 2.2f;
            currentBulletSpeed = 16f;
            magazineSize = 5;
            maxReserveAmmo = 20;
            reloadTime = 1.6f;
        }
        else
        {
            fireCooldown = 0.22f;
            currentDamage = 2;
            currentBulletLifeTime = 2f;
            currentBulletSpeed = 19f;
            magazineSize = 8;
            maxReserveAmmo = 32;
            reloadTime = 1.25f;
        }
    }

    private void SetWeaponVisuals(WeaponType? type)
    {
        if (tppWeaponHolderRoot != null && !tppWeaponHolderRoot.activeSelf)
            tppWeaponHolderRoot.SetActive(true);

        SetFPPWeaponVisuals(type);
        SetTPPWeaponVisuals(type);
    }

    private void SetFPPWeaponVisuals(WeaponType? type)
    {
        if (knifeWeaponObject != null) knifeWeaponObject.SetActive(false);
        if (fastWeaponObject != null) fastWeaponObject.SetActive(false);
        if (heavyWeaponObject != null) heavyWeaponObject.SetActive(false);
        if (rokletWeaponObject != null) rokletWeaponObject.SetActive(false);

        GameObject activeWeaponFPP = null;

        if (type == null)
        {
            if (knifeWeaponObject != null)
            {
                knifeWeaponObject.SetActive(true);
                activeWeaponFPP = knifeWeaponObject;
            }
        }
        else if (type == WeaponType.Fast && fastWeaponObject != null)
        {
            fastWeaponObject.SetActive(true);
            activeWeaponFPP = fastWeaponObject;
        }
        else if (type == WeaponType.Heavy && heavyWeaponObject != null)
        {
            heavyWeaponObject.SetActive(true);
            activeWeaponFPP = heavyWeaponObject;
        }
        else if (type == WeaponType.Roklet && rokletWeaponObject != null)
        {
            rokletWeaponObject.SetActive(true);
            activeWeaponFPP = rokletWeaponObject;
        }

        firePoint = null;

        if (activeWeaponFPP != null && type != null)
        {
            Transform fp = FindChildRecursive(activeWeaponFPP.transform, "FirePoint");
            if (fp != null) firePoint = fp;
        }

        Log($"[FPP Visuals] active={(activeWeaponFPP ? activeWeaponFPP.name : "NONE")} firePoint={(firePoint ? firePoint.name : "NULL")}");
    }

    private void SetTPPWeaponVisuals(WeaponType? type)
    {
        if (knifeWeaponObjectTPP != null) knifeWeaponObjectTPP.SetActive(false);
        if (fastWeaponObjectTPP != null) fastWeaponObjectTPP.SetActive(false);
        if (heavyWeaponObjectTPP != null) heavyWeaponObjectTPP.SetActive(false);
        if (rokletWeaponObjectTPP != null) rokletWeaponObjectTPP.SetActive(false);

        GameObject activeWeaponTPP = null;

        if (type == null)
        {
            if (knifeWeaponObjectTPP != null)
            {
                knifeWeaponObjectTPP.SetActive(true);
                activeWeaponTPP = knifeWeaponObjectTPP;
            }
        }
        else if (type == WeaponType.Fast && fastWeaponObjectTPP != null)
        {
            fastWeaponObjectTPP.SetActive(true);
            activeWeaponTPP = fastWeaponObjectTPP;
        }
        else if (type == WeaponType.Heavy && heavyWeaponObjectTPP != null)
        {
            heavyWeaponObjectTPP.SetActive(true);
            activeWeaponTPP = heavyWeaponObjectTPP;
        }
        else if (type == WeaponType.Roklet && rokletWeaponObjectTPP != null)
        {
            rokletWeaponObjectTPP.SetActive(true);
            activeWeaponTPP = rokletWeaponObjectTPP;
        }

        Log($"[TPP Visuals] active={(activeWeaponTPP ? activeWeaponTPP.name : "NONE")} holderRoot={(tppWeaponHolderRoot ? tppWeaponHolderRoot.name : "NULL")}");
    }

    private Transform FindChildRecursive(Transform root, string name)
    {
        if (root == null) return null;
        if (root.name == name) return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindChildRecursive(root.GetChild(i), name);
            if (found != null) return found;
        }

        return null;
    }

    public void SetInputLocked(bool locked)
    {
        inputLocked = locked;
        Log($"[Lock] inputLocked={inputLocked}");
    }

    public void ResetWeapon()
    {
        StopAllCoroutines();
        isReloading = false;

        if (currentWeapon == null)
        {
            EquipKnifeDefault();
            Log("[ResetWeapon] knife restored");
            return;
        }

        ApplyWeaponStats(currentWeapon.Value);

        ammoInMag = magazineSize;
        reserveAmmo = maxReserveAmmo;

        lastFireTime = 0f;
        UpdateAmmoUI();

        SetWeaponVisuals(currentWeapon);

        int weaponId = GetWeaponId(currentWeapon.Value);
        SetHandsWeaponId(weaponId);
        SetTPPWeaponId(weaponId);

        ForceHandsToWeaponIdle(currentWeapon);
        ForceTPPToWeaponIdle(currentWeapon);

        UpdateAmmoUI();

        Log($"[ResetWeapon] ammo={ammoInMag}/{magazineSize} reserve={reserveAmmo} weapon={currentWeapon.Value}");
    }

    private void Log(string msg)
    {
        if (debugLogs)
            Debug.Log(msg);
    }
}