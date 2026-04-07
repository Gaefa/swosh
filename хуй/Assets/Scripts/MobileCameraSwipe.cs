using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MobileCameraSwipe : MonoBehaviour
{
    public float swipeSensitivity = 0.12f;

    [Header("Zones")]
    public RectTransform swipeZone;

    [Header("Links")]
    public MobileMovement mobileMovement;
    public Transform yawRoot;   // Player
    public Transform pitchRoot; // HeadPivot

    [Header("Pitch Clamp")]
    public float minPitch = -80f;
    public float maxPitch = 80f;

    [HideInInspector] public bool inputLocked = false;

    private int lookFingerId = -1;
    private Vector2 lastPos;

    private float currentPitch;
    private float targetYaw;
    private Rigidbody yawRb;

    // кеш для рейкастов по UI
    private PointerEventData _ped;
    private readonly List<RaycastResult> _uiHits = new List<RaycastResult>(16);

    void Awake()
    {
        if (mobileMovement == null)
            mobileMovement = FindObjectOfType<MobileMovement>();

        if (yawRoot != null)
            yawRb = yawRoot.GetComponent<Rigidbody>();

        if (pitchRoot != null)
        {
            float x = pitchRoot.localEulerAngles.x;
            if (x > 180f) x -= 360f;
            currentPitch = Mathf.Clamp(x, minPitch, maxPitch);
        }

        if (yawRoot != null)
            targetYaw = yawRoot.eulerAngles.y;

        _ped = new PointerEventData(EventSystem.current);
    }

    public void ResetInput()
    {
        lookFingerId = -1;
        lastPos = Vector2.zero;

        if (yawRoot != null)
            targetYaw = yawRoot.eulerAngles.y;

        if (pitchRoot != null)
        {
            float x = pitchRoot.localEulerAngles.x;
            if (x > 180f) x -= 360f;
            currentPitch = Mathf.Clamp(x, minPitch, maxPitch);
        }
    }

    void Update()
    {
        if (inputLocked)
        {
            lookFingerId = -1;
            return;
        }

        if (yawRoot == null || pitchRoot == null || swipeZone == null)
            return;

        if (Input.touchCount == 0)
        {
            lookFingerId = -1;
            return;
        }

        int moveId = (mobileMovement != null) ? mobileMovement.MoveFingerId : -999;

        // 1) Захват пальца ТОЛЬКО при TouchPhase.Began
        if (lookFingerId == -1)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch t = Input.GetTouch(i);

                if (t.phase != TouchPhase.Began) continue;
                if (t.fingerId == moveId) continue;

                // палец должен стартовать в зоне свайпа
                if (!RectTransformUtility.RectangleContainsScreenPoint(swipeZone, t.position, null))
                    continue;

                // если палец НАЧАЛСЯ на кнопке/интерактивном UI — НЕ захватываем свайп
                if (IsStartedOnClickableUI(t.position))
                    continue;

                lookFingerId = t.fingerId;
                lastPos = t.position;
                break;
            }
        }

        if (lookFingerId == -1) return;

        // 2) После захвата — отслеживаем палец везде
        Touch lookTouch = default;
        bool found = false;

        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch t = Input.GetTouch(i);
            if (t.fingerId == lookFingerId)
            {
                lookTouch = t;
                found = true;
                break;
            }
        }

        if (!found)
        {
            lookFingerId = -1;
            return;
        }

        if (lookTouch.phase == TouchPhase.Moved)
        {
            Vector2 delta = lookTouch.position - lastPos;

            targetYaw += delta.x * swipeSensitivity;

            currentPitch -= delta.y * swipeSensitivity;
            currentPitch = Mathf.Clamp(currentPitch, minPitch, maxPitch);
            pitchRoot.localEulerAngles = new Vector3(currentPitch, 0f, 0f);

            lastPos = lookTouch.position;
        }

        if (lookTouch.phase == TouchPhase.Ended || lookTouch.phase == TouchPhase.Canceled)
        {
            lookFingerId = -1;
        }
    }

    void FixedUpdate()
    {
        if (inputLocked) return;
        if (yawRoot == null) return;

        Quaternion rot = Quaternion.Euler(0f, targetYaw, 0f);

        if (yawRb != null)
            yawRb.MoveRotation(rot);
        else
            yawRoot.rotation = rot;
    }

    // Возвращает true, если старт касания пришёлся на кликабельный UI
    private bool IsStartedOnClickableUI(Vector2 screenPos)
    {
        if (EventSystem.current == null) return false;

        _ped.position = screenPos;
        _uiHits.Clear();
        EventSystem.current.RaycastAll(_ped, _uiHits);

        for (int i = 0; i < _uiHits.Count; i++)
        {
            var go = _uiHits[i].gameObject;
            if (go == null) continue;

            // если попали в свою swipeZone — это не блок
            if (go == swipeZone.gameObject) continue;

            // если это что-то интерактивное — блокируем захват свайпа
            if (go.GetComponentInParent<Selectable>() != null) return true;
            if (go.GetComponentInParent<IPointerClickHandler>() != null) return true;
        }

        return false;
    }
}