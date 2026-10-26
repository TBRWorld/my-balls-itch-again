using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class TowerMenu : MonoBehaviour
{
    public static TowerMenu Instance { get; private set; }
    public static bool IsOpen { get; private set; }

    [System.Serializable]
    public struct TowerOption
    {
        public string towerName;
        public GameObject prefab;
        public int cost;
        public Sprite icon;
    }

    [Header("Menu References")]
    [Tooltip("The panel that appears on right click (starts hidden)")]
    public GameObject menuPanel;
    [Tooltip("A button prefab that gets cloned once per entry in Options")]
    public GameObject buttonPrefab;
    [Tooltip("Where the cloned buttons are parented (put a layout group here)")]
    public Transform buttonContainer;

    [Header("Towers")]
    public List<TowerOption> options = new List<TowerOption>();

    [Header("Camera")]
    [Tooltip("Optional mouse-look script that is disabled while the menu is open. " +
             "Auto-found on the player camera if left empty.")]
    public Behaviour lookScript;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (menuPanel != null) menuPanel.SetActive(false);
        IsOpen = false;
    }

    private void Start()
    {
        PopulateMenu();
    }

    private void Update()
    {
        // Escape closes the menu
        if (IsOpen && Input.GetKeyDown(KeyCode.Escape))
            HideMenu();
    }

    private void PopulateMenu()
    {
        if (buttonContainer == null || buttonPrefab == null || options == null) return;

        // Clear existing buttons
        foreach (Transform child in buttonContainer) Destroy(child.gameObject);

        foreach (var option in options)
        {
            GameObject btnObj = Instantiate(buttonPrefab, buttonContainer);
            Button btn = btnObj.GetComponent<Button>();

            // Set name/text if button has text
            var text = btnObj.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (text != null) text.text = $"{option.towerName}\n({option.cost})";

            // Set icon if button has an "Icon" child
            if (option.icon != null)
            {
                Transform iconChild = btnObj.transform.Find("Icon");
                if (iconChild != null)
                {
                    var img = iconChild.GetComponent<Image>();
                    if (img != null) img.sprite = option.icon;
                }
            }

            btn.onClick.AddListener(() => SelectTower(option));
        }
    }

    public void ShowMenu(Vector2 screenPosition)
    {
        if (menuPanel == null) return;

        // While the cursor is locked the mouse position is stale, so centre the menu instead
        Vector2 pos = Cursor.lockState == CursorLockMode.Locked
            ? new Vector2(Screen.width * 0.5f, Screen.height * 0.5f)
            : screenPosition;

        menuPanel.SetActive(true);
        menuPanel.transform.position = pos;
        IsOpen = true;

        // The player (and its mouse-look script) is spawned at runtime, so look it up lazily
        if (lookScript == null) lookScript = FindFirstObjectByType<Charaster_PlayerCam>();

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        if (lookScript != null) lookScript.enabled = false;
    }

    public void HideMenu()
    {
        if (menuPanel == null) return;

        menuPanel.SetActive(false);
        IsOpen = false;

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        if (lookScript != null) lookScript.enabled = true;
    }

    private void SelectTower(TowerOption option)
    {
        Debug.Log("Clicked tower: " + option.towerName);

        PlayerInteraction player = FindFirstObjectByType<PlayerInteraction>();
        if (player != null)
        {
            player.SetSelectedTower(option.prefab, option.cost);
        }

        HideMenu();
    }
}
