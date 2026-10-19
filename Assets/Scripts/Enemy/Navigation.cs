using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Navigation : MonoBehaviour
{
    private WorldGen worldGen;
    private EnemyMovement movement;
    bool isWaitingForDecision = false;

    public Vector3Int currentGridPos;
    public Vector3Int lastDirection = Vector3Int.forward; // Start with any direction

    void Start()
    {
        worldGen = FindFirstObjectByType<WorldGen>();
        movement = GetComponent<EnemyMovement>();

        currentGridPos = Vector3Int.RoundToInt(transform.position / worldGen.spacing);
    }

    void Update()
    {
        if (!movement.isMoving && !isWaitingForDecision) // Enemy has arrived at a new tile
        {
            currentGridPos = Vector3Int.RoundToInt(transform.position / worldGen.spacing);

            Vector3Int nextDir;
            bool isIntersection;
            (nextDir, isIntersection) = ChooseNextDirection(currentGridPos, lastDirection);

            lastDirection = nextDir;
            
            if(isIntersection) // If at intersection, wait before moving
            {
                StartCoroutine(DelayedSetDirection(nextDir));
            }
            else
            {
               movement.currentDirection = nextDir; // Set direction immediately if not at intersection
            }
        }
    }

    IEnumerator DelayedSetDirection(Vector3Int direction)
    {
        isWaitingForDecision = true;
        yield return new WaitForSeconds(movement.waitTime);
        movement.currentDirection = direction;
        isWaitingForDecision = false;
    }

    (Vector3Int, bool) ChooseNextDirection(Vector3Int current, Vector3Int previousDir)
    {
        List<Vector3Int> possibleDirections = new List<Vector3Int>();
        Dictionary<Vector3Int, float> weights = new Dictionary<Vector3Int, float>();

        Vector3Int[] directions = new Vector3Int[]
        {
            Vector3Int.forward, Vector3Int.back,
            Vector3Int.left, Vector3Int.right
        };

        foreach (var dir in directions)
        {
            if (dir == -previousDir) continue;                  //Dont go backwards
            Vector3Int neighbor = current + dir;
            if (IsWalkable(neighbor))    
            {
                possibleDirections.Add(dir);
                weights[dir] = GetAttractionValue(neighbor);    //Chance they choose this direction
            }

            bool isIntersection = possibleDirections.Count > 1;
        }

        if (possibleDirections.Count <= 1)
        {
            return (possibleDirections.Count == 1 ? possibleDirections[0] : -previousDir, false); //either one option or go back
        }
        else
        {
            // Only stop to decide at a crossroad (more than one option)
            return (WeightedRandomChoice(weights), true);       //Decide dir based on attraction
        }
    }

    bool IsWalkable(Vector3Int pos)
    {
        if (!worldGen.worldBlocks.ContainsKey(pos)) return true;

        GameObject block = worldGen.worldBlocks[pos];
        if (block == null) return true;

        // Traps and Lures do not block pathfinding
        if (block.GetComponent<Trap>() != null || block.GetComponent<Lure>() != null)
            return true;

        return false;
    }

    float GetAttractionValue(Vector3Int pos)
    {
        float attraction = 1f;

        if (worldGen.worldBlocks.TryGetValue(pos, out GameObject block))
        {
            if (block != null)
            {
                Lure lure = block.GetComponent<Lure>();
                if (lure != null)
                {
                    attraction *= lure.attractionStrength;
                }

                // Maybe traps have negative attraction? 
                // "Voeg trap en lure info aan de blok voor navigation.cs"
                // If enemies know it's a trap, they might avoid it unless lured.
                // For now, let's keep it neutral or slightly negative if we want "smart" enemies.
            }
        }

        return attraction;
    }

    Vector3Int WeightedRandomChoice(Dictionary<Vector3Int, float> weights)
    {
        float totalWeight = 0f;
        foreach (var val in weights.Values) totalWeight += val;

        float random = Random.Range(0, totalWeight);
        float runningSum = 0f;

        foreach (var pair in weights)
        {
            runningSum += pair.Value;
            if (random <= runningSum)
                return pair.Key;
        }

        return weights.Keys.First(); // fallback
    }
}
