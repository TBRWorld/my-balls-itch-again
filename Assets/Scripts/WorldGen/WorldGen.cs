using System.Collections.Generic;
using System.Drawing;
using UnityEngine;
using static UnityEngine.UI.Image;

public class WorldGen : MonoBehaviour
{
    public GameObject Cube;
    public GameObject Border;
    public GameObject SpawnRoomCube;
    public GameObject EnemySpawn;
    public GameObject Player;

    public int gridSizeX = 2;
    public int gridSizeY = 2;
    public int gridSizeZ = 2;
    public float spacing = 5f;

    public Dictionary<Vector3Int, GameObject> worldBlocks { get; private set; } = new Dictionary<Vector3Int, GameObject>();
    public Vector3Int SpawnDirection => spawnDirection;
    public Vector3Int HoleCoords => holeCoords;
    public Vector3Int Entrence => entrence;

    int spawnRoomX;
    int spawnRoomY = 0;
    int spawnRoomZ;
    int spawnRoomSize = 5; 

    Vector3Int holeCoords;
    Vector3Int spawnDirection;
    Vector3Int entrence;

    void Start()
    {
        SpawnRoomLocation();
        SpawnGrid();
        SpawnRoomBuilder(holeCoords, spawnDirection, spawnRoomSize);
    }
    void SpawnGrid()
    {
        for (int x = 0; x < gridSizeX; x++)
        {
            for (int y = 0; y < gridSizeY; y++)
            {
                for (int z = 0; z < gridSizeZ; z++)
                {
                    Vector3Int gridPos = new Vector3Int(x, y, z);
                    Vector3 position = new Vector3(x * spacing, y * spacing, z * spacing);

                    bool openSide = x == spawnRoomX && y == spawnRoomY + 1 && z == spawnRoomZ;
                    bool entrenceSide = x == entrence.x && y == entrence.y && z == entrence.z;
                    bool isBorder = x == 0 || y == 0 || z == 0 || x == gridSizeX - 1 || y == gridSizeY - 1 || z == gridSizeZ - 1;

                    if (isBorder && !openSide && !entrenceSide)
                    {
                        GameObject newCube = Instantiate(Border, position, Quaternion.identity);
                        newCube.name = $"Block_{x}_{y}_{z}";
                        worldBlocks[gridPos] = newCube;
                    }
                    else if (entrenceSide)
                    {
                        GameObject newCube = Instantiate(EnemySpawn, position, Quaternion.identity);
                        newCube.name = $"Block_{x}_{y}_{z}";
                        worldBlocks[gridPos] = newCube;
                    }
                    else
                    {
                        GameObject newCube = Instantiate(Cube, position, Quaternion.identity);
                        newCube.name = $"Block_{x}_{y}_{z}";
                        worldBlocks[gridPos] = newCube;
                    }
                }
            }
        }
    }
    void SpawnRoomBuilder(Vector3 holePosition, Vector3Int spawnDirection, int size)
    {
        //Offset a lil bit
        if (spawnDirection.x == -1)
        {
            holePosition.z -= size / 2;
        }
        else if (spawnDirection.x == 1)
        {
            holePosition.z -= size / 2;
        }
        else if (spawnDirection.z == -1)
        {
            holePosition.x -= size / 2;
        }
        else if (spawnDirection.z == 1)
        {
            holePosition.x -= size / 2;
        }

        //Decide where to start building the spawn room from
        Vector3 roomStart = holePosition * spacing + (Vector3)spawnDirection * spacing;

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                for (int z = 0; z < size; z++)
                {
                    bool isBorder = false; // x == 0 || y == 0 || z == 0 || x == size - 1 || y == size - 1 || z == size - 1;
                    if (spawnDirection.x != 0) 
                        isBorder = y == 0 || z == 0 || x == size - 1 || y == size - 1 || z == size - 1;
                    else if (spawnDirection.z != 0) 
                        isBorder = x == 0 || y == 0 || x == size - 1 || y == size - 1 || z == size - 1;

                    if (isBorder)
                    {
                        //offset to build a room in the correct direction
                        Vector3 offset = new Vector3(
                            x * spacing * (spawnDirection.x != 0 ? spawnDirection.x : 1), //if not on x axis, use 1 for x
                            y * spacing,
                            z * spacing * (spawnDirection.z != 0 ? spawnDirection.z : 1)
                        );

                        Vector3 blockPos = roomStart + offset;

                        GameObject newCube = Instantiate(SpawnRoomCube, blockPos, Quaternion.identity);

                    }
                }
            }
        }

        //spawn the player
        Vector3 playerSpawnPoint = roomStart + new Vector3(
        (size / 2) * spacing * (spawnDirection.x != 0 ? spawnDirection.x : 1),
        (1f) * spacing, 
        (size / 2) * spacing * (spawnDirection.z != 0 ? spawnDirection.z : 1));

        SpawnPlayerInsideRoom(Player, playerSpawnPoint, spawnDirection, spacing);

    }

    void SpawnRoomLocation()
    {
        //possibly replace this with a seed for more control

        //randomly select the direction
        int dir = Random.Range(0, 4); //0 = x-, 1 = x+, 2 = z-, 3 = z+

        int xDir = 0;
        int yDir = 0;
        int zDir = 0;

        int entrenceX = 0;
        int entrenceY = 1;
        int entrenceZ = 0;

        switch (dir)
        {
            case 0: //x-
                spawnRoomX = 0;
                spawnRoomZ = Random.Range(2, gridSizeZ - 2);
                xDir = -1;

                entrenceX = gridSizeX - 1;
                entrenceZ = spawnRoomZ;
                break;
            case 1: //x+
                spawnRoomX = gridSizeX - 1;
                spawnRoomZ = Random.Range(2, gridSizeZ - 2);
                xDir = 1;

                entrenceX = 0;
                entrenceZ = spawnRoomZ;
                break;
            case 2: //z-
                spawnRoomX = Random.Range(2, gridSizeX - 2);
                spawnRoomZ = 0;
                zDir = -1;

                entrenceX = spawnRoomX;
                entrenceZ = gridSizeZ - 1;
                break;
            case 3: //z+
                spawnRoomX = Random.Range(2, gridSizeX - 2);
                spawnRoomZ = gridSizeZ - 1;
                zDir = 1;

                entrenceX = spawnRoomX;
                entrenceZ = 0;
                break;
        }

        //set the holeCoords to the spawnRoom coords
        holeCoords = new Vector3Int(spawnRoomX, spawnRoomY, spawnRoomZ);

        //set the spawnDirection to the direction of the hole
        spawnDirection = new Vector3Int(xDir, yDir, zDir);

        //set the entrence of the dungeon
        entrence = new Vector3Int(entrenceX, entrenceY, entrenceZ);
    }
    void SpawnPlayerInsideRoom(GameObject player, Vector3 playerSpawnPoint, Vector3Int spawnDirection, float spacing)
    { 
        // Face the player toward the hole (i.e., back toward the grid)
        Vector3 lookDirection = spawnDirection;
        Vector3 flatLook = new Vector3(lookDirection.x, 0, lookDirection.z);

        //rotate the player to face the hole
        if (flatLook != Vector3.zero)
            player.transform.rotation = Quaternion.LookRotation(flatLook);

        // Spawn the player
        Instantiate(Player, playerSpawnPoint, Quaternion.identity);
    }
}

