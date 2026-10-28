using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Tooltip("Normal enemies. Filled automatically from Resources/enemyprefab (boss excluded)")]
    public GameObject[] enemyPrefabs;
    public GameObject bossPrefab;
    public float spawnDelay = 1f;

    public int maxEnemies = 10;
    [Tooltip("Every Nth spawn is the boss. 0 = never. Only one boss at a time")]
    public int bossEvery = 10;

    private List<GameObject> activeEnemies = new List<GameObject>();
    private int spawnCount;

    private WorldGen worldGen;

    public void Start()
    {
        // Every prefab in Resources/enemyprefab is used. The one with a BossEnemy component becomes the boss.
        List<GameObject> normal = new List<GameObject>();
        foreach (GameObject prefab in Resources.LoadAll<GameObject>("enemyprefab"))
        {
            if (prefab.GetComponent<BossEnemy>() != null) bossPrefab = prefab;
            else normal.Add(prefab);
        }
        enemyPrefabs = normal.ToArray();

        worldGen = FindFirstObjectByType<WorldGen>();
    }

    void Update()
    {
        for (int i = activeEnemies.Count - 1; i >= 0; i--)
        {
            var enemy = activeEnemies[i];
            if (enemy == null) { activeEnemies.RemoveAt(i); continue; }

            // Out of bounds check
            Vector3 pos = enemy.transform.position;
            if (pos.x < 0 || pos.y < 0 || pos.z < 0 ||
                pos.x > worldGen.gridSizeX * worldGen.spacing ||
                pos.y > worldGen.gridSizeY * worldGen.spacing ||
                pos.z > worldGen.gridSizeZ * worldGen.spacing)
            {
                Debug.LogWarning($"Enemy {i} out of bounds, destroying.");
                Destroy(enemy);
                activeEnemies.RemoveAt(i);
            }
        }
    }

    private void OnEnable()
    {
        PlayerInteraction.SpawnEnemy += HandleValidPath;
    }

    private void OnDisable()
    {
        PlayerInteraction.SpawnEnemy -= HandleValidPath;
    }

    private void HandleValidPath()
    {
        // Delay spawn slightly (optional)
        Invoke(nameof(SpawnEnemy), spawnDelay);
        Debug.Log("SpawnEnemy event received. Spawning enemy after delay.");
    }

    private bool BossAlive()
    {
        foreach (var e in activeEnemies)
            if (e != null && e.GetComponent<BossEnemy>() != null) return true;
        return false;
    }

    private void SpawnEnemy()
    {
        if (activeEnemies.Count >= maxEnemies)
            return;

        spawnCount++;
        bool spawnBoss = bossPrefab != null && bossEvery > 0
                         && spawnCount % bossEvery == 0 && !BossAlive();

        GameObject prefab;
        if (spawnBoss) prefab = bossPrefab;
        else if (enemyPrefabs.Length > 0) prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
        else return;

        Vector3 spawnPointGrid = worldGen.Entrence + worldGen.SpawnDirection;
        Vector3 spawnPoint = new Vector3(
            spawnPointGrid.x * worldGen.spacing,
            spawnPointGrid.y * worldGen.spacing,
            spawnPointGrid.z * worldGen.spacing
            );

        GameObject newEnemy = Instantiate(prefab, spawnPoint, Quaternion.identity);
        activeEnemies.Add(newEnemy);
    }
}
