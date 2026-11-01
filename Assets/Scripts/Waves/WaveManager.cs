using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Runs the waves, counts enemies that reach the spawn room and decides win / lose.
/// Builds its own UI (top right) on the existing Canvas, so nothing has to be set up by hand.
///
/// Win : survive every wave in the list (5 by default)
/// Lose: maxBreaches enemies touch the spawn room (10 by default)
/// </summary>
public class WaveManager : MonoBehaviour
{
    [System.Serializable]
    public class Wave
    {
        public int enemyCount = 6;
        [Tooltip("Seconds between two spawns")]
        public float spawnInterval = 2f;
        [Tooltip("How many enemy types from the spawner list can appear (first N of the list)")]
        public int enemyTypes = 2;
        public bool spawnBoss = false;

        public Wave(int count, float interval, int types, bool boss)
        {
            enemyCount = count; spawnInterval = interval; enemyTypes = types; spawnBoss = boss;
        }
    }

    public static bool GameOver { get; private set; }

    [Header("Rules")]
    public int maxBreaches = 10;
    public KeyCode startWaveKey = KeyCode.E;
    [Tooltip("Resources the player gets when a wave is cleared")]
    public int waveClearBonus = 100;
    [Tooltip("How close to the spawn room opening counts as touching it (in grid cells)")]
    public float breachRadiusCells = 0.75f;

    [Header("Waves (the number of entries = the number of waves to win)")]
    public Wave[] waves = new Wave[]
    {
        new Wave(6,  2.0f, 2, false),
        new Wave(8,  1.8f, 3, false),
        new Wave(10, 1.5f, 4, false),
        new Wave(12, 1.3f, 5, false),
        new Wave(12, 1.2f, 6, true),
    };

    private enum State { Preparing, Spawning, Fighting, Won, Lost }
    private State state = State.Preparing;

    private EnemySpawner spawner;
    private WorldGen worldGen;
    private Vector3 goalWorldPos;

    private int currentWave;      // index of the wave that is next / running
    private int breaches;
    private int enemiesAlive;
    private int toSpawn;
    private float nextCheckTime;

    // UI
    private TextMeshProUGUI infoText;
    private TextMeshProUGUI messageText;
    private GameObject endPanel;
    private TextMeshProUGUI endTitle;

    private void Start()
    {
        GameOver = false;
        Time.timeScale = 1f;
        PathCheck.PathFound = false;   // static, so it survives a scene reload

        spawner = FindFirstObjectByType<EnemySpawner>();
        worldGen = FindFirstObjectByType<WorldGen>();

        if (spawner == null) Debug.LogError("WaveManager: no EnemySpawner in the scene.");
        if (worldGen == null) Debug.LogError("WaveManager: no WorldGen in the scene.");

        BuildUI();
        RefreshUI();
    }

    private void Update()
    {
        if (GameOver || spawner == null || worldGen == null) return;

        // Goal = the opening of the spawn room
        Vector3Int goal = worldGen.HoleCoords;
        goal.y = 1;
        goalWorldPos = new Vector3(goal.x, goal.y, goal.z) * worldGen.spacing;

        if (Time.time >= nextCheckTime)
        {
            nextCheckTime = Time.time + 0.1f;
            CheckBreaches();
        }

        if (breaches >= maxBreaches)
        {
            EndGame(false);
            return;
        }

        switch (state)
        {
            case State.Preparing:
                if (PathCheck.PathFound)
                {
                    SetMessage($"Press {startWaveKey} to start wave {currentWave + 1}");
                    if (Input.GetKeyDown(startWaveKey))
                        StartCoroutine(RunWave());
                }
                else
                {
                    SetMessage("Dig a path from the entrance to the spawn room");
                }
                break;

            case State.Fighting:
                if (enemiesAlive == 0)
                    WaveCleared();
                break;
        }

        RefreshUI();
    }

    // ---------------- Waves ----------------

    private IEnumerator RunWave()
    {
        state = State.Spawning;
        SetMessage("");

        Wave wave = waves[currentWave];
        GameObject[] pool = spawner.NormalPrefabs;
        int types = Mathf.Clamp(wave.enemyTypes, 1, Mathf.Max(1, pool.Length));

        toSpawn = wave.enemyCount + (wave.spawnBoss && spawner.bossPrefab != null ? 1 : 0);

        for (int i = 0; i < wave.enemyCount; i++)
        {
            if (GameOver) yield break;
            if (pool.Length > 0)
                spawner.SpawnEnemy(pool[Random.Range(0, types)]);
            toSpawn--;
            yield return new WaitForSeconds(wave.spawnInterval);
        }

        if (wave.spawnBoss && spawner.bossPrefab != null)
        {
            spawner.SpawnEnemy(spawner.bossPrefab);
            toSpawn--;
        }

        toSpawn = 0;
        state = State.Fighting;
    }

    private void WaveCleared()
    {
        currentWave++;

        if (ResourceManager.Instance != null && waveClearBonus > 0)
            ResourceManager.Instance.AddResources(waveClearBonus);

        if (currentWave >= waves.Length)
            EndGame(true);
        else
            state = State.Preparing;
    }

    // ---------------- Breaches ----------------

    private void CheckBreaches()
    {
        Enemy[] enemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        enemiesAlive = enemies.Length;

        float radius = worldGen.spacing * breachRadiusCells;
        foreach (Enemy e in enemies)
        {
            if (e == null || e.IsDead) continue;
            if (Vector3.Distance(e.transform.position, goalWorldPos) <= radius)
            {
                breaches++;
                Destroy(e.gameObject);   // no reward, no split
                enemiesAlive--;
            }
        }
    }

    // ---------------- End of game ----------------

    private void EndGame(bool won)
    {
        if (GameOver) return;
        GameOver = true;
        state = won ? State.Won : State.Lost;

        RefreshUI();
        endTitle.text = won ? "YOU WIN!" : "GAME OVER";
        endTitle.color = won ? new Color(0.4f, 1f, 0.4f) : new Color(1f, 0.4f, 0.4f);
        endPanel.SetActive(true);

        // Stop looking around and show the mouse
        Charaster_PlayerCam cam = FindFirstObjectByType<Charaster_PlayerCam>();
        if (cam != null) cam.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 0f;
    }

    private void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // ---------------- UI ----------------

    private void RefreshUI()
    {
        if (infoText == null) return;

        int shownWave = Mathf.Min(currentWave + (state == State.Preparing || state == State.Won ? 0 : 1), waves.Length);
        if (state == State.Won) shownWave = waves.Length;

        int left = enemiesAlive + toSpawn;
        infoText.text =
            $"Wave {Mathf.Max(1, state == State.Preparing ? currentWave + 1 : shownWave)} / {waves.Length}\n" +
            $"Enemies: {left}\n" +
            $"Breached: {breaches} / {maxBreaches}";
    }

    private void SetMessage(string text)
    {
        if (messageText != null && messageText.text != text)
            messageText.text = text;
    }

    private void BuildUI()
    {
        Canvas canvas = null;
        if (ResourceManager.Instance != null && ResourceManager.Instance.resourceText != null)
            canvas = ResourceManager.Instance.resourceText.canvas;
        if (canvas == null) canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject c = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = c.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }
        Transform root = canvas.rootCanvas.transform;

        // Top right: wave / enemies / breached
        infoText = MakeText("WaveInfoText", root, 36, TextAlignmentOptions.TopRight);
        SetRect(infoText.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1),
                new Vector2(-20, -20), new Vector2(520, 160));

        // Top centre: hints
        messageText = MakeText("WaveMessageText", root, 32, TextAlignmentOptions.Top);
        SetRect(messageText.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                new Vector2(0, -20), new Vector2(1000, 80));

        // End screen
        endPanel = new GameObject("EndPanel", typeof(RectTransform), typeof(Image));
        endPanel.transform.SetParent(root, false);
        RectTransform panelRect = endPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = panelRect.offsetMax = Vector2.zero;
        endPanel.GetComponent<Image>().color = new Color(0, 0, 0, 0.75f);

        endTitle = MakeText("EndTitle", endPanel.transform, 96, TextAlignmentOptions.Center);
        SetRect(endTitle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, 80), new Vector2(1000, 160));

        GameObject buttonObj = new GameObject("RestartButton", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObj.transform.SetParent(endPanel.transform, false);
        SetRect(buttonObj.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0, -60), new Vector2(300, 80));
        buttonObj.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, 1f);
        buttonObj.GetComponent<Button>().onClick.AddListener(Restart);

        TextMeshProUGUI buttonText = MakeText("Label", buttonObj.transform, 36, TextAlignmentOptions.Center);
        buttonText.text = "Restart";
        buttonText.rectTransform.anchorMin = Vector2.zero;
        buttonText.rectTransform.anchorMax = Vector2.one;
        buttonText.rectTransform.offsetMin = buttonText.rectTransform.offsetMax = Vector2.zero;

        endPanel.SetActive(false);
    }

    private TextMeshProUGUI MakeText(string name, Transform parent, float size, TextAlignmentOptions align)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
        t.fontSize = size;
        t.alignment = align;
        t.color = Color.white;
        t.raycastTarget = false;
        return t;
    }

    private void SetRect(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
                         Vector2 anchoredPos, Vector2 size)
    {
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;
    }
}
