using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class Round2v2Manager : MonoBehaviour
{
    public enum State { PreRound, Round, RoundEnd, MatchOver }
    private enum RoundOutcome { None, Win, Lose, Draw }

    public static Round2v2Manager Instance { get; private set; }

    [Header("Refs")]
    public EnemySpawner enemySpawner;
    public AllySpawner allySpawner;

    [Header("Player")]
    public Transform player;
    public Transform playerSpawn;
    public PlayerHealth playerHealth;

    [Header("Timings")]
    public float buySeconds = 10f;
    public float roundSeconds = 180f;
    public float roundEndSeconds = 3f;
    public int winsToWinMatch = 10;

    [Header("UI")]
    public TMP_Text centerText;
    public TMP_Text scoreText;
    public TMP_Text timerText;
    public CanvasGroup shopCanvas;

    [Header("Match Over UI")]
    public MatchOverUI matchOverUI;

    [Header("Timer Colors")]
    public Color buyTimerColor = Color.red;
    public Color roundTimerColor = Color.white;

    public State CurrentState { get; private set; } = State.PreRound;

    private int playerWins = 0;
    private int enemyWins = 0;

    public int PlayerWins => playerWins;
    public int EnemyWins => enemyWins;

    private bool shopRequestedOpen = false;

    private float buyTimer;
    private float roundTimer;

    private MobileCameraSwipe camSwipeCached;
    private RoundOutcome lastRoundOutcome = RoundOutcome.None;

    private Coroutine delayedCameraUnlockRoutine;

    private void Awake()
    {
        Instance = this;

        Time.timeScale = 1f;
        CurrentState = State.PreRound;

        camSwipeCached = FindMobileCameraSwipeSafe();

        SetShop(false);
        SetCenter(false);

        ResolvePlayerRefs();
        ForceBuyState();
    }

    private void Start()
    {
        ResolvePlayerRefs();
        ForceBuyState();

        if (timerText != null)
            timerText.color = roundTimerColor;

        UpdateScore();
        StartCoroutine(GameLoop());
    }

    private void Update()
    {
        if (CurrentState == State.PreRound)
            ForceBuyState();

        if (CurrentState == State.PreRound)
        {
            if (shopRequestedOpen)
            {
                shopRequestedOpen = false;
                ToggleShop();
            }
        }
        else
        {
            SetShop(false);
        }
    }

    public void ToggleShopMobile()
    {
        if (CurrentState != State.PreRound) return;
        shopRequestedOpen = true;
    }

    private IEnumerator GameLoop()
    {
        while (true)
        {
            yield return BuyPhase();
            StartRoundPhase();
            yield return RoundPhase();
            yield return RoundEndPhase();

            if (playerWins >= winsToWinMatch || enemyWins >= winsToWinMatch)
            {
                MatchOverPhase();
                yield break;
            }
        }
    }

    private IEnumerator BuyPhase()
    {
        CurrentState = State.PreRound;
        Time.timeScale = 1f;

        lastRoundOutcome = RoundOutcome.None;

        SetCenter(false);
        SetShop(false);
        ResetUIFocus();

        CleanupBullets();
        CleanupEnemies();

        RespawnPlayerTeam();

        if (allySpawner != null)
            allySpawner.SpawnOrReset();

        ForceBuyState();
        EnsureCameraEnabled(true);

        buyTimer = buySeconds;
        if (timerText != null) timerText.color = buyTimerColor;

        while (buyTimer > 0f)
        {
            buyTimer -= Time.deltaTime;
            if (buyTimer < 0f) buyTimer = 0f;

            UpdateBuyTimerUI(buyTimer);
            yield return null;
        }

        UpdateBuyTimerUI(0f);
        SetShop(false);
    }

    private void StartRoundPhase()
    {
        CurrentState = State.Round;
        Time.timeScale = 1f;

        SetCenter(false);
        SetShop(false);
        ResetUIFocus();

        if (enemySpawner != null)
            enemySpawner.StartRound2v2();

        SetJoystickEnabled(true);
        SetShootLocked(false);

        if (delayedCameraUnlockRoutine != null)
            StopCoroutine(delayedCameraUnlockRoutine);

        delayedCameraUnlockRoutine = StartCoroutine(EnableCameraNextFrame());

        roundTimer = roundSeconds;
        if (timerText != null) timerText.color = roundTimerColor;
        UpdateRoundTimerUI(roundTimer);

        UpdateScore();
    }

    private IEnumerator EnableCameraNextFrame()
    {
        EnsureCameraEnabled(false);
        yield return null;
        EnsureCameraEnabled(true);
        delayedCameraUnlockRoutine = null;
    }

    private IEnumerator RoundPhase()
    {
        while (CurrentState == State.Round)
        {
            if (IsEnemyTeamDead())
            {
                playerWins++;
                lastRoundOutcome = RoundOutcome.Win;
                ShowResultText("ПОБЕДА");
                yield break;
            }

            if (IsPlayerTeamDead())
            {
                enemyWins++;
                lastRoundOutcome = RoundOutcome.Lose;
                ShowResultText("ПОРАЖЕНИЕ");
                yield break;
            }

            roundTimer -= Time.deltaTime;
            if (roundTimer < 0f) roundTimer = 0f;

            UpdateRoundTimerUI(roundTimer);

            if (roundTimer <= 0f)
            {
                bool playerAlive = playerHealth != null && !playerHealth.IsDead;
                bool allyAlive = allySpawner != null && allySpawner.IsAlive();

                if (playerAlive && allyAlive)
                {
                    lastRoundOutcome = RoundOutcome.Draw;
                    ShowResultText("НИЧЬЯ");
                }
                else
                {
                    if (IsPlayerTeamDead())
                    {
                        enemyWins++;
                        lastRoundOutcome = RoundOutcome.Lose;
                        ShowResultText("ПОРАЖЕНИЕ");
                    }
                    else if (IsEnemyTeamDead())
                    {
                        playerWins++;
                        lastRoundOutcome = RoundOutcome.Win;
                        ShowResultText("ПОБЕДА");
                    }
                    else
                    {
                        lastRoundOutcome = RoundOutcome.Draw;
                        ShowResultText("НИЧЬЯ");
                    }
                }

                yield break;
            }

            yield return null;
        }
    }

    private IEnumerator RoundEndPhase()
    {
        CurrentState = State.RoundEnd;
        SetShop(false);

        bool playerAlive = playerHealth != null && !playerHealth.IsDead;

        if (!playerAlive)
            SetJoystickEnabled(false);

        EnsureCameraEnabled(true);
        UpdateScore();

        float t = roundEndSeconds;
        while (t > 0f)
        {
            t -= Time.deltaTime;
            yield return null;
        }

        SetCenter(false);
        CleanupBullets();
        CleanupEnemies();
    }

    private void MatchOverPhase()
    {
        CurrentState = State.MatchOver;
        Time.timeScale = 0f;

        SetShop(false);
        SetJoystickEnabled(false);
        EnsureCameraEnabled(true);
        SetCenter(false);

        bool isWin = playerWins >= winsToWinMatch;
        if (matchOverUI != null)
            matchOverUI.Show(isWin, playerWins, enemyWins);
        else if (centerText != null)
        {
            centerText.gameObject.SetActive(true);
            centerText.text = isWin ? "ПОБЕДА" : "ПОРАЖЕНИЕ";
        }
    }

    private void ForceBuyState()
    {
        ResolvePlayerRefs();

        SetJoystickEnabled(false);
        SetShootLocked(true);

        StopPlayerRigidbodyMotion();
    }

    private void SetJoystickEnabled(bool enabled)
    {
        var mv = GetPlayerComponentInChildren<MobileMovement>();
        if (mv != null)
            mv.SetJoystickEnabled(enabled);
    }

    private void SetShootLocked(bool locked)
    {
        var sh = GetPlayerComponentInChildren<MobileShoot>();
        if (sh == null) return;

        if (sh.inputLocked == locked)
            return;

        sh.inputLocked = locked;
    }

    public static bool IsActionAllowedForAlivePlayer()
    {
        if (Instance == null) return true;
        if (Instance.playerHealth == null) return true;

        if (Instance.playerHealth.IsDead) return false;

        if (Instance.CurrentState == State.PreRound) return true;
        if (Instance.CurrentState == State.Round) return true;

        if (Instance.CurrentState == State.RoundEnd)
            return Instance.lastRoundOutcome == RoundOutcome.Win;

        if (Instance.CurrentState == State.MatchOver)
            return Instance.playerWins >= Instance.winsToWinMatch;

        return false;
    }

    private void ResolvePlayerRefs()
    {
        if (player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        if (playerHealth == null)
            playerHealth = GetPlayerComponentInChildren<PlayerHealth>();
    }

    private T GetPlayerComponentInChildren<T>() where T : Component
    {
        if (player == null) return null;
        return player.GetComponentInChildren<T>(true);
    }

    private void ToggleShop()
    {
        if (shopCanvas == null) return;
        bool isOpen = shopCanvas.alpha > 0.5f;
        SetShop(!isOpen);
    }

    private void SetShop(bool show)
    {
        if (shopCanvas == null) return;

        shopCanvas.alpha = show ? 1f : 0f;
        shopCanvas.interactable = show;
        shopCanvas.blocksRaycasts = show;

        EnsureCameraEnabled(!show);

        if (CurrentState == State.PreRound)
        {
            SetJoystickEnabled(false);
            SetShootLocked(true);
        }
    }

    private void EnsureCameraEnabled(bool enabled)
    {
        if (camSwipeCached == null)
            camSwipeCached = FindMobileCameraSwipeSafe();

        if (camSwipeCached == null) return;

        camSwipeCached.enabled = true;

        bool newLocked = !enabled;
        bool wasLocked = camSwipeCached.inputLocked;

        camSwipeCached.inputLocked = newLocked;

        if (wasLocked != newLocked)
            camSwipeCached.ResetInput();
    }

    private MobileCameraSwipe FindMobileCameraSwipeSafe()
    {
#if UNITY_2023_1_OR_NEWER
        return Object.FindAnyObjectByType<MobileCameraSwipe>(FindObjectsInactive.Include);
#else
        return FindObjectOfType<MobileCameraSwipe>(true);
#endif
    }

    private void ResetUIFocus()
    {
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }

    private void UpdateBuyTimerUI(float t)
    {
        if (timerText == null) return;
        int sec = Mathf.CeilToInt(t);
        if (sec < 0) sec = 0;
        timerText.text = $"0:{sec:00}";
    }

    private void UpdateRoundTimerUI(float t)
    {
        if (timerText == null) return;

        int total = Mathf.CeilToInt(t);
        if (total < 0) total = 0;

        int min = total / 60;
        int sec = total % 60;

        timerText.text = $"{min}:{sec:00}";
    }

    private void ShowResultText(string text)
    {
        SetCenter(true);
        if (centerText != null) centerText.text = text;
        UpdateScore();
    }

    private void UpdateScore()
    {
        if (scoreText != null)
            scoreText.text = $"{playerWins} : {enemyWins}";
    }

    private void SetCenter(bool show)
    {
        if (centerText != null)
            centerText.gameObject.SetActive(show);
    }

    private void StopPlayerRigidbodyMotion()
    {
        if (player == null) return;

        var rb = player.GetComponentInChildren<Rigidbody>(true);
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.Sleep();
            rb.WakeUp();
        }
    }

    private void CleanupEnemies()
    {
        var all = FindObjectsOfType<EnemyHealth>(true);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] == null) continue;
            if (all[i].team != Team.Enemy) continue;
            Destroy(all[i].gameObject);
        }
    }

    private void CleanupBullets()
    {
        var bullets = FindObjectsOfType<MagicBullet>(true);
        for (int i = 0; i < bullets.Length; i++)
        {
            if (bullets[i] != null)
                Destroy(bullets[i].gameObject);
        }
    }

    private void RespawnPlayerTeam()
    {
        if (player == null || playerSpawn == null) return;

        var rb = player.GetComponentInChildren<Rigidbody>(true);

        player.SetPositionAndRotation(playerSpawn.position, playerSpawn.rotation);

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            rb.position = playerSpawn.position;
            rb.rotation = playerSpawn.rotation;

            rb.Sleep();
            rb.WakeUp();
        }

        if (playerHealth != null)
            playerHealth.ResetForRound();

        var shoot = GetPlayerComponentInChildren<MobileShoot>();
        if (shoot != null)
            shoot.ResetWeapon();

        var move = GetPlayerComponentInChildren<MobileMovement>();
        if (move != null)
            move.ResetInput();

        if (camSwipeCached == null)
            camSwipeCached = FindMobileCameraSwipeSafe();

        if (camSwipeCached != null)
            camSwipeCached.ResetInput();
    }

    private bool IsEnemyTeamDead()
    {
        var all = FindObjectsOfType<EnemyHealth>(true);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] == null) continue;
            if (all[i].team == Team.Enemy) return false;
        }
        return true;
    }

    private bool IsPlayerTeamDead()
    {
        bool playerDead = playerHealth == null || playerHealth.IsDead;
        bool allyAlive = allySpawner != null && allySpawner.IsAlive();
        return playerDead && !allyAlive;
    }
}