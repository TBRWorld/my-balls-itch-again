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
    private List<Breadcrumb> breadcrumbMem = new List<Breadcrumb>(); // Stores intersections and chosen directions (consider making a directory if causing lag)
    public GameObject debugBreadcrumbPrefab;
    public int maxRememberedIntersections = 10;
    //private Queue<Vector3Int> breadcrumbOrder = new Queue<Vector3Int>();
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
        Breadcrumb currentBreadcrumb = breadcrumbMem.FirstOrDefault(breadcrumb => breadcrumb.position == current);  // Check if the current position is in the breadcrumb memory
        if (currentBreadcrumb == null)
        {
            currentBreadcrumb = breadcrumbMem.FirstOrDefault(breadcrumb => breadcrumb.roomInfo.cells.Contains(current)); // Check if the current position is in any of the remembered rooms
        }

        Vector3Int[] directions = new Vector3Int[]
        {
            Vector3Int.forward, Vector3Int.back,
            Vector3Int.left, Vector3Int.right
        };

        // Check for if the enemy is lost/looped.
        if(!isBacktracking && currentBreadcrumb != null) 
        {
            Debug.LogWarning("enemy is lost, checking for unexplored intersections NOT YET IMPLEMENTED");
           if (currentBreadcrumb != breadcrumbMem.Last()) //check if the current intersection is not the last one added to memory
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
                if (currentBreadcrumb == null)         //if not in the breadcrumb memory, continue as normal
                {
                    if (dir == -previousDir) continue;                  //Skip the Direction the enemy just came from.

                    possibleDirections.Add(dir);

                    Vector3Int[] dirCorridor = SeenCorridor(current, dir);
                    weights[dir] = GetAttractionValue(dirCorridor);
                }
                else    //If in the breadcrumb memory, check if this direction is in it.
                {
                    if (!currentBreadcrumb.directions.Contains(dir))    //if breadcrumb memory includes dir, ignore.
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

                if (corner == current) // exception for opposite dirs (which results in "corner" being the same as current)
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
        //ADD AFTER FINISHING ASSIGNMENT
        /*if (isBigRoom) // If a big room, run big room logic.
        {
            Debug.Log("Enemy is in a big room");
            Vector3Int chosenDirection = BigRoomLogic(possibleDirections, current, directions); // Choose thing of interest in the big room, and move towards it.
            return (chosenDirection, true);
        }*/
        //else // Else continue with normal corridor logic.
        //{
            if (possibleDirections.Count == 1)  //not intersection, only one valid direction
            {
                return (possibleDirections[0], false); 
            }
            else if (possibleDirections.Count == 0) //dead end or backtracking
            {
                //check if backtracking first
                if(currentBreadcrumb != null && isBacktracking)
                {
                    breadcrumbMem.Remove(currentBreadcrumb);    //brainfog, might break the order
                    breadcrumbMem.Add(currentBreadcrumb);
                    Vector3Int[] lastDirs = currentBreadcrumb.directions;      // Force chosen direction to be the first direction in the breadcrumb.
                    return (lastDirs[0], false);
                }
                
                isBacktracking = true;
                return (-previousDir, false);
            }
            else //intersection, multiple valid directions
        {
            Vector3Int chosenDirection = WeightedRandomChoice(weights);       //Decide dir based on attraction
            
            if (currentBreadcrumb != null) //breadcrumb memory already exists, only add new dir.
            {
                Vector3Int[] lastDirs = currentBreadcrumb.directions;
                int emptySlotIndex = System.Array.IndexOf(lastDirs, Vector3Int.zero);
                if (emptySlotIndex != -1)
                {
                    lastDirs[emptySlotIndex] = chosenDirection; 
                    currentBreadcrumb.directions = lastDirs;
                }
                else
                {
                    Debug.LogWarning("Breadcrumb memory for this intersection is full, something went wrong.");
                }
                breadcrumbMem.Remove(currentBreadcrumb);
                breadcrumbMem.Add(currentBreadcrumb);
            }
            else //breadcrumb memory does not exist, create new.
            {
                if (breadcrumbMem.Count >= maxRememberedIntersections)
                {
                    breadcrumbMem.RemoveAt(0);
                }

                Vector3Int[] dirs = new Vector3Int[possibleDirections.Count + 1]; // Create an array with the chosen direction
                dirs[0] = -previousDir;
                dirs[1] = chosenDirection;
                Breadcrumb breadcrumb = new Breadcrumb(current, dirs, null);
                //apparantly null doesn't exist in Vector3Int, so all empty routes should automatically be set to Vector3Int.zero
                breadcrumbMem.Add(breadcrumb);

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
        //}
        }
    }
    Vector3Int BigRoomLogic(List<Vector3Int> possibleDirections, Vector3Int current, Vector3Int[] directions)
    {
        // Map out the big room first.
        Vector3Int scanDir = Vector3Int.zero;
        Vector3Int insideRoomDir = lastDirection; // lastDirection is the direction the enemy walked to to get here, it is always the inside of the room at the start.
        foreach (var dir in possibleDirections)
        {
            if (dir == -insideRoomDir || dir == insideRoomDir) continue; // Skip the direction the enemy came from and the direction it is currently facing.
            scanDir = dir;
            break;
        }

        Vector3Int scanPos = current + scanDir;
        while (scanPos != current)
        {
            bool openSide = false;
            bool cornerFound = false;
            foreach (var dir in directions)
            {
                if (dir == -scanDir) continue; // Skip dir enemy came from

                Vector3Int neighbor = scanPos + dir;
                if (!worldGen.worldBlocks.ContainsKey(neighbor)) // Neighbor is not a wall
                {
                    //Opening to the side, check if it leads to a corridor or another big room.
                    if (dir == -insideRoomDir) openSide = true;                
                }
                else // Neighbor is a wall
                {
                    if (dir == insideRoomDir) // Wall in the direction of the inside of the room
                    {
                        // Go back
                        // save POI
                        cornerFound = true;
                    }
                    // Wall in the direction of scanning, rotate
                    else if (dir == scanDir) cornerFound = true;
                }
            }

            if(openSide) //opening to the side (POI)
            {
                // Save POI
            }

            if(cornerFound) //corner found, rotate
            {
                // Save corner
                // Rotate
            }
            else //no corner found, continue scanning
            {
                scanPos += scanDir;
            }
        }
        // All 1 wide openings are considered corridors, map them out together with loot.
        // Choose one of the things of interest in the big room. (-70% chance to choose if it's outside the vision range)
        // Calculate the "direction" (Enemy movement uses direction to move, so the set Direction should be reverse engineered)
        return Vector3Int.zero; // Placeholder for big room logic, to be implemented later
    }
    void LostBehavior(Vector3Int current)
    {
        int currentIndex = breadcrumbMem.FindIndex(breadcrumb => breadcrumb.position == current);
        if (currentIndex == -1)
        {
            Debug.LogWarning("Current position not found in breadcrumb memory, cannot determine lost behavior. Something went wrong before calling this method.");
            return;
        }

        int unexploredCount = 0;
        for (int i = currentIndex + 1; i < breadcrumbMem.Count; i++)
        {
            if (breadcrumbMem[i].directions.Contains(Vector3Int.zero)) //if the intersection has unexplored directions, count it.
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
            //consider enum
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

        // May break if weights is empty
        return weights.Keys.First(); // fallback
    }

    class Breadcrumb
    {
        public Vector3Int position;
        public Vector3Int[] directions;
        public RoomInfo roomInfo;
        

        public Breadcrumb(Vector3Int pos, Vector3Int[] dirs, RoomInfo room)
        {
            position = pos;
            directions = dirs;
            roomInfo = room;
        }
    }

    class RoomInfo
    {
        public List<Vector3Int> cells = new();
        public List<(Vector3Int pos, Vector3Int dir)> exits = new();
        public List<Vector3Int> loot = new();       // Remove/replace
        public Dictionary<Vector3Int, Vector3Int> cameFrom = new();  
    }
}
