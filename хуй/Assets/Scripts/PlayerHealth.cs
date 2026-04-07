using TMPro;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("HP")]
    public int maxHP = 3;
    public int currentHP;

    [Header("UI")]
    public TMP_Text hpText;

    [Header("Team")]
    public Team team = Team.Player;

    [Header("Death Animation")]
    [Tooltip("Сюда перетащи Animator TPP-тела.")]
    public Animator tppAnimator;
    [Tooltip("Имя bool параметра смерти в Animator.")]
    public string isDeadBoolName = "IsDead";

    [Header("Death Collider")]
    [Tooltip("Уменьшенная высота капсулы при смерти.")]
    public float deathCapsuleHeight = 0.35f;
    [Tooltip("Центр капсулы при смерти.")]
    public Vector3 deathCapsuleCenter = new Vector3(0f, 0.18f, 0f);

    [Header("State")]
    [SerializeField] private bool isDead = false;
    public bool IsDead => isDead;

    private bool diedLastRound = false;

    private ActorId lastHitBy = ActorId.None;
    private Round2v2Manager roundManager;

    private Rigidbody rb;
    private CapsuleCollider capsule;

    private float normalCapsuleHeight;
    private Vector3 normalCapsuleCenter;

    private MobileMovement mobileMove;
    private MobileShoot mobileShoot;
    private MobileCameraSwipe camSwipe;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        capsule = GetComponent<CapsuleCollider>();

        if (capsule != null)
        {
            normalCapsuleHeight = capsule.height;
            normalCapsuleCenter = capsule.center;
        }

        mobileMove = GetComponent<MobileMovement>();
        mobileShoot = GetComponent<MobileShoot>();
        camSwipe = GetComponentInChildren<MobileCameraSwipe>(true);

        if (tppAnimator == null)
            tppAnimator = GetComponentInChildren<Animator>(true);
    }

    private void Start()
    {
        roundManager = FindObjectOfType<Round2v2Manager>();
        ResetForRound();
    }

    public void TakeDamage(int dmg, ActorId attackerId = ActorId.None)
    {
        if (roundManager != null && roundManager.CurrentState != Round2v2Manager.State.Round)
            return;

        if (isDead) return;

        lastHitBy = attackerId;

        currentHP -= dmg;
        if (currentHP < 0) currentHP = 0;

        UpdateHPUI();

        if (currentHP <= 0)
            Die();
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;
        diedLastRound = true;

        if (tppAnimator != null && !string.IsNullOrEmpty(isDeadBoolName))
            tppAnimator.SetBool(isDeadBoolName, true);

        if (mobileShoot != null) mobileShoot.inputLocked = true;
        if (mobileMove != null) mobileMove.inputLocked = true;
        if (camSwipe != null) camSwipe.inputLocked = true;

        if (MatchStats.Instance != null &&
            roundManager != null &&
            roundManager.CurrentState == Round2v2Manager.State.Round)
        {
            var identity = GetComponent<ActorIdentity>();
            ActorId victimId = identity != null ? identity.actorId : ActorId.Player;
            MatchStats.Instance.RegisterKill(lastHitBy, victimId);
        }

        SetControlsEnabled(false);

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Вместо выключения капсулы — уменьшаем её
        if (capsule != null)
        {
            capsule.height = deathCapsuleHeight;
            capsule.center = deathCapsuleCenter;
        }

        ResetTouchStates();
    }

    public void ResetForRound()
    {
        isDead = false;
        currentHP = maxHP;
        UpdateHPUI();

        if (tppAnimator != null && !string.IsNullOrEmpty(isDeadBoolName))
            tppAnimator.SetBool(isDeadBoolName, false);

        // Возвращаем обычную капсулу
        if (capsule != null)
        {
            capsule.height = normalCapsuleHeight;
            capsule.center = normalCapsuleCenter;
        }

        if (diedLastRound && mobileShoot != null)
            mobileShoot.UnequipWeapon();

        diedLastRound = false;

        if (mobileShoot != null) mobileShoot.inputLocked = false;
        if (mobileMove != null) mobileMove.inputLocked = false;
        if (camSwipe != null) camSwipe.inputLocked = false;

        SetControlsEnabled(true);
        ResetTouchStates();
    }

    private void SetControlsEnabled(bool enabled)
    {
        if (mobileShoot != null)
            mobileShoot.enabled = enabled;

        if (camSwipe != null)
            camSwipe.enabled = enabled;

        if (mobileMove != null)
        {
            mobileMove.inputLocked = !enabled;

            if (!enabled)
                mobileMove.ResetInput();
        }

        if (!enabled)
            ResetTouchStates();
    }

    private void ResetTouchStates()
    {
        if (mobileMove != null)
            mobileMove.ResetInput();

        if (camSwipe != null)
            camSwipe.ResetInput();
    }

    private void UpdateHPUI()
    {
        if (hpText != null)
            hpText.text = currentHP.ToString();
    }
}