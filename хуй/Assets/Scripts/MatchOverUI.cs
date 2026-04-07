using TMPro;
using UnityEngine;

public class MatchOverUI : MonoBehaviour
{
    [Header("Refs")]
    public CanvasGroup canvasGroup;

    [Header("Texts")]
    public TMP_Text titleText;     // ПОБЕДА / ПОРАЖЕНИЕ
    public TMP_Text subText;       // Матч окончен
    public TMP_Text scoreText;     // Счёт: X : Y

    private void Awake()
    {
        HideImmediate();
    }

    public void Show(bool isWin, int playerWins, int enemyWins)
    {
        if (titleText != null) titleText.text = isWin ? "ПОБЕДА" : "ПОРАЖЕНИЕ";
        if (subText != null) subText.text = "Матч окончен";
        if (scoreText != null) scoreText.text = $"Счёт: {playerWins} : {enemyWins}";

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        gameObject.SetActive(true);
    }

    public void HideImmediate()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        gameObject.SetActive(false);
    }
}