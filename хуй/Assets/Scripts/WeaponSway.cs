using UnityEngine;

public class WeaponSway : MonoBehaviour
{
    [Header("Refs")]
    public Transform cameraTransform;

    [Header("Sway")]
    public float amount = 0.03f;      // сила смещения
    public float smooth = 10f;        // плавность

    private Vector3 startLocalPos;
    private Quaternion lastCamRot;

    private void Start()
    {
        startLocalPos = transform.localPosition;

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        if (cameraTransform != null)
            lastCamRot = cameraTransform.rotation;
    }

    private void LateUpdate()
    {
        if (cameraTransform == null) return;

        // насколько камера повернулась с прошлого кадра
        Quaternion delta = cameraTransform.rotation * Quaternion.Inverse(lastCamRot);
        Vector3 euler = delta.eulerAngles;

        // переводим 0..360 в -180..180
        float yaw = (euler.y > 180f) ? euler.y - 360f : euler.y;
        float pitch = (euler.x > 180f) ? euler.x - 360f : euler.x;

        Vector3 targetOffset = new Vector3(-yaw, -pitch, 0f) * amount / 10f;
        Vector3 targetPos = startLocalPos + targetOffset;

        transform.localPosition = Vector3.Lerp(transform.localPosition, targetPos, Time.deltaTime * smooth);

        lastCamRot = cameraTransform.rotation;
    }
}