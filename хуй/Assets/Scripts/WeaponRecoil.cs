using UnityEngine;

public class WeaponRecoil : MonoBehaviour
{
    [Header("Kick (position)")]
    public float kickBack = 0.06f;
    public float kickUp = 0.02f;

    [Header("Kick (rotation)")]
    public float rotX = 6f;
    public float rotY = 1.5f;

    [Header("Speed")]
    public float kickSpeed = 18f;
    public float returnSpeed = 22f;

    private Vector3 startLocalPos;
    private Quaternion startLocalRot;

    private Vector3 targetPos;
    private Quaternion targetRot;

    private void Awake()
    {
        startLocalPos = transform.localPosition;
        startLocalRot = transform.localRotation;

        targetPos = startLocalPos;
        targetRot = startLocalRot;
    }

    public void Kick(float strength = 1f)
    {
        // уходим назад и чуть вверх
        targetPos = startLocalPos + new Vector3(0f, kickUp, -kickBack) * strength;

        // чуть поворачиваем вверх + немного в сторону (рандом минимальный)
        float y = rotY * Random.Range(-1f, 1f);
        targetRot = startLocalRot * Quaternion.Euler(-rotX * strength, y, 0f);
    }

    private void LateUpdate()
    {
        // быстро к target, потом возвращаемся к старту
        transform.localPosition = Vector3.Lerp(transform.localPosition, targetPos, Time.deltaTime * kickSpeed);
        transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRot, Time.deltaTime * kickSpeed);

        targetPos = Vector3.Lerp(targetPos, startLocalPos, Time.deltaTime * returnSpeed);
        targetRot = Quaternion.Slerp(targetRot, startLocalRot, Time.deltaTime * returnSpeed);
    }
}