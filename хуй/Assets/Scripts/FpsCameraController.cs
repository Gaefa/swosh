using UnityEngine;

public class FpsCameraController : MonoBehaviour
{
    [Header("Refs")]
    public Transform playerRoot;      // вращаем по Y (yaw)
    public Transform cameraPivot;     // точка головы (HeadPivot)

    [Header("Look")]
    public float sensitivity = 2.0f;
    public float minPitch = -80f;
    public float maxPitch = 80f;

    [Header("Cursor")]
    public bool lockCursorOnStart = true;

    private float yaw;
    private float pitch;

    private void Start()
    {
        if (playerRoot == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerRoot = p.transform;
        }

        if (cameraPivot == null)
            cameraPivot = playerRoot;

        Vector3 e = transform.eulerAngles;
        yaw = e.y;
        pitch = e.x;

        if (lockCursorOnStart)
            LockCursor();
    }

    private void Update()
    {
        // ✅ Если курсор разлочен (магазин/меню) — не крутим камеру
        if (Cursor.lockState != CursorLockMode.Locked)
            return;

        if (playerRoot == null || cameraPivot == null) return;

        float mx = Input.GetAxisRaw("Mouse X") * sensitivity;
        float my = Input.GetAxisRaw("Mouse Y") * sensitivity;

        yaw += mx;
        pitch -= my;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        playerRoot.rotation = Quaternion.Euler(0f, yaw, 0f);

        transform.position = cameraPivot.position;
        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    public void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}