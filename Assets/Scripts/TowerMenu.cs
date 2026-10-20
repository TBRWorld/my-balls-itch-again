using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class TowerMenu : MonoBehaviour
{
    public static TowerMenu Instance { get; private set; }

    [System.Serializable]
    public struct TowerOption
    {
        public string towerName;
        public GameObject prefab;
        public int cost;
        public Sprite icon;
    }

    public List<TowerOption> options;
    public GameObject menuPanel;
    public GameObject buttonPrefab;
    public Transform buttonContainer;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (menuPanel != null) menuPanel.SetActive(false);
    }

    private void Start()
    {
        PopulateMenu();
    }

    private void PopulateMenu()
    {
        if (buttonContainer == null || buttonPrefab == null) return;

        // Clear existing buttons
        foreach (Transform child in buttonContainer) Destroy(child.gameObject);

        foreach (var option in options)
        {
            GameObject btnObj = Instantiate(buttonPrefab, buttonContainer);
            Button btn = btnObj.GetComponent<Button>();
            
            // Set name/text if button has text
            var text = btnObj.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (text != null) text.text = $"{option.towerName}\n({option.cost})";

            // Set icon if button has image
            if (option.icon != null)
            {
                var img = btn.transform.Find("Icon")?.GetComponent<Image>();
                if (img != null) img.sprite = option.icon;
            }

            btn.onClick.AddListener(() => SelectTower(option));
        }
    }

    public void ShowMenu(Vector2 screenPosition)
    {
        if (menuPanel == null) return;
        
        menuPanel.SetActive(true);
        menuPanel.transform.position = screenPosition;
        
        // Lock cursor or handle UI focus if needed
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void HideMenu()
    {
        if (menuPanel == null) return;
        menuPanel.SetActive(false);
        
        // Re-lock cursor if the game expects it
        // Cursor.visible = false;
        // Cursor.lockState = CursorLockMode.Locked;
    }

    private void SelectTower(TowerOption option)
    {
        PlayerInteraction player = FindFirstObjectByType<PlayerInteraction>();
        if (player != null)
        {
            player.SetSelectedTower(option.prefab, option.cost);
        }
        HideMenu();
    }
}
