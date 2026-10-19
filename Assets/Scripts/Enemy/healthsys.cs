using UnityEngine;

public class healthsys : MonoBehaviour
{
    private int health = 100;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        health = 100;
        
    }

    // Update is called once per frame
    void Update()
    {
        if (health < 0)
        {
            Destroy(gameObject);
        }
        
    }
}
