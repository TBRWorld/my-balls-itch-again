using System.Collections;
using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    public float moveSpeed = 2f;
    public float waitTime = 1f;
    public Vector3 targetPosition;
    public bool isMoving = false;

    [HideInInspector] public Vector3 currentDirection = Vector3.zero;

    private Navigation navigation;
    private WorldGen worldGen;
    private Breadcrumbs breadcrumbs;

    void Start()
    {
        worldGen = GameObject.FindFirstObjectByType<WorldGen>(); //may cause lag

        navigation = GetComponent<Navigation>();
        targetPosition = transform.position;

        breadcrumbs = new Breadcrumbs();

        StartCoroutine(MovementLoop());
    }

    IEnumerator MovementLoop()
    {
        while (true)
        {
            if (currentDirection == Vector3.zero)
            {
                yield return null; 
                continue;
            }

            Vector3 destination = transform.position + currentDirection * worldGen.spacing;
            yield return MoveTo(destination);

            currentDirection = Vector3.zero; // Reset so navigation must pick new
        }
    }

    IEnumerator MoveTo(Vector3 destination)
    {
        isMoving = true;
        while ((transform.position - destination).sqrMagnitude > 0.01f)
        {
            transform.position = Vector3.MoveTowards(transform.position, destination, moveSpeed * Time.deltaTime);
            yield return null;
        }
        transform.position = destination;
        isMoving = false;
    }
}
