using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PlayerInteraction : MonoBehaviour
{
    public float reachDistance = 8f;
    public LayerMask blockMask;
    public GameObject blockPrefab;
    public static event Action OnWorldChanged;
    public static event Action SpawnEnemy;

    [Header("Placement Settings")]
    public GameObject selectedPrefab;
    public int selectedCost;

    [Header("UI Settings")]
    public Image crosshairImage;
    public Color crosshairInRangeColor = Color.green;
    public Color crosshairDefaultColor = Color.white;

    private GameObject currentHighlighted;
    private Collider playerCollider;
    private float nextEnemySpawnTime;

    //add worldGen to player interaction
    private WorldGen worldGen;

    void Start()
    {
        worldGen = FindFirstObjectByType<WorldGen>();

        if (worldGen == null)
        {
            Debug.LogError("WORLDGEN IS NULL");
        }
        else
        {
            Debug.Log("Found WorldGen: " + worldGen.name);
        }

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
        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log("LEFT CLICK DETECTED");
        }
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
                
                if (crosshairImage != null)
                {
                    crosshairImage.color = crosshairInRangeColor;
                }
            }
        }
        else 
        {
            ClearHighlight();
            if (crosshairImage != null)
            {
                crosshairImage.color = crosshairDefaultColor;
            }
        }

        // Cancel a pending placement
        if (selectedPrefab != null && Input.GetKeyDown(KeyCode.Escape))
        {
            ClearSelection();
        }

        // Ignore clicks that land on UI (e.g. the tower menu buttons)
        bool pointerOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

        if (!pointerOverUI && Input.GetMouseButtonDown(1))
        {
            if (TowerMenu.IsOpen)
            {
                TowerMenu.Instance.HideMenu();
            }
            else if (TowerMenu.Instance != null)
            {
                TowerMenu.Instance.ShowMenu(Input.mousePosition);
            }
        }

        if (!pointerOverUI && Input.GetMouseButtonDown(0))
        {
            Debug.Log("selectedPrefab = " + selectedPrefab);

            if (selectedPrefab != null)
            {
                Debug.Log("CALLING PLACE()");
                Place();
            }
            else
            {
                Debug.Log("GOING TO MINE()");
                Mine();
            }
        }

        //temp enemy spawning
        if (Input.GetKey(KeyCode.E) && PathCheck.PathFound && Time.time >= nextEnemySpawnTime)
        {
            SpawnEnemy?.Invoke();
            nextEnemySpawnTime = Time.time + 1f;
        }
    }

    public void SetSelectedTower(GameObject prefab, int cost)
    {
        Debug.Log("Selected tower: " + prefab.name);

        selectedPrefab = prefab;
        selectedCost = cost;

        Debug.Log("selectedPrefab now = " + selectedPrefab);
    }

    public void ClearSelection()
    {
        Debug.Log("CLEAR SELECTION CALLED");

        selectedPrefab = null;
        selectedCost = 0;
    }

    void Mine()
    {
        Ray ray = new Ray(transform.position, transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, reachDistance, blockMask))
        {
            Debug.Log(hit.collider.name);

            if (
                hit.collider.CompareTag("Diggable") ||
                hit.collider.GetComponentInParent<Tower>() != null ||
                hit.collider.GetComponentInParent<Trap>() != null
            )
            {
                GameObject block = hit.collider.transform.root.gameObject;

                Vector3Int gridPos = GetBlockGridPosition(block.transform.position);

                if (worldGen.worldBlocks.ContainsKey(gridPos))
                {
                    worldGen.worldBlocks.Remove(gridPos);
                }

                Destroy(block);

                OnWorldChanged?.Invoke();
            }
        }
    }

    void Place()
    {
        Debug.Log("PLACE ENTERED");

        if (selectedPrefab == null)
        {
            Debug.Log("selectedPrefab NULL");
            return;
        }

        if (ResourceManager.Instance != null && !ResourceManager.Instance.CanAfford(selectedCost))
        {
            Debug.Log("Not enough resources!");
            return;
        }

        //add to grid
        Ray ray = new Ray(transform.position, transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, reachDistance, blockMask))
        {
            Vector3 normal = hit.normal;
            bool isFloor = Vector3.Dot(normal, Vector3.up) > 0.9f;

            // Placement constraints
            Trap trap = selectedPrefab.GetComponent<Trap>();
            if (trap != null && !isFloor)
            {
                Debug.Log("Traps can only be placed on the floor!");
                return;
            }

            Tower tower = selectedPrefab.GetComponent<Tower>();

            Vector3 rawPlacePos = hit.collider.transform.position + normal * worldGen.spacing;
            Vector3Int gridPos = new Vector3Int(
             Mathf.RoundToInt(rawPlacePos.x / worldGen.spacing),
             Mathf.RoundToInt(rawPlacePos.y / worldGen.spacing),
             Mathf.RoundToInt(rawPlacePos.z / worldGen.spacing)
            );
            
            Vector3 snappedPlacePos = new Vector3(
                gridPos.x * worldGen.spacing,
                gridPos.y * worldGen.spacing,
                gridPos.z * worldGen.spacing
            );

            // Check if position already contains a block
            if (worldGen.worldBlocks.ContainsKey(gridPos))
            {
                Debug.Log("Blocked: Cannot place inside existing block.");
                return;
            }

            // Check if it overlaps the player
            Bounds blockBounds = new Bounds(snappedPlacePos, Vector3.one * worldGen.spacing);
            if (playerCollider != null && blockBounds.Intersects(playerCollider.bounds))
            {
                Debug.Log("Blocked: Cannot place block inside the player.");
                return;
            }

            if (ResourceManager.Instance != null)
            {
                ResourceManager.Instance.SpendResources(selectedCost);
            }

            GameObject newBlock = Instantiate(
            selectedPrefab,
            snappedPlacePos,
            Quaternion.identity
            );

            worldGen.worldBlocks[gridPos] = newBlock;

            OnWorldChanged?.Invoke();//patchCheck

            // One tower per menu pick, so left click goes back to mining afterwards
            ClearSelection();
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
