using UnityEngine;

public class EntranceWaypoint : MonoBehaviour
{
    public GameObject waypointPrefab;
    private GameObject currentWaypoint;

    private Camera mainCam;

    void Start()
    {
        /*
         *  Make waypoint work in this script. 
         */
        mainCam = Camera.main;

        var worldGen = FindFirstObjectByType<WorldGen>();
        Vector3 waypointPosition = new Vector3(
        worldGen.Entrence.x * worldGen.spacing,
        worldGen.Entrence.y * worldGen.spacing + 1.5f,
        worldGen.Entrence.z * worldGen.spacing
        );
        currentWaypoint = Instantiate(waypointPrefab, waypointPosition, Quaternion.identity);

        PathCheck.OnValidPathFound += RemoveWaypoint;
    }
    void RemoveWaypoint()
    {
        if (currentWaypoint != null)
        { 
        Destroy(currentWaypoint);
        currentWaypoint = null;
        }
    }
    void LateUpdate()
    {
        // Face the camera at all times
        if (currentWaypoint != null)
        {
            currentWaypoint.transform.LookAt(currentWaypoint.transform.position + mainCam.transform.forward);
        }
    }

    void OnDestroy()
    {
        PathCheck.OnValidPathFound -= RemoveWaypoint;
    }
}
