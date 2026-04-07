using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public enum SpawnMode
    {
        TrainingWaves,
        Round2v2
    }

    [Header("Mode")]
    public SpawnMode mode = SpawnMode.TrainingWaves;

    [Header("Prefab")]
    public GameObject enemyPrefab;

    [Header("Spawn Points")]
    public Transform[] enemySpawnPoints;

    [Header("Bot Difficulty (Whole Match)")]
    public EnemyChase.BotDifficulty botDifficulty = EnemyChase.BotDifficulty.Easy;

    [Header("Safe Spawn")]
    public float minSpawnDistanceFromPlayer = 6f;
    public float spawnCheckRadius = 0.6f;
    public LayerMask obstacleMask; // Walls + Obstacles
    public int maxSpawnTries = 20;

    // ===== TRAINING WAVES SETTINGS =====
    [Header("Training Waves")]
    public float nextWaveDelay = 2f;

    public int baseCount = 5;
    public int countIncreasePerRound = 1;

    public float baseEnemySpeed = 3f;
    public float speedIncreaseEvery3Rounds = 0.5f;

    public int baseEnemyHP = 3;
    public int hpIncreaseEvery5Rounds = 1;

    public int maxAliveEnemies = 6;
    public float spawnInterval = 0.2f;

    // ===== ROUND 2v2 SETTINGS =====
    [Header("Round 2v2")]
    public int roundEnemyCount = 2;
    public float roundEnemySpeed = 3.5f;
    public int roundEnemyHP = 3;

    private int currentRound = 0;
    private bool waveRunning;
    private bool spawningWave;

    private Transform player;

    private int enemiesLeftToSpawn = 0;
    private float waveEnemySpeed;
    private int waveEnemyHP;

    // Для 2v2: чтобы строго назначить Enemy1 и Enemy2
    private int round2v2SpawnIndex = 0;

    private void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;

        if (mode == SpawnMode.TrainingWaves)
            StartCoroutine(WaveLoop());
    }

    // =========================
    //          2v2 API
    // =========================
    public void StartRound2v2()
    {
        if (mode != SpawnMode.Round2v2) return;

        round2v2SpawnIndex = 0;

        if (enemyPrefab == null) return;
        if (enemySpawnPoints == null || enemySpawnPoints.Length == 0) return;

        // ✅ Делаем список точек и перемешиваем — чтобы 2 врага не появлялись в одной и той же точке
        List<Transform> points = new List<Transform>(enemySpawnPoints);
        Shuffle(points);

        int toSpawn = Mathf.Max(0, roundEnemyCount);
        for (int i = 0; i < toSpawn; i++)
        {
            Transform preferredPoint = points[i % points.Count];
            SpawnOneEnemy(roundEnemySpeed, roundEnemyHP, preferredPoint.position);
        }
    }

    // =========================
    //      TRAINING WAVES
    // =========================
    private IEnumerator WaveLoop()
    {
        while (true)
        {
            if (!waveRunning && !spawningWave && CountAliveEnemies() == 0)
            {
                waveRunning = true;

                yield return new WaitForSeconds(nextWaveDelay);

                StartNextWave();
                waveRunning = false;
            }

            yield return null;
        }
    }

    private void StartNextWave()
    {
        currentRound++;

        if (GameManager.Instance != null)
            GameManager.Instance.NextRound();

        enemiesLeftToSpawn = baseCount + (currentRound - 1) * countIncreasePerRound;

        waveEnemySpeed = baseEnemySpeed + ((currentRound - 1) / 3) * speedIncreaseEvery3Rounds;
        waveEnemyHP = baseEnemyHP + ((currentRound - 1) / 5) * hpIncreaseEvery5Rounds;

        StartCoroutine(SpawnWaveOverTime());
    }

    private IEnumerator SpawnWaveOverTime()
    {
        spawningWave = true;

        while (enemiesLeftToSpawn > 0)
        {
            int alive = CountAliveEnemies();

            if (alive < maxAliveEnemies)
            {
                SpawnOneEnemy(waveEnemySpeed, waveEnemyHP, null);
                enemiesLeftToSpawn--;
            }

            yield return new WaitForSeconds(spawnInterval);
        }

        spawningWave = false;
    }

    // =========================
    //          SPAWN
    // =========================
    private void SpawnOneEnemy(float enemySpeed, int enemyHP, Vector3? preferredPos)
    {
        if (enemyPrefab == null) return;
        if (enemySpawnPoints == null || enemySpawnPoints.Length == 0) return;

        Vector3 spawnPos;

        // ✅ В 2v2 стараемся взять конкретную точку (чтобы не совпало)
        if (preferredPos.HasValue)
        {
            spawnPos = preferredPos.Value;

            // если точка плохая — пробуем найти безопасную
            if (!IsSpawnSafe(spawnPos) && !TryGetSafeSpawnPoint(out spawnPos))
                spawnPos = preferredPos.Value;
        }
        else
        {
            if (!TryGetSafeSpawnPoint(out spawnPos))
            {
                Transform fallback = enemySpawnPoints[Random.Range(0, enemySpawnPoints.Length)];
                spawnPos = fallback.position;
            }
        }

        GameObject e = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);

        // ✅ Назначаем ActorId только в режиме Round2v2 и только двум врагам
        if (mode == SpawnMode.Round2v2)
        {
            ActorId id = ActorId.None;

            if (round2v2SpawnIndex == 0) id = ActorId.Enemy1;
            else if (round2v2SpawnIndex == 1) id = ActorId.Enemy2;

            round2v2SpawnIndex++;

            if (id != ActorId.None)
                EnsureIdentity(e, id);
        }

        var chase = e.GetComponent<EnemyChase>();
        if (chase != null)
        {
            chase.speed = enemySpeed;
            chase.ApplyDifficulty(botDifficulty);
        }

        var hp = e.GetComponent<EnemyHealth>();
        if (hp != null)
            hp.hp = enemyHP;
    }

    private void EnsureIdentity(GameObject obj, ActorId id)
    {
        var identity = obj.GetComponent<ActorIdentity>();
        if (identity == null)
            identity = obj.AddComponent<ActorIdentity>();

        identity.actorId = id;
    }

    private bool TryGetSafeSpawnPoint(out Vector3 spawnPos)
    {
        spawnPos = Vector3.zero;

        for (int i = 0; i < maxSpawnTries; i++)
        {
            Transform sp = enemySpawnPoints[Random.Range(0, enemySpawnPoints.Length)];
            Vector3 p = sp.position;

            if (!IsSpawnSafe(p))
                continue;

            spawnPos = p;
            return true;
        }

        return false;
    }

    private bool IsSpawnSafe(Vector3 p)
    {
        if (player != null)
        {
            Vector3 flatPlayer = player.position;
            flatPlayer.y = p.y;

            if (Vector3.Distance(p, flatPlayer) < minSpawnDistanceFromPlayer)
                return false;
        }

        if (Physics.CheckSphere(p, spawnCheckRadius, obstacleMask))
            return false;

        return true;
    }

    private int CountAliveEnemies()
    {
        return GameObject.FindGameObjectsWithTag("Enemy").Length;
    }

    private void Shuffle(List<Transform> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int j = Random.Range(i, list.Count);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
