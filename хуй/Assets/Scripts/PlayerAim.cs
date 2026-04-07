using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerAim : MonoBehaviour
{
    [SerializeField] private Camera aimCamera;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float radius = 0.7f; // расстояние firePoint от игрока

    private void Awake()
    {
        if (aimCamera == null) aimCamera = Camera.main;
    }

    private void Update()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        if (aimCamera == null || firePoint == null) return;

        // плоскость на высоте игрока
        Plane plane = new Plane(Vector3.up, transform.position);
        Ray ray = aimCamera.ScreenPointToRay(Input.mousePosition);

        if (!plane.Raycast(ray, out float enter)) return;

        Vector3 hit = ray.GetPoint(enter);
        Vector3 dir = hit - transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.0001f) return;

        dir.Normalize();

        // ✅ firePoint ВСЕГДА на окружности вокруг игрока
        firePoint.position = transform.position + dir * radius;

        // (не обязательно, но удобно)
        firePoint.rotation = Quaternion.LookRotation(dir, Vector3.up);

        // (по желанию) можно крутить самого игрока
        transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
    }
}