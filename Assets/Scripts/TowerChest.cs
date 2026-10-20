using UnityEngine;

public class TowerChest : Tower
{
    [Header("Lure Settings")]
    public float chestAttraction = 10f;
    
    private Lure lure;

    private void Awake()
    {
        lure = GetComponent<Lure>();
        if (lure == null)
        {
            lure = gameObject.AddComponent<Lure>();
        }
        lure.attractionStrength = chestAttraction;
    }

    // Override Update to disable default tower firing logic if it's just a chest
    // Or keep it if you want the chest to also shoot! 
    // The user said "treasure chest type thing", so we'll make it passive by default.
    protected override void AcquireTarget()
    {
        // Chest doesn't target anything
        currentTarget = null;
    }
}
