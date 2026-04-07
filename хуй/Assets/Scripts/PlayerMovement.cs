using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 6f;

    private Rigidbody rb;
    private Vector3 moveWorld; // движение в мире

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
    }

    private void Update()
    {
        float x = Input.GetAxisRaw("Horizontal"); // A/D
        float z = Input.GetAxisRaw("Vertical");   // W/S

        // ввод в локальных осях игрока
        Vector3 moveLocal = new Vector3(x, 0f, z);
        if (moveLocal.sqrMagnitude > 1f)
            moveLocal.Normalize();

        // переводим в мировой вектор относительно поворота игрока
        moveWorld = transform.TransformDirection(moveLocal);
        moveWorld.y = 0f;
        if (moveWorld.sqrMagnitude > 0.0001f)
            moveWorld.Normalize();
    }

    private void FixedUpdate()
    {
        Vector3 targetPos = rb.position + moveWorld * moveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(targetPos);
    }
}