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
    public Dictionary<Vector3Int, Vector3Int[]> breadcrumbMemory = new Dictionary<Vector3Int, Vector3Int[]>(); // Stores intersections and chosen directions
    public int maxRememberedIntersections = 10;
    private Queue<Vector3Int> breadcrumbOrder = new Queue<Vector3Int>();
    public int visionRange = 4;
    private bool isBacktracking = false;

    void Start()
    {
        worldGen = FindFirstObjectByType<WorldGen>();
        movement = GetComponent<EnemyMovement>();

        currentGridPos = Vector3Int.RoundToInt(transform.position / worldGen.spacing);
    }

    void Update()
    {
        if (!movement.isMoving && movement.currentDirection == Vector3.zero && !isWaitingForDecision) // Enemy has arrived at a new tile
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
            if (dir == -previousDir) continue;                  //Don't go backwards
            Vector3Int neighbor = current + dir;
            if (!worldGen.worldBlocks.ContainsKey(neighbor))    
            {
                if (!breadcrumbMemory.ContainsKey(current))         //if not in the breadcrumb memory, contiune as normal
                {
                    possibleDirections.Add(dir);
                    Vector3Int[] dirCorridor = seenCorridor(current, dir);
                    weights[dir] = GetAttractionValue(dirCorridor);
                }
                else    //If in the breadcrumb memory, check if this direction is in it.
                {
                    Vector3Int[] lastDirs = breadcrumbMemory[current];
                    if (!lastDirs.Contains(dir))    //if breadcrumb memory includes dir, ignore.
                    {
                        //if at intersection that is a breacrumb but not the one predicted (last one), then look at memory if there are unexplored directions, then decide on the amount of direction and panic stat if they follow the same path, go back or flee.
                        
                        possibleDirections.Add(dir);
                        Vector3Int[] dirCorridor = seenCorridor(current, dir);
                        weights[dir] = GetAttractionValue(dirCorridor);
                    }
                }
            }
        }

        Debug.Log($"Possible directions from {current}: {string.Join(", ", possibleDirections)}");

        if (possibleDirections.Count == 1)
        {
            return (possibleDirections[0], false); // Only one option, no intersection
        }
        else if (possibleDirections.Count == 0)
        {
            // No valid directions, backtrack
            isBacktracking = true;
            return (-previousDir, false);
        }
        else
        {
            Vector3Int chosenDirection = WeightedRandomChoice(weights);       //Decide dir based on attraction
            //remember this intersection and its dir in breadcrumb memory
            if (maxRememberedIntersections > 0)
            {
                if (breadcrumbMemory.ContainsKey(current))
                {
                    Vector3Int[] lastDirs = breadcrumbMemory[current]; //add new direction to existing array of directions
                    int emptySlotIndex = System.Array.IndexOf(lastDirs, Vector3Int.zero);
                    if (emptySlotIndex != -1)
                    {
                        lastDirs[emptySlotIndex] = chosenDirection;
                        breadcrumbMemory[current] = lastDirs;
                    }
                    else
                    {
                        Debug.LogWarning("Breadcrumb memory for this intersection is full");
                    }
                }
                else 
                {
                    if (breadcrumbMemory.Count >= maxRememberedIntersections)
                    {
                        breadcrumbMemory.Remove(breadcrumbOrder.Dequeue());
                    }

                    Vector3Int[] breadcrumb = new Vector3Int[possibleDirections.Count]; // Create an array with the chosen direction
                    breadcrumb[0] = -previousDir;
                    breadcrumb[1] = chosenDirection;
                    int index = 0;
                    foreach(var dir in breadcrumb)
                    {
                        if (dir == null) breadcrumb[index] = Vector3Int.zero; // Fill remaining slots with zero
                        index++;
                    }
                    breadcrumbMemory.Add(current, breadcrumb);
                    breadcrumbOrder.Enqueue(current);
                }
            }

            Debug.Log($"Chosen direction from {current}: {chosenDirection}");
            //Debug.Log($"This Breadcrumb Memory: {string.Join(", ", breadcrumbMemory.Select(kvp => $"{kvp.Key}: [{string.Join(", ", kvp.Value)}]"))}");

            return (chosenDirection, true);
        }
    }

    float GetAttractionValue(Vector3Int[] Corridor)
    {
        //TODO: MAKE ATTRACTION SYSTEM
        return 1f; // Neutral weight
    }
    Vector3Int[] seenCorridor(Vector3Int current, Vector3Int direction)
    {
        List<Vector3Int> corridor = new List<Vector3Int>();
        Vector3Int nextPos = current + direction;

        while (!worldGen.worldBlocks.ContainsKey(nextPos) && corridor.Count <= visionRange)
        {
            corridor.Add(nextPos);
            nextPos += direction;
        }

        return corridor.ToArray();
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
