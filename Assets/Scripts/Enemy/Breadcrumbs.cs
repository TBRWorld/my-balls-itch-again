using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracks memory of explored positions and paths for an enemy.
/// </summary>
public class Breadcrumbs : MonoBehaviour
{
    // Tracks visited positions
    private HashSet<Vector3Int> visitedPositions = new HashSet<Vector3Int>();

    // Tracks directions taken from each position
    private Dictionary<Vector3Int, HashSet<Vector3Int>> exploredDirections = new Dictionary<Vector3Int, HashSet<Vector3Int>>();

    // Marks a block as visited.
    public void MarkVisited(Vector3Int pos)
    {
        visitedPositions.Add(pos);
    }

    // Marks a direction from given position when being explored.
    public void MarkDirectionTaken(Vector3Int fromPos, Vector3Int direction)
    {
        if (!exploredDirections.ContainsKey(fromPos))               
        {
            exploredDirections[fromPos] = new HashSet<Vector3Int>();    
        }

        exploredDirections[fromPos].Add(direction);
    }

    // Checks if the given block was visited before.
    public bool HasVisited(Vector3Int pos)
    {
        return visitedPositions.Contains(pos);
    }

    // Checks if a direction from a block has been explored.
    public bool IsDirectionExplored(Vector3Int fromPos, Vector3Int direction)
    {
        return exploredDirections.ContainsKey(fromPos) && exploredDirections[fromPos].Contains(direction);
    }

    // Returns how many directions were explored from a given position.
    public int ExploredDirectionCount(Vector3Int fromPos)
    {
        return exploredDirections.ContainsKey(fromPos) ? exploredDirections[fromPos].Count : 0;
    }

    // Checks if all directions from this position have been explored.
    public bool IsFullyExplored(Vector3Int fromPos, List<Vector3Int> availableDirs)
    {
        if (!exploredDirections.ContainsKey(fromPos)) return false;

        foreach (Vector3Int dir in availableDirs)
        {
            if (!exploredDirections[fromPos].Contains(dir))
                return false;
        }

        return true;
    }

    // Returns the directions already explored from a given position.
    public HashSet<Vector3Int> GetExploredDirections(Vector3Int fromPos)
    {
        return exploredDirections.ContainsKey(fromPos) ? exploredDirections[fromPos] : new HashSet<Vector3Int>();
    }
}
