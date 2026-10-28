using UnityEngine;

/// <summary>Breaks into smaller enemies when it dies.</summary>
public class SplitterEnemy : Enemy
{
    [Header("Splitter")]
    [Tooltip("What spawns on death. Use a weaker prefab (e.g. a Runner), not another Splitter")]
    public GameObject splitPrefab;
    public int splitCount = 2;

    protected override void Reset()
    {
        base.Reset();
        maxHealth = 120f;
        resourceReward = 30;
        applyLook = true;
        bodyColor = new Color(0.7f, 0.3f, 0.9f);   // purple
        sizeMultiplier = 1.2f;
    }

    protected override void OnDeath()
    {
        if (splitPrefab == null) return;

        Vector3 spawnPos = SnapToGrid(transform.position);
        for (int i = 0; i < splitCount; i++)
            Instantiate(splitPrefab, spawnPos, Quaternion.identity);
    }
}
