using TMPro;
using UnityEngine;

public class StatsDisplayUI : MonoBehaviour
{
    public TMP_Text moneyText;
    public TMP_Text xpText;
    public TMP_Text killsText;

    private int money;
    private int xp;
    private int kills;

    private void Start()
    {
        UpdateUI();
    }

    public void AddMoney(int amount)
    {
        money += amount;
        UpdateUI();
    }

    public void AddXP(int amount)
    {
        xp += amount;
        UpdateUI();
    }

    public void AddKill()
    {
        kills++;
        UpdateUI();
    }

    private void UpdateUI()
    {
        moneyText.text = "Money: " + money;
        xpText.text = "XP: " + xp;
        killsText.text = "Kills: " + kills;
    }

    // Testen: M = geld, X = XP, K = kill
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.M)) AddMoney(10);
        if (Input.GetKeyDown(KeyCode.X)) AddXP(25);
        if (Input.GetKeyDown(KeyCode.K)) AddKill();
    }
}