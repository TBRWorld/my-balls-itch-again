using System.Collections.Generic;
using UnityEngine;
using System;

public class PathCheck : MonoBehaviour
{
    private WorldGen worldGen;
    public static event Action OnValidPathFound;

    // Subscribe to the event when a block is broken
    void OnEnable()
    {
        PlayerInteraction.OnWorldChanged += HandleBlockBroken;
        //listen for the event in PlayerInteraction called "OnBlockBroken", and subscribe (+=) method HandleBlockBroken to the event
    }

    // Unsubscribe from the event when the object is disabled (to prevent memory leaks)
    void OnDisable()
    {
        PlayerInteraction.OnWorldChanged -= HandleBlockBroken;
    }

    private void HandleBlockBroken()
    {
        worldGen = GameObject.FindFirstObjectByType<WorldGen>();

        Vector3Int entrance = worldGen.Entrence;
        Vector3Int inwardBlock = entrance + worldGen.SpawnDirection; // one block inside the cube
        Vector3Int goalPos = worldGen.HoleCoords; //this is a public getter
        goalPos.y = 1;

        // ONLY check path if that inward block is empty (i.e. the entrance tunnel is opened)
        if (!worldGen.worldBlocks.ContainsKey(inwardBlock))
        {
            if (PathExists(inwardBlock, goalPos))
            {
                Debug.Log("Path found! Spawning enemy.");
                OnValidPathFound?.Invoke();
            }
        }
        else
        {
            Debug.Log("Entrance is still blocked. Waiting for tunnel to be opened.");
        }
    }

    public bool PathExists(Vector3Int start, Vector3Int goal)
    {
        Queue<Vector3Int> frontier = new Queue<Vector3Int>();
        HashSet<Vector3Int> visited = new HashSet<Vector3Int>();

        frontier.Enqueue(start);
        visited.Add(start);

        Vector3Int[] directions = new Vector3Int[]
        {
            Vector3Int.right, Vector3Int.left,
            Vector3Int.forward, Vector3Int.back
        };

        while (frontier.Count > 0)
        {
            Vector3Int current = frontier.Dequeue();

            if (current == goal)
            { 
                Debug.Log("Path reached goal at " + current);
                return true;
            }

            foreach (var dir in directions)
            {
                Vector3Int neighbor = current + dir;

                if (!visited.Contains(neighbor) && !worldGen.worldBlocks.ContainsKey(neighbor))
                {
                    frontier.Enqueue(neighbor);
                    visited.Add(neighbor);
                }
            }
        }
        return false;
    }
}
