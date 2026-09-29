using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class PlayerInteraction : MonoBehaviour
{
    public float reachDistance = 8f;
    public LayerMask blockMask;
    public GameObject blockPrefab;
    public static event Action OnWorldChanged;
    public static event Action SpawnEnemy;
    private GameObject currentHighlighted;
    private Collider playerCollider;
    private float nextEnemySpawnTime;

    //add worldGen to player interaction
    private WorldGen worldGen;

    void Start()
    {
        worldGen = GameObject.FindFirstObjectByType<WorldGen>();

        //define playerCollider
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            playerCollider = player.GetComponent<Collider>();
        }
        else
        {
            Debug.LogWarning("Player GameObject with tag 'Player' not found!");
        }
    }

    void Update()
    {
        //make a Raycast to see if a block is within range
        Ray ray = new Ray(transform.position, transform.forward);
        Debug.DrawRay(ray.origin, ray.direction * reachDistance, Color.red, 10f);

        //highlight
        if (Physics.Raycast(ray, out RaycastHit hit, reachDistance, blockMask))
        {
            GameObject hitObject = hit.collider.gameObject;
            if (hitObject != currentHighlighted && hit.collider.CompareTag("Diggable"))
            {
                ClearHighlight();

                currentHighlighted = hitObject;
                var outline = currentHighlighted.GetComponent<Outline>();   //outline does not work
                if (outline != null) outline.enabled = true;
            }
        }
        else ClearHighlight();

        //mining
        if (Input.GetMouseButtonDown(0)) 
        {
            Mine();
        }

        if (Input.GetMouseButtonDown(1))
        {
            Place();
        }

        //temp enemy spawning
        if (Input.GetKey(KeyCode.E) && PathCheck.PathFound && Time.time >= nextEnemySpawnTime)
        {
            SpawnEnemy?.Invoke();
            nextEnemySpawnTime = Time.time + 1f;
        }

    }

    void Mine()
    {
        Ray ray = new Ray(transform.position, transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, reachDistance, blockMask))
        {
            if (hit.collider.CompareTag("Diggable"))
            {
                //remove the block from the worldGen dictionary
                GameObject block = hit.collider.gameObject;
                Vector3Int gridPos = GetBlockGridPosition(block.transform.position);

                if (worldGen.worldBlocks.ContainsKey(gridPos))
                {
                    worldGen.worldBlocks.Remove(gridPos);
                }
                else
                {
                    Debug.LogWarning("Block not found in worldGen dictionary.");
                    Debug.Log(gridPos);
                }

                //destroy the block
                Destroy(hit.collider.gameObject);
                OnWorldChanged?.Invoke(); //check for path
            }
        }
    }
    void Place()
    {
        //add to grid
        Ray ray = new Ray(transform.position, transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, reachDistance, blockMask))
        {
            Vector3 placePos = hit.collider.transform.position + hit.normal * worldGen.spacing;
            Vector3Int gridPos = new Vector3Int(
             Mathf.RoundToInt(placePos.x / worldGen.spacing),
             Mathf.RoundToInt(placePos.y / worldGen.spacing),
             Mathf.RoundToInt(placePos.z / worldGen.spacing)
            );

            // Check if position already contains a block
            if (worldGen.worldBlocks.ContainsKey(gridPos))
            {
                Debug.Log("Blocked: Cannot place inside existing block.");
                return;
            }

            // Check if it overlaps the player
            Bounds blockBounds = new Bounds(gridPos * Mathf.RoundToInt(worldGen.spacing), Vector3.one * worldGen.spacing);
            if (playerCollider != null && blockBounds.Intersects(playerCollider.bounds))
            {
                Debug.Log("Blocked: Cannot place block inside the player.");
                return;
            }


            GameObject newBlock = Instantiate(blockPrefab, placePos, Quaternion.identity);
            //newBlock.name = $"Block_{gridPos.x}_{gridPos.y}_{gridPos.z}";
            worldGen.worldBlocks[gridPos] = newBlock;

            OnWorldChanged?.Invoke();//patchCheck
        }
    }
    Vector3Int GetBlockGridPosition(Vector3 worldPos)
    {
        float spacing = worldGen.spacing;
        int x = Mathf.RoundToInt(worldPos.x / spacing);
        int y = Mathf.RoundToInt(worldPos.y / spacing);
        int z = Mathf.RoundToInt(worldPos.z / spacing);
        return new Vector3Int(x, y, z);
    }
    void ClearHighlight()
    {
        if (currentHighlighted != null)
        {
            var outline = currentHighlighted.GetComponent<Outline>();
            if (outline != null) outline.enabled = false;
            currentHighlighted = null;
        }
    }
}

