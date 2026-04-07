using UnityEngine;

public class FloatingJoystickVisual : MonoBehaviour
{
    public RectTransform baseRect;
    public RectTransform handleRect;

    public void Show(Vector2 localPos)
    {
        gameObject.SetActive(true);
        baseRect.anchoredPosition = localPos;
        handleRect.anchoredPosition = Vector2.zero;
    }

    public void UpdateHandle(Vector2 delta)
    {
        if (baseRect == null || handleRect == null) return;

        // хэндл всегда внутри базы
        float maxRadius = (baseRect.sizeDelta.x * 0.5f) - (handleRect.sizeDelta.x * 0.5f) - 4f;
        handleRect.anchoredPosition = Vector2.ClampMagnitude(delta, maxRadius);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}