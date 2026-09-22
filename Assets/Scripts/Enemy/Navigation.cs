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
            
            if(IsIntersection) // If at intersection, wait before moving
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
    /*bool IsIntersection(Vector3Int current, Vector3Int excludeDirection)
    {
        int openPaths = 0;

        Vector3Int[] directions = new Vector3Int[]
        {
        Vector3Int.forward, Vector3Int.back,
        Vector3Int.left, Vector3Int.right
        };

        foreach (var dir in directions)
        {
            if (dir == -excludeDirection) continue;

            Vector3Int neighbor = current + dir;
            if (!worldGen.worldBlocks.ContainsKey(neighbor))
                openPaths++;
        }

        return openPaths > 1; // Intersection = multiple valid forward directions
    } */

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
            if (dir == -previousDir) continue;                  //Don’t go backwards
            Vector3Int neighbor = current + dir;
            if (!worldGen.worldBlocks.ContainsKey(neighbor))    
            {
                possibleDirections.Add(dir);
                weights[dir] = GetAttractionValue(neighbor);    //Chance they choose this direction
            }

            bool isIntersection = possibleDirections.Count > 1;

            Vector3Int chosenDirection;
            if (possibleDirections.Count == 0)
            {
                // Dead end
                chosenDirection = -previousDir;
            }
            else if (possibleDirections.Count == 1)
            {
                // One way
                chosenDirection = possibleDirections[0];
            }
            else
            {
                chosenDirection = WeightedRandomChoice(weights);
            }
        }

        if (possibleDirections.Count <= 1)
        {
            return possibleDirections.Count == 1 ? possibleDirections[0] : -previousDir; //either one option or go back
        }
        else
        {
            // Only stop to decide at a crossroad (more than one option)
            return WeightedRandomChoice(weights);               //Decide dir based on attraction
        }
    }

    float GetAttractionValue(Vector3Int pos)
    {
        //TODO: MAKE ATTRACTION SYSTEM
        return 1f; // Neutral weight
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
