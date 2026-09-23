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

        if(!isBacktracking && breadcrumbMemory.ContainsKey(current)) //check for if the enemy is lost/looped.
        {
           if (current != breadcrumbOrder.Last()) //check if the current intersection is not the last one in memory
            {
                int currentIndex = breadcrumbOrder.ToList().IndexOf(current) + 1; //may cause lag, and/or errors if breadcrumbOrder is empty
                int unexploredCount = 0;
                for (int i = currentIndex; i < breadcrumbOrder.Count; i++)
                {
                    if (breadcrumbMemory[breadcrumbOrder.ElementAt(i)].Contains(Vector3Int.zero)) //if the intersection has unexplored directions, count it.
                    {
                        unexploredCount++;
                    }
                }

                if (unexploredCount > 0)
                {
                    //IMPORTANT: MAKE ADJUSTABLE LATER FOR PANIC STAT, HEALTH, PERSONALITIES ETC
                    float sameDirChance = unexploredCount * 0.2f; // 20% chance for each unexplored intersection
                    float fleeChance = 0.5f; // 50% chance to flee PLACEHOLDER
                    Dictionary<Vector3Int, float> lostRoll = new Dictionary<Vector3Int, float>
                    {
                        { Vector3Int.zero, sameDirChance },
                        { Vector3Int.one, fleeChance },
                        { Vector3Int.down, (sameDirChance + fleeChance) / 2} // Neutral option
                    };

                    Vector3Int lostBehavior = WeightedRandomChoice(lostRoll); // Roll to determine behavior
                    if(lostBehavior == Vector3Int.zero)
                    {
                        // Continue in the same direction
                        // Must engage a follow behabior to follow the breadcrumb memory up till the point of a randomly chosen unexplored intersection.
                    }
                    else if(lostBehavior == Vector3Int.one)
                    {
                        // Flee behavior
                        // Engage flee behavior and backtracking behavior.
                    }
                    else
                    {
                        // Try a different path (blacklist the previously chosen path, include path enemy came from))
                    }
                }
                else //if no unexplored intersections, start backtracking.
                {
                    isBacktracking = true;
                }
            }
        }

        foreach (var dir in directions) //if enemy not lost, check for valid directions and calculate weights.
        {
            if (dir == -previousDir) continue;                  //Skip dir where enemy came from
            Vector3Int neighbor = current + dir;
            if (!worldGen.worldBlocks.ContainsKey(neighbor))    //if neighbor is not a wall, then it is a valid direction
            {
                if (!breadcrumbMemory.ContainsKey(current))         //if not in the breadcrumb memory, contiune as normal
                {
                    possibleDirections.Add(dir);

                    Vector3Int[] dirCorridor = SeenCorridor(current, dir);
                    weights[dir] = GetAttractionValue(dirCorridor);
                }
                else    //If in the breadcrumb memory, check if this direction is in it.
                {
                    if (!breadcrumbMemory[current].Contains(dir))    //if breadcrumb memory includes dir, ignore.
                    {            
                        possibleDirections.Add(dir);

                        Vector3Int[] dirCorridor = SeenCorridor(current, dir);
                        weights[dir] = GetAttractionValue(dirCorridor);
                    }
                }
            }
        }

        //Debug.Log($"Possible directions from {current}: {string.Join(", ", possibleDirections)}");

        // handle how to choose the next direction based on the number of possible directions
        if (possibleDirections.Count == 1)  //not intersection, only one valid direction
        {
            return (possibleDirections[0], false); 
        }
        else if (possibleDirections.Count == 0) //dead end or backtracking
        {
            //check if backtracking first
            if(breadcrumbMemory.ContainsKey(current) && isBacktracking)
            {
                Vector3Int[] lastDirs = breadcrumbMemory[current];
                return (lastDirs[0], false);
            }
            isBacktracking = true;
            return (-previousDir, false);
        }
        else //intersection, multiple valid directions
        {
            Vector3Int chosenDirection = WeightedRandomChoice(weights);       //Decide dir based on attraction
            //remember this intersection and its dir in breadcrumb memory
            if (breadcrumbMemory.ContainsKey(current)) //breadcrumb memory already exists, only add new dir.
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
                breadcrumbOrder = new Queue<Vector3Int>(breadcrumbOrder.Where(pos => pos != current)); // Remove current from order
                breadcrumbOrder.Enqueue(current); // Update the order of intersections
            }
            else //breadcrumb memory does not exist, create new.
            {
                if (breadcrumbMemory.Count >= maxRememberedIntersections)
                {
                    breadcrumbMemory.Remove(breadcrumbOrder.Dequeue());
                }

                Vector3Int[] breadcrumb = new Vector3Int[possibleDirections.Count + 1]; // Create an array with the chosen direction
                breadcrumb[0] = -previousDir;
                breadcrumb[1] = chosenDirection;
                //apperantly null doesn't exist in Vecto3Int, so all empty routes should automatically be set to Vector3Int.zero
                /*int index = 0;
                foreach(var dir in breadcrumb)
                {
                    if (dir == null) breadcrumb[index] = Vector3Int.zero; // Fill remaining slots with zero
                    index++;
                }*/
                breadcrumbMemory.Add(current, breadcrumb);
                breadcrumbOrder.Enqueue(current);
            }

            //Debug.Log($"Chosen direction from {current}: {chosenDirection}");
            //Debug.Log($"This Breadcrumb Memory: {string.Join(", ", breadcrumbMemory.Select(kvp => $"{kvp.Key}: [{string.Join(", ", kvp.Value)}]"))}");

    
            return (chosenDirection, true);
        }
    }

    float GetAttractionValue(Vector3Int[] corridor)
    {
        //TODO: MAKE ATTRACTION SYSTEM
        return 1f; // Neutral weight
    }
    Vector3Int[] SeenCorridor(Vector3Int current, Vector3Int direction)
    {
        List<Vector3Int> corridor = new List<Vector3Int>();
        Vector3Int nextPos = current + direction;

        while (!worldGen.worldBlocks.ContainsKey(nextPos) && corridor.Count < visionRange)
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
