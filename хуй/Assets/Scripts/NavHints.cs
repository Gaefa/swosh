using UnityEngine;

public class NavHints : MonoBehaviour
{
    public static NavHints Instance;

    [Tooltip("Пустышки в концах длинных стен/перекрытий.")]
    public Transform[] points;

    private void Awake()
    {
        Instance = this;
    }

    public Transform GetBestHint(Vector3 from, Vector3 targetPos, LayerMask obstacleMask)
    {
        if (points == null || points.Length == 0) return null;

        Transform best = null;
        float bestScore = float.NegativeInfinity;

        Vector3 fromFlat = new Vector3(from.x, 0f, from.z);
        Vector3 toFlat   = new Vector3(targetPos.x, 0f, targetPos.z);
        float curDist    = Vector3.Distance(fromFlat, toFlat);

        Vector3 origin = from + Vector3.up * 0.2f;

        for (int i = 0; i < points.Length; i++)
        {
            Transform p = points[i];
            if (p == null) continue;

            Vector3 pFlat = new Vector3(p.position.x, 0f, p.position.z);

            // 1) До точки должен быть прямой луч (иначе бесполезно)
            if (Physics.Linecast(origin, p.position + Vector3.up * 0.2f, obstacleMask))
                continue;

            float distFrom = Vector3.Distance(fromFlat, pFlat);
            float distToTargetFromPoint = Vector3.Distance(pFlat, toFlat);

            // 2) Из точки желательно видеть цель или хотя бы быть ближе к ней
            bool pointHasLOS = !Physics.Linecast(p.position + Vector3.up * 0.2f, targetPos + Vector3.up * 0.2f, obstacleMask);

            // 3) Оценка: чем ближе к цели и чем короче путь до точки — тем лучше
            float score = 0f;
            if (pointHasLOS) score += 3f;                   // жирный бонус за LOS
            score += Mathf.Clamp01((curDist - distToTargetFromPoint) / (curDist + 0.001f)) * 2f; // насколько ближе к цели
            score += Mathf.Clamp01(1f / (1f + distFrom));   // чуть поощряем близкие точки

            if (score > bestScore)
            {
                bestScore = score;
                best = p;
            }
        }

        return best;
    }
}