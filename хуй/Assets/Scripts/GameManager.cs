using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("HUD")]
    public TMP_Text hudText;

    [Header("Stats")]
    public int round = 0;
    public int kills = 0;

    [Header("Victory (MVP)")]
    public int killsToWin = 30;
    public TMP_Text victoryText; // по центру, выключен по умолчанию

    [Header("Menu (optional)")]
    [Tooltip("Если есть отдельная сцена меню — укажи имя. Если пусто, кнопка 'Меню' просто рестартит сцену.")]
    public string menuSceneName = "";

    private bool matchEnded = false;

    private void Awake()
    {
        // singleton без дублей
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        Time.timeScale = 1f;

        if (victoryText != null)
            victoryText.gameObject.SetActive(false);

        UpdateHUD();
    }

    private void Update()
    {
        // Оставил как запасной дебаг (можешь удалить, если не надо)
        if (matchEnded && Input.GetKeyDown(KeyCode.P))
            RestartMatch();
    }

    // ---------------- UI Buttons ----------------

    public void RestartMatch()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void GoToMenu()
    {
        Time.timeScale = 1f;

        if (!string.IsNullOrEmpty(menuSceneName))
            SceneManager.LoadScene(menuSceneName);
        else
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); // временно
    }

    // ---------------- Game Stats ----------------

    public void AddKill()
    {
        if (matchEnded) return;

        kills++;
        UpdateHUD();
        CheckVictory();
    }

    public void NextRound()
    {
        if (matchEnded) return;

        round++;
        UpdateHUD();
    }

    private void CheckVictory()
    {
        if (matchEnded) return;
        if (kills >= killsToWin)
            Victory();
    }

    private void Victory()
    {
        matchEnded = true;

        if (victoryText != null)
            victoryText.gameObject.SetActive(true);

        Time.timeScale = 0f;
    }

    private void UpdateHUD()
    {
        if (hudText == null) return;
        hudText.text = $"Round: {round}\nKills: {kills}";
    }
}