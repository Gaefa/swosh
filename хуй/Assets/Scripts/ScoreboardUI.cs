using UnityEngine;
using TMPro;

public class ScoreboardUI : MonoBehaviour
{
    [Header("Panel")]
    public GameObject scoreboardPanel;

    [Header("Rounds")]
    public TMP_Text roundsText;
    public Round2v2Manager roundManager;

    [Header("Row: Player")]
    public TMP_Text playerK;
    public TMP_Text playerD;

    [Header("Row: Ally")]
    public TMP_Text allyK;
    public TMP_Text allyD;

    [Header("Row: Enemy1")]
    public TMP_Text enemy1K;
    public TMP_Text enemy1D;

    [Header("Row: Enemy2")]
    public TMP_Text enemy2K;
    public TMP_Text enemy2D;

    private void Start()
    {
        if (scoreboardPanel != null)
            scoreboardPanel.SetActive(false);

        if (roundManager == null)
            roundManager = FindObjectOfType<Round2v2Manager>();
    }

    // ✅ Вызовешь с EventTrigger (удержание)
    public void Show()
    {
        if (scoreboardPanel != null)
            scoreboardPanel.SetActive(true);

        UpdateTexts();
    }

    // ✅ Вызовешь с EventTrigger (отпустил/вышел)
    public void Hide()
    {
        if (scoreboardPanel != null)
            scoreboardPanel.SetActive(false);
    }

    private void UpdateTexts()
    {
        var m = MatchStats.Instance;
        if (m == null) return;

        if (roundsText != null && roundManager != null)
            roundsText.text = $"ROUNDS  {roundManager.PlayerWins} : {roundManager.EnemyWins}";

        if (playerK != null) playerK.text = m.playerKills.ToString();
        if (playerD != null) playerD.text = m.playerDeaths.ToString();

        if (allyK != null) allyK.text = m.allyKills.ToString();
        if (allyD != null) allyD.text = m.allyDeaths.ToString();

        if (enemy1K != null) enemy1K.text = m.enemy1Kills.ToString();
        if (enemy1D != null) enemy1D.text = m.enemy1Deaths.ToString();

        if (enemy2K != null) enemy2K.text = m.enemy2Kills.ToString();
        if (enemy2D != null) enemy2D.text = m.enemy2Deaths.ToString();
    }
}