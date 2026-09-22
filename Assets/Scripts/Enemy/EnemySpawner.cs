using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public GameObject enemyPrefab;
    public float spawnDelay = 1f;

    public int maxEnemies = 10;
    private List<GameObject> activeEnemies = new List<GameObject>();

    private WorldGen worldGen;

    public void Start()
    {
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

    private void SpawnEnemy()
    {
        if (activeEnemies.Count >= maxEnemies)
            return;

        Vector3 spawnPointGrid = worldGen.Entrence + worldGen.SpawnDirection;
        Vector3 spawnPoint = new Vector3(
            spawnPointGrid.x * worldGen.spacing, 
            spawnPointGrid.y * worldGen.spacing, 
            spawnPointGrid.z * worldGen.spacing
            );

        GameObject newEnemy = Instantiate(enemyPrefab, spawnPoint, Quaternion.identity);
        activeEnemies.Add(newEnemy);
    }
}
