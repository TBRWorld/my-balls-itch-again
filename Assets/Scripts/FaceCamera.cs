using UnityEngine;

public class FaceCamera : MonoBehaviour
{
    private Camera mainCam;

    void Start()
    {
        mainCam = Camera.main;
    }

    void LateUpdate()
    {
        if (mainCam != null)
        {
            Vector3 dirToCam = mainCam.transform.position - transform.position;
            dirToCam.y = 0; // Optional: lock Y rotation if not needed
            transform.forward = dirToCam.normalized;
        }
    }
}