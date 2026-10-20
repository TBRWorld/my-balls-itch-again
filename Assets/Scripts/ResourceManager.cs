using UnityEngine;
using TMPro;

public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance { get; private set; }

    [Header("Starting Resources")]
    public int currentResources = 500;

    [Header("UI (Optional)")]
    public TextMeshProUGUI resourceText;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // Don't destroy on load if this is meant to persist across scenes
            // DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        UpdateUI();
    }

    public void AddResources(int amount)
    {
        currentResources += amount;
        UpdateUI();
    }

    public bool CanAfford(int cost)
    {
        return currentResources >= cost;
    }

    public bool SpendResources(int cost)
    {
        if (CanAfford(cost))
        {
            currentResources -= cost;
            UpdateUI();
            return true;
        }
        return false;
    }

    private void UpdateUI()
    {
        if (resourceText != null)
        {
            resourceText.text = $"Resources: {currentResources}";
        }
    }
}
