using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0f, 20f, 0f);

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 pos = target.position + offset;
        transform.position = pos;

        // чтобы всегда смотрела вниз
        transform.rotation = Quaternion.Euler(90f, 0f, 0f);
    }
}