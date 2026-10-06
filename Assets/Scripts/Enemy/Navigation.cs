using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Navigation : MonoBehaviour
{
    private WorldGen worldGen;
    private EnemyMovement movement;
    bool isWaitingForDecision = false;

    private Vector3Int currentGridPos;
    public Vector3Int lastDirection = Vector3Int.forward; // Start with any direction
    public Dictionary<Vector3Int, Vector3Int[]> breadcrumbMemory = new Dictionary<Vector3Int, Vector3Int[]>(); // Stores intersections and chosen directions
    public GameObject debugBreadcrumbPrefab;
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
            bool waitBeforeMoving;
            (nextDir, waitBeforeMoving) = NavLoop(currentGridPos, lastDirection);

            lastDirection = nextDir;
            
            if(waitBeforeMoving) // If at intersection, wait before moving
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

    (Vector3Int, bool) NavLoop(Vector3Int current, Vector3Int previousDir)
    {
        List<Vector3Int> possibleDirections = new List<Vector3Int>();
        Dictionary<Vector3Int, float> weights = new Dictionary<Vector3Int, float>();

        Vector3Int[] directions = new Vector3Int[]
        {
            Vector3Int.forward, Vector3Int.back,
            Vector3Int.left, Vector3Int.right
        };

        // Check for if the enemy is lost/looped.
        if(!isBacktracking && breadcrumbMemory.ContainsKey(current)) 
        {
            Debug.LogWarning("enemy is lost, checking for unexplored intersections NOT YET IMPLEMENTED");
           if (current != breadcrumbOrder.Last()) //check if the current intersection is not the last one in memory
            {
                LostBehavior(current); //engage lost behavior to find unexplored intersections
            }
            else Debug.LogWarning("Enemy is lost, but the current intersection is the last one in memory. This should not happen.");
        }

        // Check for valid directions and calculate weights
        foreach (var dir in directions)
        {
            Vector3Int neighbor = current + dir;
            if (!worldGen.worldBlocks.ContainsKey(neighbor))    //if neighbor is not a wall, then it is a valid direction
            {
                if (!breadcrumbMemory.ContainsKey(current))         //if not in the breadcrumb memory, contiune as normal
                {
                    if (dir == -previousDir) continue;                  //Skip the Direction the enemy just came from.

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

        // Check if a big room (missing corner walls)
        bool isBigRoom = false;
        for (int dir1 = 0; dir1 < possibleDirections.Count; dir1++)
        {
            for (int dir2 = dir1 + 1; dir2 < possibleDirections.Count; dir2++)
            {
                Vector3Int corner = current + possibleDirections[dir1] + possibleDirections[dir2];

                if (corner == current) // exception for where the enemy came from, which is not a corner
                {
                    continue;
                }

                if (!worldGen.worldBlocks.ContainsKey(corner)) // If corner has no wall, then it's a big room
                {
                    isBigRoom = true;
                    break;
                }
            }

            if (isBigRoom)
            {
                break;
            }
        }

        // Handle how to choose the next direction
        if (isBigRoom) // If a big room, run big room logic.
        {
            Debug.Log("Enemy is in a big room");
            Vector3Int chosenDirection = BigRoomLogic(); // Choose thing of interest in the big room, and move towards it.
            return (chosenDirection, true);
        }
        else // Else continue with normal corridor logic.
        {
            if (possibleDirections.Count == 1)  //not intersection, only one valid direction
            {
                return (possibleDirections[0], false); 
            }
            else if (possibleDirections.Count == 0) //dead end or backtracking
            {
                //check if backtracking first
                if(breadcrumbMemory.ContainsKey(current) && isBacktracking)
                {
                    Vector3Int[] lastDirs = breadcrumbMemory[current];      // Force chosen direction to be the first direction in the breadcrumb.
                    return (lastDirs[0], false);
                }
                
                isBacktracking = true;
                return (-previousDir, false);
            }
            else //intersection, multiple valid directions
        {
            Vector3Int chosenDirection = WeightedRandomChoice(weights);       //Decide dir based on attraction
            
            if (breadcrumbMemory.ContainsKey(current)) //breadcrumb memory already exists, only add new dir.
            {
                Vector3Int[] lastDirs = breadcrumbMemory[current];
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
                breadcrumbMemory.Add(current, breadcrumb);
                breadcrumbOrder.Enqueue(current);

                //debug visible breadcrumbs at intersections
                /*Vector3 BreadcrumbLocation = new Vector3(    
                current.x * worldGen.spacing,
                current.y * worldGen.spacing,
                current.z * worldGen.spacing);
                
                Instantiate(debugBreadcrumbPrefab, BreadcrumbLocation, Quaternion.identity);*/
            }

            //Debug.Log($"Chosen direction from {current}: {chosenDirection}");
            //Debug.Log($"This Breadcrumb Memory: {string.Join(", ", breadcrumbMemory.Select(kvp => $"{kvp.Key}: [{string.Join(", ", kvp.Value)}]"))}");
   
            isBacktracking = false;
            return (chosenDirection, true);
        }
        }
    }
    Vector3Int BigRoomLogic()
    {
        return Vector3Int.zero; // Placeholder for big room logic, to be implemented later
    }
    void LostBehavior(Vector3Int current)
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
            float backtrackChance = 0.5f; // 50% chance to flee (this will be lightly influenced by panic/morale stat).
            float fleeChance = 0.05f;   // 5% chance to flee (Later this will be based on health, panic stat and maybe morale if added)
            float randomChance = (sameDirChance + backtrackChance) / 2; // Random option, disable for specific enemy types
                
            Dictionary<Vector3Int, float> lostRoll = new Dictionary<Vector3Int, float>
            {
                { Vector3Int.zero, sameDirChance },
                { Vector3Int.one, backtrackChance },
                { Vector3Int.down, fleeChance },
                { Vector3Int.right, (sameDirChance + fleeChance) / 2} // Neutral option
            };

            Vector3Int lostBehavior = WeightedRandomChoice(lostRoll); // Roll to determine behavior
            if(lostBehavior == Vector3Int.zero)
            {
                // Continue in the same direction
                // Must engage a follow behabior to follow the breadcrumb memory up till the point of a randomly chosen unexplored intersection.
            }
            else if(lostBehavior == Vector3Int.one)
            {
                // Backtrack behavior
                // Engage backtracking behavior.
            }
            else if(lostBehavior == Vector3Int.down)
            {
                // Flee behavior
                // Engage fleeing behavior.
            }
            else if(lostBehavior == Vector3Int.right)
            {
                // Neutral behavior
                // Try a different path (blacklist the previously chosen path, include path enemy came from))
            }
        }
        else //if no unexplored intersections, start backtracking from the breadcrumb[0] direction.
        {                   
            isBacktracking = true;
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
