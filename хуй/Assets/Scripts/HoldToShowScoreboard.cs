using UnityEngine;
using UnityEngine.EventSystems;

public class HoldToShowScoreboard : MonoBehaviour,
    IPointerDownHandler,
    IPointerUpHandler,
    IPointerExitHandler
{
    public CanvasGroup scoreboardCanvas;

    private bool isHolding;

    private void Start()
    {
        SetVisible(false);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isHolding = true;
        SetVisible(true);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isHolding = false;
        SetVisible(false);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isHolding) return;
        isHolding = false;
        SetVisible(false);
    }

    private void SetVisible(bool show)
    {
        if (scoreboardCanvas == null) return;

        scoreboardCanvas.alpha = show ? 1f : 0f;

        // ✅ ВАЖНО: таблица только отображается, но НЕ перехватывает пальцы
        scoreboardCanvas.interactable = false;
        scoreboardCanvas.blocksRaycasts = false;
    }
}