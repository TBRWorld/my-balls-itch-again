using UnityEngine;

public class healthsys : MonoBehaviour
{
    [SerializeField] private int health;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {


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
