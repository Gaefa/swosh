using UnityEngine;

public class MobileMovement : MonoBehaviour
{
    public RectTransform moveZone;
    public float deadZone = 20f;

    public FloatingJoystickVisual joystickVisual;

    [HideInInspector] public bool inputLocked = false;

    [Header("Movement")]
    public float moveSpeed = 5f;
    public Transform cameraRoot;

    [Header("Crouch Move")]
    public float crouchSpeedMultiplier = 0.6f;

    [Header("Animator (TPP)")]
    [Tooltip("Сюда перетащи Animator с TPP тела (например UndeadMain -> Animator).")]
    public Animator tppAnimator;
    [Tooltip("Имя bool параметра движения в Animator.")]
    public string isMovingBoolName = "IsMoving";
    [Tooltip("Имя bool параметра crouch в Animator.")]
    public string isCrouchingBoolName = "IsCrouching";
    [Tooltip("Имя bool параметра grounded в Animator.")]
    public string isGroundedBoolName = "IsGrounded";
    [Tooltip("Имя bool параметра jumping в Animator.")]
    public string isJumpingBoolName = "IsJumping";
    [Tooltip("Порог скорости (по XZ), чтобы считать что мы движемся. Убирает дрожание.")]
    public float moveAnimSpeedThreshold = 0.15f;

    public bool IsMoving { get; private set; }

    [Header("Crosshair")]
    public CrosshairUI crosshairUI;

    [Header("EDITOR DEBUG (Keyboard)")]
    [Tooltip("Только в Unity Editor: движение WASD. В билде не работает.")]
    public bool editorKeyboardMove = true;

    [Header("Wall Slide (fix jitter)")]
    public LayerMask obstacleLayer = ~0;
    public float wallCheckExtra = 0.06f;
    public float wallDotThreshold = 0.01f;

    [Header("Jump Pop (fast up, same height)")]
    public float jumpUpVelocity = 8.0f;
    public float upGravityMultiplier = 3.0f;

    [Header("CS Jump Feel")]
    public float coyoteTime = 0.08f;
    public float jumpBufferTime = 0.10f;
    public float airControl = 0.5f;

    [Header("Gravity Feel")]
    public float fallGravityMultiplier = 2.4f;
    public float groundSnap = 2f;

    [Header("Ground Check")]
    public float groundCheckDistance = 0.15f;
    public LayerMask groundLayer;

    [Header("Stand Collider")]
    public float standHeight = 1.8f;
    public Vector3 standCenter = new Vector3(0f, 0.9f, 0f);
    public float standRadius = 0.5f;

    [Header("Crouch Collider")]
    public float crouchHeight = 1.02f;
    public Vector3 crouchCenter = new Vector3(0f, 0.52f, 0f);
    public float crouchRadius = 0.5f;

    [Header("Left Stick Feel")]
    public float stickRadius = 120f;

    [Header("POV Crouch Camera")]
    public Transform povCameraRoot;
    public float standCamLocalY = 0f;
    public float crouchCamDeltaY = -0.25f;
    public float camLerpSpeed = 12f;

    private bool joystickEnabled = true;

    private CapsuleCollider capsule;
    private bool isCrouching;

    private Vector2 moveInput;
    private Rigidbody rb;
    private bool isGrounded;

    private float lastGroundedTime = -999f;
    private float lastJumpPressedTime = -999f;
    private float disableSnapUntil = -999f;

    private int moveFingerId = -1;
    public int MoveFingerId => moveFingerId;

    private Vector2 startLocalPos;

    private Vector3 camStandLocalPos;
    private bool camStandCached = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        capsule = GetComponent<CapsuleCollider>();

        if (rb != null)
        {
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            rb.maxAngularVelocity = 1f;
        }

        if (capsule != null)
        {
            ApplyStandColliderImmediate();
        }

        CacheCameraStandPos();

        if (joystickVisual != null)
            joystickVisual.Hide();

        SetIsMoving(false);
        UpdateTppCrouchAnimator();
        UpdateTppGroundedAnimator(true);
        UpdateTppJumpingAnimator(false);
    }

    public void SetJoystickEnabled(bool enabled)
    {
        joystickEnabled = enabled;

        if (!enabled)
        {
            inputLocked = true;
            ResetInput();

            if (joystickVisual != null)
            {
                joystickVisual.Hide();
                joystickVisual.gameObject.SetActive(false);
            }
        }
        else
        {
            if (joystickVisual != null)
                joystickVisual.gameObject.SetActive(true);

            inputLocked = false;
        }
    }

    public void ResetInput()
    {
        moveFingerId = -1;
        moveInput = Vector2.zero;
        startLocalPos = Vector2.zero;
        lastJumpPressedTime = -999f;

        ForceStand();

        if (joystickVisual != null)
            joystickVisual.Hide();

        SetIsMoving(false);
        UpdateTppJumpingAnimator(false);
    }

    private void CacheCameraStandPos()
    {
        if (povCameraRoot == null) return;

        camStandLocalPos = povCameraRoot.localPosition;

        if (Mathf.Approximately(standCamLocalY, 0f))
            standCamLocalY = camStandLocalPos.y;

        camStandLocalPos.y = standCamLocalY;
        camStandCached = true;
    }

    private void ApplyStandColliderImmediate()
    {
        if (capsule == null) return;

        capsule.height = standHeight;
        capsule.center = standCenter;
        capsule.radius = standRadius;
    }

    private void ApplyCrouchColliderImmediate()
    {
        if (capsule == null) return;

        capsule.height = crouchHeight;
        capsule.center = crouchCenter;
        capsule.radius = crouchRadius;
    }

    private void ForceStand()
    {
        if (capsule == null) return;

        isCrouching = false;
        ApplyStandColliderImmediate();

        SnapCameraToStand();
        UpdateTppCrouchAnimator();
    }

    private void SnapCameraToStand()
    {
        if (povCameraRoot == null) return;
        if (!camStandCached) CacheCameraStandPos();
        if (!camStandCached) return;

        Vector3 p = povCameraRoot.localPosition;
        p.y = standCamLocalY;
        povCameraRoot.localPosition = p;
    }

    private void UpdateTppCrouchAnimator()
    {
        if (tppAnimator != null && !string.IsNullOrEmpty(isCrouchingBoolName))
            tppAnimator.SetBool(isCrouchingBoolName, isCrouching);
    }

    private void UpdateTppGroundedAnimator(bool grounded)
    {
        if (tppAnimator != null && !string.IsNullOrEmpty(isGroundedBoolName))
            tppAnimator.SetBool(isGroundedBoolName, grounded);
    }

    private void UpdateTppJumpingAnimator(bool jumping)
    {
        if (tppAnimator != null && !string.IsNullOrEmpty(isJumpingBoolName))
            tppAnimator.SetBool(isJumpingBoolName, jumping);
    }

    void Update()
    {
        if (!joystickEnabled)
        {
            moveFingerId = -1;
            moveInput = Vector2.zero;
            if (joystickVisual != null) joystickVisual.Hide();
            SetIsMoving(false);
            return;
        }

        if (inputLocked)
        {
            moveFingerId = -1;
            moveInput = Vector2.zero;
            if (joystickVisual != null) joystickVisual.Hide();
            SetIsMoving(false);
            return;
        }

#if UNITY_EDITOR
        if (editorKeyboardMove)
        {
            float x = Input.GetAxisRaw("Horizontal");
            float y = Input.GetAxisRaw("Vertical");

            moveInput = new Vector2(x, y);
            moveInput = Vector2.ClampMagnitude(moveInput, 1f);

            moveFingerId = -1;

            if (joystickVisual != null)
                joystickVisual.Hide();

            return;
        }
#endif

        moveInput = Vector2.zero;

        if (moveZone == null)
        {
            moveFingerId = -1;
            if (joystickVisual != null) joystickVisual.Hide();
            SetIsMoving(false);
            return;
        }

        if (Input.touchCount == 0)
        {
            moveFingerId = -1;
            moveInput = Vector2.zero;
            if (joystickVisual != null) joystickVisual.Hide();
            return;
        }

        if (moveFingerId == -1)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch t = Input.GetTouch(i);
                if (t.phase != TouchPhase.Began) continue;

                if (RectTransformUtility.RectangleContainsScreenPoint(moveZone, t.position, null))
                {
                    moveFingerId = t.fingerId;

                    RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        moveZone, t.position, null, out startLocalPos);

                    if (joystickVisual != null)
                        joystickVisual.Show(startLocalPos);

                    break;
                }
            }
        }

        if (moveFingerId != -1)
        {
            bool found = false;

            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch t = Input.GetTouch(i);
                if (t.fingerId != moveFingerId) continue;

                found = true;

                if (t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary)
                {
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        moveZone, t.position, null, out Vector2 curLocalPos);

                    Vector2 deltaLocal = curLocalPos - startLocalPos;

                    if (joystickVisual != null)
                        joystickVisual.UpdateHandle(deltaLocal);

                    if (deltaLocal.magnitude < deadZone)
                    {
                        moveInput = Vector2.zero;
                    }
                    else
                    {
                        float radius = Mathf.Max(1f, stickRadius);
                        moveInput = Vector2.ClampMagnitude(deltaLocal / radius, 1f);
                    }
                }

                if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
                {
                    moveFingerId = -1;
                    moveInput = Vector2.zero;

                    if (joystickVisual != null)
                        joystickVisual.Hide();
                }

                break;
            }

            if (!found)
            {
                moveFingerId = -1;
                moveInput = Vector2.zero;

                if (joystickVisual != null)
                    joystickVisual.Hide();
            }
        }
    }

    public void Jump()
    {
        if (!joystickEnabled) return;
        if (inputLocked) return;

        lastJumpPressedTime = Time.time;
    }

    public void Crouch()
    {
        if (!joystickEnabled) return;
        if (inputLocked) return;
        if (capsule == null) return;

        if (!isCrouching)
        {
            isCrouching = true;
            ApplyCrouchColliderImmediate();
        }
        else
        {
            ForceStand();
            return;
        }

        UpdateTppCrouchAnimator();
    }

    void FixedUpdate()
    {
        if (rb == null || capsule == null) return;

        rb.angularVelocity = Vector3.zero;

        if (!joystickEnabled || inputLocked)
        {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            SetIsMoving(false);
            return;
        }

        Vector3 bottom = transform.position + capsule.center - Vector3.up * (capsule.height * 0.5f);
        Vector3 origin = bottom + Vector3.up * 0.05f;

        isGrounded = Physics.Raycast(origin, Vector3.down, groundCheckDistance, groundLayer);
        if (isGrounded)
        {
            lastGroundedTime = Time.time;
            UpdateTppJumpingAnimator(false);
        }

        UpdateTppGroundedAnimator(isGrounded);

        bool canCoyote = (Time.time - lastGroundedTime) <= coyoteTime;
        bool hasBufferedJump = (Time.time - lastJumpPressedTime) <= jumpBufferTime;

        if (hasBufferedJump && canCoyote)
        {
            lastJumpPressedTime = -999f;
            disableSnapUntil = Time.time + 0.12f;

            Vector3 v = rb.linearVelocity;
            rb.linearVelocity = new Vector3(v.x, jumpUpVelocity, v.z);

            isGrounded = false;
            UpdateTppGroundedAnimator(false);
            UpdateTppJumpingAnimator(true);
        }

        if (!isGrounded && rb.linearVelocity.y > 0f)
            rb.AddForce(Physics.gravity * (upGravityMultiplier - 1f), ForceMode.Acceleration);

        if (!isGrounded && rb.linearVelocity.y < 0f)
            rb.AddForce(Physics.gravity * (fallGravityMultiplier - 1f), ForceMode.Acceleration);

        if (Time.time >= disableSnapUntil)
        {
            if (isGrounded && rb.linearVelocity.y < 0f)
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, -groundSnap, rb.linearVelocity.z);
        }

        Transform cam = cameraRoot != null ? cameraRoot : transform;

        Vector3 forward = cam.forward;
        Vector3 right = cam.right;
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        float currentMoveSpeed = isCrouching ? moveSpeed * crouchSpeedMultiplier : moveSpeed;

        Vector3 dir = (forward * moveInput.y + right * moveInput.x);
        Vector3 desiredHoriz = dir * currentMoveSpeed;

        Vector3 velNow = rb.linearVelocity;
        float control = isGrounded ? 1f : airControl;

        Vector3 currentHoriz = new Vector3(velNow.x, 0f, velNow.z);
        Vector3 newHoriz = Vector3.Lerp(currentHoriz, desiredHoriz, control);

        newHoriz = SlideAlongObstacles(newHoriz);

        rb.linearVelocity = new Vector3(newHoriz.x, rb.linearVelocity.y, newHoriz.z);

        UpdatePovCameraHeight();

        Vector3 xz = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        bool movingNow = xz.magnitude > moveAnimSpeedThreshold;
        SetIsMoving(movingNow);

        if (crosshairUI != null)
            crosshairUI.SetMoveSpeed(Mathf.Clamp01(xz.magnitude / moveSpeed));
    }

    private void SetIsMoving(bool value)
    {
        if (IsMoving == value) return;

        IsMoving = value;

        if (tppAnimator != null && !string.IsNullOrEmpty(isMovingBoolName))
            tppAnimator.SetBool(isMovingBoolName, IsMoving);
    }

    private void UpdatePovCameraHeight()
    {
        if (povCameraRoot == null) return;
        if (!camStandCached) CacheCameraStandPos();
        if (!camStandCached) return;

        float targetY = isCrouching ? (standCamLocalY + crouchCamDeltaY) : standCamLocalY;

        Vector3 p = povCameraRoot.localPosition;
        p.y = Mathf.Lerp(p.y, targetY, Time.deltaTime * camLerpSpeed);
        povCameraRoot.localPosition = p;
    }

    private Vector3 SlideAlongObstacles(Vector3 horizVel)
    {
        if (horizVel.sqrMagnitude < 0.00001f)
            return horizVel;

        float radius = Mathf.Max(0.05f, capsule.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.z));
        float height = Mathf.Max(radius * 2f, capsule.height * transform.lossyScale.y);

        Vector3 center = transform.TransformPoint(capsule.center);
        float half = (height * 0.5f) - radius;

        Vector3 p1 = center + Vector3.up * half;
        Vector3 p2 = center - Vector3.up * half;

        Vector3 moveDir = horizVel.normalized;
        float checkDist = radius + wallCheckExtra;

        if (Physics.CapsuleCast(p1, p2, radius, moveDir, out RaycastHit hit, checkDist, obstacleLayer, QueryTriggerInteraction.Ignore))
        {
            Vector3 n = hit.normal;
            n.y = 0f;

            if (n.sqrMagnitude > 0.0001f)
            {
                n.Normalize();

                float into = Vector3.Dot(horizVel, n);
                if (into > wallDotThreshold)
                    horizVel = Vector3.ProjectOnPlane(horizVel, n);
            }
        }

        return horizVel;
    }
}