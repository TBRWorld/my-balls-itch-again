using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Creates enemies at the entrance. It no longer decides WHEN to spawn.
/// The WaveManager calls SpawnEnemy().
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [Tooltip("Normal enemies, in the order they unlock. Drag prefabs here (or leave empty to use Resources/enemyprefab)")]
    public GameObject[] enemyPrefabs;
    public GameObject bossPrefab;

    private List<GameObject> activeEnemies = new List<GameObject>();
    private WorldGen worldGen;

    public GameObject[] NormalPrefabs => enemyPrefabs;

    public void Start()
    {
        // Prefabs dragged into this component in the Inspector are used first.
        // Only if none are assigned, everything in Resources/enemyprefab is used instead.
        List<GameObject> candidates = new List<GameObject>();
        foreach (GameObject prefab in enemyPrefabs)
            if (prefab != null) candidates.Add(prefab);
        if (bossPrefab != null) candidates.Add(bossPrefab);

        if (candidates.Count == 0)
            candidates.AddRange(Resources.LoadAll<GameObject>("enemyprefab"));

        // The prefab with a BossEnemy component becomes the boss, the rest are normal enemies.
        List<GameObject> normal = new List<GameObject>();
        foreach (GameObject prefab in candidates)
        {
            if (prefab.GetComponent<BossEnemy>() != null) bossPrefab = prefab;
            else if (!normal.Contains(prefab)) normal.Add(prefab);
        }
        enemyPrefabs = normal.ToArray();

        worldGen = FindFirstObjectByType<WorldGen>();
        Debug.Log("EnemySpawner ready. Normal prefabs: " + enemyPrefabs.Length + ", boss: " + (bossPrefab != null));
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

    /// <summary>Spawns one enemy at the entrance. Returns it, or null if it failed.</summary>
    public GameObject SpawnEnemy(GameObject prefab)
    {
        if (prefab == null || worldGen == null) return null;

        Vector3 spawnPointGrid = worldGen.Entrence + worldGen.SpawnDirection;
        Vector3 spawnPoint = new Vector3(
            spawnPointGrid.x * worldGen.spacing,
            spawnPointGrid.y * worldGen.spacing,
            spawnPointGrid.z * worldGen.spacing
            );

        GameObject newEnemy = Instantiate(prefab, spawnPoint, Quaternion.identity);
        activeEnemies.Add(newEnemy);
        return newEnemy;
    }
}
