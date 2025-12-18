using UnityEngine;
using UnityEngine.UI;

public class CoinUI : MonoBehaviour
{
    [Header("UI References")]
    public Text coinText;                          // Для старого UI
    public TMPro.TextMeshProUGUI coinTextTMP;      // Для TextMeshPro

    [Header("Display Settings")]
    public bool showTotalCoins = false;            // true = общие монеты, false = монеты сессии
    public string prefix = "Coins: ";              // Префикс перед числом

    private Player player;

    void Start()
    {
        // Ищем игрока
        GameObject playerObj = GameObject.Find("Player");
        if (playerObj != null)
        {
            player = playerObj.GetComponent<Player>();
            Debug.Log("✅ CoinUI: Player found!");
        }
        else
        {
            Debug.LogError("❌ CoinUI: Player NOT FOUND! Make sure Player object exists.");
        }

        // Проверяем привязки UI
        if (coinText == null && coinTextTMP == null)
        {
            Debug.LogError("❌ CoinUI: No UI Text assigned! Drag Text or TextMeshPro component to CoinUI in Inspector.");
        }
        else
        {
            if (coinText != null) Debug.Log("✅ CoinUI: Legacy Text assigned");
            if (coinTextTMP != null) Debug.Log("✅ CoinUI: TextMeshPro assigned");
        }
    }

    void Update()
    {
        if (player == null)
        {
            // Пробуем найти снова (на случай если игрок создался позже)
            GameObject playerObj = GameObject.Find("Player");
            if (playerObj != null)
            {
                player = playerObj.GetComponent<Player>();
            }
            return;
        }

        // Выбираем какие монеты показывать
        int coins = showTotalCoins ? player.GetTotalCoins() : player.GetSessionCoins();
        string coinString = prefix + coins.ToString();

        // Обновляем UI
        if (coinText != null)
        {
            coinText.text = coinString;
        }

        if (coinTextTMP != null)
        {
            coinTextTMP.text = coinString;
        }
    }

    // Для отладки в Inspector
    void OnValidate()
    {
        if (coinText == null && coinTextTMP == null)
        {
            Debug.LogWarning("CoinUI: Assign a Text or TextMeshProUGUI component!");
        }
    }
}