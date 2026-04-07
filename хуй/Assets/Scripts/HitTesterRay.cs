using UnityEngine;

public class HitTesterRay : MonoBehaviour
{
    [Header("Camera")]
    public Camera cam;

    [Header("Raycast")]
    public float maxDistance = 120f;
    public LayerMask mask = ~0;

    [Header("Visual markers")]
    public GameObject hitMarkerPrefab;   // зелёный шарик
    public GameObject missMarkerPrefab;  // красный шарик
    public float missDistance = 3f;      // где рисовать "MISS" перед камерой
    public float markerLifeTime = 1.2f;  // через сколько удалять маркер

    private void Awake()
    {
        if (cam == null) cam = Camera.main;
    }

    private void Update()
    {
        if (!IsFirePressedThisFrame())
            return;

        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, mask, QueryTriggerInteraction.Ignore))
        {
            if (hitMarkerPrefab != null)
            {
                GameObject m = Instantiate(hitMarkerPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                Destroy(m, markerLifeTime);
            }
        }
        else
        {
            if (missMarkerPrefab != null)
            {
                Vector3 p = ray.origin + ray.direction * missDistance;
                GameObject m = Instantiate(missMarkerPrefab, p, Quaternion.identity);
                Destroy(m, markerLifeTime);
            }
        }
    }

    // ✅ Работает и в Editor (мышь), и на мобиле (тап)
    private bool IsFirePressedThisFrame()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        if (Input.GetMouseButtonDown(0))
            return true;
#endif
        if (Input.touchCount > 0)
        {
            Touch t = Input.GetTouch(0);
            return t.phase == TouchPhase.Began;
        }

        return false;
    }
}